---
baseline_commit: 1afd157
---

# Story 1.6: Cenário de referência e linha de base

Status: in-progress

## Story

Como autor do mod,
quero um procedimento reproduzível do cenário de referência com as condições 1 e 2 medidas,
para que existam números reais para definir as metas de FPS e RAM (FR-1, NFR-1).

## Acceptance Criteria

(Fonte: `epics.md`, Story 1.6.)

1. Procedimento em `docs/` reproduzível (3 jogadores, seed fixo, `give_item` Fungus 50+ e Fireworks ~200); o mod não consome RNG de `Run.instance`.
2. Condições 1 e 2 medidas por ao menos uma fase de duração fixa; CSVs em `docs/` com data e versão do jogo; custo do próprio `Probe` registrado à parte.
3. Metas numéricas de FPS e RAM (NFR-1) registradas ou explicitamente adiadas até a condição 3, após revisão com o autor.

## Tasks / Subtasks

- [x] Task 1: documentar o procedimento em `docs/linha-de-base.md` (AC: #1)
- [ ] Task 2: medir condição 1 e condição 2 (+ custo do Probe) em jogo, com 3 jogadores (AC: #2)
- [ ] Task 3: revisar resultados com o autor e registrar metas ou adiá-las (AC: #3)

## Dev Notes

- Depende das Stories 1.4 e 1.5 validadas em jogo.
- As tasks 2 e 3 exigem jogar e a decisão do autor; não podem ser feitas por agente sem acesso ao jogo.

## Dev Agent Record

### Completion Notes

- Só o procedimento foi escrito. Nenhuma medição existe; a tabela de resultados está vazia de propósito.

### File List

- `docs/linha-de-base.md` (novo)

### Change Log

- 2026-10-11: procedimento documentado.
