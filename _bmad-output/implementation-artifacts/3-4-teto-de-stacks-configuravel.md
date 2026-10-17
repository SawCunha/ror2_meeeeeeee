---
baseline_commit: 1afd157
---

# Story 3.4: Teto de stacks configurável

Status: in-progress

## Story

Como autor do mod, quero um teto de stacks por item e por jogador, opcional, para que a multiplicação não agrave o desempenho de itens sem cálculo limitado (FR-7).

## Acceptance Criteria

1. `Multiplier.StackCap` padrão desligado (0).
2. Com N ligado e item sem `LimitedCalc`: total concedido reduzido para não passar de N, checado depois da multiplicação.
3. Item sob `LimitedCalc`: teto não aplicado.
4. Testes do Core: stack abaixo, no teto, acima.

## Tasks / Subtasks

- [x] Teto no `MultiplierPipeline.Resolve` (AC: #1, #2, #4)
- [x] `LimitedCalcFeature.Covers(ItemDef)` e `CapExempt` (AC: #3)
- [ ] Validação em jogo com `StackCap` ligado (AC: #2, #3)

## Dev Notes

- "Sob LimitedCalc" = entrada com `Mode != Off`.
- O teto conta o stack **permanente** antes do pickup (`GetItemCountPermanent`).

## Dev Agent Record

### File List

- `src/Ror2Meeeee.Core/Pipeline/MultiplierPipeline.cs`, `src/Ror2Meeeee/Features/LimitedCalc/LimitedCalcFeature.cs`

### Change Log

- 2026-10-11: teto de stacks.
