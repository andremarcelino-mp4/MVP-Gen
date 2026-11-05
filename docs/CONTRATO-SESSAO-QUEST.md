# Contrato — sessão Quest (admin no site, sem código no headset)

O visitante **não digita código** no Quest. O anfitrião escolhe a pessoa em `https://genjutsuai.vercel.app/admin`. O headset lê a sessão activa.

## O que o site já faz (verificado 2026-11-05)

| Pedido | Resultado observado |
|---|---|
| `GET /api/session` (sem query) | `404 {"error":"no_current_session"}` se o admin ainda não escolheu ninguém |
| `GET /api/session` com sessão activa | (mesmo path) corpo da pessoa escolhida |
| `GET /api/session?code=3V9TXC` | `200` com `session_code`, `display_name`, `profile_id`, `archetype` |
| `GET /api/admin/session` | `current_session_code` + `recent[]` (`display_name`, `session_code`) |
| Admin HTML | “O Quest em `GET /api/session` carrega a pessoa escolhida.” |

Exemplo real (`?code=3V9TXC`):

```json
{
  "session_code": "3V9TXC",
  "display_name": "Cyber 365",
  "personality_text": "…",
  "archetype": "Perfil em formação",
  "profile_id": null
}
```

`profile_id` válido no produto: `Zen` | `Cyber` | `Excentrico` | `Acolhedor` (strings exactas). `null` = sessão existe, look ainda não.

## O que o Unity faz

1. Poll `GET https://genjutsuai.vercel.app/api/session` a ~2 s. **Sem** campo de código.
2. `no_current_session` → card “Bem-vindo” / à espera do admin. Branco + mesa + pergaminho. Sem look inventado.
3. `display_name` → **“Bem-vindo, {nome}”**.
4. `profile_id` válido → morph gradual no DojoShell.
5. `profile_id` ausente/`null` → boas-vindas com nome, **fica no branco**.
6. Rede em baixo → mensagem, fica no branco.

Não usa `GET /api/admin/session` no headset (página de backoffice).

## Follow-up no repo `genjustuai` (não este PR)

Não é obrigatório mudar o path: `GET /api/session` sem código **já é** a sessão activa.

Pedidos ao site, se o morph não disparar em feira:

1. Garantir que **Iniciar sessão no Quest** em `/admin` grava `current_session_code` para o `GET /api/session` sem query passar a 200.
2. Preencher `profile_id` no JSON quando o questionário inferir ZCEA. Hoje um cadastro real veio com `"profile_id": null` — o Unity não inventa look.
3. Manter `display_name` para o cartão de boas-vindas.
4. CORS já está `access-control-allow-origin: *` neste endpoint.

Editor QA: `SessionGate.useEditorMock` (só Editor) simula nome + perfil sem o admin.
