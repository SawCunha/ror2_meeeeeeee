# Trabalho adiado

## Deferred from: code review of story-1-1 (2026-10-11)

- `GameDir` definido em `Directory.Build.props` mas não usado; `HintPath` do R2API.Core depende de `$(HOME)` e do perfil r2modman, então o build não é portável. Motivo: ambiente de dev é Linux (documentado). Revisar na primeira story que referenciar `RoR2.dll`/`MMHOOK`. Story 1-2: `GameDir` passou a ser usado (`RoR2.dll`); o `HintPath` do MMHOOK e do R2API.Core ainda depende de `$(R2modmanProfile)`.
- Feed `nuget.bepinex.dev` aplicado a todos os projetos via `RestoreAdditionalProjectSources`, sem lock file nem source mapping. Motivo: o feed é exigido pelo CLAUDE.md. Endurecer quando houver CI.
- ~~`BepInEx.Core` sem `PrivateAssets`~~ RESOLVIDO na story 1-2: a saída do build não contém `BepInEx.dll` nem DLLs do jogo.
- `baseline_commit: NO_VCS` na story 1-1 ficará defasado após o primeiro commit. Motivo: ainda não há commit para apontar.

## Deferred from: code review of story-1-2 (2026-10-11)

- Criação das features não é isolada: `Name` inválido para `ConfigDefinition` (`=`, `\n`, `[`, `]`, vazio) lança no construtor de `Feature`; dois `Name` iguais compartilham o mesmo `ConfigEntry`. Motivo: lista de features vazia hoje. Tratar na Story 2.1 (try/catch por feature no `Plugin`, checagem de duplicata).
- `Feature.ConfigEnabled` lê o `ConfigEntry` a cada chamada, sem cache em `SettingChanged`. Motivo: lido uma vez no `Awake`, fora do caminho quente. Cachear se o liga/desliga em runtime for implementado.
