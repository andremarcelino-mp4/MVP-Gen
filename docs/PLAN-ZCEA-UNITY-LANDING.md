# Plano de implementação — Casas ZCEA, sessão Quest e gaze na landing

Documento de arquitetura e contratos. **Não implementa** C#, shaders, prefabs, GLTF nem alterações no site `genjustuai`. Implementação de runtime fica em PRs posteriores, fase a fase.

| Campo | Valor |
|---|---|
| Repo Unity | `andremarcelino-mp4/MVP-Gen` (este repositório) |
| Site | `https://genjutsuai.vercel.app` — repo **externo** `Andremarcelin/genjustuai` (não editar neste agente) |
| Unity Editor | `6000.5.10f1` (`ProjectSettings/ProjectVersion.txt`) |
| Cena de build | `Assets/Scenes/SampleScene.unity` (`ProjectSettings/EditorBuildSettings.asset`) |
| Branch de referência do demo de 4 casas | `origin/cursor/desktop-xr-simulator-play` (já mergeada parcialmente via PR #1; **ProfileDemo ainda não está em `main`**) |
| Data do inventário | 2026-11-05 |

---

## 1. Princípio de produto

GenJutsu AI é uma **Smart Home Cognitiva**: o ambiente compreende, responde e espelha o contexto do visitante. O Quest é gêmeo digital e camada de interação; o site é a porta de entrada anônima.

Regras que o runtime **não pode violar**:

1. **O visitante não escolhe a casa.** Nome + questionário no site mapeiam sozinhos para Z / C / E / A. O headset lê `profile_id` estável. Não há picker de casa no Quest (nem tokens de mesa, nem teclas 1–4 em build de dispositivo).
2. **Uma casca, quatro looks.** A jornada espacial é: sala branca (neutro) **com mesa + pergaminho** → **morph gradual** (não corte seco) no **mesmo shell de dojo** com variante de perfil. Mesa e pergaminho **dissolvem como mágica** durante o morph. Não são quatro geometrias de casa independentes como destino final do produto (o demo atual em `ProfileDemo` ainda constrói quatro casas distintas — ver inventário e gaps).
3. **Sessão anônima + código.** O site emite um código; o Quest envia esse código a `GET /api/session` e recebe `profile_id`. Sem conta Google obrigatória no headset.
4. **Físico ↔ digital via gaze.** O yaw/pitch da cabeça no Quest espelha a casa GLTF na landing. Canal: **Supabase Realtime Broadcast** (não Presence, não `UPDATE` Postgres a 30 Hz).
5. **Hero Mix B2 v1 é contrato visual da landing, não commit agora.** Casa 3D orbitável **sem moldura/bezel**, **sem corvo no dojo**, órbita real Three.js/GLTF (não CSS spin de PNG). **Não subir o hero até André pedir.**
6. Recusar feature que não ajude a compreender, aprender, responder ou ligar físico↔digital — salvo infra/QA/CI pedidos explicitamente (`docs/TIME_AGENTICO.md` na branch de referência).

### Mapeamento ZCEA

Valores de `profile_id` alinhados ao site (copy em `genjutsuai.vercel.app`) e ao enum já existente no demo:

| Letra | `profile_id` (string API) | `ProfileId` (C# em ProfileDemo) | Título | Tom |
|---|---|---|---|---|
| Z | `Zen` | `ProfileId.Zen` | Zen / Equilibrado | Dojo silencioso |
| C | `Cyber` | `ProfileId.Cyber` | Cyber / Estratégico | Central de dados |
| E | `Excentrico` | `ProfileId.Excentrico` | Excêntrico / Criativo | Ateliê |
| A | `Acolhedor` | `ProfileId.Acolhedor` | Acolhedor / Empático | Casa quente |

Contrato: a API devolve **exatamente** essas quatro strings (sem `Z`/`C`/`E`/`A` soltos, sem acento em `Excentrico`). O Unity faz parse case-sensitive; fallback de parse só em Editor.

### UUID da casa na landing

`f0ac53d5-4706-4ad8-9897-aabd6ba2499f` — citado por André como referência da casa 3D.

- **Busca neste repo (`main` e `origin/cursor/desktop-xr-simulator-play`):** nenhuma ocorrência (código, docs, assets, nomes de ficheiro).
- **Classificação:** **id externo a resolver** (asset Spline/composer, ficheiro no repo `genjustuai`, ou id interno de ferramenta 3D). Não tratar como GUID Unity (`Assets/` não contém esse guid).
- **Ação de produto:** o Diretor confirma com André se o GLTF Mix B2 v1 no site usa este id; o plano da landing (secção 7) usa um alias `LANDING_HOUSE_ASSET_ID` até haver path real.

---

## 2. Quem faz o quê (diretor → QA)

Fonte: `docs/TIME_AGENTICO.md` e `docs/ALINHAMENTO_GTI.md` em `origin/cursor/desktop-xr-simulator-play` (ainda **ausentes em `main`** — gap: trazer esses docs no PR de implementação 0 ou neste merge de plano se o Diretor quiser). Skills: `.cursor/skills/gti-*`, `.cursor/skills/graphify`, `.agents/skills/unity-cli` (+ URP, uGUI, optimize-audio).

| Papel | Neste plano | Skills |
|---|---|---|
| **Diretor de produto** | Corta picker de casa, hero commit, ESP32; confirma UUID e strings `profile_id`. | `gti-casa-que-aprende`, `gti-genai-prompt` |
| **Tech lead / AI-Driven SE** | Arquitetura DojoShell / SessionGate / GazePublisher; Graphify após C#; git. | `gti-ai-driven-se`, `.agents/skills/unity-cli`, Graphify |
| **Especialista smarthome** | Sensor (cabeça) → decisão (look) → atuador (morph + landing). MQTT só fase 2. | `gti-casa-que-aprende` |
| **Especialista VR (Quest 3)** | Conforto do morph **gradual** (≤ ~2–3 s, sem hop de câmara, sem corte); URP Performance; Device Simulator. | XRI / URP skills; Meta comfort |
| **Design de interiores** | Hub branco com mesa + pergaminho de orientação; fidelidade do dojo; paleta ZCEA no **mesmo shell**. | WELL/IEQ |
| **QA** | Play com `Time.frameCount` avançando (`playmode-verification-loop.md`); SessionGate; regressão mesa. | `unity-cli` |
| **Cybersegurança** | TLS no `GET /api/session`; anon keys só publishable; sem secrets no git; Broadcast ≠ vazamento de PII. | OWASP MASVS; recusar exploits |
| **Dados e privacidade** | Código de sessão + `profile_id` persistidos no device; gaze **efémero** (não gravar 15 Hz). LGPD. | TIME_AGENTICO |
| **CI/CD** | Sem GitHub Actions nesta rodada. APK: `unity command build`, nunca `unity build` de topo; bump `bundleVersion`. | `AGENTS.md` (só na branch de referência) |
| **Knowledge / Graphify** | Se `graphify-out/` existir, `graphify query` antes de Grep em `Assets/GenJutsu`; `graphify update .` depois de C#. | `.cursor/skills/graphify` |
| **Audio lead** | One-shot leve **opcional** no início do morph; trilha por look depois; sem clip 3D estéreo. | `optimize-audio` |
| **UX / pesquisa** | Jornada: código na sala branca (mesa + pergaminho) → morph mágico; sem “escolha outro objeto”. | — |
| **Ética de IA** | ZCEA são modos de casa, não laudo. | — |
| **Landing (repo externo)** | Viewer GLTF, look por `profile_id`, idle vs live gaze. **Outro agente / outro repo.** | — |

Fluxo obrigatório em PRs de código: Diretor (escopo) → Tech lead (diff) → Cyber se houver rede → **QA fecha**.

---

## 3. Arquitetura alvo

```
[Site genjustuai]                  [Supabase Realtime]
  questionário → profile_id            canal gaze:{CODE}
  código de sessão                     evento "pose" 10–15 Hz
  GET /api/session  ←──────────────────┘
         │                              ▲
         │ HTTPS                        │ Broadcast (não Presence,
         ▼                              │ não Postgres UPDATE)
[Quest SessionGate] ── profile_id ──► [DojoRuntime]
  GuestSession (código, profile)                                                  NeutralLook (branco + mesa + pergaminho)
                                         HubPropsDissolve (mesa/scroll fade)
                                         DojoShell (mesh/estrutura única)
                                         ProfileLook Z|C|E|A (materiais, luz, props)
                                         ProfileMorph (gradual: luz → mats → props)
                                         GazePublisher (head forward yaw/pitch)
                                                │
                                                ▼
                                       [Landing Three.js]
                                         GLTF sem moldura
                                         idle orbit XOR live gaze
```

### 3.1 UNITY — Casas

Componentes lógicos (nomes de contrato; pastas sugeridas `Assets/GenJutsu/Dojo/`):

| Componente | Responsabilidade |
|---|---|
| `DojoShell` | Geometria compartilhada: piso, pilares, telhado/linhas do dojo, escala humana. Uma hierarquia. Sem corvo. |
| `NeutralLook` | Sala branca: chão/céu pálidos (`BackdropGround` / horizonte branco, PR #1) **e hub com mesa + pergaminho** (orientação / SessionGate). Não é um vazio sem props. |
| `HubProps` | Mesa japonesa existente (`JapaneseTableSet` / `TableProps`) + **pergaminho** (`Scroll` / `ScrollCanvas` em `Assets/GenJutsu/TableProps/`). Copy de sugestão, **não** seletor ZCEA. |
| `ProfileLook` | ScriptableObject ou volume+materiais por `ProfileId`: paleta, fog, luz, props de perfil **parented ao shell**, não um segundo edifício. |
| `ProfileMorph` | Transição **gradual** branco → look no **mesmo shell** (nunca corte seco / `SetActive` instantâneo do destino). Sequência: **luz → materiais → props do look**. Duração total **≤ ~2–3 s**. Opcionalmente look → branco no fim, também gradual. |
| `HubPropsDissolve` | Durante o morph, mesa e pergaminho **somem como mágica** (dissolve/fade/VFX leve). Proibido teleport, `Destroy` no frame 0, ou pop-out. |
| `DojoDirector` | Substitui o loop “escolha token → EnterProfile”. Só dispara o morph depois de `GuestSession.profile_id` válido. |

Fidelidade: Interior Design compara com **imagens de referência do dojo** (fornecer pasta `docs/refs/dojo/` quando existirem; hoje **não há** imagens de dojo versionadas neste repo). Até lá, o Zen house em `ProfileDemoHouses.cs` (tatami, papel, lanternas, bambu) é a melhor proxy **procedural**, mas é uma casa inteira, não um look.

**Proibido no headset:** `ProfileToken` como seletor de destino; teclado 1–4 em player; UI “escolha sua casa”. Tokens da mesa, se existirem, **não** abrem casas; no hub só mesa + pergaminho de orientação.

#### Contrato do morph (pedido André, 2026-11-05)

Estado inicial do hub = **sala branca com mesa + pergaminho**. O pergaminho sugere o código / a jornada; a mesa ancora o espaço. Depois do SessionGate com `profile_id`:

| Tempo (relativo, total ≤ ~2–3 s) | O que muda | O que **não** acontece |
|---|---|---|
| t0 | One-shot de áudio **leve e opcional** (whoosh/chime baixo). Câmara **fixa no XR Origin** (o jogador não é teleportado). | Hard cut para o dojo; swap de cena. |
| ~0–30% | **Luz:** directional + ambient + fog interpolam rumo ao `ProfileLook`. Volume URP `weight` sobe. | Flash full-white; exposição que estoura. |
| ~20–70% | **Materiais / shell:** albedo/emission do `DojoShell` revelam o look (lerp ou dissolve de superfície). | `SetActive(true)` no dojo inteiro num único frame. |
| ~40–100% | **Props do look** (lanternas, estações, etc.) aparecem em fade/rise suave. | Spawn pop; scale bounce agressivo (`EaseOutBack` forte do demo). |
| **Sobreposto ~0–80%** | **Mesa + pergaminho dissolvem** (alpha / dither / VFX de partículas leve). Colliders da mesa desligam quando o fade passou o ponto de não-interação (~50%). | Teleporte da mesa para (0,−99,0); `Destroy` imediato; mesa a atravessar o chão do dojo. |

Critérios de pronto (QA + VR):

- Um observador **não** descreve a transição como “corte” ou “a mesa sumiu de um frame para o outro”.
- No fim, a mesa e o pergaminho **não** estão no dojo (nem invisíveis com collider).
- Duração medida (Time) entre 2,0 e 3,0 s inclusive, salvo o Diretor afinar.
- Áudio one-shot ausente **não** bloqueia o morph (feature flag / clip nulo).

O `HouseTransition` atual (anéis de scale a partir da cabeça) pode inspirar o **rise do shell**, mas **não** substitui o dissolve da mesa nem a ordem luz → materiais → props.

### 3.2 UNITY — Secções / cenas

Inventário e proposta (não criar as cenas neste PR).

**Hoje (`main`):**

| Área | Path | Papel atual |
|---|---|---|
| Hub XR | `Assets/Scenes/SampleScene.unity` | Cena de build; mesa japonesa + props; horizonte branco (PR #1) |
| Cena vazia | `Assets/Scenes/BasicScene.unity` | Template |
| AR / passthrough | `Assets/GenJutsu/AR/` | Ghost + OpenXR Meta camera |
| Mesa | `Assets/GenJutsu/TableProps/` | Builders editor (mesa, chá, shuriken, dragão) |
| Teste de voz | `Assets/GenJutsu/AI_Test/` | Scaffolding Groq |
| Samples XRI | `Assets/Samples/XR Interaction Toolkit/3.5.1/` | Simulator desktop (PR #1) |

**Na branch `origin/cursor/desktop-xr-simulator-play` (não em `main`):**

| Área | Path | Papel |
|---|---|---|
| Demo 4 casas | `Assets/GenJutsu/ProfileDemo/` | Root `ProfileDemo` injetado **na mesma** `SampleScene` |
| Director | `ProfileDemoDirector.cs` | Estados WhiteSpace → Transforming → InHouse → Dissolving |
| 4 casas procedurais | `Editor/ProfileDemoHouses.cs` | Zen / Cyber / Excêntrico / Acolhedor como `HouseTransition` separados |
| Picker | `ProfileToken.cs` | Grab na mesa abre a casa — **a remover do produto** |
| Atividades | `Activities/*.cs` | 3 estações por casa (L/R/F) |
| Recorder | `DemoSessionRecorder.cs` | `persistentDataPath/GenJutsuSessions/{id}/session.json` |
| Volumes URP | `ProfileDemo/Volumes/*Volume.asset` | Um volume por perfil |
| Docs GTI | `docs/TIME_AGENTICO.md`, `docs/ALINHAMENTO_GTI.md`, `AGENTS.md` | Papéis e build APK |
| Skills GTI | `.cursor/skills/gti-*`, `.cursor/skills/graphify` | Currículo + Graphify |

**Proposta de estrutura (fases 1–3):**

Manter **uma cena de runtime** (`SampleScene` ou rename futuro `QuestSession.unity`) com áreas lógicas, não additive scenes no v1 (menos churn de XR Origin):

1. **Hub branco** — **obrigatório:** mesa + pergaminho (orientação) + UI de código (`SessionGate` world-space uGUI — skill `.agents/skills/ui-ugui`). Outros props da mesa (chá, shuriken, dragão) ficam **desligados** no hub de produto, salvo o Diretor reabrir.
2. **Dojo shell** — presente na cena mas em NeutralLook / peso 0 até sessão válida; morph **in-place e gradual**.
3. **Conteúdo por perfil** — children tagged do `ProfileLook`, enable por id. Reaproveitar atividades do ProfileDemo **só as do look ativo**, sem as outras três casas em memória se o budget Quest apertar (ver riscos).
4. **Não** criar cena por letra ZCEA.

Additive scenes (`Hub.unity` + `Dojo.unity`) só se o Tech lead medir pico de RAM; default = uma cena.

### 3.3 UNITY — Utilizador / sessão

| Conceito | Persistência | Conteúdo |
|---|---|---|
| `GuestSession` | PlayerPrefs ou ficheiro local (código + `profile_id` + `fetchedAt`) | Sem nome real obrigatório no Quest; o nome vive no site |
| Código digitado | Efêmero no campo UI até sucesso; depois persistido para reabrir o app |
| `profile_id` | Persistido até “trocar código” |
| Gaze stream | **Nunca persistir** no device nem em Postgres |
| `DemoSessionRecorder` | Continua só no device (`session.json` + PNG); não enviar scores no canal gaze |
| Conta Google | Fora do Quest v1 | Site opcional |

`SessionGate` (contrato de comportamento):

1. Mostrar teclado/código world-space (uGUI + XR poke).
2. `GET {SITE}/api/session?code={CODE}` (query exacta a **confirmar** com o repo do site; o GET sem query em 2026-11-05 devolveu HTTP 404 — esperado).
3. Sucesso → guardar `GuestSession` → esconder gate (fade da UI) → `DojoDirector.Morph(profile_id)` **gradual** (secção 3.1). Mesa/pergaminho ainda visíveis em t0; dissolvem **durante** o morph.
4. Código inválido → mensagem, permanece no branco **com mesa + pergaminho**, **não morph**.
5. API down / timeout → retry limitado + modo degradado: permanece NeutralLook (mesa + pergaminho); **não** inventar perfil; opcional “usar último `profile_id` cacheado” só se o código coincidir.
6. Sem seletor ZCEA de fallback no headset.

Modelo User vs Guest: no Quest v1 existe só **Guest** (código). Não há Unity Authentication. O site pode ter Google; o headset não precisa do token Google.

---

## 4. Contratos

### 4.1 Sessão HTTP

**Produtor:** site `genjustuai`. **Consumidor:** Unity `SessionGate`.

Pedido (proposta; alinhar path/query com o backend existente):

```http
GET /api/session?code=ABCD-1234 HTTP/1.1
Host: genjutsuai.vercel.app
Accept: application/json
```

Resposta sucesso (200) — campos mínimos:

```json
{
  "code": "ABCD-1234",
  "profile_id": "Zen",
  "display_name": "Visitante",
  "created_at": "2026-11-05T12:00:00.000Z"
}
```

- `profile_id`: enum `Zen` | `Cyber` | `Excentrico` | `Acolhedor`.
- `display_name`: opcional; não é PII extra a gravar no gaze.
- Erros: `404` código desconhecido; `400` código malformado; `5xx` API down. Corpo de erro: `{ "error": "invalid_code" }` (a confirmar no repo do site).

**Gap:** este agente não inspecionou `Andremarcelin/genjustuai`. O Tech lead deve colar o OpenAPI real no PR da fase SessionGate. A landing afirma: *“O headset inicia esta sessão quando o admin escolhe o código em /admin.”* — o Quest ainda precisa de um fluxo de digitação **além** do admin, senão o visitante não entra sozinho. Diretor decide: (A) digitação no headset, (B) só admin pairing. Este plano assume **(A)** como default de produto + (B) como atalho de feira.

### 4.2 Gaze — Realtime Broadcast

| Item | Contrato |
|---|---|
| Transporte | Supabase Realtime **Broadcast** |
| Canal | `gaze:{SESSION_CODE}` exemplo `gaze:ABCD-1234` |
| Evento | `pose` |
| Taxa | throttle **10–15 Hz** (não 30 Hz; não cada `Update` sem dt) |
| Não usar | Presence (join/leave noise), `postgres_changes`, `UPDATE` em tabela de pose |
| Auth | chave **anon/publishable** no cliente Unity e no browser; RLS/Broadcast config no projeto Supabase (site já tem projeto — **não** criar segundo projeto) |
| Coordenadas | yaw/pitch da **head forward** (câmara XR), radianos, Y-up, yaw 0 = −Z Unity (alinhar com Three.js Y-up no PR de landing; documentar um único “forward”) |
| `profile_id` | **só no join / primeiro payload de sessão**, não em cada pose |
| `roll` | opcional; landing pode ignorar |

Join / hello (uma vez após SessionGate):

```json
{
  "type": "hello",
  "code": "ABCD-1234",
  "profile_id": "Zen",
  "t": 1730000000.123
}
```

Pose (10–15 Hz):

```json
{
  "type": "pose",
  "code": "ABCD-1234",
  "yaw": 0.42,
  "pitch": -0.08,
  "roll": 0.0,
  "t": 1730000000.190
}
```

`t`: epoch seconds UTC (float). Landing descarta poses com `|now - t| > 2s` e volta a idle orbit.

Unity: ler `Camera.main.transform.forward` (XR Origin head), converter para yaw/pitch; **não** eye-tracking (`GazeInputManager` do sample XRI é outro domínio).

ESP32 / MQTT: **fase 2** (roteiro). Não entra no APK v1 nem neste contrato.

### 4.3 Landing (contrato para `genjustuai`, sem editar aqui)

| Requisito | Detalhe |
|---|---|
| Viewer | Three.js + GLTF, **órbita real** (controls / slerp), não PNG + CSS `rotate` |
| Moldura | **Sem** bezel, card 3D, ou moldura à volta da casa (Mix B2 v1) |
| Corvo | **Sem** corvo no dojo |
| Asset | GLTF da casa; UUID `f0ac53d5-…` é id externo até o path no repo do site ser citado |
| Look ZCEA | Aplicar materiais/env no GLTF a partir de `profile_id` da sessão (não quatro modelos se o shell for um) |
| Idle | Órbita automática quando não há pose fresca |
| Live | Se Broadcast `pose` < 2 s: aplicar yaw/pitch (GazeMarker na casa **ou** rotate Y do grupo GLTF — um dos dois; default **rotate Y + pitch limitado** para não virar a casa de cabeça para baixo) |
| Hero no git do site | **Não mergear** até André pedir |
| Sessão | Mesmo `code` que o Quest; `profile_id` no join |

---

## 5. Inventário do repo atual e gaps

| Item | `main` | `cursor/desktop-xr-simulator-play` | Gap vs produto |
|---|---|---|---|
| Mesa + props | Sim (`JapaneseTableSet`, `Scroll`) | Sim + dragão mais realista | Hub **deve** usar mesa + pergaminho; dissolve no morph ainda **não existe** |
| Horizonte branco | Sim (PR #1) | Sim | NeutralLook parcial |
| XR Simulator desktop | Sim (PR #1) | Sim | QA no Editor |
| ProfileDemo 4 casas | **Não** | Sim | Trazer código **sem** o picker |
| DojoShell único | Não | 4 `HouseTransition` | Refactor maior |
| SessionGate / HTTP | Não | Não | Novo |
| Pacote Supabase | Não em `Packages/manifest.json` | Não | Novo (Realtime SDK ou websocket mínimo) |
| GazePublisher | Não | Não | Novo |
| Docs TIME_AGENTICO / GTI skills | Não | Sim | Trazer docs+skills no merge de implementação |
| `graphify-out/` | gitignored / ausente | idem | Gerar localmente |
| UUID landing | Ausente | Ausente | Externo |
| Imagens ref dojo | Ausente | Ausente | Pedir a André |
| Site API | 404 sem `code` | — | Confirmar schema no repo web |
| Corvo | Não encontrado | Não encontrado | Manter ausente |
| Hero GLTF | Não neste repo | Não | Repo web |

Packages relevantes (`Packages/manifest.json`): URP 17.5, XRI 3.5.1, XR Hands 1.8.1, OpenXR 1.17.1, Meta OpenXR 2.5.1, Input System, `com.unity.pipeline` 0.8.0-exp.1. **Não** há cliente Supabase.

---

## 6. Roteiro por fases

### Fase 0 — Docs e alinhamento (este PR)

- [ ] Este ficheiro em `docs/PLAN-ZCEA-UNITY-LANDING.md`
- [ ] Diretor confirma: UUID, query `/api/session`, pairing admin vs digitação
- **Pronto:** PR só com Markdown; zero runtime.

### Fase 1 — Trazer ProfileDemo sem o tornar o produto

- [ ] Merge seletivo de `Assets/GenJutsu/ProfileDemo/` + docs GTI/`AGENTS.md`/`.cursor/skills` a partir de `origin/cursor/desktop-xr-simulator-play`
- [ ] Desligar `ProfileToken` e teclas 1–4 **em player** (manter teclas só `#if UNITY_EDITOR` para QA)
- [ ] QA: Play mode loop; **mesa + pergaminho visíveis no hub**; demo oculta não quebra hub
- **Pronto:** `main` compila; 4 casas ainda existem como **laboratório**, não como jornada do visitante.

### Fase 2 — DojoShell + looks + morph gradual

- [ ] Extrair shell comum (começar pelo layout Zen como geometria base)
- [ ] `ProfileLook` Z/C/E/A (volumes já em `ProfileDemo/Volumes/`)
- [ ] `NeutralLook` = branco **com mesa + pergaminho**
- [ ] `ProfileMorph` no mesmo objeto: **luz → materiais → props**; duração ≤ ~2–3 s; **sem corte seco**
- [ ] `HubPropsDissolve`: mesa e pergaminho fade/VFX **durante** o morph (não teleport)
- [ ] One-shot de áudio leve **opcional** em t0
- [ ] Sem corvo; Interior Design vs refs quando existirem
- **Pronto:** um shell visível; quatro looks via API **simulada** no Editor; QA descreve a transição como contínua; mesa/scroll ausentes no estado InHouse; Device Simulator sem náusea (sem hop de câmara).

### Fase 3 — SessionGate

- [ ] UI código (uGUI world-space, skill ui-ugui)
- [ ] `UnityWebRequest` TLS para `GET /api/session`
- [ ] `GuestSession` persistido; erros inválido / down
- [ ] Cyber: sem secrets; URL base em config, não hardcoded token
- **Pronto:** código válido → morph **gradual** no look certo (mesa/scroll dissolvem); inválido → branco **com mesa + pergaminho** + copy; API down → sem look inventado.

### Fase 4 — Gaze Broadcast

- [ ] Cliente Realtime Broadcast, canal `gaze:{code}`, evento `pose`, 10–15 Hz
- [ ] Hello com `profile_id` uma vez
- [ ] Contrato JSON da secção 4.2
- [ ] Landing (repo externo) espelha; Unity não espera ACK
- **Pronto:** segundo browser em idle vs live; cortar rede → landing volta a orbit em < 2 s; Profiler < orçamento (ver riscos).

### Fase 5 — Conteúdo por perfil (opcional após shell)

- [ ] Portar estações ProfileDemo para children do look ativo
- [ ] Recorder continua local
- **Pronto:** uma atividade por look smoke-testada.

### Fase 6 — Landing hero (repo `genjustuai`, **não agora**)

- [ ] GLTF sem moldura, look ZCEA, idle vs live
- [ ] Só merge quando André pedir
- **Pronto:** definido no repo web, fora deste agente.

### Fase 7 — ESP32 / MQTT (mencionar apenas)

Sensor físico / gêmeo IoT. Fora do APK até o Diretor abrir módulo. Não misturar com Broadcast gaze.

---

## 7. Riscos Quest 3

| Risco | Mitigação |
|---|---|
| 4 casas procedurais + mesa = draw calls / overdraw | Um shell; looks por material/volume; desativar looks inativos |
| Morph com scale bounce (`EaseOutBack`) → comfort | VR review; overshoot mínimo; **nada de hopping de câmara**; preferir lerp de luz/materiais |
| Corte seco / mesa que “pisca” | Dissolve da mesa no mesmo correio do morph; proibir `SetActive(false)` no frame 0 dos hub props |
| Transparency sorting URP no fade da mesa | VFX lead: dither / shader fade testado em Quest; partículas leves se alpha falhar |
| URP post em 4 volumes | Skill `urp-postprocessing`; HDR off; Performance path; não `stereoTargetEye` |
| WebRequest + Realtime no main thread | throttle; não JSON gigante; timeout SessionGate |
| Foco do Editor congela Play | QA usa `set_autotick` + `wait_for` frameCount (`playmode-verification-loop.md`) |
| Passthrough AR vs dojo opaco | Manter AR como módulo separado (`Assets/GenJutsu/AR`); sessão ZCEA é CG opaca no v1 |
| Canal gaze com código curto | Código entropy do site; não logar poses; Cyber revisa |
| Drift yaw Unity vs Three.js | Um doc de eixos no PR da fase 4; fixture de yaw=0 |
| SDK Supabase Unity imaturo | Avaliar websocket Broadcast REST; não Presence “porque o sample usa” |

---

## 8. O que **não** fazer agora

- Implementar C#, shaders, prefabs, morph, SessionGate, GazePublisher neste PR.
- Commitar / mergear o **hero Mix B2** no site.
- Picker de casa no headset (`ProfileToken` de produto, menu ZCEA, teclas 1–4 em device).
- ESP32, MQTT, Matter, Home Assistant.
- Presence Realtime ou `UPDATE` Postgres da pose.
- Eye tracking como fonte de gaze (head forward apenas).
- Corvo no dojo ou moldura na casa da landing.
- Unity Authentication / login Google no Quest.
- GitHub Actions (CI/CD só se pedido).
- Tratar o UUID `f0ac53d5-…` como asset Unity.
- Copiar as quatro casas ProfileDemo como quatro destinos finais.
- **Corte seco** branco → dojo, ou **teleporte** da mesa/pergaminho (sumir num frame, mover para o void, `Destroy` imediato).
- Hub de produto **sem** mesa e pergaminho (vazio branco só com UI).
- Morph mais longo que ~3 s sem pedido do Diretor, ou flash de câmara.

---

## 9. Checklist do próximo PR de código (fase 1)

Para o Tech lead, depois deste plano aprovado:

1. Ler `docs/TIME_AGENTICO.md` (após merge dos docs da branch de referência).
2. Graphify se `graphify-out/` existir.
3. Não editar C# em Play; builders idempotentes.
4. QA: `unity command set_autotick` → `editor_play` → `wait_for` em `Time.frameCount`.
5. Cyber se tocar em URL/keys.
6. Não incluir hero nem ESP32.

---

## 10. Referências verificadas neste repo

- `Assets/Scenes/SampleScene.unity` — cena principal
- `Assets/GenJutsu/AR/`, `TableProps/` (`JapaneseTableBuilder`, `ScrollCanvas.cs`), `AI_Test/` — módulos atuais em `main`
- `Packages/manifest.json` — URP, XRI, OpenXR Meta, Pipeline
- `origin/cursor/desktop-xr-simulator-play:Assets/GenJutsu/ProfileDemo/` — demo ZCEA
- `origin/cursor/desktop-xr-simulator-play:docs/TIME_AGENTICO.md`
- `origin/cursor/desktop-xr-simulator-play:docs/ALINHAMENTO_GTI.md`
- `origin/cursor/desktop-xr-simulator-play:AGENTS.md`
- `.agents/skills/unity-cli/SKILL.md` e `references/playmode-verification-loop.md`
- Site público: `https://genjutsuai.vercel.app` (jornada visitante, `profile_id`, `/admin`)
- `GET https://genjutsuai.vercel.app/api/session` sem query → HTTP 404 (schema completo = gap no repo web)
