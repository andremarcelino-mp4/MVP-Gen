// Copie este arquivo para config.h (config.h é ignorado pelo git) e preencha.
#pragma once

// Redes Wi-Fi conhecidas (tentadas em ordem pelo WiFiMulti).
// Redes novas também podem ser gravadas sem reflash pelo portal "GenJutsu-Maquete".
#define WIFI_SSID_1 "SEU_SSID"
#define WIFI_PASS_1 "SUA_SENHA"
#define WIFI_SSID_2 ""
#define WIFI_PASS_2 ""

// MQTT público (sem credenciais). O tópico precisa ser igual ao do GazePublisher no Unity.
#define MQTT_HOST_1 "broker.emqx.io"
#define MQTT_HOST_2 "broker.hivemq.com"
#define MQTT_PORT 1883
#define MQTT_TOPIC_BASE "genjutsu-9de75d60960b/maquete"

// Portal de configuração (AP aberto quando nenhuma rede conhecida é encontrada).
#define PORTAL_AP_NAME "GenJutsu-Maquete"
#define PORTAL_AP_PASS ""   // vazio = AP aberto; ou 8+ caracteres
