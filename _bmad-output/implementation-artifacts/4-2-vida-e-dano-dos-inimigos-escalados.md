---
baseline_commit: 1afd157
---

# Story 4.2: Vida e dano dos inimigos escalados

Status: in-progress

## Acceptance Criteria

(Fonte: `epics.md`.) Investigação em `docs/`; `DifficultyFeature` com hooks registrados e logados; `Factor > 1` multiplica vida e dano de inimigos (jogadores e aliados não); `Factor = 1` não altera; sem alocação por tick.

## Tasks / Subtasks

- [x] Investigação do `RoR2.dll`: `docs/relatorio-dificuldade.md`
- [x] `DifficultyFeature` com `CharacterMaster.onStartGlobal` dando `BoostHp`/`BoostDamage` uma vez por inimigo (times Monster, Lunar, Void)
- [ ] Validação em jogo: `Factor = 2` com inimigos de maior vida/dano; aliados intactos

## Dev Notes

- Vida e dano via itens Boost (mecanismo do próprio jogo; +10% por stack), não por hook em `RecalculateStats`: sem trabalho por tick.
- Arredonda para múltiplos de 10% (`round((m-1)*10)`): `Factor` com ganho pequeno pode virar 0 stacks.
- `HashSet<int>` por run evita aplicar duas vezes ao mesmo master; limpo em `ResetRunState`.
- Não funciona para inimigos que já existem quando a feature liga ou quando o fator muda (só novos spawns).

## Dev Agent Record

### File List

- `src/Ror2Meeeee/Features/Difficulty/DifficultyFeature.cs`, `docs/relatorio-dificuldade.md`
