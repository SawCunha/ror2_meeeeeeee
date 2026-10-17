---
baseline_commit: 1afd157
---

# Story 4.5: Artefato do Dízimo (ouro também vai para o diretor)

Status: in-progress

## Acceptance Criteria

(Pedido do autor, fora do épico.) Artefato do mod (ligado no lobby): todo ouro ganho por jogadores é somado também a `CombatDirector.monsterCredit`; só no host; kill-switch `Gold.Enabled`; cálculo puro e testado no Core.

## Tasks / Subtasks

- [x] `GoldCredit.Compute` no Core + 10 testes (`GoldCreditTests`)
- [x] `TitheArtifact` (`ArtifactDef` `Ror2MeeeeeTithe`, via `ContentAddition.AddArtifactDef`, ícones gerados)
- [x] `GoldFeature`: hook `On.RoR2.CharacterMaster.GiveMoney_uint_bool`, `orig` sempre chamado, `Fail()` em exceção
- [ ] Validação em jogo: ligar o "Artefato do Dízimo" no lobby, ganhar ouro; log `[Gold] +N ouro ... => +N crédito`

## Dev Notes

- Config: `[Gold] Enabled=false` (exige reiniciar), `Ratio=0.1` (10% do ouro vira crédito).
- Só diretores `TeamIndex.Monster` com `moneyWaves` não vazio (o principal); os de boss/evento ficam de fora.
- Desempenho: elegibilidade por diretor cacheada (reflexão uma vez por diretor), `Ratio` em cache, laço `for` sem enumerador; tudo na thread principal (API do Unity não é thread-safe e `monsterCredit` disputa com o `FixedUpdate` do diretor).
- Convertido de item para Artefato a pedido do autor: vale para o ouro de qualquer jogador (master com `playerCharacterMasterController`), run inteira.
- Nova dependência: `R2API.ContentManagement` (`com.bepis.r2api.content_management`).
- O gasto de ouro (baús) não passa por `GiveMoney`, então só ganhos contam.
- Ícones são placeholders gerados (**visual não validado**).

## Dev Agent Record

### File List

- `src/Ror2Meeeee.Core/Gold/GoldCredit.cs`, `tests/Ror2Meeeee.Core.Tests/GoldCreditTests.cs`
- `src/Ror2Meeeee/Features/Gold/{GoldFeature,TitheArtifact}.cs`, `Plugin.cs`, `DifficultyFeature.cs` (`GetMoneyWaves`)
