---
baseline_commit: 1afd157
---

# Story 4.3: Quantidade e ritmo escalados

Status: in-progress

## Acceptance Criteria

(Fonte: `epics.md`.) Orçamento de spawn e velocidade da escalada multiplicados pelos ganhos do Core; `Factor = 1` = jogo base; custo dos hooks visível no `Probe`; exceção contada em `Fail()` e `orig` chamado.

## Tasks / Subtasks

- [x] Quantidade: `CombatDirector.Awake` (hook) + reflexão em `moneyWaves[].multiplier`, aplicada também a diretores ativos em mudanças
- [x] Ritmo: `Run.RecalculateDifficultyCoefficentInternal` (hook), coeficientes multiplicados após `orig`
- [x] Item "Chamado da Horda" (`HordeItem`, `HordeCurve`): +25% por stack, saturado em 10×
- [ ] Validação em jogo e medição no `Probe` (AC: custo adicional e aumento de inimigos)

## Dev Notes

- O item foi adicionado a pedido do autor (fora do épico; o `project-context` dizia que o mod não cria itens).
  Mudança de projeto: dependência de `R2API.Items` e `R2API.Language` (`[BepInDependency]`) e `EveryoneMustHaveMod` já cobre a rede.
- Item branco (Tier1), tag `AIBlacklist`. Ícone: arte do autor (`Assets/chamado_da_horda.png`, embutida); modelo = placeholder `PickupMystery` (**visual não validado**).
- A contagem de stacks do item só é relida ao receber o item e no `Awake` de cada diretor.
- O custo do hook de `CombatDirector.Awake`/`Recalculate` é por evento, não por tick; o custo de IA com mais inimigos aparece em `mean_ms`/`p95_ms`.

## Dev Agent Record

### File List

- `src/Ror2Meeeee/Features/Difficulty/DifficultyFeature.cs`, `src/Ror2Meeeee/Features/Horde/HordeItem.cs`, `Plugin.cs`, `Ror2Meeeee.csproj`
