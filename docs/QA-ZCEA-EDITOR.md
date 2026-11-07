# QA — hub ZCEA no Editor (Device Simulator)

Fluxo: **sala branca com mesa + pergaminho** → o admin escolhe a sessão no site → card **“Bem-vindo, {nome}”** → **morph** no look `Zen|Cyber|Excentrico|Acolhedor`. O visitante **não escolhe casa** e **não digita código**.

## Cena

1. Abrir `Assets/Scenes/SampleScene.unity` (`ProfileDemo` activo; `TableTokens` desligado).
2. Play. Deve haver **um único** card (não dois painéis “Código da sessão”).
3. Sem InputField e sem botão “Entrar no dojo”.

## Admin no site → morph

1. Abrir `https://genjutsuai.vercel.app/admin` e iniciar a sessão de um visitante.
2. No headset/Editor, o poll `GET /api/session` (sem código) deve mostrar **Bem-vindo, {nome}**.
3. Se `profile_id` for `Zen|Cyber|Excentrico|Acolhedor`: morph ~2,6 s (luz → materiais → props). Mesa e pergaminho dissolvem.
4. Se `profile_id` for `null`: nome no card, **permanece no branco** (o site ainda não inferiu o look).
5. Sem sessão no admin (`no_current_session` ou corpo com `status` `ended`/`encerrado`): morph reverso até o hub branco. O poll continua. Outra visita do mesmo perfil entra de novo se o `session_code` (ou id) mudou.
6. No dojo a única UI é **Bem-vindo, {nome}**. Sem placa de perfil e sem callouts Move/Turn/Blink/Grab/UI Press.
7. O botão A / Jump do rig não faz o visitante pular.

## API down

Desligar rede: branco + mensagem. Sem look inventado.

## Mock só no Editor

No `SessionGate`, **Use Editor Mock**, `editorMockName` (ex. André) e `editorMockProfile`. Não existe em build de device.

## Atalhos Editor

`1`–`4` looks, `0` volta ao branco. Só QA.

## Gaze

Stub. Ver `docs/GAZE-PUBLISHER.md`.

## Contrato HTTP

`docs/CONTRATO-SESSAO-QUEST.md`.
