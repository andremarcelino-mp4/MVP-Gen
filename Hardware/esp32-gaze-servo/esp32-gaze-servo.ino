// GenJutsu maquete: ESP32 + MG996R (inclinação cima/baixo) + MPU6050 na maquete.
//
// O Quest 3S envia a inclinação da cabeça (pitch) por dois caminhos; vale o que chegar mais novo:
//   1) MQTT público pela internet  -> funciona com o ESP32 em QUALQUER rede (sem IP fixo)
//   2) UDP broadcast :9000 na LAN   -> funciona sem internet quando Quest e ESP32 dividem a rede
// Linha (ASCII): Y,<yaw rad>,P,<pitch rad>,S,<servo>,PD,<pitch graus>,N,<seq>\n
// (linhas antigas Y,..,P,..,S,.. também são aceitas; usa P em radianos)
//
// Ligações: servo sinal GPIO18 (fonte 5-6 V/2 A+ externa, GND comum)
//           MPU6050 VCC 3V3, GND, SDA GPIO21, SCL GPIO19 (colado na parte que inclina)
// Wi-Fi: redes do config.h + redes gravadas pelo portal "GenJutsu-Maquete".
//        Segure o botão BOOT (GPIO0) ao ligar para abrir o portal à força.

#if __has_include("config.h")
#include "config.h"
#else
#include "config.example.h"
#endif

#include <WiFi.h>
#include <WiFiUdp.h>
#include <WiFiManager.h>
#include <PubSubClient.h>
#include <Preferences.h>
#include <Wire.h>
#include <MPU6050_light.h>
#include <ESP32Servo.h>

// ---------- ajustes ----------
static const int SERVO_PIN = 18;
static const int SDA_PIN = 21;
static const int SCL_PIN = 19;
static const int PORTAL_BTN_PIN = 0;
static const uint16_t UDP_PORT = 9000;
static const float PITCH_LIMIT = 60.0f;   // graus de cabeça aceitos
static const float PITCH_SIGN = 1.0f;     // troque para -1 se a maquete inclinar ao contrário
static const float SERVO_CENTER = 90.0f;  // servo nesta posição = maquete nivelada
static const float SERVO_MIN = 20.0f;
static const float SERVO_MAX = 160.0f;
static const float KP = 0.5f;             // correção proporcional (graus de servo por grau de erro)
static const float KI = 1.2f;             // integral (graus/s por grau de erro)
static const float I_LIMIT = 25.0f;
static const float SLEW_DEG_S = 360.0f;   // velocidade máxima do comando
static const float TARGET_SMOOTH = 0.15f; // 0..1 por ciclo de 10 ms
static const uint32_t HOLD_MS = 1000;     // sem pacotes > 1 s: segura a posição
static const uint32_t LEVEL_MS = 8000;    // sem pacotes > 8 s: volta devagar ao nível
static const uint32_t PORTAL_TIMEOUT_S = 180;
static const uint32_t OFFLINE_PORTAL_MS = 120000; // 2 min sem Wi-Fi -> abre portal de novo

// ---------- estado compartilhado (rede -> controle) ----------
static volatile float gTarget = 0.0f;       // graus da maquete (frame do servo)
static volatile uint32_t gLastRxMs = 0;
static volatile bool gHaveRx = false;
static volatile char gSrc = '-';            // 'M' mqtt, 'U' udp
static volatile uint32_t gRxCount = 0;
static int32_t gLastSeq = -1;
static portMUX_TYPE gMux = portMUX_INITIALIZER_UNLOCKED;

// ---------- objetos ----------
WiFiUDP udp;
WiFiClient netClient;
PubSubClient mqtt(netClient);
Preferences prefs;
MPU6050 mpu(Wire);
Servo servo;

static bool mpuOk = false;
static int mpuAxis = 0;        // 0 = X, 1 = Y
static float mpuSign = 1.0f;   // converte ângulo do MPU para o frame do servo
static float mpuGain = 1.0f;   // graus de maquete por grau de servo

