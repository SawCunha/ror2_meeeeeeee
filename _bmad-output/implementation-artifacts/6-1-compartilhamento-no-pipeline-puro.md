---
baseline_commit: 1afd157
---

# Story 6.1: Compartilhamento no pipeline puro

Status: review

## Acceptance Criteria

(Fonte: `epics.md`.) `Resolve` puro devolve o total por destinatário; ordem pickup → multiplicação → compartilhamento; concessão vinda do compartilhamento não é compartilhada de novo; desligado = Épico 3; morto/desconectado e inelegíveis recebem 0; saturação sem estouro; Core sem referência ao jogo.

## Tasks / Subtasks

- [x] `SharingPipeline.Resolve(ctx, rules, enabled, fromSharing, others)` reaproveitando `MultiplierPipeline`
- [x] 12 testes (`SharingPipelineTests`)

## Dev Notes

- Desvio: assinatura recebe `enabled` e `fromSharing` em vez de um `SharingRules`; só há um booleano de config (YAGNI).
- `StackCap` é aplicado por destinatário, ao stack dele.
- Story 6.2 (hook no host) e 6.3 seguem em backlog: Epic 6 é v2, fora do MVP.

## Dev Agent Record

### File List

- `src/Ror2Meeeee.Core/Pipeline/SharingPipeline.cs`, `tests/Ror2Meeeee.Core.Tests/SharingPipelineTests.cs`
