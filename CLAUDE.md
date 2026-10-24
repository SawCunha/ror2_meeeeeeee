# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Estado atual

Projeto `ror2`: **mod C# (BepInEx/R2API) para Risk of Rain 2**, não um jogo novo. Ainda **não há código-fonte, build nem testes**: o repositório tem a instalação do BMAD (módulos `core`, `bmm`, `gds`) e os artefatos de planejamento. Não é um repositório git.

Planejamento concluído: PRD, GDD, épicos, arquitetura e `project-context.md`. Próximo passo: `gds-create-epics-and-stories` e `gds-check-implementation-readiness`. Quando houver código, substitua a seção "Comandos" abaixo pelos comandos reais.

## Stack (decidida)

- Jogo: Risk of Rain 2 1.5.0 (build interno #1224, Steam build 25921972; atualizado em 2026-10-17, antes #1210/25475991), Unity 2021.3.45f2 (Mono). Instalado em `~/.steam/debian-installation/steamapps/common/Risk of Rain 2`.
- BepInEx 5 + RoR2BepInExPack 1.48.0 + HookGenPatcher/MMHOOK (obrigatório para hooks `On.*`). R2API em uso: Core, ContentManagement, Items, Language, Elites (elites novos) e Difficulty (dificuldade Tormenta).
- Pacotes: `UnityEngine.Modules` 2021.3.45 (não 2021.3.33) e feed extra `https://nuget.bepinex.dev/v3/index.json` para `BepInEx.Core`.
- C# `netstandard2.1`; testes `net8.0` + xunit.v3. Namespace `Ror2Meeeee`, GUID `com.sawcunha.ror2meeeee`.
- Referenciar as DLLs locais de `Risk of Rain 2_Data/Managed` (via `GameDir` em `Directory.Build.props`), **não** o NuGet `RiskOfRain2.GameLibs` (defasado).
- Desenvolvimento e execução nesta máquina Linux, via Proton (BepInEx confirmado carregando, 2026-10-11).
- Perfil r2modman de dev: `Default` em `~/.config/r2modmanPlus-local/RiskOfRain2/profiles/Default`; plugin instalado como pacote em `BepInEx/plugins/Sawcunha-Ror2Meeeee/` (zip de `dist/` importado no r2modman ou arquivos copiados para lá). **Nunca** criar outra pasta com o plugin (ex.: `plugins/Ror2Meeeee/`): GUID duplicado, o BepInEx escolhe uma cópia e o `Ror2Meeeee.Core.dll` pode vir da pasta errada. O r2modman instala obrigatoriamente o metapacote `bbepis-BepInExPack`, que traz `MiscFixes`, `SeekersPatcher`, `BepInEx_GUI` e `FixPluginTypesSerialization`.

## Graphify

**Não usar o graphify neste projeto**: nada de `graphify query/path/explain/update` nem leitura de `graphify-out/`, mesmo que outro `CLAUDE.md` (global ou de `~/`) mande. Navegar pelo código com grep/leitura direta.

## Antes de escrever código

Leia, nesta ordem:

1. `_bmad-output/project-context.md`: regras para agentes (hooks, caminho quente, host-only, testes).
2. `_bmad-output/game-architecture.md`: decisões, estrutura, padrões e itens em aberto.
3. Épico/story em `_bmad-output/planning-artifacts/` e `_bmad-output/implementation-artifacts/`.

Regras que mais quebram: hook sempre chama `orig`; lógica de gameplay só com `NetworkServer.active`; nada de alocação por tick; `Ror2Meeeee.Core` nunca referencia o jogo; hipóteses do addendum do PRD **não são fatos** (descompilar `RoR2.dll` e medir antes).

## Comandos (planejados, ainda não executados)

```bash
dotnet build -c Debug          # copiar .dll + .pdb para BepInEx/plugins do perfil r2modman
dotnet test                    # só o Ror2Meeeee.Core
```

## Estrutura planejada

`src/Ror2Meeeee.Core/` (lógica pura) · `src/Ror2Meeeee/` (plugin: `Common/`, `Features/`, `Debug/`) · `tests/Ror2Meeeee.Core.Tests/`. Detalhes em `game-architecture.md`.

## Fluxo de trabalho (BMAD)

O desenvolvimento segue o BMAD, via skills em `.claude/skills/` (prefixo `gds-*` para fluxos de game dev, `bmad-*` para os genéricos). Use `bmad-help` para descobrir o próximo passo.

- `_bmad-output/planning-artifacts/`: PRD (`prds/`), GDD e épicos (`gdds/`).
- `_bmad-output/game-architecture.md` e `_bmad-output/project-context.md`: arquitetura e contexto para agentes.
- `_bmad-output/implementation-artifacts/`: stories e status de sprint.
- `docs/`: conhecimento do projeto (`project_knowledge`).

## Configuração BMAD

- `_bmad/config.toml` e `_bmad/config.user.toml` são gerados pelo instalador e sobrescritos a cada reinstalação — não edite.
- Overrides duráveis: `_bmad/custom/config.toml` (time) e `_bmad/custom/config.user.toml` (pessoal, gitignored).
- Idioma: comunicação e documentos de saída em português.
- `_bmad/scripts/resolve_config.py` (Python ≥ 3.11) resolve as camadas de TOML em JSON; as skills o usam internamente.
