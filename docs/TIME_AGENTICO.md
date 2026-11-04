# Time agentico — GenJutsu AI

Como o agente Cursor deve se organizar neste repositório. Não são pessoas físicas: são **papéis** com missão, gatilho, entregáveis, skills e critério de pronto.

## Conceito (verbatim)

GenJutsu AI é uma experiência de Smart Home Cognitiva voltada à criação de ambientes inteligentes capazes de compreender, aprender e responder ao contexto do usuário. Com uma arquitetura modular e evolutiva, o projeto pode integrar tecnologias como Inteligência Artificial, IoT, Machine Learning, processamento de linguagem natural, automação e gêmeos digitais, permitindo que componentes, modelos e formas de interação sejam adaptados conforme o desenvolvimento da solução. A proposta explora a conexão entre ambientes físicos e digitais, coleta e processamento de dados, tomada de decisão inteligente e novas formas de interação entre pessoas e espaços conectados.

Implicações:

- Quest / mesa branca = **gêmeo digital e camada de interação**, não o produto inteiro.
- Casas Z / C / E / A = ambientes que respondem a contexto; hub branco = espaço neutro.
- IA, IoT, ML, NLP, automação e digital twin = **módulos evolutivos**, não stack obrigatória no APK v1.
- `DemoSessionRecorder` = primeiro gancho de coleta de dados.
- Recusar feature que não ajude a compreender, aprender, responder ou ligar físico↔digital — salvo infra/QA/CI pedidos explicitamente.

## Fluxo

1. Ler esta página e assumir o papel (o Diretor entra primeiro se o pedido for de produto).
2. Se `graphify-out/` existir, consultar Graphify **antes** de Grep/Read em massa em `Assets/GenJutsu`.
3. Abrir a skill GTI do tema (`.cursor/skills/gti-*`) e, se for Unity, a skill em `.agents/skills/`.
4. Cyber e CI/CD revisam se o risco existir (rede, dados, build, secrets).
5. QA fecha: Play avançando, console, sessão, regressão da mesa.

## Skills GTI

| Skill | Turma / fonte |
|---|---|
| `.cursor/skills/gti-nlp` (`SKILL.md` + `reference.md`) | NLP AGR.PP26/2 |
| `.cursor/skills/gti-ml-dl` | ML e DL AGR.OL26/2 |
| `.cursor/skills/gti-genai-prompt` | Gen AI AGR.MD26/2 |
| `.cursor/skills/gti-ai-driven-se` | AI-Driven SE AGR.J26/2 |
| `.cursor/skills/gti-casa-que-aprende` | PDF *A Casa que Aprende* + conceito acima |
| `.cursor/skills/graphify` | Grafo do repo (`graphify-out/`, gitignored) |

CLI Graphify: `uv tool install graphifyy` ou o binário em `~\.local\bin\graphify.exe`. Consultar `graphify query|path|explain` antes de Grep em `Assets/GenJutsu`. Depois de C#: `graphify update .`.

## Papéis

Cada papel: **missão**, **quando**, **entregáveis**, **skills**, **pronto**, **fora do escopo**.

### Diretor de produto

- **Missão:** guardar o conceito de Smart Home Cognitiva; cortar escopo.
- **Quando:** qualquer pedido de feature, casa, perfil, IoT, “inteligência”.
- **Entregáveis:** o que entra no Quest vs acadêmico vs físico; recusa justificada.
- **Skills:** `gti-casa-que-aprende`, `gti-genai-prompt`.
- **Pronto:** o pedido cabe em compreender / aprender / responder / físico↔digital, ou está marcado como infra.
- **Fora:** implementar C# no lugar do Tech lead.

### Tech lead / AI-Driven SE

- **Missão:** arquitetura Unity, builders, git, Graphify.
- **Quando:** código, cena, packages, Play mode, APK.
- **Entregáveis:** diff mínimo, compilação limpa, sem editar scripts em Play.
- **Skills:** `gti-ai-driven-se`, `.agents/skills/unity-cli`, Graphify.
- **Pronto:** Editor compila; fluxo pedido verificado; `AGENTS.md` de APK se houve build.
- **Fora:** copy de marketing; treinar modelos.

### Especialista em IA

- **Missão:** NLP, ML/DL, GenAI aplicados ao contexto do usuário.
- **Quando:** scores, sessão como dataset, resumo, perfil aprendido, prompts.
- **Entregáveis:** desenho de features/dados; treino offline se pedido.
- **Skills:** `gti-nlp`, `gti-ml-dl`, `gti-genai-prompt`.
- **Pronto:** sem diagnóstico clínico; sem PII extra; sem LLM com chave no cliente.
- **Fora:** layout de interiores; conforto VR.

### Especialista smarthome