static String topicGaze, topicStatus, clientId;
static const char* brokers[] = { MQTT_HOST_1, MQTT_HOST_2 };
static int brokerIdx = 0;
static uint32_t mqttNextTry = 0, mqttBackoff = 1000;
static uint32_t wifiNextTry = 0, wifiLostAt = 0;
static bool udpStarted = false;
static volatile float gReal = 0, gCmd = SERVO_CENTER;
static volatile bool gWifiUp = false, gMqttUp = false;

// ================= parsing =================
static bool findField(const char* line, const char* key, float* out) {
  // procura ",KEY," ou "KEY," no início
  size_t klen = strlen(key);
  const char* p = line;
  while ((p = strstr(p, key)) != nullptr) {
    bool startOk = (p == line) || (*(p - 1) == ',');
    if (startOk && p[klen] == ',') {
      *out = atof(p + klen + 1);
      return true;
    }
    p += klen;
  }
  return false;
}

static void handleLine(const char* line, char src) {
  float pd;
  if (!findField(line, "PD", &pd)) {
    float pr;
    if (!findField(line, "P", &pr)) return;
    pd = pr * 57.2957795f;
  }
  if (isnan(pd)) return;
  float seqF;
  int32_t seq = findField(line, "N", &seqF) ? (int32_t)seqF : -1;
  uint32_t now = millis();
  // Mesmo pacote chegando pelos dois caminhos: ignora duplicado/velho (o mais novo vence).
  if (seq >= 0 && gLastSeq >= 0 && (now - gLastRxMs) < 1000) {
    int32_t d = seq - gLastSeq;
    if (d <= 0 && d > -100) return;
  }
  if (seq >= 0) gLastSeq = seq;
  pd = constrain(pd, -PITCH_LIMIT, PITCH_LIMIT);
  portENTER_CRITICAL(&gMux);
  gTarget = PITCH_SIGN * pd;
  gLastRxMs = now;
  gHaveRx = true;
  gSrc = src;
  gRxCount++;
  portEXIT_CRITICAL(&gMux);
}

static void onMqtt(char* topic, byte* payload, unsigned int len) {
  char buf[128];
  if (len >= sizeof(buf)) len = sizeof(buf) - 1;
  memcpy(buf, payload, len);
  buf[len] = 0;
  handleLine(buf, 'M');
}

// ================= controle (task própria, não trava com a rede) =================
static float readMpu() {
  mpu.update();
  float a = (mpuAxis == 0) ? mpu.getAngleX() : mpu.getAngleY();
  return mpuSign * a;
}

static void autoCalibrate() {
  // Maquete parada e nivelada: zera o MPU e descobre eixo/sentido/ganho com um pequeno movimento.
  servo.write((int)SERVO_CENTER);
  delay(1200);
  Serial.println("[MPU] calibrando (deixe a maquete parada)...");
  mpu.calcOffsets();
  for (int i = 0; i < 50; i++) { mpu.update(); delay(5); }
  float x0 = mpu.getAngleX(), y0 = mpu.getAngleY();
  const float step = 15.0f;
  for (int i = 0; i <= 30; i++) {           // sobe 15° em ~300 ms
    servo.write((int)(SERVO_CENTER + step * i / 30.0f));
    mpu.update();
    delay(10);
  }
  for (int i = 0; i < 70; i++) { mpu.update(); delay(10); }
  float dx = mpu.getAngleX() - x0, dy = mpu.getAngleY() - y0;
  for (int i = 30; i >= 0; i--) {
    servo.write((int)(SERVO_CENTER + step * i / 30.0f));
    mpu.update();
    delay(10);
  }
  for (int i = 0; i < 50; i++) { mpu.update(); delay(10); }
  float best = fabsf(dx) >= fabsf(dy) ? dx : dy;
  mpuAxis = fabsf(dx) >= fabsf(dy) ? 0 : 1;
  Serial.printf("[MPU] teste 15 graus: dX=%.1f dY=%.1f\n", dx, dy);
  if (fabsf(best) < 4.0f) {
    mpuOk = false;
    Serial.println("[MPU] maquete nao se moveu no teste -> malha ABERTA (servo direto)");
    return;
  }
  mpuSign = best > 0 ? 1.0f : -1.0f;
  mpuGain = constrain(fabsf(best) / step, 0.3f, 3.0f);
  Serial.printf("[MPU] eixo=%c sinal=%+.0f ganho=%.2f -> malha FECHADA\n", mpuAxis ? 'Y' : 'X', mpuSign, mpuGain);
}

