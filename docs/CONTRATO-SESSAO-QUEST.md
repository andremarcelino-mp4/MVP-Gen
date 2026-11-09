# Contrato — sessão Quest (admin no site, sem código no headset)

O visitante **não digita código** no Quest. O anfitrião escolhe a pessoa em `https://genjutsuai.vercel.app/admin`. O headset lê `GET /api/session` (sem query). Várias visitas podem usar o **mesmo** `profile_id`. O Quest distingue uma visita da seguinte pelo **`session.id`**, nunca pela letra Z/C/E/A.

Fonte: `Andremarcelin/genjustuai` PR #9 (`cursor/end-session-and-multi-profile-133f`). `id` é o código único da visita.

## Contrato actual — `GET /api/session`

Sessão activa, HTTP 200:

```json
{
  "session": {
    "id": "AB3K7Q",
    "session_code": "AB3K7Q",
    "display_name": "Lina",
    "personality_text": "Lina, o dia abre em água parada.",
    "archetype": "Zen",
    "profile_id": "Zen",
    "status": "active"
  }
}
```

Sem sessão activa — o admin ainda não iniciou, **ou** a sessão foi encerrada. HTTP 200, não é erro:

```json
{ "session": null }
```

Consulta com `?code=` de uma visita já encerrada (o poll sem query não devolve este objeto; devolve `{ "session": null }`):

```json
{
  "session": {
    "id": "AB3K7Q",
    "session_code": "AB3K7Q",
    "display_name": "Lina",
    "profile_id": "Zen",
    "status": "ended"
  }
}
```

`profile_id` válido: `Zen` | `Cyber` | `Excentrico` | `Acolhedor`.

## Fim de sessão

O Quest volta ao hub branco (morph reverso, sem corte seco) e o card regressa a “Bem-vindo”. O poll continua. O card **“Bem-vindo, {nome}”** permanece durante a visita; o que sai são os painéis de explicação do perfil.

1. **`{ "session": null }`** em HTTP 200. É o sinal do poll quando não há visita ou o admin finalizou.
2. **Objecto `session` com visita encerrada** (`status` / `state` / `session_status` / `lifecycle` em `ended`, `encerrado`, `encerrada`, `closed`, `finished`, `complete`, `completed`, `cancelled`, `canceled`, `inactive`, `finalizado`, `finalizada`, `terminado`, `terminada`), ou `ended: true`, `active: false`, `ended_at` com data. `status: "active"` não encerra.
3. **Legado:** HTTP 404/204, em particular `{"error":"no_current_session"}`, e um JSON plano sem wrapper `session` (deploy anterior ao PR #9).

Falha de rede não encerra o dojo.

## Identidade

Chave = `session.id`. O mesmo perfil com outro `id` é outra visita e o morph corre de novo. Sem `id`, o fallback é `session_code` (corpo plano antigo).

## O que o Unity faz

1. Poll `GET https://genjutsuai.vercel.app/api/session` a ~2 s, no hub e dentro do dojo.
2. `session: null` → card só com “Bem-vindo”. Sem a frase de espera. Branco + mesa, pincel, pergaminho, bule, alvo e a placa COMEÇAR.
3. `display_name` com `status: "active"` → a UI do óculos é o card **“Bem-vindo, {nome}”**. Sem painel “Acolhedor / Empático” nem callouts de controle.
4. `profile_id` válido e `id` novo → a visita fica pronta. O morph só começa quando o visitante toca a placa 始 / COMEÇAR. Mesa, pincel, pergaminho, bule e alvo dissolvem. O card de boas-vindas fica.
5. `profile_id` ausente → nome no card, fica no branco.
6. Fim (casos acima) → morph reverso ~2,6 s até o hub, pronto para outra visita.
7. Rede em baixo no hub → mensagem, sem look inventado. Rede em baixo no dojo → permanece.

Não usa `GET /api/admin/session` no headset.

Editor QA: `SessionGate.useEditorMock` (só Editor) simula nome + perfil sem o admin. `SessionGate.Ingest` aplica um JSON sem rede (testes e capturas).

Enquanto o PR #9 não estiver em produção, um corpo plano (`session_code` na raiz) ou um `404 no_current_session` continua válido.
