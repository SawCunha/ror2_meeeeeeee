---
baseline_commit: 1afd157
---

# Story 2.1: Bustling Fungus com custo limitado

Status: in-progress

## Story

Como jogador do grupo, quero que o Bustling Fungus deixe de custar mais a cada stack sem perder a cura, para que partidas com 50+ stacks continuem jogáveis (FR-2).

## Acceptance Criteria

(Fonte: `epics.md`, Story 2.1.)

1. `IStackPolicy` no Core com `Off` e `Clamp`; testes cobrem stack 0, 1, no limite, acima, `int.MaxValue`; `Off` devolve o original. (`Aggregate` só se justificado.)
2. `LimitedCalcFeature` com lista de itens, política e flag por entrada (`LimitedCalc.BustlingFungus.Mode`); hook registrado e logado; novo item = nova entrada.
3. Fungus com 50+ stacks em `Clamp`, só no host: trabalho por tick não cresce acima do limite, cura continua, `orig` sempre chamado; p95 com 50+ stacks <= X% do p95 no limite (X da linha de base 1.6); sem alocação por tick.
4. Flag em `Off` = jogo base; limite e modo registrados com justificativa.

## Tasks / Subtasks

- [x] Task 1: `StackPolicy` no Core + 14 testes (AC: #1)
- [x] Task 2: `LimitedCalcFeature` com entrada do Fungus, hook em `MushroomBodyBehavior.FixedUpdate` (AC: #2, #3 parcial)
- [ ] Task 3: validação em jogo e medição (AC: #3, #4) — depende da linha de base 1.6
  - [ ] `LimitedCalc.Enabled=true`, `BustlingFungus.Mode=Clamp`: hook `ok` no `ror2meeeee_status`, cura segue ocorrendo
  - [ ] p95 com 50+ stacks vs. no limite; X% registrado em `docs/`
  - [ ] Limite escolhido e justificativa em `docs/`

## Dev Notes

- **Relatório 1.3:** o custo do Fungus **não cresce por stack** (1 ward por corpo; só o raio e a cura crescem). Limitar stacks reduz o efeito (raio e cura), não necessariamente o custo. O `Clamp` implementado é o que o épico pede, mas **o benefício de desempenho é uma hipótese a medir**; a alavanca real parece ser nº de corpos × membros do time e o churn de `Instantiate`/`Destroy` do ward. Se a medição confirmar, abrir correção de curso (`gds-correct-course`).
- Mecanismo: `On.RoR2.Items.MushroomBodyBehavior.FixedUpdate`; `stack` (campo público) é trocado só durante a chamada de `orig` e restaurado em `finally`. Só com `NetworkServer.active`.
- Limite padrão 10 é **provisório**; `Mode=Off` por padrão: nada muda no jogo até medir.
- `NetworkServer` vem de `com.unity.multiplayer-hlapi.Runtime.dll` (referência nova no csproj).

## Dev Agent Record

### Completion Notes

- Testes: vermelho (namespace inexistente) e depois verde, 17/17. Build do plugin 0 erros, 0 avisos.
- Não testado em jogo; nenhum limite foi medido.

### File List

- `src/Ror2Meeeee.Core/Limits/StackPolicy.cs`, `tests/Ror2Meeeee.Core.Tests/StackPolicyTests.cs` (novos)
- `src/Ror2Meeeee/Features/LimitedCalc/LimitedCalcFeature.cs` (novo)
- `src/Ror2Meeeee/Common/Cfg.cs`, `Plugin.cs`, `Ror2Meeeee.csproj` (alterados)

### Change Log

- 2026-10-11: política pura e feature com entrada do Fungus.
