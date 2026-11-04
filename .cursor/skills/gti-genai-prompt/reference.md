# Gen AI AGR.MD26/2 — currículo

Fonte: TOC Brightspace org 37695 + labs da turma.

Premissa da disciplina: IA generativa **não “pensa”**. Matemática, probabilidade e padrões. Prompt fraco gera genérico; prompt estruturado (contexto, persona, restrições) condiciona a amostragem. Diferente de mecanismo de busca.

## 01 Aula inaugural

Tópico AVA: `01 - Aula Inaugural`. Conceitos básicos de Gen AI. Criatividade digital como amostragem, não mente.

## 02 Conceitos de GANs

Tópico: `02 - Conceitos de GANs`. Gerador vs discriminador; treino adversarial; modo collapse em GAN (ligado ao colapso de diversidade em prompt).

## 03 Máquinas criativas

Tópico: `03 - Máquinas Criativas`. Uso de modelos para arte/jogo/texto com **restrições** (briefing), não geração livre sem critério.

## 04 Deep Learning

Tópico: `04 - Deep Learning`. Latente, VAE, diffusion (ruído → imagem, analogia Midjourney) como arquiteturas do lab, não como treino obrigatório local.

## Prompt Engineering

Tópicos AVA: `Prompt Engineering para Desenvolvedores`; `Atividade`; `Entrega 05.10`; `Advanced Prompt Engineering`; `Entrega Projetos`.

Template que a disciplina ensina:

1. Persona
2. Contexto (gênero, público, restrições)
3. Estrutura de saída (campos obrigatórios)
4. Critérios (evitar clichê, viabilidade)
5. Limites (poder, espaço, tom)

Tabela fraco → melhor:

| Fraco | Melhor |
|---|---|
| Crie um personagem | Nome, idade, personalidade, habilidades, objetivo, conflito |
| Crie um cenário | Cidade com residencial, industrial e um mistério |
| Crie uma missão | Objetivo, 3 secundários, dificuldade, recompensa, consequência |
| Crie uma imagem | Câmera, hora, iluminação, estilo |
| Crie um jogo completo | Título, gênero, público, história, pró/antag, mecânicas, fases, progressão |

Falhas a nomear: pedido amplo; sem limite de poder / regras; cenário “detalhado” sem hierarquia espacial; baixa diversidade em 10 amostras (colapso de modo).

PE avançado: few-shot, cadeia de restrições, formato rígido, papéis (role/goal/backstory) — cruzar com NLP agêntica se o pedido for multiagente.

## Laboratórios

- Laboratório de arquiteturas gerativas: GAN, VAE, diffusion (conceito + notebook da turma)
- Investigação de falhas de prompt
- Diversidade vs colapso
- Hands-on RAG: retrieve → generate; citar fonte
- Deploy de modelo (vizinho F1/API da engenharia): custo por token; SLM vs API fechada (FinOps também em NLP 9–10)

## Manus

Tópico: `Acesso a Manus`. Ferramenta da **turma** para projetos. Não é runtime do produto Unity. Prompts e critérios ficam versionados no repo.

## AAI

Tópicos: `AAI - Avaliação Intermediária`; `AAI - Fechamento`. Cobram briefing e critérios da disciplina. Não copiar a entrega.
---
