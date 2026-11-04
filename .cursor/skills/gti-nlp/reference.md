# NLP AGR.PP26/2 — currículo

Fonte: TOC Brightspace org 37799 + labs da turma. Nomes de arquivo, não enunciados.

Corpus das aulas 1–4 / 6: Hugging Face `maritaca-ai/imdb_pt` (POSITIVO/NEGATIVO, ~230 palavras). Encoder: `neuralmind/bert-base-portuguese-cased`.

## Aula 1

Materiais: `Intro PLN.pptx`; podcast tokenização → transformers.

Conteúdo: o que é PLN; token vs palavra vs subword; por que contar antes de “IA”; pipeline clássico (corpus → tokens → features → modelo); salto até transformers como **contexto**, não como único método.

Fazer: olhar classes, comprimento, exemplos reais **antes** de vetorizar.

## Aulas 2 e 3

Materiais: Representação e Embeddings; podcast de vetores; labs BoW/TF-IDF.

Conteúdo:

- BoW, TF-IDF, stopwords PT, assinatura de classe
- N-gramas (contagem e probabilidade)
- Word2Vec Skip-Gram típico das aulas: `sg=1`, `vector_size=100`, `window=5`
- Embedding **estático**: o mesmo vetor em qualquer frase; “ótimo” e “péssimo” podem ficar próximos pelo contexto compartilhado — não é bug

## Aula 4

Materiais: `Modelos de Linguagem.pptx`; labs 1–4; `Roteiro_Aulas12` / Aula 3; podcast “lê nas entrelinhas”.

Conteúdo: BERTimbau = vetor **contextual**; LLM zero-shot vs few-shot (exemplos PT-BR no prompt); prompt sem treino vs encoder treinado. Competir famílias no **mesmo** corpus.

## Aula 5

Materiais: `Ecossistema IA NLP GTI FINAL.pptx`; podcast fine-tuning (~dezenas de USD, não cluster); `notafinetuning_notebook_corrigido.ipynb`.

Conteúdo: mapa API fechada vs encoder vs SLM; custo e GPU Colab; quando fine-tune vale vs few-shot/RAG. Escolher pelo alvo e pelo orçamento.

## Aula 6

Materiais: `Aula5_FineTuning_IMDbPT.docx` (numeração AVA vs arquivo).

Conteúdo: fine-tune de sentimento no IMDb-PT; labels; overfitting em review curta; só fine-tune com corpus rotulado.

## Aula 7

Materiais: Chatbot Colab; livro *How to Build and Fine-Tune a Small Language Model* (Liu) — caps. GPT from scratch, dataset, treino, eval, SFT, DPO, deploy; `notas_aula6.pdf`; PDF GPU Colab.

Conteúdo: chatbot como produto; SLM vs API; SFT ≠ DPO; GPU no Colab não é produção.

## Aula 8

Materiais: notebook RAG, `hands_on_rag`, quiz.

Conteúdo: chunk → embed → retrieve → generate; citar o trecho; não inventar fora do chunk; quiz cobra alucinação vs recuperação.

## Prova AA1

Materiais: Colab prova aulas 1–5.

Cobra: as cinco famílias no mesmo conjunto; métrica + interpretação; ruído de texto (espaçamento, gíria). Não copiar a prova.

## Aula 9

Materiais: Colabs sumarização/tradução; deploy parte 1.

Cenários (método, não copiar o lab):

- Tradução en→pt: `Helsinki-NLP/opus_books`, opus-mt
- Sumarização PT: `recogna-nlp/recognasumm`, PTT5
- Sentimento PT: IMDb-PT, FinBERT-PT-BR / BERTimbau

Ruídos a nomear: tradução literal, espaçamento, gíria, expressões multi-palavra.

NLP vira produto só se sair do notebook (Gradio no mínimo).

## Aula 10

Materiais: lab SLM Cloud Run; Gemini para alunos; demo trycloudflare.

Conteúdo: Dockerfile, Cloud Run, FinOps (API vs SLM próprio); túnel Cloudflare só demo; chaves em secret de nuvem, nunca no cliente.

## Agentes de IA

Materiais: slides NLP agêntica; docx multiagentes; notebooks LangChain / CrewAI.

Conteúdo: LangGraph orquestra estado e ramos (`Literal`); CrewAI nos nós (role, goal, backstory PT); LangChain `with_structured_output` (Pydantic); roteador → especialista → auditor; teto de revisões (ex. 2). Chatbot de uma chamada ≠ sistema multiagente.

## Trabalho final

`Trabalho_Final_NLP.docx` no AVA. Recorte típico: classificação ou RAG ou deploy de SLM com relatório de métrica e limite. Não colar o docx.
---
