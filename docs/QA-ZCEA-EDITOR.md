# QA — hub ZCEA no Editor (Device Simulator)

Fluxo de produto: **sala branca com mesa + pergaminho** → código da sessão → **morph gradual** no look `Zen|Cyber|Excentrico|Acolhedor`. O visitante **não escolhe casa**.

## Cena

1. Abrir `Assets/Scenes/SampleScene.unity` (o root `ProfileDemo` está **activo**; `TableTokens` está desligado).
2. Play. `ZceaHubBootstrap` cria SessionGate / morph / dissolve se o builder ainda não tiver corrido.
3. Opcional: menu **GenJutsu / Profile Demo / Build Demo** para regenerar a hierarquia.
4. **Game** view → Device Simulator (Quest / tablet paisagem).

## Código válido → morph

1. No painel **Código da sessão**, escrever o código emitido por `https://genjutsuai.vercel.app`.
2. **Entrar no dojo** (ou Enter).
3. Esperado: `GET https://genjutsuai.vercel.app/api/session?code=…` → `profile_id` → luz, depois materiais, depois props (~2,6 s). Mesa e pergaminho **dissolvem**; não desaparecem num frame.
4. Look correcto (volumes URP já existentes). Sem corvo.

## Código inválido / API down

1. Código lixo, ou desligar rede.
2. Esperado: permanece no **branco com mesa + pergaminho**, mensagem de erro, **nenhum look inventado**.

## Mock só no Editor (se a API estiver em baixo)

No `SessionGate`, ligar **Use Editor Mock** e escolher `editorMockProfile`. Isto **não existe** em build de device. Não é um picker de produto.

## Atalhos Editor (compilados fora do player)

`1`–`4` Zen/Cyber/Excentrico/Acolhedor, `0` volta ao branco. Só para QA.

## Tokens da mesa

Se existirem de um build antigo, `ProfileToken.opensHouse` fica **false**. Não abrem casa. Rebuild do demo deixa de criar tokens como seletor.

## Gaze

`GazePublisher` calcula yaw/pitch da cabeça; **não** envia Broadcast nesta PR (`docs/GAZE-PUBLISHER.md`).

## Sem Unity neste ambiente

A VM pode não ter o Editor. Entrega = C# + builder. No Mac/PC com Unity `6000.5.10f1`, correr o menu Build Demo e o loop acima.