// Roda no loop() a cada 10 ms (sem task separada: a task travava o Wi-Fi no core 3.x).
static float tgtSmooth = 0, integ = 0, cmd = SERVO_CENTER;
static uint32_t lastLog = 0, lastCtl = 0;
static void controlStep() {
  if (millis() - lastCtl < 10) return;
  lastCtl = millis();
  {
    const float dt = 0.01f;
    uint32_t now = millis();
    float target;
    uint32_t last;
    bool have;
    portENTER_CRITICAL(&gMux);
    target = gTarget; last = gLastRxMs; have = gHaveRx;
    portEXIT_CRITICAL(&gMux);
    uint32_t age = have ? now - last : 0xFFFFFFFF;
    if (!have) {
      target = 0;
    } else if (age > LEVEL_MS) {
      target = tgtSmooth * 0.995f;   // volta devagar ao nível
      portENTER_CRITICAL(&gMux); gTarget = target; portEXIT_CRITICAL(&gMux);
    } else if (age > HOLD_MS) {
      target = tgtSmooth;            // segura a última posição
    }
    tgtSmooth += (target - tgtSmooth) * TARGET_SMOOTH;

    float real = tgtSmooth;
    float want;
    if (mpuOk) {
      real = readMpu();
      float err = tgtSmooth - real;
      if (fabsf(err) > 0.5f) integ += KI * err * dt;
      integ = constrain(integ, -I_LIMIT, I_LIMIT);
      want = SERVO_CENTER + (tgtSmooth + KP * err) / mpuGain + integ;
    } else {
      want = SERVO_CENTER + tgtSmooth;
    }
    want = constrain(want, SERVO_MIN, SERVO_MAX);
    float maxStep = SLEW_DEG_S * dt;
    cmd += constrain(want - cmd, -maxStep, maxStep);
    if (cmd <= SERVO_MIN || cmd >= SERVO_MAX) integ *= 0.98f;  // anti-windup no fim de curso
    servo.writeMicroseconds(map((long)(cmd * 10), 0, 1800, 500, 2400));
    gReal = real; gCmd = cmd;

    if (now - lastLog >= 200) {      // ~5 Hz
      char ageBuf[12];
      if (have) snprintf(ageBuf, sizeof(ageBuf), "%lu", (unsigned long)age); else strcpy(ageBuf, "-");
      lastLog = now;
      Serial.printf("alvo=%6.1f real=%6.1f servo=%5.1f %s src=%c rx=%lu idade=%s wifi=%s mqtt=%s\n",
                    tgtSmooth, real, cmd, mpuOk ? "PI" : "OL", (char)gSrc, (unsigned long)gRxCount,
                    ageBuf,
                    gWifiUp ? "ok" : "off", gMqttUp ? "ok" : "off");
    }
  }
}

// ================= rede =================
static String knownSsid[8], knownPass[8];
static int knownN = 0, wifiIdx = 0;
static void addKnown(const char* s, const char* p) {
  for (int i = 0; i < knownN; i++) if (knownSsid[i] == s) { knownPass[i] = p; return; }
  if (knownN < 8) { knownSsid[knownN] = s; knownPass[knownN] = p; knownN++; }
}

