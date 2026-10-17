---
baseline_commit: 1afd157
---

# Story 3.2: Multiplicação no pickup, no host

Status: in-progress

## Story

Como jogador do grupo, quero receber cópias extras a cada pickup elegível, para ter mais itens e poder de build (FR-6).

## Acceptance Criteria

1. `MultiplierFeature` com `Multiplier.Value` e hook no ponto do relatório 1.3, registrado e logado; `Value = 1` desliga sem custo mensurável.
2. Host, pickup elegível com `Value = 2`: inventário recebe 2 no total; extra via `GiveItemPermanent` com guarda `_granting`; `orig` chamado; só com `NetworkServer.active`.
3. Cliente não concede nada.
4. Exceção na concessão: `Fail()` registra e `_granting` volta a `false`.

## Tasks / Subtasks

- [x] `MultiplierFeature` com hook em `On.RoR2.ItemDef.AttemptGrant` (AC: #1 a #4)
- [ ] Validação em jogo (AC: #1 a #3), roteiro em `docs/teste-epic3.md`
  - [x] Hook `ok`; log `[Multiplier] Mushroom (White): +99 (stack 0 -> 100)`; com Value=300: Infusion, Missile e itens void `+299` (2026-10-11)
  - [ ] `Value = 1`: nenhum extra
  - [ ] Cliente real: sem extras do lado cliente

## Dev Notes

- `Value` padrão **100** (pedido do autor para validar desempenho; o épico dizia 2).
- Com `Value <= 1`, no cliente ou durante a concessão própria, o hook só repassa a `orig`.
- O estado (stack, tipo) é capturado **antes** de `orig`; a concessão extra vem depois.
- Pendente de confirmar em jogo: que o delegate `PickupDef.attemptGrant` já ligado a `ItemDef.AttemptGrant` passa pelo detour do `On.*` (esperado, o MonoMod troca o corpo do método).
- Duplicator (DLC3) e `MutualExclusivity` ficam como estão: o extra usa o `ItemIndex` do pickup original.

## Dev Agent Record

### Validação em jogo (host, 1 jogador)

- Delegate `PickupDef.attemptGrant` passa pelo hook: confirmado.
- 0 falhas no log; Probe com 7 janelas sem degradação (ver 3.5).
- Pendentes: `Value = 1` e cliente real.

### File List

- `src/Ror2Meeeee/Features/Multiplier/MultiplierFeature.cs` (novo); `Plugin.cs`, `LimitedCalcFeature.cs` (alterados)

### Change Log

- 2026-10-11: feature de multiplicação.
