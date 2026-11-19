# Maquete GenJutsu — ESP32 + MG996R + MPU6050 (inclinação cima/baixo)

O app do Quest 3S (`GazePublisher`) manda a inclinação da cabeça (pitch, ±60°) e o ESP32 move a
maquete com **um MG996R**. Um **MPU6050 colado na maquete** mede o ângulo real e um controle PI
corrige a diferença (folga, peso, imprecisão do servo).

## Ligações

| Peça | Pino | Liga em |
|---|---|---|
| MG996R laranja (sinal) | — | **GPIO 18** |
| MG996R vermelho (+) | — | **fonte externa 5–6 V, 2 A ou mais** (nunca no 5V/VIN do ESP32) |
| MG996R marrom (−) | — | GND da fonte **e** GND do ESP32 (terra comum) |
| MPU6050 VCC | — | 3V3 |
| MPU6050 GND | — | GND |
| MPU6050 SDA | — | **GPIO 21** |
| MPU6050 SCL | — | **GPIO 22** |

Capacitor de 470–1000 µF entre + e − da fonte do servo, perto do servo. Cole o MPU firme na parte
que inclina e deixe os fios com folga.

## Como funciona a rede (não depende de IP)

O Quest envia cada amostra por dois caminhos; o ESP32 usa o que chegar primeiro e descarta o
duplicado pelo número de sequência (`N`):

1. **MQTT pela internet** — broker público `broker.emqx.io:1883` (reserva `broker.hivemq.com`),
   tópico `genjutsu-9de75d60960b/maquete/gaze`. Funciona com Quest e ESP32 em **redes diferentes**,
   basta ter internet. O ESP32 publica `.../maquete/status` (retido): `online <ssid> <ip> PI|OL` ou `offline`.
2. **UDP broadcast** `255.255.255.255:9000` — funciona sem internet quando os dois estão na mesma rede.

Linha: `Y,<yaw rad>,P,<pitch rad>,S,<servo yaw>,PD,<pitch graus>,N,<seq>` (linhas antigas sem `PD` também valem).

Ambos os lados reconectam sozinhos (Wi-Fi e MQTT com espera crescente 1→30 s, alternando broker).

## Wi-Fi e troca de rede

- `config.h` (não vai pro git; modelo em `config.example.h`) tem as redes padrão: `Romano` (hotspot do PC).
- Se nenhuma rede conhecida aparecer em 15 s no boot (ou ficar 2 min sem Wi-Fi), o ESP32 abre o portal
  **`GenJutsu-Maquete`** (Wi-Fi aberto). Conecte o celular nele, abra `192.168.4.1` se a página não abrir
  sozinha, escolha a rede nova e a senha. Ela fica gravada (até 5 redes) e entra na lista do WiFiMulti.
- Para abrir o portal à força: segure o botão **BOOT** e aperte **EN/RST**, solte o BOOT depois de ~1 s.
- Para usar outra rede de forma fixa, edite `config.h` (`WIFI_SSID_2`/`WIFI_PASS_2`) e grave de novo.
- O Quest só precisa de internet (qualquer Wi-Fi); não precisa estar na mesma rede do ESP32.

## Controle

- Boot: servo vai a 90° (nível), MPU calibra (deixe parado ~2 s), servo sobe 15° e volta para
  descobrir sozinho o eixo (X/Y), o sentido e a relação servo→maquete.
- Se o MPU não responder ou a maquete não se mexer no teste: **malha aberta** (servo = 90° + pitch).
- Sem pacotes por 1 s: segura a posição. Sem pacotes por 8 s: volta devagar ao nível.
- Maquete inclinando ao contrário da cabeça: `PITCH_SIGN = -1` no `.ino` (ou `invertPitch` no Unity).
- Ajuste fino: `KP` (0,5) e `KI` (1,2). Oscila → diminua; lenta/erro parado → aumente.
- Monitor serial 115200, ~5 Hz: `alvo= real= servo= PI|OL src=M|U rx= idade= wifi= mqtt=`.

## Gravar

Bibliotecas: `ESP32Servo`, `PubSubClient`, `WiFiManager` (tzapu), `MPU6050_light`; core `esp32:esp32` 3.x.

```powershell
$ac = "$env:LOCALAPPDATA\Programs\Arduino IDE\resources\app\lib\backend\resources\arduino-cli.exe"
cd "Hardware\esp32-gaze-servo"
Copy-Item config.example.h config.h   # só na primeira vez, depois edite
& $ac compile --fqbn esp32:esp32:esp32 .
& $ac upload  --fqbn esp32:esp32:esp32 -p COM5 .   # troque COM5 pela porta do ESP32
& $ac monitor -p COM5 -c baudrate=115200
```

## Testar sem o Quest

```powershell
powershell -ExecutionPolicy Bypass -File tools\send-test.ps1          # varre -40..+40° por 20 s
powershell -ExecutionPolicy Bypass -File tools\send-test.ps1 -Fixed 20 -Seconds 10
```