static void addKnownNetworks() {
  if (strlen(WIFI_SSID_1)) addKnown(WIFI_SSID_1, WIFI_PASS_1);
  if (strlen(WIFI_SSID_2)) addKnown(WIFI_SSID_2, WIFI_PASS_2);
  prefs.begin("wifi", true);
  int n = prefs.getInt("n", 0);
  for (int i = 0; i < n && i < 5; i++) {
    String s = prefs.getString(("s" + String(i)).c_str(), "");
    String p = prefs.getString(("p" + String(i)).c_str(), "");
    if (s.length()) {
      addKnown(s.c_str(), p.c_str());
      Serial.printf("[WiFi] rede salva: %s\n", s.c_str());
    }
  }
  prefs.end();
}

static void saveNetwork(const String& ssid, const String& pass) {
  if (!ssid.length()) return;
  prefs.begin("wifi", false);
  int n = prefs.getInt("n", 0);
  for (int i = 0; i < n; i++) {
    if (prefs.getString(("s" + String(i)).c_str(), "") == ssid) {
      prefs.putString(("p" + String(i)).c_str(), pass);
      prefs.end();
      return;
    }
  }
  int slot = n < 5 ? n : (prefs.getInt("next", 0) % 5);
  prefs.putString(("s" + String(slot)).c_str(), ssid);
  prefs.putString(("p" + String(slot)).c_str(), pass);
  if (n < 5) prefs.putInt("n", n + 1); else prefs.putInt("next", slot + 1);
  prefs.end();
  addKnown(ssid.c_str(), pass.c_str());
  Serial.printf("[WiFi] rede nova gravada: %s\n", ssid.c_str());
}

static void runPortal() {
  Serial.printf("[WiFi] abrindo portal '%s' (192.168.4.1) por %lus\n", PORTAL_AP_NAME, (unsigned long)PORTAL_TIMEOUT_S);
  WiFiManager wm;
  wm.setConfigPortalTimeout(PORTAL_TIMEOUT_S);
  wm.setConnectTimeout(20);
  bool ok = strlen(PORTAL_AP_PASS) >= 8 ? wm.startConfigPortal(PORTAL_AP_NAME, PORTAL_AP_PASS)
                                        : wm.startConfigPortal(PORTAL_AP_NAME);
  if (ok && WiFi.status() == WL_CONNECTED) {
    saveNetwork(WiFi.SSID(), WiFi.psk());
  } else {
    Serial.println("[WiFi] portal fechou sem rede nova");
  }
  WiFi.mode(WIFI_STA);
}

static void onWifiUp() {
  Serial.printf("[WiFi] conectado em %s  IP %s  RSSI %d\n", WiFi.SSID().c_str(), WiFi.localIP().toString().c_str(), WiFi.RSSI());
  if (!udpStarted) {
    udp.begin(UDP_PORT);
    udpStarted = true;
    Serial.printf("[UDP] ouvindo broadcast na porta %u\n", UDP_PORT);
  }
  wifiLostAt = 0;
  mqttNextTry = 0;
  mqttBackoff = 1000;
}

static void ensureWifi() {
  static bool wasUp = false;
  bool up = WiFi.status() == WL_CONNECTED;
  if (up && !wasUp) onWifiUp();
  if (!up && wasUp) {
    Serial.println("[WiFi] caiu, reconectando...");
    if (udpStarted) { udp.stop(); udpStarted = false; }
    wifiLostAt = millis();
  }
  wasUp = up;
  if (up) return;
  if (!wifiLostAt) wifiLostAt = millis();
  if (millis() < wifiNextTry) return;
  wifiNextTry = millis() + 10000;    // tenta uma rede conhecida por vez, sem bloquear
  if (knownN > 0) {
    int i = wifiIdx++ % knownN;
    Serial.printf("[WiFi] tentando %s...\n", knownSsid[i].c_str());
    WiFi.disconnect();
    WiFi.begin(knownSsid[i].c_str(), knownPass[i].c_str());
  }
  if (millis() - wifiLostAt > OFFLINE_PORTAL_MS) {
    runPortal();
    wifiLostAt = millis();
  }
}

