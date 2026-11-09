# Gen no Quest — fase 1 (voz + estado da casa)

Papel desta entrega: a casa responde. O Quest continua a ser o gêmeo e a camada de interação. A voz compreende o visitante e os atuadores (luz, ambiência, morph visual) respondem. O perfil da visita não muda por comando de voz.

Não há chave de API no APK. O headset só recebe o token efémero que o backend emite.

## URL

`GenVoiceController.baseUrl` e `HouseStateListener.baseUrl` copiam `SessionApiClient.baseUrl`. O valor de origem é `https://genjutsuai.vercel.app`. Trocar o campo no cliente de sessão muda a voz e a casa juntas.

## Contrato usado

Fonte pretendida: `Andremarcelin/genjustuai` `docs/gen-api.md`. O cliente implementa o que está abaixo. `GET /api/session` não mudou (`docs/CONTRATO-SESSAO-QUEST.md`).

| Pedido | Uso |
|---|---|
| `POST /api/gen/voice-session` | Corpo `{"session_id":"<id da visita>"}`. Resposta `{provider, model, token, expiresAt, wsUrl, systemInstruction, tools}`. `provider` tem de ser `gemini`. O `token` é o token efémero do Gemini Live. |
| WebSocket Gemini Live | `BidiGenerateContent`. Ver mensagens abaixo. |
| `POST /api/gen/commands` | Corpo exacto `{"tool":"<nome>","args":{...}}`. A resposta JSON (objecto) volta ao Gemini como `toolResponse`. Se trouxer `state`, a casa aplica-o. |
| `POST /api/gen/chat` | Reserva. `multipart/form-data` com o ficheiro `audio` (`turn.wav`, PCM16 16 kHz mono) e, quando há visita, o campo `session_id`. Resposta `{transcript, reply_text, actions, state}`. |
| `GET /api/house/state` | `{state:{session_id, version, mode, dojo, lights:{main,lanterns}, ambience:{preset,volume}, updated_at}}` ou `{state:null}`. |

Cores: `#RGB`, `#RRGGBB` ou `{r,g,b}` em 0–1 (acima de 1 trata-se como 0–255).

### Gemini Live

