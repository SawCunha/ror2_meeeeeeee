---
baseline_commit: 1afd157
---

# Story 3.1: Pipeline puro de multiplicação no Core

Status: review

## Story

Como autor do mod, quero a regra de multiplicação e elegibilidade como funções puras testadas, para que a lógica seja correta antes de tocar no jogo (FR-6, FR-7, FR-8).

## Acceptance Criteria

1. `Core/Pipeline.Resolve(PickupContext, MultiplierRules)` e `Core/Eligibility`; testes: multiplicador 1, 2, valor muito alto saturando em `int.MaxValue`; 0 ou negativo corrigido para o padrão.
2. Lunar e void só com `IncludeLunar`/`IncludeVoid`; temporário, scrap e equipamento nunca.
3. Jogador morto ou desconectado: nenhuma concessão.
4. Core sem referência ao jogo, BepInEx ou Unity.

## Tasks / Subtasks

- [x] `EligibilityTable` e `ItemKind` (AC: #2)
- [x] `MultiplierPipeline.Resolve`, `PickupContext`, `MultiplierRules` (AC: #1, #3)
- [x] 28 testes em `MultiplierPipelineTests` (AC: #1 a #3), 45/45 no total
- [x] Core sem novas referências (AC: #4)

## Dev Notes

- Desvio: `Resolve` devolve `int` (cópias **extras**), não uma "lista de concessões": hoje há um único destinatário. Vira lista na v2 (compartilhamento).
- `Multiplier` padrão = 100 (`MultiplierRules.DefaultMultiplier`), provisório e alto de propósito para validar desempenho; o épico dizia 2.
- Aritmética em `long`, com saturação e proteção para o stack final não passar de `int.MaxValue`.
- Os testes foram escritos junto da implementação (sem rodada vermelha separada).

## Dev Agent Record

### File List

- `src/Ror2Meeeee.Core/Eligibility/EligibilityTable.cs`, `src/Ror2Meeeee.Core/Pipeline/MultiplierPipeline.cs`, `tests/Ror2Meeeee.Core.Tests/MultiplierPipelineTests.cs` (novos)

### Change Log

- 2026-10-11: pipeline puro e testes.