static void ensureMqtt() {
  if (WiFi.status() != WL_CONNECTED) return;
  if (mqtt.connected()) { mqtt.loop(); return; }
  if (millis() < mqttNextTry) return;
  const char* host = brokers[brokerIdx];
  mqtt.setServer(host, MQTT_PORT);
  Serial.printf("[MQTT] conectando %s:%d ...\n", host, MQTT_PORT);
  if (mqtt.connect(clientId.c_str(), topicStatus.c_str(), 0, true, "offline")) {
    mqtt.subscribe(topicGaze.c_str(), 0);
    String st = "online " + WiFi.SSID() + " " + WiFi.localIP().toString() + (mpuOk ? " PI" : " OL");
    mqtt.publish(topicStatus.c_str(), st.c_str(), true);
    Serial.printf("[MQTT] conectado %s  assinando %s\n", host, topicGaze.c_str());
    mqttBackoff = 1000;
  } else {
    Serial.printf("[MQTT] falhou rc=%d, nova tentativa em %lus\n", mqtt.state(), (unsigned long)(mqttBackoff / 1000));
    mqttNextTry = millis() + mqttBackoff;
    mqttBackoff = min<uint32_t>(mqttBackoff * 2, 30000);
    brokerIdx = (brokerIdx + 1) % 2;   // alterna broker reserva
  }
}

static void pollUdp() {
  if (!udpStarted) return;
  int n;
  while ((n = udp.parsePacket()) > 0) {
    char buf[128];
    int len = udp.read(buf, sizeof(buf) - 1);
    if (len <= 0) break;
    buf[len] = 0;
    handleLine(buf, 'U');
  }
}

// ================= setup/loop =================
void setup() {
  Serial.begin(115200);
  delay(300);
  Serial.println("\n[GenJutsu] maquete pitch v2 (MQTT+UDP, MPU6050 PI)");
  pinMode(PORTAL_BTN_PIN, INPUT_PULLUP);

  servo.setPeriodHertz(50);
  servo.attach(SERVO_PIN, 500, 2400);
  servo.write((int)SERVO_CENTER);

  Wire.begin(SDA_PIN, SCL_PIN);
  Wire.setClock(400000);
  byte st = mpu.begin();
  if (st == 0) {
    mpuOk = true;
    autoCalibrate();
  } else {
    Serial.printf("[MPU] MPU6050 nao encontrado (status %d) -> malha ABERTA\n", st);
  }

  uint64_t mac = ESP.getEfuseMac();
  clientId = "gj-maquete-" + String((uint32_t)(mac >> 16), HEX) + String((uint32_t)esp_random() & 0xFFF, HEX);
  topicGaze = String(MQTT_TOPIC_BASE) + "/gaze";
  topicStatus = String(MQTT_TOPIC_BASE) + "/status";
  mqtt.setCallback(onMqtt);
  mqtt.setKeepAlive(15);
  mqtt.setSocketTimeout(4);
  mqtt.setBufferSize(256);


  WiFi.mode(WIFI_STA);
  WiFi.setSleep(false);
  WiFi.setAutoReconnect(true);
  addKnownNetworks();
  bool forcePortal = digitalRead(PORTAL_BTN_PIN) == LOW;
  Serial.printf("[WiFi] procurando redes conhecidas (%s)...\n", WIFI_SSID_1);
  if (forcePortal) runPortal();       // sem rede conhecida por 2 min o portal abre sozinho
}

void loop() {
  controlStep();
  ensureWifi();
  pollUdp();
  ensureMqtt();
  gWifiUp = WiFi.status() == WL_CONNECTED;
  gMqttUp = mqtt.connected();
  delay(2);
}
