---
baseline_commit: 1afd157
---

# Story 1.4: `Probe` de medição

Status: review

## Story

Como autor do mod,
quero medir frame time, RAM e contadores por fase com custo desprezível,
para que eu compare as condições da linha de base com números confiáveis (FR-1, NFR-3, NFR-4).

## Acceptance Criteria

(Fonte: `epics.md`, Story 1.4.)

1. `Ror2Meeeee.Core.Tests` (`net8.0`, xunit.v3) cobre p95/média: buffer vazio, 1 amostra, buffer cheio, valores repetidos; `dotnet test` passa.
2. Com `General.ProbeEnabled=false` (padrão): sem `Stopwatch` nem alocação do `Probe` nos hooks; `VerboseLogging` segue `false`.
3. Com `ProbeEnabled=true` e uma fase nomeada: ao terminar, CSV com frame time médio e p95, RAM e contadores (projéteis, procs); nada escrito durante a fase; buffers pré-alocados.
4. Hook vazio só para medição, com o custo separado no CSV (A6).

## Tasks / Subtasks

- [x] Task 1: `FrameStats` no Core + testes (AC: #1)
- [x] Task 2: `Probe` (Feature) com `General.ProbeEnabled`/`ProbeEmptyHook`, fases por console (AC: #2, #3, #4)
- [x] Task 3: build do plugin com 0 erros e 0 avisos (AC: #2)
- [x] Task 4: validação em jogo (AC: #2, #3, #4)
  - [x] `ProbeEnabled=false`: log `Probe: desligada por config` e nenhum CSV criado na sessão (2026-10-11)
  - [x] `ProbeEnabled=true`: `ror2meeeee_probe_begin teste` / `ror2meeeee_probe_end` gravou a linha em `BepInEx/ror2meeeee-probe.csv` (4818 frames, média 16,68 ms, p95 16,67 ms, 63 projéteis, 120 damage_events; 2026-10-11)
  - [x] `ProbeEmptyHook=true`: `empty_hook=true` na linha, 2 hooks registrados, `ram_unity_mb`=1853,9 (2026-10-11)

## Dev Notes

- Frame time vem de `Time.unscaledDeltaTime` em `RoR2Application.onUpdate` (sem `Stopwatch`, sem novo `MonoBehaviour`).
- Hooks: `On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo` (contador e vazio; as outras sobrecargas desembocam nela) e o evento `GlobalEventManager.onServerDamageDealt`.
- Desvio do épico: "procs" virou `damage_events` (contagem de `onServerDamageDealt`); contar procs de verdade exigiria hook em `ProcessHitEnemy` e custo maior. Rever se a linha de base pedir.
- Desvio: o kill-switch é `General.ProbeEnabled`, não `Probe.Enabled`; a `Feature` ganhou um construtor que aceita a `ConfigEntry` de ativação.
- Fases por console: `ror2meeeee_probe_begin <nome>` e `ror2meeeee_probe_end` (o épico não define o gatilho).
- O custo do hook vazio aparece como linhas com `empty_hook=true` versus `false` (comparação entre fases), não como coluna de tempo.

## Dev Agent Record

### Completion Notes

- 5 testes de `FrameStats` (vazio, 1 amostra, 100 amostras fora de ordem, repetidos, `count` menor que o buffer) escritos antes da implementação (falharam por tipo inexistente) e depois passando.
- Build do plugin: 0 erros, 0 avisos. DLLs copiadas para o perfil r2modman.
- Validado em jogo com `ProbeEnabled=true`. Defeito achado: `Environment.WorkingSet` devolvia 0 sob Proton; a coluna virou `ram_unity_mb` (`Profiler.GetTotalReservedMemoryLong`). Confirmado em jogo (1853,9 MB). `ProbeEnabled=false` também confirmado. Janela automática (`ProbeAutoWindowSeconds`, 30 s) adicionada e validada: 7 janelas em log e CSV.
- Nota para a 1.6: frame time de 16,68 ms = 60 fps, limitado por VSync/fps cap; desligar antes de medir a linha de base.

### File List

- `src/Ror2Meeeee.Core/Measurement/FrameStats.cs` (novo)
- `tests/Ror2Meeeee.Core.Tests/` (novo: csproj, `FrameStatsTests.cs`)
- `src/Ror2Meeeee/Debug/Probe.cs` (novo)
- `src/Ror2Meeeee/Common/Cfg.cs`, `Common/Feature.cs`, `Plugin.cs`, `Ror2Meeeee.csproj` (alterados)
- `Ror2Meeeee.sln` (alterado)

### Change Log

- 2026-10-11: `FrameStats`, `Probe` e projeto de testes.
