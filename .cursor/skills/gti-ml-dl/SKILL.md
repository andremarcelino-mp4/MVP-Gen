---
name: gti-ml-dl
description: >-
  Applies UniFECAF Machine Learning e Deep Learning (AGR.OL26/2): pipeline R
  (tidyverse, tidymodels, ggplot2), EDA, regressão, Lasso, logística, árvores,
  RF, XGBoost, não-supervisionado, Graph ML/Neo4j, DL/backprop, churn, lab GTI
  de bugs. Use for aulas, notebooks R/qmd, provas, scoring, clustering, grafos
  ou modelos além de regras hardcoded.
---

# GTI — Machine Learning e Deep Learning (AGR.OL26/2)

Turma: [ML e DL AGR.OL26/2](https://fecaf.brightspace.com/d2l/home/37768).  
Ementa: projetar, implementar e **avaliar**; coleta → modelo; regularização; overfitting; desbalanceamento.  
Currículo: [reference.md](reference.md). Não copiar enunciados.

## Instructions

1. Localizar a aula (1–9, prova, trabalho).
2. Abrir [reference.md](reference.md).
3. Separar EDA de modelagem; split **antes** de estatística do teste.
4. Nomear métrica (desbalanceamento → precision/recall/F1/ROC-AUC, não accuracy rainha).
5. Justificar o insight; código sem interpretação não conta na disciplina.
6. Se o pedido for GenJutsu, usar o apêndice — treino offline, não no Editor.

Livro-base das aulas R: [R for Data Science](https://pt.r4ds.hadley.nz/).  
Fontes públicas: [tidymodels](https://www.tidymodels.org/), [scikit-learn](https://scikit-learn.org/), [PyTorch](https://pytorch.org/docs/stable/index.html), [Keras](https://keras.io/).

## Percurso (índice)

| Bloco | Conteúdo | Detalhe |
|---|---|---|
| 1–2 | EDA tidyverse, Happiness, exercício Aula 2 | [reference.md](reference.md#aulas-1-e-2) |
| 3 | Crickets + Ames início | [reference.md](reference.md#aula-3) |
| 3–4 | Ames tidymodels, Lasso | [reference.md](reference.md#aulas-3-e-4-ames) |
| 5 | Classificação Brasileirão | [reference.md](reference.md#aula-5) |
| 6 | Árvores, RF, XGBoost | [reference.md](reference.md#aula-6) |
| 7 | Não supervisionado | [reference.md](reference.md#aula-7) |
| 8 | Grafos Neo4j/Cypher | [reference.md](reference.md#aula-8) |
| ATT_1 | Prova supervisionados / churn | [reference.md](reference.md#prova-att_1) |
| 9 | Deep Learning, backprop | [reference.md](reference.md#aula-9) |
| TF | Trabalho final | [reference.md](reference.md#trabalho-final) |
| Extra | Lab bugs GTI; A*/K-Means | [reference.md](reference.md#complementos) |

## Examples

**Regressão:** baseline 1 feature → modelo completo → Lasso (`mixture = 1`); CV; log10 do alvo se o preço for enviesado; interpretar coeficientes que o Lasso zerou.

**Classificação desbalanceada (~14% churn):** `vfold_cv` estratificado; cinco famílias (logística, Lasso, árvore, RF, XGBoost); `tidy()` dos coeficientes; custo FP/FN.

## Aplicação no GenJutsu

`ProfileActivity.Report` ainda é regra, não modelo. `session.json` = features (id, best, unit, tempo, capturas). Alvo contínuo vs perfil Z/C/E/A. v1: tabela/limiar/ONNX se pedido. Sem diagnóstico de personalidade.

## Não fazer

- Fit no dataset inteiro sem holdout.
- Accuracy em classe rara sem aviso.
- Leakage (estatística do teste no treino).
- CNN no Editor como release.
---
