---
baseline_commit: 1afd157
---

# Story 6.2: Replicação no host

Status: in-progress

## Acceptance Criteria

(Fonte: `epics.md`.) `SharingFeature` com hook em `ItemDef.AttemptGrant`, sem referenciar a `MultiplierFeature`; host concede o total do `SharingPipeline` aos demais jogadores vivos; guarda `_granting`; `Fail()` em exceção; desligada = Épico 3.

## Tasks / Subtasks

- [x] `SharingFeature` (`Sharing.Enabled`, padrão desligado); `ItemClassifier` extraído e compartilhado
- [ ] Validação em jogo com host + 1 cliente: item do host chega ao cliente (`[Sharing]` no log), sem duplicação

## Dev Notes

- Lê as mesmas chaves `Multiplier.*` (BepInEx devolve a mesma entrada); com `Multiplier.Enabled=false` o multiplicador vale 1 (compartilha só o pickup).
- Concede por `GiveItemPermanent`, que não passa por `AttemptGrant`: sem recursão nem multiplicação em dobro.
- Itens de `LimitedCalc` não sofrem `StackCap` (mesma regra do Epic 3).

## Dev Agent Record

### File List

- `src/Ror2Meeeee/Features/Sharing/SharingFeature.cs`, `src/Ror2Meeeee/Common/ItemClassifier.cs` (novo); `MultiplierFeature.cs`, `Plugin.cs` (alterados)
