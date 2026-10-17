---
baseline_commit: 1afd157
---

# Story 1.5: Comando `ror2meeeee_status`

Status: review

## Story

Como autor do mod,
quero listar features, falhas e hooks registrados pelo console do jogo,
para que eu veja o estado do mod sem abrir o log.

## Acceptance Criteria

(Fonte: `epics.md`, Story 1.5.)

1. `ror2meeeee_status` lista cada feature (ligada/desligada), falhas e o resultado de registro de cada hook, e mostra a versão do mod e do jogo.
2. Feature auto-desligada por `MaxFailures` aparece como desligada por falha, com a contagem.
3. O comando não aloca nem registra nada por tick (só sob demanda).

## Tasks / Subtasks

- [x] Task 1: `Feature.Track`/`HookResults`/`ShutdownByFailures` e `Plugin.Features` (AC: #1, #2)
- [x] Task 2: `Commands.cs` com `[ConCommand]` e `[assembly: SearchableAttribute.OptIn]` (AC: #1, #3)
- [x] Task 3: validação em jogo (AC: #1, #2)
  - [x] `ror2meeeee_status` no console mostra versão do mod e do jogo (log: `Ror2Meeeee v0.1.0 | jogo 1.5.0#1210`)
  - [x] `[ConCommand]` descoberto via `SearchableAttribute.OptIn`

## Dev Notes

- R2API.CommandHelper não está instalado no perfil; o registro depende do `SearchableAttribute.OptIn` (lido em `HG.Reflection.SearchableAttribute.ScanAssembly`). **Hipótese ainda não confirmada em jogo.**
- Resultado por hook, não só por feature, via `Feature.Track`.
- Saída por `Debug.Log` (o console do jogo exibe).

## Dev Agent Record

### Completion Notes

- Compila sem avisos. Sem teste automatizado: depende de `RoR2.Console`, que não roda fora do jogo.
- Comando validado em jogo. `ligada | falhas 0` confirmado em jogo. "Desligada por falha" (AC #2) só por leitura de código: nenhuma feature atingiu `MaxFailures`.

### File List

- `src/Ror2Meeeee/Debug/Commands.cs` (novo)
- `src/Ror2Meeeee/Common/Feature.cs`, `Plugin.cs`, `Ror2Meeeee.csproj` (alterados)

### Change Log

- 2026-10-11: comando de status.
