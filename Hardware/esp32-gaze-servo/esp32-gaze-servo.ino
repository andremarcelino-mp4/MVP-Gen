// GenJutsu maquete: ESP32 + MG996R
// Wi-Fi UDP :9000  lines  Y,<yaw>,P,<pitch>,S,<servo>\n
// Servo signal GPIO 18. Power the MG996R from 5–6 V (2 A+), common GND.

#include <WiFi.h>
#include <WiFiUdp.h>
#include <ESP32Servo.h>

const char* WIFI_SSID = "SEU_SSID";
const char* WIFI_PASS = "SUA_SENHA";
const uint16_t UDP_PORT = 9000;
const int SERVO_PIN = 18;

WiFiUDP udp;
Servo servo;
int lastAngle = 90;

void setup() {
  Serial.begin(115200);
  servo.setPeriodHertz(50);
  servo.attach(SERVO_PIN, 500, 2500);
  servo.write(lastAngle);

  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASS);
  Serial.print("WiFi");
  while (WiFi.status() != WL_CONNECTED) {
    delay(400);
    Serial.print(".");
  }
  Serial.println();
  Serial.print("IP ");
  Serial.println(WiFi.localIP());

  udp.begin(UDP_PORT);
}

void loop() {
  int n = udp.parsePacket();
  if (n > 0) {
    char buf[96];
    int len = udp.read(buf, sizeof(buf) - 1);
    buf[len] = 0;
    int servoDeg = lastAngle;
    if (parseServo(buf, &servoDeg)) {
      lastAngle = constrain(servoDeg, 0, 180);
      servo.write(lastAngle);
      Serial.println(buf);
    }
  }
}

bool parseServo(const char* line, int* outDeg) {
  const char* s = strstr(line, "S,");
  if (!s)
    return false;
  *outDeg = atoi(s + 2);
  return true;
}
