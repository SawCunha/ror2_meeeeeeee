---
baseline_commit: 1afd157
---

# Story 5.1: Levantamento e medição das partículas

Status: in-progress

## Acceptance Criteria

(Fonte: `epics.md`.) Ponto de instanciação dos efeitos identificado; contador por efeito; lista inicial ordenada por custo, com medição.

## Tasks / Subtasks

- [x] Ponto de hook: `EffectManager.SpawnEffect(EffectIndex, EffectData, bool)`; efeitos de rede/host entram com `transmit=false`; sem toque em dano/colisão
- [x] Contador por efeito: `Particles.CountSpawns` + `ror2meeeee_effects [top]` (frequência de spawn por prefab)
- [ ] Medir o cenário de referência, ordenar por custo e registrar em `docs/`; decidir seguir/adiar o Epic 5

## Dev Notes

- Medida é **frequência de spawn**, não sistemas de partículas ativos nem frame time por efeito (isso exige o jogo rodando).
- Roteiro: `docs/teste-epic5.md`.

## Dev Agent Record

### File List

- `src/Ror2Meeeee/Features/Particles/ParticlesFeature.cs`, `Debug/Commands.cs`, `Plugin.cs`
