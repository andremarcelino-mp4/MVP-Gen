---
name: gti-casa-que-aprende
description: >-
  Product north star for GenJutsu AI Smart Home Cognitiva and GTI “Inteligência:
  A Casa que Aprende”. Use when shaping houses, profiles, IoT, digital twins,
  automation, or any feature that should understand, learn, or respond to user context.
---

# GTI — A Casa que Aprende / Smart Home Cognitiva

PDF integrador (não há TOC de 10 aulas). Disciplinas com currículo completo: `gti-nlp`, `gti-ml-dl`, `gti-genai-prompt`, `gti-ai-driven-se` (`SKILL.md` + `reference.md`).

Conceito do produto (verbatim):

GenJutsu AI é uma experiência de Smart Home Cognitiva voltada à criação de ambientes inteligentes capazes de compreender, aprender e responder ao contexto do usuário. Com uma arquitetura modular e evolutiva, o projeto pode integrar tecnologias como Inteligência Artificial, IoT, Machine Learning, processamento de linguagem natural, automação e gêmeos digitais, permitindo que componentes, modelos e formas de interação sejam adaptados conforme o desenvolvimento da solução. A proposta explora a conexão entre ambientes físicos e digitais, coleta e processamento de dados, tomada de decisão inteligente e novas formas de interação entre pessoas e espaços conectados.

## Instructions

1. Classificar o pedido: compreender, aprender, responder, físico↔digital, ou infra/QA explícito.
2. Recusar feature que não caiba nesses eixos, salvo infra/QA pedido.
3. Quest / mesa branca = gêmeo digital e interação, não o produto inteiro.
4. Abrir a skill GTI da disciplina se o tema for NLP, ML, GenAI ou engenharia.

## Percurso (PDF)

| Módulo | v1 (Quest / mesa) | Evolutivo |
|---|---|---|
| Interação | XRI grab/poke, 4 casas | voz, IoT, sensores reais |
| Compreender | scores + fotos da sessão | NLP, ML de comportamento |
| Aprender | JSON em LocalLow | modelos offline, perfis persistentes |
| Responder | luz, volume, música, geometria | Matter / Home Assistant |
| Gêmeo | casas procedurais | espelho de espaço físico |

Hub branco = neutro; Z/C/E/A = contexto. `DemoSessionRecorder` = primeiro gancho de dados.

Fontes (não stack obrigatória): [Matter](https://csa-iot.org/all-solutions/matter/), [Home Assistant](https://www.home-assistant.io/docs/), Meta XR, WELL/IEQ.

## Examples

**Entra:** persistir sessão e resumir jornada (compreender + aprender).  
**Não entra no APK v1:** broker MQTT sem pedido; apagar props da mesa (só ocultar).

## Não fazer

- Exigir IoT real no APK atual.
- Apagar props da mesa.
- Parafrasear o conceito verbatim acima.
---
