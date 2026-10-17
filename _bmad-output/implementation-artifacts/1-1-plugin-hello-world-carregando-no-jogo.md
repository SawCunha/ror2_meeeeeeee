---
baseline_commit: NO_VCS
---

# Story 1.1: Plugin "hello world" carregando no jogo

Status: done

<!-- Nota: validação é opcional. Rode a validação da story antes do gds-dev-story, se quiser. -->

## Story

Como autor do mod,
quero compilar o plugin no Linux e vê-lo carregar no Risk of Rain 2,
para que o build cruzado e a carga do BepInEx estejam validados antes de qualquer feature.

## Acceptance Criteria

1. **Build.** Dado o .NET SDK 8+ instalado e a solução `Ror2Meeeee.sln` com `src/Ror2Meeeee.Core` e `src/Ror2Meeeee` (`netstandard2.1`), `GameDir` definido em `Directory.Build.props` e o feed `https://nuget.bepinex.dev/v3/index.json` configurado, quando executo `dotnet build -c Debug`, então o build conclui sem erro e gera `.dll` e `.pdb`.
2. **Repositório limpo.** Nenhuma DLL do jogo, `MMHOOK_RoR2.dll` nem a pasta `BepInEx` entram no repositório.
3. **Carga no jogo.** Dados `.dll` e `.pdb` copiados para `BepInEx/plugins/Ror2Meeeee/` do perfil r2modman `Default` (com `MiscFixes` e `SeekersPatcher` desativados), quando o jogo inicia até o menu principal, então o `LogOutput.log` registra o GUID `com.sawcunha.ror2meeeee`, a versão do mod e a versão do jogo (`1.5.0#1210`).
4. **Compatibilidade de rede.** A classe do plugin declara `[NetworkCompatibility(EveryoneMustHaveMod, EveryoneNeedSameModVersion)]`.
5. **Divergência de versão.** Dada uma versão do jogo diferente da esperada, quando o plugin inicia, então um `Warning` de divergência é logado e o plugin segue carregado (NFR-5).
6. **Git.** `git init` foi executado e o `.gitignore` cobre DLLs do jogo, `bin/`, `obj/` e a pasta `BepInEx`.

## Tasks / Subtasks

