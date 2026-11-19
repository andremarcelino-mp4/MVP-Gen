# GazePublisher — cabeça Quest → maquete

Contrato de pose (Y-up, yaw 0 ≈ +Z da câmara):

- yaw = `Atan2(forward.x, forward.z)`
- pitch = `Asin(forward.y)` (positivo = olhando pra cima)
- 12 Hz (`sendHz`, 5–30)
- Sem eye-tracking, sem Presence, sem `UPDATE` Postgres

## Maquete física (ESP32 + MG996R + MPU6050) — inclinação cima/baixo

A maquete segue o **pitch** da cabeça (limitado a ±`pitchLimitDeg`, padrão 60°). O ESP32 fecha a
malha com o MPU6050 colado na maquete. Firmware, ligações e troca de rede:
[`Hardware/esp32-gaze-servo/README.md`](../Hardware/esp32-gaze-servo/README.md).

Linha ASCII (cultura invariante), igual nos dois transportes:

```
Y,<yaw rad>,P,<pitch rad>,S,<servo yaw 30-150>,PD,<pitch graus limitado>,N,<seq>
```

`S` continua presente para firmwares antigos (servo de giro). `N` deixa o ESP32 descartar o
pacote duplicado que chega pelo outro caminho (o mais novo vence).

### Transportes (sem IP fixo)

| Campo | Padrão | Para quê |
|---|---|---|
| `sendMqtt` | on | MQTT 3.1.1 QoS0 pela internet: funciona com Quest e ESP32 em redes diferentes |
| `mqttHost` / `mqttFallbackHost` | `broker.emqx.io` / `broker.hivemq.com` | brokers públicos sem senha, porta 1883 |
| `mqttTopic` | `genjutsu-9de75d60960b/maquete/gaze` | tem que ser igual ao `MQTT_TOPIC_BASE` + `/gaze` do firmware |
| `sendUdpLan` | on | UDP broadcast `255.255.255.255:udpPort` (9000) para LAN sem internet |
| `udpExtraHost` | vazio | unicast opcional (redes que bloqueiam broadcast) |
| `sendBeforeSession` | on | envia mesmo antes do código de sessão |
| `invertPitch` | off | inverte o sentido |

`MaqueteMqttLink` roda numa thread própria: reconecta com espera 1→30 s alternando brokers,
manda PINGREQ a cada 10 s e só publica a amostra mais nova (o main thread nunca bloqueia).
O antigo `udpHost` fixo (`192.168.137.121`) foi removido.

Logs (`adb logcat -s Unity`): `[GazePublisher] MQTT connected broker.emqx.io:1883 topic=...` e,
a cada 5 s, `[GazePublisher] pitch=..deg seq=.. mqtt=MQTT conectado .. pub=.. udp=..`.

Observação: broker público = qualquer um que souber o tópico pode ler/publicar. O tópico tem um id
aleatório; para produção troque por broker com usuário/senha/TLS.

## Supabase Broadcast (ainda stub)

Canal alvo `gaze:{SESSION_CODE}`, evento `pose`, hello com `profile_id`. Sem SDK no `Packages/manifest.json`. A maquete não espera por esse canal.
