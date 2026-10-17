---
baseline_commit: 1afd157
---

# Story 4.6: Artefato Taxade (abrir baú custa vida)

Status: in-progress

## Acceptance Criteria

(Pedido do autor, fora do épico.) Artefato do mod (ligado no lobby): ao abrir um baú, o jogador perde 30% da vida máxima; só no host; kill-switch `Tax.Enabled`; `Percent` configurável.

## Tasks / Subtasks

- [x] `TaxArtifact` (`ArtifactDef` `Ror2MeeeeeTax`) e `TaxFeature` via `PurchaseInteraction.onPurchaseGlobalServer`
- [ ] Validação em jogo: abrir baú com o artefato ligado; vida cai 30%; sem o artefato nada muda

## Dev Notes

- Config: `[Tax] Enabled=false`, `Percent=30` (da vida máxima), `Lethal=false` (a taxa nunca mata: deixa 1 de vida).
- Só baús (`ChestBehavior`); cápsulas, impressoras e multishops não cobram.
- Dano via `TakeDamage` com `BypassArmor|BypassBlock|BypassOneShotProtection`; sem atacante.
- Nome exibido "Artefato Taxade" como pedido (mensagem do autor veio truncada: "Taxade Que..."); trocar em `TaxArtifact.cs` se for outro.

## Dev Agent Record

### File List

- `src/Ror2Meeeee/Features/Tax/{TaxArtifact,TaxFeature}.cs`, `Plugin.cs`, `TitheArtifact.cs` (`MakeIcon` interno)
