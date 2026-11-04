---
name: gti-nlp
description: >-
  Applies UniFECAF NLP (AGR.PP26/2, Prof. Felipe Amorim): tokenização, BoW/TF-IDF,
  n-gramas, Word2Vec, BERTimbau, fine-tuning IMDb-PT, chatbots, RAG, SLM, Gradio,
  Cloud Run, CrewAI/LangGraph. Use for aulas, Colabs, provas da disciplina, copy
  PT-BR, captions, intent, embeddings, sentimento, RAG, ou resumo de sessão.
---

# GTI — Natural Language Processing (AGR.PP26/2)

Turma: [NLP AGR.PP26/2](https://fecaf.brightspace.com/d2l/home/37799).  
Não copiar enunciados/gabaritos do AVA. Currículo completo: [reference.md](reference.md).

Fontes públicas: [Hugging Face](https://huggingface.co/docs), [spaCy](https://spacy.io/usage), [neuralmind/bert-base-portuguese-cased](https://huggingface.co/neuralmind/bert-base-portuguese-cased).

## Instructions

1. Identificar o bloco da aula (1–10, Agentes, prova, trabalho).
2. Ler a seção correspondente em [reference.md](reference.md).
3. Aplicar o **método** da aula (família de representação, métrica, pipeline). Não colar o lab.
4. Em texto: evidência + interpretação; PT-BR; schema se for LLM.
5. Só então, se o pedido for do jogo, usar o apêndice GenJutsu.

Cinco famílias no mesmo corpus (`maritaca-ai/imdb_pt`): n-gramas; BoW/TF-IDF; Word2Vec estático; BERTimbau contextual; LLM zero/few-shot.

## Percurso (índice)

| Bloco | Conteúdo | Detalhe |
|---|---|---|
| 1 | Intro PLN, tokenização → transformers | [reference.md](reference.md#aula-1) |
| 2–3 | Representação, embeddings, vetores | [reference.md](reference.md#aulas-2-e-3) |
| 4 | Modelos de linguagem, labs 1–4 | [reference.md](reference.md#aula-4) |
| 5 | Ecossistema IA/NLP, fine-tune barato | [reference.md](reference.md#aula-5) |
| 6 | Fine-tuning IMDb-PT | [reference.md](reference.md#aula-6) |
| 7 | Chatbot, livro SLM, GPU Colab | [reference.md](reference.md#aula-7) |
| 8 | RAG | [reference.md](reference.md#aula-8) |
| Prova | AA1 Aulas 1–5 | [reference.md](reference.md#prova-aa1) |
| 9 | Sumarização, tradução, deploy 1 | [reference.md](reference.md#aula-9) |
| 10 | SLM Cloud Run, Gemini, Cloudflare | [reference.md](reference.md#aula-10) |
| Agentes | LangChain, CrewAI, LangGraph | [reference.md](reference.md#agentes-de-ia) |
| TF | Trabalho final | [reference.md](reference.md#trabalho-final) |

## Examples

**Classificar reviews PT:** escolher família, split, métrica (F1 se classes importam), interpretar erros — não só acurácia.

**Chat/resumo de produto:** recuperar trechos (Aula 8) ou SLM fora do notebook (Aulas 9–10); structured output; sem secret no cliente.

**Multiagente:** LangGraph orquestra, CrewAI nos nós, Pydantic no estado; máx. 2 revisões; papéis em português.

## Aplicação no GenJutsu

`ProfileCaption`, `ResultsBoard`, `session.json`. TF-IDF/intent = compreender; classificador offline = aprender; legendas PT = responder. Agentes só no time Cursor (`TIME_AGENTICO.md`). Sem BERT/LLM no Play; sem chave no APK.

## Não fazer

- Colar enunciados ou notebooks de entrega.
- Confundir embedding estático com contextual.
- Treinar BERT no Editor ou LLM in-game com secret.
---
