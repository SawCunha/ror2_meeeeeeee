---
baseline_commit: 1afd157
---

# Story 4.1: Fator de dificuldade e curva no Core

Status: review

## Acceptance Criteria

(Fonte: `epics.md`.) Curva pura testada (fator 1.0 = identidade, maior, inválido, extremos); seção `Difficulty` em config, valores em cache, inválido com `Warning`.

## Tasks / Subtasks

- [x] `DifficultyCurve.Evaluate` e `DifficultyGains` no Core, linear e isolada
- [x] 16 testes (`DifficultyCurveTests`): identidade, linear por ganho, fator inválido (0, negativo, NaN, ±Inf), extremos clampados em [0,1; 1000]
- [x] Config `Difficulty.Factor` e ganhos por eixo, relidos em `SettingChanged`; inválido volta a 1.0 com `Warning`

## Dev Notes

- Desvio: a seção também carrega `Horde.*` (item).
- Multiplicador por eixo = `1 + (Factor-1)·ganho`, clamp [0,1; 1000].

## Dev Agent Record

### File List

- `src/Ror2Meeeee.Core/Difficulty/DifficultyCurve.cs`, `tests/Ror2Meeeee.Core.Tests/DifficultyCurveTests.cs`

### Change Log

- 2026-10-11: curva de dificuldade e curva do item.
