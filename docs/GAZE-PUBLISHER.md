# GazePublisher (stub nesta PR)

Contrato alvo (fase 4 do plano ZCEA):

- Canal Supabase Realtime **Broadcast** `gaze:{SESSION_CODE}`
- Evento `pose` a 10–15 Hz
- Hello uma vez com `profile_id`
- Coordenadas: yaw/pitch do `Camera.main.transform.forward` (XR head), Y-up, yaw 0 ≈ +Z da câmara convertida com `Atan2(x,z)`
- Sem Presence, sem `UPDATE` Postgres, sem eye-tracking

Nesta PR o componente `GazePublisher`:

- Guarda `code`, `profile`, `channel`
- Actualiza `yaw` / `pitch` em `Update`
- **Não** abre websocket nem envia JSON (não há SDK Supabase no `Packages/manifest.json`)

Ligar Broadcast completo sem bloquear o morph: próximo PR, após SessionGate estável.
