---
name: gti-genai-prompt
description: >-
  Applies UniFECAF Gen AI and Prompt Engineering (AGR.MD26/2): princípios
  (IA não pensa), GANs, máquinas criativas, DL gerativo, persona+contexto+formato,
  falhas de prompt, colapso de modo, VAE/diffusion, RAG, PE avançado, Manus, AAI.
  Use for aulas da disciplina, prompts, RAG, copy gerativa, imagem/áudio, ou papéis
  de agente.
---

# GTI — Gen AI and Prompt Engineering (AGR.MD26/2)

Turma: [Gen AI AGR.MD26/2](https://fecaf.brightspace.com/d2l/home/37695).  
Currículo: [reference.md](reference.md). Não copiar AAI nem entregas Manus.

Fontes públicas: [OpenAI prompting](https://platform.openai.com/docs/guides/prompt-engineering), [Hugging Face diffusers](https://huggingface.co/docs/diffusers).

## Instructions

1. Classificar o pedido: princípios (01–04), prompt engineering, lab gerativo, RAG, ou AAI.
2. Ler a seção em [reference.md](reference.md).
3. Prompt longo = persona + contexto + formato + critérios + limites (não é busca Google).
4. Saída vaga → investigar **falha do prompt** antes de culpar o modelo.
5. RAG: recuperar, citar chunk, não inventar.
6. Temperatura baixa em extração; alta só em brainstorm explícito.

## Percurso (índice)

| Bloco | Conteúdo | Detalhe |
|---|---|---|
| 01 | Aula inaugural — probabilidade e padrões | [reference.md](reference.md#01-aula-inaugural) |
| 02 | Conceitos de GANs | [reference.md](reference.md#02-conceitos-de-gans) |
| 03 | Máquinas criativas | [reference.md](reference.md#03-maquinas-criativas) |
| 04 | Deep Learning gerativo | [reference.md](reference.md#04-deep-learning) |
| PE | PE para desenvolvedores + Advanced | [reference.md](reference.md#prompt-engineering) |
| Labs | GAN/VAE/diffusion, diversidade, RAG | [reference.md](reference.md#laboratorios) |
| Manus | Acesso e entrega de projetos da turma | [reference.md](reference.md#manus) |
| AAI | Avaliação intermediária / fechamento | [reference.md](reference.md#aai) |

## Examples

**Fraco:** “Crie um personagem.”  
**Da aula:** nome, idade, personalidade, habilidades, objetivo, conflito.

**Imagem:** câmera, hora, iluminação, estilo (ex. neon, terceira pessoa) — não “faça algo incrível”.

**Mesmo prompt 10×:** se os outputs colapsam, nomear colapso de modo; variar restrição ou temperatura só se a aula pedir diversidade.

## Aplicação no GenJutsu

Prompts de casa = interior + VR + URP/primitivas. Caption = few-shot do perfil. RAG futuro = `session.json` + repo (Graphify). Sem GAN no Editor; sem Manus no APK; sem chave no cliente.

## Não fazer

- Confiar em LLM para fatos do repositório.
- Copiar modelo de entrega da turma como README do jogo.
- Treinar GAN/diffusion no Unity.
---
