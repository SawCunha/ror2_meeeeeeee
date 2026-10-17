---
baseline_commit: 1afd157
---

# Story 2.3: Validação de desempenho e de efeito reconhecível

Status: ready-for-dev

## Story

Como autor do mod, quero comparar o cenário de referência com o cálculo limitado ligado e desligado, para provar o ganho e confirmar que os itens seguem úteis (NFR-1).

## Acceptance Criteria

(Fonte: `epics.md`, Story 2.3: medir `LimitedCalc` ligado e desligado com `Probe`, comparar com a linha de base; registrar critério de "efeito reconhecível" e o valor medido em `docs/`; fase completa com 3 jogadores sem dessincronização nem exceções em `Fail()`.)

## Tasks / Subtasks

- [ ] Task 1: medir com `LimitedCalc` desligado e ligado (3 jogadores, `docs/linha-de-base.md`)
- [ ] Task 2: definir e medir "efeito reconhecível" (cura/dano total por segundo vs. jogo base)
- [ ] Task 3: fase completa com 3 jogadores; `ror2meeeee_status` sem falhas

## Dev Notes

- Instrumentação pronta: `Probe` grava `damage_total` e `heal_total`; roteiro e tabela de resultados em `docs/teste-epic2.md`.
- **Bloqueada** por 1.6 (linha de base) e por jogar com 3 jogadores; exige o autor.

## Dev Agent Record

(vazio)
