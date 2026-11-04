# ML/DL AGR.OL26/2 — currículo

Fonte: TOC Brightspace org 37768. Nomes de arquivo, não enunciados.

Ementa (além das aulas R): TF / Keras / PyTorch; RNA; ativação; perda; backprop; CNN; RNN/LSTM; GPU.

Princípios transversais:

- Treino/teste **antes** de transformar com estatística do teste
- Outlier pode ser fato (país corrupto), não erro de digitação
- Lasso zera coeficientes — interpretar o que sumiu
- Overfitting: CV, regularização, leakage (“modelo nota 10 que trapaceava”)

## Aulas 1 e 2

Materiais: `Exercicio_pratico_Aula2_ML.docx`; [R4DS](https://pt.r4ds.hadley.nz/).

Dataset típico: World Happiness Report 2021.

Conteúdo: `tidyverse`; `str`; média vs mediana; histograma, boxplot, scatter, heatmap; Pearson × Spearman; NA (remoção vs imputação); outliers IQR; one-hot (`model.matrix`).

## Aula 3

Materiais: `Aula3_R_walkthrough_crickets.R`; Colabs (descrição do módulo “Código aula 3”); `Atividade_Lab_Aula2_NLP.pptx` aparece no mesmo módulo do AVA (não misturar com NLP).

Conteúdo: regressão simples temperatura × chirps (dado físico). Depois: Ames Housing.

## Aulas 3 e 4 (Ames)

Ferramenta: **tidymodels**. Dataset: Ames Housing.

Conteúdo: `initial_split`; receita; baseline 1 feature (`linear_reg` / `lm`); linear completo; polinômios + normalização; Lasso `glmnet` (`penalty`, `mixture = 1`); CV; linear × Lasso; log10 do alvo (`Sale_Price`).

## Aula 5

Materiais: `aula5-classificacao-brasileirao.qmd`; `brasileirao_2026.csv`; notas Aula 5; GitHub da turma.

Conteúdo: classificação binária (vitória); sigmoide; z = βx; Log-Loss (punir confiança errada); probabilidade ≠ rótulo cru.

## Aula 6

Materiais: slides; aquecimento; GitHub; notas; teste de conhecimentos; `Exercicio_pratico_Aula6_*`; notebook; dataset Ames.

Conteúdo: `decision_tree` / rpart; Random Forest; XGBoost; comparar famílias no mesmo split.

## Aula 7

Materiais: playground; quizzes; notebook; slides; GitHub.

Conteúdo: não supervisionado (agrupar sem rótulo); K-Means e variantes cobradas no notebook/quiz; visualização de grupos.

## Aula 8

Materiais: atividade, código, quiz.

Conteúdo: Graph ML; Neo4j Aura/Desktop; Cypher; NetworkX ou R; Degree, Betweenness, PageRank, Closeness; classificador (ex. RF) em features de grafo (ex. facção Karate Club).

## Prova ATT_1

Materiais: Prova AT1 + dataset; nas aulas também `mlc_churn` (~14% churn).

Cobra: logística 1 feat, logística+Lasso, árvore, RF, XGBoost; **não** accuracy como métrica rainha; `vfold_cv` estratificado; `tidy()` dos coeficientes. Não copiar a prova.

## Aula 9

Materiais: código, slides, exercício, quiz, playground, código parte 2, playground back-propagation.

Conteúdo: RNA; ativação; função de perda; backprop como algoritmo; CNN/RNN na ementa — conceito e playground, não treino pesado obrigatório no lab.

## Trabalho final

`Trabalho_Final_MLDL.docx`. Recorte: pipeline completo (EDA → split → modelo → métrica → interpretação). Não colar o docx.

## Complementos

- Lab GTI *Bugs e Vulnerabilidades com ML*: features de comportamento, matriz de confusão, custo do erro, leakage, Isolation Forest (`contamination`), humano no loop. Detalhe dos 10 exercícios na skill `gti-ai-driven-se` / `reference.md`.
- Downloads IA Fundamentals: grafo de entregas, A*/BFS/DFS, K-Means de zonas — não misturar com a prova de churn.
---
