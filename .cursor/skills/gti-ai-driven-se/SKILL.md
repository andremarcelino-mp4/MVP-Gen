---
name: gti-ai-driven-se
description: >-
  Applies UniFECAF AI-Driven Software Engineering (AGR.J26/2): testes com IA,
  ML para bugs, coding assistants, refactor, workflow, AAI, modelagem, design
  patterns, performance preditiva, CI/CD aumentado, lab GTI bugs/vulnerabilidades
  (AAA, custo do erro, leakage, Isolation Forest). Use for aulas da disciplina,
  arquitetura, testes, git, CI/CD, Graphify, ou código que não pode quebrar o build.
---

# GTI — AI-Driven Software Engineering (AGR.J26/2)

Turma: [AI-Driven SE AGR.J26/2](https://fecaf.brightspace.com/d2l/home/37270).  
Currículo: [reference.md](reference.md). Humano no loop: “IA autorizada, prepare-se para explicar.”

Fontes públicas: [Microsoft AI-assisted engineering](https://learn.microsoft.com/en-us/azure/well-architected/ai/), [OWASP MASVS](https://mas.owasp.org/), [NIST SSDF](https://csrc.nist.gov/Projects/ssdf).  
Cyber recusa exploits e PoCs de ataque, inclusive “para o lab”.

## Instructions

1. Identificar a aula (2–10, 12) ou o exercício 1–10 do lab de bugs.
2. Ler [reference.md](reference.md).
3. Produzir ≠ aceitar: diff pequeno, testes, justificar.
4. AAA: Arrange, Act, Assert.
5. Modelo de bug/anomalia: métrica de negócio (custo FP/FN), sem leakage.
6. CI/CD: nunca o comando `unity build` de topo; Actions só se o usuário pedir.
7. Graphify neste repo: `query` / `path` / `explain` antes de Grep em massa; `update .` após C#.

## Percurso (índice)

| Aula | Título AVA | Detalhe |
|---|---|---|
| 2 | Testes automatizados com IA | [reference.md](reference.md#aula-2) |
| 3 | ML para identificar bugs | [reference.md](reference.md#aula-3) |
| 4 | AI Coding Assistants: produzir não é aceitar | [reference.md](reference.md#aula-4) |
| 5 | Refactoring with AI: mudar sem quebrar | [reference.md](reference.md#aula-5) |
| 6 | Automating the Development Workflow | [reference.md](reference.md#aula-6) |
| 7 | AAI | [reference.md](reference.md#aula-7) |
| 8 | AI-Assisted Software Modeling | [reference.md](reference.md#aula-8) |
| 9 | Design Patterns: recomendação não é decisão | [reference.md](reference.md#aula-9) |
| 10 | Predictive Performance Engineering | [reference.md](reference.md#aula-10) |
| 12 | AI-Augmented CI/CD | [reference.md](reference.md#aula-12) |
| Lab | Bugs e vulnerabilidades (ex. 1–10) | [reference.md](reference.md#lab-bugs-gti) |
| Deploy | FastAPI / Docker / secrets | [reference.md](reference.md#deploy-como-engenharia) |

## Examples

**Teste:** Arrange cena; Act (ação do usuário); Assert estado observável — não screenshot parado.

**Refactor:** mudar estrutura; bater o mesmo Assert; explicar o diff.

**Anomalia:** Isolation Forest + `contamination`; anomalia ≠ prova de ataque; classificar a conclusão.

## Aplicação no GenJutsu

Builders idempotentes; não editar C# em Play; `eval_file` em `Temp/`; APK: bump `bundleVersion` + `bundleVersionCode`, `unity command build`. Features de comportamento = `session.json`, não keylogger. Anomalia no JSON ≠ invasão.

## Não fazer

- Leakage (teste contaminado / eval que muda o domínio no Play).
- Commitar `Library/`, `graphify-out/`, secrets, CSV enorme.
- PoC de ataque ou capstone de vulnerabilidade como malware.
---