- [x] Task 1: Estrutura da solução (AC: #1, #2)
  - [x] `dotnet new sln -n Ror2Meeeee` na raiz (`/home/sawcunha/Projetos/ror2`)
  - [x] `src/Ror2Meeeee.Core` (`classlib`, `netstandard2.1`, sem referência ao jogo/BepInEx/Unity) com uma classe mínima ou um `.gitkeep` (ver Dev Notes)
  - [x] `src/Ror2Meeeee` (`classlib`, `netstandard2.1`) referenciando `Ror2Meeeee.Core`
  - [x] Adicionar os dois projetos à `.sln`
- [x] Task 2: `Directory.Build.props` (AC: #1)
  - [x] `LangVersion preview`, `GameDir` (padrão `$(HOME)/.steam/debian-installation/steamapps/common/Risk of Rain 2`, sobrescrevível por env/propriedade), `RestoreAdditionalProjectSources` com o feed `nuget.bepinex.dev`
  - [x] Nenhum caminho de usuário dentro de `.csproj`
- [x] Task 3: Referências do plugin (AC: #1, #4)
  - [x] `BepInEx.Core` 5.4.21 (feed bepinex.dev), `UnityEngine.Modules` **2021.3.45**
  - [x] Referência às DLLs locais de `$(GameDir)/.../Managed`: dispensada, o hello world só precisa de `UnityEngine.Modules` (`Application.version`); `GameDir` fica definido para as próximas stories
  - [x] `R2API.Core.dll` (onde vive `NetworkCompatibilityAttribute`, namespace `R2API.Utils`) por `HintPath` no perfil r2modman via propriedade (ver Dev Notes)
- [x] Task 4: `Plugin.cs` (AC: #3, #4, #5)
  - [x] `[BepInPlugin("com.sawcunha.ror2meeeee", "Ror2Meeeee", "0.1.0")]`, `[BepInDependency]` do R2API Core, `[NetworkCompatibility(...)]`
  - [x] `Awake`: `Logger.LogInfo` com GUID, versão do mod e versão do jogo; `Warning` se divergir de `1.5.0#1210`
  - [x] `Plugin.Instance` (único singleton permitido)
- [x] Task 5: Entrega e validação em jogo (AC: #3)
  - [x] ~~Desativar `MiscFixes` e `SeekersPatcher`~~ dispensado por decisão do autor: ficam sempre ativos
  - [x] Copiar `.dll` + `.pdb` para `BepInEx/plugins/Ror2Meeeee/` do perfil
  - [x] Iniciar o jogo até o menu e colar as linhas do `LogOutput.log` nas Completion Notes
  - [x] Forçar a divergência (constante de versão esperada alterada temporariamente) e confirmar o `Warning`
- [x] Task 6: Git (AC: #2, #6)
  - [x] `git init`, `.gitignore` (`bin/`, `obj/`, `BepInEx/`, `*.dll` do jogo, `MMHOOK_RoR2.dll`, `.vs/`)
  - [x] Conferir com `git status` que nenhuma DLL do jogo entrou

### Review Findings

Code review de 2026-10-11 (Blind Hunter, Edge Case Hunter, Acceptance Auditor). Contexto: diff de arquivos novos, sem commits (`NO_VCS`).

- [x] [Review][Decision] Task 5 marcada `[x]` sem evidência com o código final — "Forçar a divergência e confirmar o Warning" foi aceito sem forçar; o Warning observado veio do primeiro boot, com a lógica anterior à reflexão. Além disso, as Completion Notes admitem que não foi checado se o jogo chegou ao menu principal (AC #3), e o texto do AC #3 ainda exige `MiscFixes`/`SeekersPatcher` desativados, embora a decisão do autor seja mantê-los ativos. Opções: (a) rodar o jogo uma vez, forçar a divergência e conferir o menu; (b) reabrir os subitens `[ ]` e ajustar o texto dos AC #3/#5 à decisão tomada.
- [x] [Review][Patch] `GetGameVersion` pode derrubar o plugin: `GetRawConstantValue()` lança `InvalidOperationException` se o campo deixar de ser `const`, e `Type.GetType` pode lançar `FileLoadException`; a exceção sobe do `Awake` [src/Ror2Meeeee/Plugin.cs:62-67]
- [x] [Review][Patch] Aviso espúrio de "versão divergente" quando a reflexão devolve `null`: cai em `Application.version` (`1.5.0`), que nunca bate com `1.5.0#1210`; logar "build string indisponível" separadamente [src/Ror2Meeeee/Plugin.cs:56-57]
- [x] [Review][Patch] ~695 MB de binários do r2modman (`.zip`, `.AppImage`, `.exe`) em `docs/` estão staged e fora do `.gitignore`; a story chama `docs/` de vazio. Ignorar `docs/ebkr-r2modman-*` e remover do índice [.gitignore]
- [x] [Review][Defer] `GameDir` definido mas não usado, e `HintPath` do R2API.Core depende de `$(HOME)` e do perfil r2modman (build não portável fora do Linux/da máquina) [Directory.Build.props:5-6] — deferred, ambiente de dev é Linux documentado; revisar na 1ª story que referenciar `RoR2.dll`/`MMHOOK`
- [x] [Review][Defer] Feed `nuget.bepinex.dev` aplicado a todos os projetos, sem lock file nem source mapping [Directory.Build.props:7] — deferred, o feed é exigido pelo CLAUDE.md; endurecer quando houver CI
- [x] [Review][Defer] `BepInEx.Core` sem `PrivateAssets="all"`; não verificado se `BepInEx.dll` é copiado para a saída [src/Ror2Meeeee/Ror2Meeeee.csproj:8] — deferred, sem evidência de problema; conferir ao montar o pacote de deploy
- [x] [Review][Defer] `baseline_commit: NO_VCS` no frontmatter fica defasado quando o primeiro commit existir [story:2] — deferred, não há commit para apontar ainda

## Dev Notes

### Escopo: só o "hello world"

Esta story **não** cria `Feature`, `Cfg`, `Probe`, hooks nem `ror2meeeee_status` (Stories 1.2 a 1.5). Nenhuma feature é escrita antes de o hello world carregar no jogo. Não crie `Common/`, `Features/` nem `Debug/` vazios. O `tests/Ror2Meeeee.Core.Tests` (net8.0, xunit.v3) só entra na Story 1.4; não crie agora.

### Estado atual do ambiente (verificado em 2026-10-11)

- **.NET SDK 8.0.131 já está instalado** (`/usr/bin/dotnet`). `game-architecture.md` e `epics.md` dizem "não instalado": estão defasados nesse ponto. O build cruzado (`netstandard2.1` no Linux) continua não testado.
- Só o feed `nuget.org` está configurado em `dotnet nuget list source`; o feed `nuget.bepinex.dev` precisa vir do `Directory.Build.props` (`RestoreAdditionalProjectSources`).
- `ilspycmd` **não** está instalado (necessário só na Story 1.3). `uv` e `git` existem; o diretório `/home/sawcunha/Projetos/ror2` **não é** repositório git.
- Raiz do projeto hoje: `.claude/`, `_bmad/`, `_bmad-output/`, `docs/` (vazio), `CLAUDE.md`. Nada de `src/`, `.sln`, `.gitignore`.
- Jogo em `~/.steam/debian-installation/steamapps/common/Risk of Rain 2`; `Managed/` tem `RoR2.dll`, `Assembly-CSharp.dll`, `UnityEngine.CoreModule.dll`, `com.unity.multiplayer-hlapi.Runtime.dll` etc.
- Perfil r2modman `Default`: `~/.config/r2modmanPlus-local/RiskOfRain2/profiles/Default`. `BepInEx/plugins/` já tem `MMHOOK/MMHOOK_RoR2.dll`, `RiskofThunder-R2API_Core/R2API.Core/R2API.Core.dll` (5.3.0), `RoR2BepInExPack` 1.45.0, `Risk_Of_Options`, `R2API_Networking`, `R2API_Items`, `R2API_Language`, `score-MiscFixes`, `pseudopulse-SeekersPatcher`.
- O log do último boot desse perfil mostra BepInEx 5.4.21.0, Unity 2021.3.45 e `Risk of Rain 2 build: ver. 1.5.0#1210*`. **Atenção:** esse boot ainda tinha `MiscFixes` e `SeekersPatcher` ativos (o `SeekersPatcher` altera a `RoR2.dll`); desative-os antes da validação desta story.

### Decisões que o dev precisa fechar (e registrar)

1. **Como referenciar `R2API.Core` para o `[NetworkCompatibility]`.** Existe `R2API.Core.dll` no perfil; o pacote NuGet equivalente e o feed que o hospeda **não foram verificados**. Caminho mais curto e verificável: `HintPath` para a DLL do perfil via propriedade em `Directory.Build.props` (ex.: `R2modmanProfile`, sobrescrevível), `Private=false`. Se achar um pacote NuGet com versão 5.3.0 compatível, pode usá-lo, mas não adicione dependência NuGet sem aprovação (regra do `project-context.md`).
2. **GUID do `[BepInDependency]` do R2API Core.** Strings da DLL mostram `com.bepis.r2api` (o GUID do plugin principal do R2API 5.x); **confirme** lendo o atributo `[BepInPlugin]` na própria DLL antes de fixar. Dependência incorreta faz o plugin não carregar.
3. **Nomes do enum.** Na `R2API.Core.dll` existem os valores `EveryoneMustHaveMod` (`CompatibilityLevel`) e `EveryoneNeedSameModVersion` e `DifferentModVersionsAreOk` (`VersionStrictness`), no namespace `R2API.Utils`. Confirme os nomes de tipo exatos ao compilar.
4. **Fonte da versão do jogo.** O log do jogo exibe `ver. 1.5.0#1210*`, mas a API que o plugin usa para ler essa string não está confirmada (candidatos: `RoR2.RoR2Application.GetBuildId()` / `RoR2Application.buildId`, ou `Application.version`). Descubra no compilado (via ILSpy ou ao compilar contra `RoR2.dll`) e registre qual funcionou. Não use string literal de log como "detecção".
5. **Conteúdo do `Ror2Meeeee.Core`.** Core precisa existir na `.sln` e compilar, mas não tem lógica nesta story. Uma classe mínima (por exemplo, constantes de identidade `Guid`/`Name` ou `ExpectedGameVersion`, sem dependência do jogo) é aceitável e útil, pois o plugin as consome. Não invente abstrações.

### Armadilhas conhecidas

- `UnityEngine.Modules` **2021.3.45**, não 2021.3.33 do template: com 2021.3.33 o restore falha com `NU1605` por causa do RoR2BepInExPack 1.45.0.
- `BepInEx.Core` **não restaura do nuget.org**; sem o feed `nuget.bepinex.dev` o restore falha.
- **Não** usar o NuGet `RiskOfRain2.GameLibs` (1.3.1.275, defasado). Usar DLLs locais de `Managed`.
- `Debug` gera o `.pdb`; copie os dois. O plugin roda sob Proton: nada de caminho absoluto Linux no código; use `BepInEx.Paths`/`Path.Combine` quando houver caminho.
- Plugin e Core: `netstandard2.1`. Classes do plugin referenciam tipos do RoR2 só em `Features/*` e `Debug/` no futuro; no hello world, evite tocar tipos do jogo além do necessário para ler a versão.
- `?.`, `??` e `== null` são proibidos em `UnityEngine.Object`.
- `HookGenPatcher` gera `MMHOOK_RoR2.dll` em `BepInEx/plugins/MMHOOK/`. Já existe no perfil; **não** referencie nem commite nesta story (nenhum `On.*` é usado).
- Pode haver aviso do R2API 5.3.0 ("built for build id 1.4.1, you are running 1.5.0"): é esperado e não bloqueia.

### Project Structure Notes

Estrutura alvo (de `game-architecture.md`, "Directory Structure"); esta story cria só o que está em negrito:

```
ror2/
├── **Ror2Meeeee.sln**
├── **Directory.Build.props**
├── **.gitignore**
├── src/
│   ├── **Ror2Meeeee.Core/**      # netstandard2.1, SEM referência ao jogo
│   └── **Ror2Meeeee/**           # plugin; **Plugin.cs**
├── tests/                        # Story 1.4
└── docs/
```

Identificadores: namespace `Ror2Meeeee`, GUID `com.sawcunha.ror2meeeee`, nome `Ror2Meeeee`. Arquivos `PascalCase.cs`, um tipo principal por arquivo; campos privados `_camelCase`.

### Project Context Rules

Extraídas de `_bmad-output/project-context.md`, aplicáveis a esta story:

- Plugin com `[BepInPlugin]`, um `[BepInDependency]` por submódulo R2API realmente usado e `[NetworkCompatibility(EveryoneMustHaveMod, EveryoneNeedSameModVersion)]`.
- BepInEx **5.4.21** (não 6); RoR2BepInExPack 1.45.0; C# `preview`; `netstandard2.1`.
- `GameDir` vem de `Directory.Build.props`, com sobrescrita por env/propriedade; nunca caminho de usuário em `.csproj`.
- Não commitar DLLs do jogo, `MMHOOK_RoR2.dll` nem `BepInEx`.
- Não adicionar dependência NuGet sem aprovação.
- Versão do jogo e do mod logadas no `Awake`; divergência gera `Warning`, sem derrubar o plugin.
- `Ror2Meeeee.Core` nunca referencia o jogo, BepInEx nem Unity.
- Único singleton: `Plugin.Instance`.
- Antes do primeiro "hello world" funcionar, nenhuma feature é escrita.
- Código novo no `Core` sem teste não fecha a story: se o Core ganhar lógica, ela precisa de teste (por isso mantenha-o sem lógica nesta story, e o projeto de testes fica para a 1.4).
- Hipóteses do addendum do PRD não são fatos.

### Testing

- Não há teste automatizado nesta story (Core sem lógica; projeto de testes é da Story 1.4).
- Validação: `dotnet build -c Debug` sem erro/aviso novo; `git status` limpo de DLLs do jogo; boot do jogo até o menu com as linhas esperadas no `LogOutput.log`; `Warning` de divergência observado ao forçar versão esperada diferente.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.1: Plugin "hello world" carregando no jogo]
- [Source: _bmad-output/game-architecture.md#Project Initialization]
- [Source: _bmad-output/game-architecture.md#Project Structure]
- [Source: _bmad-output/game-architecture.md#Development Environment]
- [Source: _bmad-output/project-context.md#Engine-Specific Rules, #Platform & Build Rules]
- [Source: CLAUDE.md#Stack (decidida)]

## Dev Agent Record

### Agent Model Used

claude-sonnet-5-5

### Debug Log References

- `dotnet build -c Debug`: 0 avisos, 0 erros (netstandard2.1 cruzado no Linux funciona).
- `R2API.Core.dll` (ilspycmd): `[BepInPlugin("com.bepis.r2api", "R2API", "5.3.0")]`; atributo é `NetworkCompatibility` (namespace `R2API.Utils`); enums `CompatibilityLevel.EveryoneMustHaveMod` e `VersionStrictness.EveryoneNeedSameModVersion`.
- `RoR2.RoR2Application.GetBuildId()` devolve `Application.version`; o plugin usa `Application.version` direto.

### Completion Notes List

- Tasks 1-4 e 6 concluídas. Decisões: `R2API.Core` por `HintPath` (propriedade `R2modmanProfile` em `Directory.Build.props`); nenhuma DLL do jogo referenciada (o hello world só precisa de `UnityEngine.Modules`); `Core` contém só `ModInfo` (constantes).
- `git status` sem DLLs do jogo/`bin`/`obj` (verificado com `--ignored`).
- Task 5 concluída. `.dll`/`.pdb` (plugin e Core) em `BepInEx/plugins/Ror2Meeeee/` do perfil `Default`, com `MiscFixes` e `SeekersPatcher` ativos (decisão do autor). Boot final do `LogOutput.log`:
  ```
  [Info   :   BepInEx] Loading [Ror2Meeeee 0.1.0]
  [Info   :Ror2Meeeee] com.sawcunha.ror2meeeee v0.1.0 carregado; jogo 1.5.0#1210
  [Info   :     R2API] [NetworkCompatibility] Adding to the networkModList :
  [Info   :     R2API] com.sawcunha.ror2meeeee;0.1.0
  ```
  Zero linhas `[Error]`, sem `Warning` do plugin.
- Bug achado na validação: `Application.version` devolve só `1.5.0`; o `#1210` vem de `AutomatedBuildVersion.buildVersionString` (RoR2.dll, `const`, inlinada em compilação). `Plugin.GetGameVersion()` lê por reflexão (`GetRawConstantValue`), sem referenciar a `RoR2.dll`.
- AC #5: no primeiro boot (antes da correção) o plugin logou `Warning: Versão do jogo divergente: esperada 1.5.0#1210, atual 1.5.0` e seguiu carregado. Aceito pelo autor como evidência, sem forçar divergência artificial.
- Pendência para o AC #3 literal: o log foi lido após o plugin carregar (`Chainloader startup complete`); o jogo estava subindo, não foi checado se chegou ao menu principal.

### File List

- Ror2Meeeee.sln (novo)
- Directory.Build.props (novo)
- .gitignore (novo)
- src/Ror2Meeeee.Core/Ror2Meeeee.Core.csproj (novo)
- src/Ror2Meeeee.Core/ModInfo.cs (novo)
- src/Ror2Meeeee/Ror2Meeeee.csproj (novo)
- src/Ror2Meeeee/Plugin.cs (novo)

## Change Log

- 2026-10-11: estrutura da solução, build, plugin hello world e git; validado em jogo; correção da leitura da versão do jogo (reflexão em `AutomatedBuildVersion`).