Referência actual: [Live API WebSockets](https://ai.google.dev/api/live). Cada mensagem do cliente tem um único campo de topo.

O URL é `wsUrl` do backend. Se o token ainda não estiver na query, acrescenta-se `access_token`. Se `wsUrl` vier vazio, o cliente usa:

`wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContent`

Setup (primeira mensagem), áudio de microfone e fim de fala:

```json
{"setup":{"model":"models/...","generationConfig":{"responseModalities":["AUDIO"]},"systemInstruction":{},"tools":[]}}
{"realtimeInput":{"audio":{"mimeType":"audio/pcm;rate=16000","data":"<base64 pcm16>"}}}
{"realtimeInput":{"audioStreamEnd":true}}
```

O servidor manda áudio em `serverContent.modelTurn.parts[].inlineData` (`audio/pcm;rate=24000`). `serverContent.interrupted` corta a fala local. `serverContent.turnComplete` fecha o turno.

O `wsUrl` de produção já traz `access_token` e aponta para `BidiGenerateContentConstrained`. O token já trava `voiceName` `Charon`; o setup do cliente não repete esse nome. Manda `models/gemini-3.8-live`, `responseModalities: ["AUDIO"]`, `languageCode` `pt-BR`, a `systemInstruction` e as tools dentro de `functionDeclarations`. O schema do backend inclui `additionalProperties`; o Gemini Live fecha o socket com `InvalidPayloadData` se esse campo for reenviado, por isso o cliente tira-o antes do setup. `setupComplete` pode chegar num frame binário; esses bytes são JSON UTF-8. O POST `/api/gen/voice-session` usa `UnityWebRequest` (o mesmo caminho do `/api/session`) e não repete o pedido quando a resposta é 429. A reserva `/api/gen/chat` só corre depois de uma falha real do Live, e também não corre num 429. O motivo fica no chip e em `Debug.Log` / `Debug.LogWarning` com a tag `[GenVoice]`.

O botão B do comando direito é lido nos dois caminhos: `secondaryButton` do OpenXR legado e o binding Oculus Touch `buttonB` / `secondaryButton` da mão direita no Input System. O projeto está em Input System only, então o caminho legado sozinho não via o B. `adb shell input keyevent 97` não chega nesse eixo. Para um teste no aparelho, escrever milissegundos em `Android/data/com.DefaultCompany.VRTemplate/files/gen-ptt.pulse` segura o mesmo caminho do B. O microfone abre com `Microphone.Start(null` ou `devices[0], loop, 1s, 48000 ou 16000)`, espera `GetPosition > 0` e lê blocos de cerca de 100 ms, reamostrados para 16 kHz. Enquanto ouve, o chip fica verde com "OUVINDO" e uma barra de nível. O TTS Android, se o motor padrão falhar, tenta `com.google.android.tts` ou o primeiro motor instalado; se nenhum iniciar, a fala fica no áudio do Gemini e a legenda continua.

Os presets `quiet`, `rain`, `bamboo` e `night` tocam laços sintetizados. Não há clips gravados no projeto.

Tool call e resposta:

```json
{"toolCall":{"functionCalls":[{"id":"call-1","name":"switch_dojo","args":{"dojo":"Cyber"}}]}}
{"toolResponse":{"functionResponses":[{"id":"call-1","name":"switch_dojo","response":{"ok":true}}]}}
```

`args` segue para `/api/gen/commands` sem ser reescrito. A resposta da função tem de ser um objecto JSON.

## Voz no headset

Estados: `idle`, `listening`, `thinking`, `speaking`, `offline`. Carregar para falar durante `speaking` ou `thinking` é a interrupção: o buffer de áudio esvazia e o microfone volta a enviar.

Push-to-talk: **botão secundário do comando direito (B)** (`CommonUsages.secondaryButton`, só a mão direita). Não é o botão primário A/X (o salto antigo do rig, já desligado), nem o gatilho, nem o grip — esses continuam a servir para agarrar props. No Editor, segurar **V**.

Indicador pequeno à frente da cabeça, por baixo da linha do olhar: `Gen · a ouvir` e a legenda. O cartão único `Bem-vindo, {nome}` mantém-se, sem a linha “à espera do anfitrião”. Quando o nome chega, a Gen diz essa frase em voz (Charon no Live; TTS Android se o socket falhar). O morph da visita só começa na placa da mesa (`始` / COMEÇAR). Cada fala, ferramenta e mudança de casa vai em `POST /api/gen/history` (`session_id`, `role`, `text`, `kind`, `source: quest`), sem chave no APK.

O microfone pede `android.permission.RECORD_AUDIO` em Android (`Permission.Microphone` e o library manifest `Assets/Plugins/Android/GenVoice.androidlib`). O `Microphone` da Unity lê na thread principal; o WebSocket e o HTTP correm fora dela e voltam por `MainThread`. O playback é um `AudioClip` em stream, mono, `spatialBlend` 0, 24 kHz, lido na thread de áudio.

Se o Gemini falhar, o mesmo turno vai em WAV para `/api/gen/chat`. `reply_text` aparece na legenda. **TTS:** no player Android (Quest) usa `android.speech.tts.TextToSpeech`. No Editor e nos outros players só há legenda — não há TTS.

## Casa

`HouseStateListener` faz poll de `GET /api/house/state` a cada 1 s enquanto há visita. Não há cliente Supabase Realtime no APK: em IL2CPP/Android o poll é o caminho fiável. O canal documentado no backend (`house:<session_id>`, evento `house_state_changed`) fica para uma fase seguinte. Só entra uma `version` estritamente maior, e só se `session_id` for o da visita aberta (ou vier vazio). `{state:null}` não mexe na sala.

`HouseStateApplier` interpola a luz direccional (`main`) e as luzes pontuais mais a emissão dos candeeiros de papel (`lanterns`). A ambiência usa a cama já gerada por `ProceduralMusic`: `off` / `silence` / `mute` param; qualquer outro `preset` (incluindo `night`) aplica `volume`.

`dojo` (ou a acção `switch_dojo`) dispara o morph que já existe (`ProfileMorph` + dissolve/build do look). É só visual. `GuestSession` e o `profile_id` da visita não são escritos. Quando o admin encerra a sessão, o regresso ao hub branco é o de hoje (`DojoDirector` → `ReturnToWhiteSpace`). O applier larga as luzes comandadas para não lutar com esse regresso.

## O que não está verificado no dispositivo

O token Gemini e o site podem ainda não estar publicados. Esta fase não abriu um socket real contra `generativelanguage.googleapis.com` nem um Quest com microfone. O que corre no Editor são os testes de versão, o mapeamento do plano e o roundtrip do tool call com um transporte simulado, mais as capturas da sala.
