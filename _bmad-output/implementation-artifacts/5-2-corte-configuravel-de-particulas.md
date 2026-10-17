---
baseline_commit: 1afd157
---

# Story 5.2: Corte configurável de partículas

Status: in-progress

## Acceptance Criteria

(Fonte: `epics.md`.) `ParticlesFeature` com lista na config; hook registrado e logado; `orig` chamado quando não corta; efeito identificado por índice resolvido uma vez; lista vazia = jogo base, sem alocação por evento.

## Tasks / Subtasks

- [x] `Particles.Disable` (nomes de prefab, vírgula), resolvido para `bool[]` por `EffectIndex` uma vez
- [ ] Ação `Shorten`: **adiada** até a 5.1 mostrar ganho
- [ ] Validação em jogo: efeito listado some só para este jogador

## Dev Notes

- Só corta `transmit=false`: chamadas com `transmit=true` precisam de `orig` para enviar aos outros. Limitação: efeitos originados pelo próprio cliente (não-host) não são cortados nele.

## Dev Agent Record

### File List

- `src/Ror2Meeeee/Features/Particles/ParticlesFeature.cs`
