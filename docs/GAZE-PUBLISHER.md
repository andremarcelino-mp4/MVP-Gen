# GazePublisher — cabeça Quest → maquete Wi‑Fi

Contrato de pose (Y-up, yaw 0 ≈ +Z da câmara):

- yaw = `Atan2(forward.x, forward.z)`
- pitch = `Asin(forward.y)`
- 10–15 Hz
- Sem eye-tracking, sem Presence, sem `UPDATE` Postgres

## LAN UDP (maquete ESP32 + MG996R) — nesta PR

O APK envia UDP se `sendUdpLan` estiver ligado (predefinição).

- Destino Inspector: `udpHost` (IPv4) + `udpPort` (9000)
- Linha ASCII: `Y,<yaw>,P,<pitch>,S,<servo>\n` (cultura invariante)
- `S` = yaw mapeado 30–150° (±60° de cabeça)
- `sendBeforeSession`: envia em Play mesmo sem código (útil para calibrar a base)
- Falha de rede: um warning, o jogo continua

Firmware: `Hardware/esp32-gaze-servo/esp32-gaze-servo.ino`  
SSID/senha e IP só no ESP32. No Unity, põe o IP do ESP32 (ex. `192.168.1.80`).

Fonte 5–6 V no MG996R, GND comum. Quest e ESP32 no **mesmo Wi‑Fi 2.4 GHz**, sem isolamento de AP.

## Supabase Broadcast (ainda stub)

Canal alvo `gaze:{SESSION_CODE}`, evento `pose`, hello com `profile_id`. Sem SDK no `Packages/manifest.json`. O UDP local não espera por esse canal.