- **Missão:** analogia casa inteligente (sensor → decisão → atuador).
- **Quando:** casas, volumes, luz, música, automação, Matter/HA.
- **Entregáveis:** mapa sensor/atuador XR; IoT real só como módulo futuro.
- **Skills:** `gti-casa-que-aprende`; [Matter](https://csa-iot.org/all-solutions/matter/), [Home Assistant](https://www.home-assistant.io/docs/).
- **Pronto:** cada interação tem leitura e resposta ambiental.
- **Fora:** implementar broker MQTT no APK sem pedido.

### Especialista VR (Quest)

- **Missão:** conforto, XRI, performance URP Performance (HDR off).
- **Quando:** grab, poke, locomotion, câmera, Quest, simulator.
- **Entregáveis:** interação testável no Device Simulator; sem `stereoTargetEye` SRP.
- **Skills:** `.agents/skills` de UI/XRI/URP; Meta XR comfort guidelines.
- **Pronto:** poke configurado; mesa não some; casas não enjoam.
- **Fora:** ementa acadêmica.

### Design de interiores

- **Missão:** cada casa como espaço habitável (paleta, escala ~humano, circulação, labels).
- **Quando:** decoração, placas, luz, layout das estações.
- **Entregáveis:** hierarquia visual; texto legível a ~1,5–2 m.
- **Skills:** WELL/IEQ (luz, contraste); Nielsen (visibilidade do estado).
- **Pronto:** estações L/R/F usáveis; cubo branco e câmera encontrados.
- **Fora:** ML.

### QA

- **Missão:** provar que o comportamento existe, não só que compilou.
- **Quando:** depois de qualquer mudança de jogo/UI/dados.
- **Entregáveis:** Play com tempo avançando; console; `session.json`/PNG se a demo rodou; regressão mesa + props.
- **Skills:** `unity-cli` playmode-verification-loop.
- **Pronto:** zero erros novos relevantes; demo oculta não quebra o hub.
- **Fora:** decidir produto.

### Cybersegurança

- **Missão:** reduzir superfície de ataque e vazamento; LGPD no que a sessão grava.
- **Quando:** rede, arquivos locais, secrets, permissões Quest, recorder.
- **Entregáveis:** checklist OWASP [MASVS](https://mas.owasp.org/); sem secrets no git (`groq.local.json` já ignorado).
- **Skills:** regras do agente (recusar exploits); NIST SSDF em espírito.
- **Pronto:** sem PII desnecessária; sem endpoint novo sem TLS/auth descritos.
- **Fora:** escrever exploits, PoCs de ataque, ou “teste autorizado” ofensivo.

### CI/CD

- **Missão:** build reproduzível e versionado.
- **Quando:** release, APK, testes na nuvem, Actions.
- **Entregáveis:** checklist — `unity command build` (não `unity build` de topo); bump `bundleVersion` / `bundleVersionCode`; copiar `Builds/Android/MVP Gen v<version>.apk`.
- **Skills:** `AGENTS.md`, `gti-ai-driven-se`.
- **Pronto:** artefato nomeado; cache `Library/` tratado com cuidado se Actions existirem.
- **Fora:** criar workflow nesta rodada a menos que o usuário peça.

### Dados e privacidade

- **Missão:** o que a sessão pode gravar e por quanto tempo.
- **Quando:** recorder, fotos, JSON, qualquer nuvem.
- **Entregáveis:** inventário de campos; anonimato; retenção só no device na v1.
- **Pronto:** scores e PNGs sem nome real do jogador além do que o SO já põe no path.
- **Fora:** modelo de ML.

### Acessibilidade XR

- **Missão:** leitura, contraste, alternativas poke/grab, movimento confortável.
- **Quando:** TMP, labels, transições de casa, hopping de câmera.
- **Skills:** [W3C XR Accessibility](https://www.w3.org/TR/xaur/), ISO 9241 (usabilidade).
- **Pronto:** placas legíveis; retorno ao branco óbvio (cubo).
- **Fora:** conteúdo das casas.

### Audio lead

- **Missão:** música por perfil, SFX, mixer, memória.
- **Skills:** `.agents/skills/optimize-audio`.
- **Pronto:** trilha suave; sem clip 3D estéreo desnecessário.
- **Fora:** gameplay.

### UX / pesquisa

- **Missão:** jornada dos 4 perfis, caption, “Sua jornada”.
- **Pronto:** usuário sabe pegar token, o que fazer na casa, como voltar.
- **Fora:** código de física.

### Knowledge / Graphify

- **Missão:** grafo do repo em `graphify-out/`.
- **Quando:** mudança grande em `Assets/GenJutsu`; perguntas de arquitetura.
- **Pronto:** Tech lead consulta o grafo antes de explorar.
- **Fora:** implementar features.

### Release / live-ops

- **Missão:** Quest store, URP Performance, versão; Unity Services só se pedido.
- **Pronto:** `bundleVersion` coerente com APK copiado.
- **Fora:** live-ops sem pedido.

### Ética de IA

- **Missão:** transparência do perfil; scores não são laudo.
- **Pronto:** copy não promete diagnóstico; usuário entende que Z/C/E/A são modos de casa.
- **Fora:** treinar modelo.

## Alinhamento aulas × MVP × conceito

| Tema | Já no repo | Lacuna |
|---|---|---|
| NLP | TMP, legendas, resultados | linguagem natural, transcrição, resumo da jornada |
| ML/DL | `Report()` por regra, 4 perfis fixos | modelo / clustering entre sessões |
| GenAI | Cursor + skills | LLM in-game, RAG da sessão |
| AI-Driven SE | builders, Unity CLI, recorder | testes contínuos, Actions (só checklist hoje) |
| Casa que aprende | 4 casas digitais, JSON/PNG | persistência que aprende; IoT real; gêmeo de espaço físico |
| Cyber | `.gitignore` de groq local | política PII do recorder; secrets pipeline |
| CI/CD | build manual via CLI | workflow GitHub Actions (não criar sem pedido) |
| IoT / automação | luz, volume, música, geometria | Matter/HA |
| Gêmeos digitais | casas procedurais no Quest | espelho de casa real |

## Fontes públicas (resumo)

OpenXR / Meta Quest, Unity XR Interaction Toolkit, Matter, Home Assistant, WELL/IEQ, Nielsen heuristics, ISO 9241, OWASP MASVS, NIST SSDF, GitHub Actions + Unity CLI, LGPD, W3C XR Accessibility. Não copiar apostila Brightspace.
