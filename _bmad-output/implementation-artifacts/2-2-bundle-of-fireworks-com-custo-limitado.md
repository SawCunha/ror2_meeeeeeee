---
baseline_commit: 1afd157
---

# Story 2.2: Bundle of Fireworks com custo limitado

Status: in-progress

## Story

Como jogador do grupo, quero que o Bundle of Fireworks não gere custo ilimitado com ~200 stacks, para que ativar interagíveis não derrube o FPS (FR-3).

## Acceptance Criteria

(Fonte: `epics.md`, Story 2.2.)

1. Entrada do Fireworks na `LimitedCalcFeature` (`LimitedCalc.BundleOfFireworks.Mode`) sem alterar Core nem `Feature`; hook registrado e logado.
2. Com ~200 stacks: custo por interação não cresce sem limite; p95 <= X% do p95 no limite; dano total por interação registrado em `docs/`; `orig` chamado; só no host.
3. Se `Aggregate`: menos objetos criados que o jogo base; sem alocação por tick.
4. Flag `Off` = jogo base.

## Tasks / Subtasks

- [x] Task 1: entrada do Fireworks, hook em `FireworkLauncher.FixedUpdate` limitando `remaining` a `4 + 4·Limit` (AC: #1, #2 parcial)
- [ ] Task 2: validação em jogo e medição (AC: #2, #4) — depende da linha de base 1.6
  - [ ] Hook `ok` no `ror2meeeee_status`; com `Mode=Clamp` e 200 stacks, a interação dispara no máximo `4+4·Limit` mísseis
  - [ ] p95 da interação com ~200 stacks vs. no limite; X% e dano total por interação em `docs/`
- [x] Task 3: `Aggregate` implementado (AC: #3): mesmo teto de mísseis do `Clamp`, com `damageCoefficient *= remaining/cap` (dano total do disparo preservado). Medir e decidir entre `Clamp` e `Aggregate`

## Dev Notes

- Relatório 1.3: o disparo é por interação (`4 + 4·stack` mísseis, 1 a cada 0,1 s). Limitar reduz o pico linear nos stacks, mas também o dano total (cada míssil causa 3× dano). `Aggregate` (menos mísseis, mais dano por míssil) preserva o dano total do disparo.
- `remaining` só diminui: o teto é idempotente por tick.
- `Mode=Off` por padrão; limite 50 provisório.

## Dev Agent Record

### Completion Notes

- Compila sem avisos. Não testado em jogo. Roteiro: `docs/teste-epic2.md`.

### File List

- `src/Ror2Meeeee/Features/LimitedCalc/LimitedCalcFeature.cs` (mesma feature da 2.1)

### Change Log

- 2026-10-11: entrada do Fireworks.
