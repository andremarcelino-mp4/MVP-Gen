# AI-Driven SE AGR.J26/2 — currículo

Fonte: TOC Brightspace org 37270 (`Material das Aulas` + módulo Aula 12). Lab irmão GTI de bugs.

## Aula 2

**Testes automatizados com IA.** Arrange–Act–Assert; IA sugere casos, humano define oráculo; testes como especificação executável.

## Aula 3

**ML para identificar bugs.** Features de comportamento (logs, traces), não payload de exploit. Predizer **antes** de alterar código; depois `fit`. Humano detecta primeiro.

## Aula 4

**AI Coding Assistants: produzir não é aceitar.** Diff pequeno; review; explicar o que a IA gerou; não mergear dump.

## Aula 5

**Refactoring with AI: mudar sem quebrar.** Comportamento preservado; suíte verde; refactor ≠ feature nova.

## Aula 6

**Automating the Development Workflow.** Scripts, hooks, eval isolado, graph/update de repo; automação não substitui critério.

## Aula 7

**AAI – Atividade Avaliativa Individual.** Entrega da disciplina. Não copiar. Aplicar o checklist das aulas 2–6.

## Aula 8

**AI-Assisted Software Modeling.** Diagramas e modelos gerados por IA são hipótese; validar no código (neste repo: Graphify `query`/`path`).

## Aula 9

**Design Patterns: recomendação não é decisão.** IA sugere padrão; o engenheiro decide pelo ciclo de vida real (Unity ≠ Spring). Recusar padrão cosmética.

## Aula 10

**Predictive Performance Engineering.** Medir (profiler, orçamento de frame, memória) **antes** de otimizar; predição ≠ micro-otimização no escuro.

## Aula 12

**AI-Augmented CI/CD.** Data AVA: 29/10/2026, Lab 6. CI gera checagens e resumos; humano aprova. Neste projeto: sem `unity build` de topo; `unity command build` + poll `build_status`; GitHub Actions só se pedido.

## Lab bugs GTI

Notebook: `Aula_03_Laboratorio_ML_Bugs_Vulnerabilidades_GTI`. Método, não malware.

| Ex. | Título | Lição |
|---|---|---|
| 1 | Você é o detector | Olho humano antes do `fit` |
| 2 | Comportamento → features | AAA (Aula 2) |
| 3 | Modelo de acesso suspeito | Prediga, depois `fit` |
| 4 | O modelo funciona? | Matriz de confusão; FP vs FN |
| 5 | Quem paga o erro? | Custo de negócio, não só acurácia |
| 6 | Commits a investigar | Linguagem profissional; dados que faltam |
| 7 | Nota 10 trapaceando | Leakage / teste contaminado |
| 8 | O que não se parece com o resto | Isolation Forest, `contamination`; anomalia ≠ ataque |
| 9 | A IA achou um ataque? | Classificar a conclusão; o modelo erra |
| 10 | Mini-capstone | Escopo, dados, risco de falso positivo — sem PoC ofensivo |

## Deploy como engenharia

Guia F1 da turma: FastAPI (`/health`, `/prever`, `/docs`); CORS; joblib na **mesma** versão sklearn; Docker; freeze Render/HF; não commitar CSV de 150 MB; secrets fora do git.
---
