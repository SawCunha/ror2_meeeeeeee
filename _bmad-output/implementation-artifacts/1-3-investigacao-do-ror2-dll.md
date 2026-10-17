---
baseline_commit: 1afd157
---

# Story 1.3: Investigação do `RoR2.dll`

Status: review

## Story

Como autor do mod,
quero um relatório do que o `RoR2.dll` realmente faz nos itens e no pickup,
para que as próximas stories se apoiem em fatos, não nas hipóteses do addendum.

## Acceptance Criteria

(Fonte: `_bmad-output/planning-artifacts/epics.md`, Story 1.3.)

1. `RoR2.dll` descompilado com `ilspycmd` (versão registrada); para Bustling Fungus e Bundle of Fireworks: tipo e método do efeito, se roda só no servidor (A8), o que escala por stack, frequência; hipótese do addendum confirmada ou refutada.
2. Ponto de interceptação recomendado para a multiplicação, com o que cada alternativa captura, e nomes reais de tipos/métodos para os hooks `On.*`.
3. Todas as origens de concessão listadas, com o método do jogo que as executa (regra da Story 3.3).
4. Decisão A8 registrada ("host-only suficiente": sim/não); se não, a Story 2.4 passa a obrigatória.
5. Sem código de produção alterado nesta story.

## Tasks / Subtasks

- [x] Descompilar `RoR2.dll` com `ilspycmd` 9.1.0.7988 (AC: #1)
- [x] Analisar Fungus e Fireworks (AC: #1, #4)
- [x] Comparar `GiveItemPermanent` com o ponto de pickup e confirmar nomes em `MMHOOK_RoR2.dll` (AC: #2)
- [x] Levantar origens de concessão (AC: #3)
- [x] Escrever `docs/relatorio-ror2-dll.md` (AC: #1 a #4)

## Dev Notes

- Hooks e regras: `_bmad-output/project-context.md`.
- Relatório: `docs/relatorio-ror2-dll.md`.

## Dev Agent Record

### Completion Notes

- Resultado central: ambos os itens são server-only (A8 = sim, Story 2.4 não obrigatória). A hipótese do addendum foi **parcialmente refutada**: o custo do Fungus não cresce por stack (1 ward por corpo; só o raio cresce), e o Fireworks é um pico por interação (`4 + 4·stack` mísseis), não custo por tick.
- Ponto de interceptação recomendado: `ItemDef.AttemptGrant(ref PickupDef.GrantContext)`.
- Limite: leitura estática do código; nada medido em jogo. Lista de origens marcada "verificar" confirmada só na Story 3.3.

### File List

- `docs/relatorio-ror2-dll.md` (novo)

### Change Log

- 2026-10-11: relatório criado.
