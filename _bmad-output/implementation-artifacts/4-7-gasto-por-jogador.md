---
baseline_commit: 1afd157
---

# Story 4.7: Gasto por jogador (SpendFeature)

Status: in-progress

## Acceptance Criteria

(Pedido do autor, fora do épico.) Saber quanto cada jogador gasta em compras na run; só no host; kill-switch `Spend.Enabled`; consulta por comando; CSV no fim da run.

## Tasks / Subtasks

- [x] `SpendLedger` no Core (acúmulo, snapshot ordenado, formato) + 6 testes
- [x] `SpendFeature` via `PurchaseInteraction.onPurchaseGlobalServer`; comando `ror2meeeee_spend`
- [x] CSV `ror2meeeee-spend.csv` gravado por worker (cópia dos dados) em início/fim de run
- [ ] Validação em jogo com host + cliente

## Dev Notes

- Conta só jogadores (`playerCharacterMasterController`); ouro = `ctx.cost` quando `costType == Money`; outras compras contam em "Compras" com 0 de ouro.
- `ctx.cost` é o custo já ajustado pelo jogo (pode divergir do ouro realmente debitado em compras grátis).
- Total é da run inteira; por estágio não implementado (não houve resposta do autor).
- `Spend.LogEachPurchase=false` por padrão (ruído).

## Dev Agent Record

### File List

- `src/Ror2Meeeee.Core/Spend/SpendLedger.cs`, `tests/Ror2Meeeee.Core.Tests/SpendLedgerTests.cs`
- `src/Ror2Meeeee/Features/Spend/SpendFeature.cs`, `Debug/Commands.cs`, `Plugin.cs`
