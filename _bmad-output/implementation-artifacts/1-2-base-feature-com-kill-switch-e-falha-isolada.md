---
baseline_commit: NO_VCS
---

# Story 1.2: Base `Feature` com kill-switch e falha isolada

Status: done

<!-- Nota: validação é opcional. Rode a validação da story antes do gds-dev-story, se quiser. -->

## Story

Como autor do mod,
quero uma base de feature com liga/desliga e falha isolada,
para que um hook ruim nunca derrube a partida e cada alteração possa ser desligada (FR-4).

## Acceptance Criteria

1. **Given** a classe abstrata `Feature` (`Enable`, `Disable`, `ResetRunState`, `Fail`) e `Cfg` com a seção `General` (`VerboseLogging=false`, `MaxFailures=50`, mínimo 50)
   **When** o plugin inicia
   **Then** cada feature só é habilitada se o seu `ConfigEntry<bool>` estiver ligado
   **And** `Disable()` remove todos os hooks registrados por `Enable()`.
2. **Given** uma feature de teste com um hook que lança exceção
   **When** o hook é disparado
   **Then** a exceção é logada e contada em `Fail()`, e `orig` é chamado mesmo assim
   **And** ao atingir `MaxFailures` a feature é desligada com um único `Warning`.
3. **Given** um valor de config inválido (por exemplo `MaxFailures=3`)
   **When** a config é carregada
   **Then** o valor é corrigido para o padrão com `Warning`.
4. **Given** o início e o fim de uma run
   **When** os eventos da run disparam
   **Then** `ResetRunState()` de todas as features é chamado em um único ponto
   **And** cada hook registra sucesso ou falha no `Awake`.
5. **Sem regressão da Story 1.1** (critério derivado, não está no épico): `dotnet build -c Debug` com 0 erros e 0 avisos; o jogo chega ao menu principal; a linha `com.sawcunha.ror2meeeee v0.1.0 carregado; jogo 1.5.0#1210` continua no `LogOutput.log`.

## Tasks / Subtasks

- [x] Task 1: Referências do jogo no plugin (AC: #4, #5)
  - [x] Adicionar `Reference` a `RoR2.dll` via `$(GameDir)/Risk of Rain 2_Data/Managed/` (`Private=false`); só as DLLs extras que o compilador exigir (erro `CS0012`), nada além
  - [x] Adicionar `MMHOOK_RoR2.dll` via `$(R2modmanProfile)/BepInEx/plugins/MMHOOK/` (`Private=false`); `MonoMod.RuntimeDetour`/`MonoMod.Utils` 21.12.13.1 já chegam transitivos (`BepInEx.Core` → `HarmonyX 2.7.0`, presentes no cache NuGet; o build confirma); **não** adicionar pacote NuGet novo sem aprovação
  - [x] `dotnet build -c Debug` continua com 0 erros e 0 avisos
- [x] Task 2: `Cfg` (AC: #1, #3)
  - [x] `src/Ror2Meeeee/Common/Cfg.cs`: único lugar que chama `ConfigFile.Bind`; seção `General` com `VerboseLogging` (bool, `false`) e `MaxFailures` (int, padrão 50, mínimo 50)
  - [x] Valores lidos uma vez para campos em cache; atualizados só em `SettingChanged`
  - [x] Validação explícita de `MaxFailures < 50`: corrigir para 50 e logar `Warning` (não depender do clamp silencioso de `AcceptableValueRange`; ver Dev Notes)
  - [x] Método para a feature obter o seu `ConfigEntry<bool>` `Enabled` na própria seção (PascalCase), padrão `false`
- [x] Task 3: `Feature` (AC: #1, #2, #4)
  - [x] `src/Ror2Meeeee/Common/Feature.cs` conforme o esboço de `game-architecture.md#State Patterns`: `Enabled`, `Enable()`, `Disable()`, `OnEnable()`/`OnDisable()` abstratos, `ResetRunState()` virtual, `Fail(Exception)` protegido
  - [x] Construtor `protected Feature(string name, Cfg cfg, ManualLogSource log)`: `name` serve ao log e à seção de config; o `ConfigEntry<bool>` `Enabled` vem de `Cfg`
  - [x] `Enable()` envolve `OnEnable()` em try/catch: falha ⇒ `Error` no log, feature permanece desligada, plugin segue (não relança); sucesso ⇒ `Info` "hooks registrados"
  - [x] `Fail(e)`: loga a exceção, incrementa o contador **da própria feature**; ao atingir `MaxFailures` chama `Disable()` e loga **um único** `Warning` (guarda contra repetição). O contador **não** é zerado por `ResetRunState()` (ver Dev Notes)
  - [x] `Disable()` idempotente e seguro depois de `OnEnable` parcial
- [x] Task 4: Ligação no `Plugin` (AC: #1, #4)
  - [x] `Plugin.Awake` cria `Cfg`, a lista `_features` (vazia nesta story, ver Dev Notes) e habilita só as com `Enabled` ligado
  - [x] Inscrever `Run.onRunStartGlobal` e `Run.onRunDestroyGlobal` **uma vez** em `Plugin`, chamando um único método que percorre **todas** as `_features` (ligadas ou não) e chama `ResetRunState()` de cada uma (try/catch por feature, falha conta em `Fail`)
  - [x] Desinscrever os dois eventos e fazer `Disable()` das features em `OnDestroy`
  - [x] Manter intactos: o log de versão do `Awake`, `Plugin.Instance`, os três atributos, `GetBuildString()`
- [x] Task 5: Validação em jogo (AC: #1 a #5), cada subitem só vira `[x]` com a evidência colada nas Completion Notes
  - [x] Feature de teste temporária (ver Dev Notes) com hook que lança: confirmar no log exceção contada, `orig` executado e, no `MaxFailures`, um único `Warning`
  - [x] Config inválida (`MaxFailures=3` no `.cfg`): `Warning` e valor 50
  - [x] Iniciar e encerrar uma run: `ResetRunState` disparado nos dois eventos (usar `VerboseLogging=true` para a linha de log)
  - [x] Jogo chega ao menu principal com o plugin carregado (fecha o AC #3 da 1.1 que ficou sem evidência)
  - [x] Remover a feature de teste (ou deixá-la desligada por padrão, se o autor decidir; ver Dev Notes)

### Review Findings

Code review de 2026-10-11 (Blind Hunter, Edge Case Hunter, Acceptance Auditor). Diff por lista de arquivos (sem commits); Plugin.cs e o `.csproj` entram inteiros.

- [x] [Review][Decision] Estado final entregue não foi validado em jogo — o 3º boot rodou com o build que ainda tinha a `SelfTestFeature` (log com 106 menções a `SelfTest`; DLL implantada às 12:44:12, depois do boot). O AC #5 ("jogo chega ao menu", linha de versão) e a lista de features vazia só têm evidência de build. Opções: (a) um 4º boot rápido até o menu para confirmar `carregado; jogo 1.5.0#1210` sem erros do plugin; (b) aceitar com o build limpo como evidência.
- [x] [Review][Patch] Feature desligada (por config ou já passada do limite) continua contando falhas de `ResetRunState`: "falha 51/50…" a cada fronteira de run e, numa feature nunca habilitada, "desligada após 50 falhas" com `_shutdownByFailures` permanente. Fazer `Fail` retornar sem contar/logar de novo após o desligamento e não contar falha de reset em feature que não está `Enabled` (logar só) [src/Ror2Meeeee/Common/Feature.cs:239-256]
- [x] [Review][Patch] A descrição do `<Seção>.Enabled` diz só "Liga esta feature." mas o valor é lido uma vez no `Awake`; mudar em runtime não faz nada. Dizer "requer reiniciar o jogo" [src/Ror2Meeeee/Common/Cfg.cs:159]
- [x] [Review][Defer] Criação das features não é isolada: `Name` inválido para `ConfigDefinition` ou duplicado lança/compartilha o `Bind` e pode matar o `Awake` [src/Ror2Meeeee/Common/Feature.cs:197-203] — deferred, a lista de features está vazia; tratar na Story 2.1 ao registrar a primeira feature real (try/catch por feature no `Plugin` e checagem de nome duplicado)
- [x] [Review][Defer] `ConfigEnabled` lê `_enabledEntry.Value` a cada chamada, sem cache atualizado em `SettingChanged` (regra do `project-context.md`) [src/Ror2Meeeee/Common/Feature.cs:207] — deferred, lido uma vez no `Awake`, fora do caminho quente; cachear se o liga/desliga em runtime for implementado

## Dev Notes

### Escopo desta story

Cria **só** a infraestrutura: `Common/Cfg.cs`, `Common/Feature.cs` e a ligação no `Plugin.cs`. **Não** crie `Probe`, `Debug/`, `ror2meeeee_status` (Story 1.5), nem nenhuma feature real (`Features/LimitedCalc` etc. são do Épico 2) nem `tests/` (Story 1.4). Não crie `Common/Log.cs` vazio: as features recebem `ManualLogSource` por construtor (`game-architecture.md#Data Patterns`); só crie um helper de log se houver o que ele fazer (guarda de `VerboseLogging`).

**Nada vai para `Ror2Meeeee.Core`.** O `project-context.md` diz que código novo no `Core` sem teste não fecha a story, e o projeto de testes só nasce na 1.4. A contagem de falhas e a validação do mínimo ficam no plugin, onde já precisam de `ManualLogSource`.

### Estado atual do código (verificado em 2026-10-11)

- `src/Ror2Meeeee/Plugin.cs`: `[BepInPlugin]`, `[BepInDependency("com.bepis.r2api")]` (GUID confirmado na DLL), `[NetworkCompatibility(...)]`, `Instance`, `Awake` com log de versão e `Warning` de divergência; `GetBuildString()` com try/catch (corrigido no code review da 1.1). **Preservar tudo isso.**
- `src/Ror2Meeeee/Ror2Meeeee.csproj`: `BepInEx.Core 5.4.21`, `UnityEngine.Modules 2021.3.45`, `ProjectReference` ao `Core`, `Reference` a `R2API.Core` por `HintPath` com `$(R2modmanProfile)`. **Não tem** referência a `RoR2.dll` nem a `MMHOOK_RoR2.dll`: esta story é a primeira a precisar.
- `Directory.Build.props` já define `GameDir` e `R2modmanProfile` (sobrescrevíveis por `-p:`/env). `GameDir` existe mas nenhum `.csproj` o usava até agora.
- Não existem `Common/`, `Features/`, `Debug/` nem `tests/`.
- `RoR2.dll` contém os nomes `onRunStartGlobal` e `onRunDestroyGlobal` (confirmado por `grep -a`). **Não confirmados:** a classe dona (esperado `RoR2.Run`) e a assinatura (esperado `Action<Run>`); confirme ao compilar. O MMHOOK do perfil tem `add_onRunAmbientLevelUp` mas **não** os eventos de início/destruição: são eventos normais, assine com `+=` direto, não via `On.*`.

### Desenho (siga `game-architecture.md`, não reinvente)

- Esboço de `Feature` em `game-architecture.md#State Patterns`; exemplo de hook em `#Error Handling`; criação das features em `#Communication Patterns` (`_features.Add(new XFeature(cfg, log))`, depois `foreach ... if (f.Enabled) f.Enable()`; ajuste "Enabled" para "config ligada", pois `Enabled` da base é o estado atual).
- Cada feature recebe `Cfg` e `ManualLogSource` por construtor; **nenhum** outro singleton além de `Plugin.Instance`; uma feature não referencia outra.
- `Fail(Exception)` é o **único** ponto de contagem e desligamento. "Nunca engolir exceção sem contar."
- `MaxFailures`: padrão 50, **mínimo 50**. Lido do cache de `Cfg`, não por chamada de `ConfigFile`.
- **Contador de falhas:** um por feature, **não** global e **não** zerado por `ResetRunState()` nem por nova run. O kill-switch vale para a sessão; zerar por run faria uma feature quebrada falhar `MaxFailures` vezes em cada partida.
- **`ResetRunState()` roda em todas as features**, ligadas ou desligadas (por config ou por `MaxFailures`): é barato e evita cache velho se a feature for religada.
- **Escopo do `Enabled`:** os toggles são lidos localmente no `Awake`. O gate de host (`NetworkServer.active`) pertence aos hooks das features futuras; não tente ler "config do host" no `Awake`.
- `Fail` é caminho frio (só roda em exceção); alocar mensagem de log ali é aceitável. O **hook** em si continua sem alocação: nada de lambda capturando para chamar `Fail`.

### Armadilhas e pontos a confirmar

1. **Clamp silencioso.** `AcceptableValueRange<int>(50, int.MaxValue)` (usado no exemplo de `#Configuration`) provavelmente corrige o valor **sem log** ao ler do arquivo. O AC #3 exige `Warning`, então compare o valor lido com o mínimo e logue. Confirme no jogo com `MaxFailures=3` (não assuma).
2. **Hook parcial.** Se `OnEnable` lança no meio de vários hooks, os já registrados ficam ativos. `OnDisable` precisa remover todos de forma idempotente (remover hook não registrado não pode lançar) e `Enable()` deve chamá-lo na falha.
3. **Subscrição de evento estático.** `Run.onRunStartGlobal += X` mantém o plugin vivo e dispara mesmo depois de destruído se não houver `-=`; desinscreva em `OnDestroy`.
4. **Catálogos não estão prontos no `Awake`** (`project-context.md#Plugin e carga`): nada de buscar `ItemIndex` aqui. Não é necessário nesta story; só não introduza.
5. **`UnityEngine.Object`:** proibido `?.`, `??`, `== null`. Use `if (obj)`.
6. **Compilar contra `RoR2.dll`** pode exigir outras DLLs de `Managed/` (por exemplo `com.unity.multiplayer-hlapi.Runtime.dll` para tipos de rede) por herança de tipos. Adicione só o que o `CS0012` pedir, sempre com `Private=false`. Nunca copie DLL do jogo para o repositório (`.gitignore` já cobre `*.dll`).
7. **Itens adiados da revisão da 1.1 que esta story reabre:** `HintPath` dependente de `$(HOME)`/perfil r2modman e `GameDir` sem uso. Ao adicionar as referências do jogo, use `$(GameDir)`/`$(R2modmanProfile)` (nada de caminho de usuário no `.csproj`) e anote em `deferred-work.md` o que ainda ficar dependente de máquina. Aproveite para verificar se `BepInEx.dll` aparece na saída do build (`PrivateAssets`), item adiado também.
8. **Proton/Windows:** o plugin roda sob Wine; nenhum caminho absoluto Linux em código.
9. **Hipóteses não são fatos:** não assuma assinatura nem ordem de disparo dos eventos de run; registre o que observou no log.

### Feature de teste (para o AC #2)

Não há teste automatizado possível (depende de `RoR2.dll`). Crie uma feature **temporária** com um hook (`On.*` do MMHOOK) num método de `Run` ou outro tipo que o jogo chame com frequência e que exista no `MMHOOK_RoR2.dll` (verifique antes com `grep -a`/ILSpy; a descompilação formal é a Story 1.3). O hook faz `throw` dentro do try, chama `Fail(e)` e **sempre** chama `orig`. Para o teste de `MaxFailures`, o mínimo é 50, então a falha precisa disparar 50 vezes: um método por frame/tick resolve em poucos segundos.

Decisão que o dev fecha e registra: **remover** a feature de teste depois (padrão; nenhum arquivo extra) ou **mantê-la** em `Debug/` desligada por padrão, para repetir a validação depois de patches do jogo. Se mantiver, `Enabled=false` por padrão e sem custo no caminho quente.

### Project Structure Notes

Esta story cria o que está em negrito (de `game-architecture.md#Directory Structure`):

```
src/
├── Ror2Meeeee.Core/               # intocado
└── Ror2Meeeee/
    ├── Plugin.cs                  # alterado
    ├── **Common/**
    │   ├── **Cfg.cs**
    │   └── **Feature.cs**
    └── Ror2Meeeee.csproj          # alterado (Reference RoR2.dll, MMHOOK)
```

Arquivos `PascalCase.cs`, um tipo principal por arquivo; campos privados `_camelCase`; constantes `PascalCase`; hooks `On<Tipo><Método>`; config em PascalCase.

### Project Context Rules

Extraídas de `_bmad-output/project-context.md`, aplicáveis a esta story:

- Hook: try/catch, `orig` sempre, `Fail()`; remover todo hook em `Disable()` (ordem LIFO entre mods, nunca assumir ser o único).
- Hook que falha ao registrar: feature fica desligada com `Error` no log, **não** derruba o plugin.
- Lógica de gameplay só com `NetworkServer.active` (aplica-se às features futuras; a base não toca gameplay).
- Toda config via `Cfg`; ninguém chama `ConfigFile.Bind` solto. Valores em cache, atualizados em `SettingChanged`. Valor inválido: corrigir para o padrão com `Warning`.
- `ResetRunState()` ligado ao início e ao fim da run em um único ponto; todo cache por run é limpo ali.
- `VerboseLogging` padrão `false`; log não guardado no caminho quente é proibido; sem alocação por tick.
- Único singleton: `Plugin.Instance`; uma feature não referencia outra; só `Features/*` e `Debug/` tocam em tipos do RoR2 (a ligação de `Run.on*Global` no `Plugin` é a exceção mínima e deve ficar em um método só).
- `Ror2Meeeee.Core` nunca referencia jogo, BepInEx nem Unity; código novo no Core exige teste.
- Referências do jogo por DLLs locais de `Managed` via `GameDir`; **não** usar `RiskOfRain2.GameLibs`; não adicionar dependência NuGet sem aprovação.
- Não commitar DLLs do jogo, `MMHOOK_RoR2.dll` nem `BepInEx`.
- Não "engolir" exceção sem contar em `Fail()`.

### Inteligência da story anterior (1.1)

- Build cruzado `netstandard2.1` no Linux funciona; `dotnet build -c Debug` gera `.dll` + `.pdb`; copiar ambos (plugin e Core) para `BepInEx/plugins/Ror2Meeeee/` do perfil `Default`.
- `MiscFixes` e `SeekersPatcher` **ficam ativos** (decisão do autor); não peça para desativar.
- Lição da revisão: subitens só vão para `[x]` com evidência real. A 1.1 marcou "forçar divergência" e "chegou ao menu" sem fazê-lo e foi corrigida na revisão. Cole as linhas do `LogOutput.log` nas Completion Notes.
- `Application.version` devolve só `1.5.0`; o `#1210` vem de `AutomatedBuildVersion.buildVersionString` por reflexão (já tratado em `GetBuildString()`).
- Aviso esperado do R2API 5.3.0 sobre build id 1.4.1 vs 1.5.0: não bloqueia.
- O índice git está com tudo staged e **sem commits**; `.gitignore` cobre `bin/`, `obj/`, `BepInEx/`, `*.dll`, `docs/ebkr-r2modman-*`.

### Testing

- Sem teste automatizado (nada novo no `Core`; projeto de testes é da 1.4).
- Validação: `dotnet build -c Debug` 0 erros/0 avisos; no `LogOutput.log` do `Awake`, uma linha por feature (registrada ou falhou); feature de teste disparando `Fail` e desligando com um `Warning`; `MaxFailures=3` corrigido com `Warning`; `ResetRunState` visível ao iniciar e ao encerrar uma run; jogo chega ao menu.
- Cada story de hook termina com checagem de "hook registrou" no `Awake` e validação em jogo descrita aqui (`project-context.md#Testing Rules`).

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2: Base `Feature` com kill-switch e falha isolada]
- [Source: _bmad-output/game-architecture.md#Feature Structure, #Failure Handling & Logging, #Error Handling, #Logging, #Configuration, #State Patterns, #Data Patterns, #Communication Patterns]
- [Source: _bmad-output/project-context.md#Engine-Specific Rules, #Code Organization Rules, #Critical Don't-Miss Rules]
- [Source: _bmad-output/implementation-artifacts/1-1-plugin-hello-world-carregando-no-jogo.md#Review Findings]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md]

## Dev Agent Record

### Agent Model Used

claude-sonnet-5-5

### Debug Log References

- `dotnet build -c Debug`: 0 avisos, 0 erros, na primeira compilação com `RoR2.dll` + `MMHOOK_RoR2.dll` referenciados. Não foi preciso nenhuma DLL extra de `Managed/` (nenhum `CS0012`).
- Confirmado ao compilar: `Run.onRunStartGlobal`/`Run.onRunDestroyGlobal` aceitam um método `void(Run)`; existe `On.RoR2.Run.FixedUpdate` / `orig_FixedUpdate` no MMHOOK.
- Saída do build só tem `Ror2Meeeee(.Core).dll/.pdb` e `.deps.json`: `BepInEx.dll`, `RoR2.dll` e MMHOOK **não** são copiados (`Private=false`, `IncludeAssets="compile"`).
- `.dll` + `.pdb` (plugin e Core) copiados para `BepInEx/plugins/Ror2Meeeee/` do perfil `Default`. O `.cfg` do mod ainda não existe (criado no 1º boot).

### Completion Notes List

- Tasks 1 a 4 implementadas e compilando. **Task 5 (validação em jogo) pendente**: exige rodar o jogo; nenhum subitem foi marcado sem evidência.
- Evidência do `LogOutput.log` (boots de 2026-10-11, perfil `Default`, `MiscFixes`/`SeekersPatcher` ativos):
  - 1º boot, `SelfTest.Enabled=false`: `Ror2Meeeee] SelfTest: desligada por config`; `Chainloader startup complete`; nenhum `Error` do plugin.
  - 2º boot, com `MaxFailures = 3`, `SelfTest.Enabled = true`, `VerboseLogging = true`:
    ```
    [Warning:Ror2Meeeee] General.MaxFailures=3 inválido (mínimo 50); usando 50.
    [Debug  :RoR2BepInExPack] OnHook Ror2Meeeee.SelfTestFeature.OnRunFixedUpdate added ... for: RoR2.Run.FixedUpdate
    [Info   :Ror2Meeeee] SelfTest: hooks registrados
    [Debug  :Ror2Meeeee] ResetRunState de todas as features
    [Info   :Ror2Meeeee] SelfTest: ResetRunState
    [Error  :Ror2Meeeee] SelfTest: falha 1/50: System.InvalidOperationException: falha induzida pelo SelfTest
    ... (falhas 2 a 49)
    [Error  :Ror2Meeeee] SelfTest: falha 50/50: ...
    [Debug  :RoR2BepInExPack] OnHook Ror2Meeeee.SelfTestFeature.OnRunFixedUpdate removed ...
    [Warning:Ror2Meeeee] SelfTest: desligada após 50 falhas.
    ```
    Contagens no log: 50 linhas `falha N/50`, **1** `desligada após`. O `Warning` do `MaxFailures` apareceu uma vez.
  - Menu: `BaseMainMenuScreen: OnEnter()` e `Risk of Rain 2 build: ver. 1.5.0#1210*` na mesma sessão.
  - `orig` chamado: evidência indireta (a fase `blackbeach` carregou e rodou durante os 50 FixedUpdate com o hook lançando); a garantia estrutural é o `orig(self)` fora do try.
  - `Error` de `Unity Log` (`InvalidKeyException`, "connection already set as ready", gates de `blackbeach`) não vêm do plugin; não foram atribuídos a outro mod nem ao jogo base (sem boot de controle).
- ~~Lacuna do fim da run~~ **Fechada no 3º boot** (iniciar run, sair para o menu, fechar): 2 ocorrências de `ResetRunState de todas as features` + `SelfTest: ResetRunState`, a 1ª no início da run (log linha 410) e a 2ª ao sair para o menu (linha 529), logo antes de `BaseMainMenuScreen: OnEnter()` (linha 543). No 2º boot o jogo foi fechado dentro da run, por isso o fim não aparecia.
- `SelfTestFeature` **removida** (decisão do autor), junto com a linha em `Plugin.cs` e a pasta `Features/`. `dotnet build -c Debug`: 0 erros, 0 avisos. `.dll`/`.pdb` reimplantados e `.cfg` do perfil restaurado (`VerboseLogging=false`, `MaxFailures=50`, seção `SelfTest` removida).
- ~~Boot com a lista de features vazia não verificado~~ **Verificado no 4º boot** (code review, DLL final de 12:49:59, log de 12:51:13, 0 menções a `SelfTest`):
  ```
  [Info   :   BepInEx] Loading [Ror2Meeeee 0.1.0]
  [Info   :Ror2Meeeee] com.sawcunha.ror2meeeee v0.1.0 carregado; jogo 1.5.0#1210
  [Info   : Unity Log] BaseMainMenuScreen: OnEnter()
  [Info   : Unity Log] Risk of Rain 2 build: ver. 1.5.0#1210*
  ```
  Nenhum `Error` nem `Warning` do plugin. `.cfg` do perfil com só `General.VerboseLogging=false` e `General.MaxFailures=50`.
- Decisões de implementação:
  - `Feature` tem `OnEnable`/`OnDisable` abstratos; `Enable()` captura exceção, loga `Error`, remove hooks parciais e mantém o plugin vivo; `Enable()` não religa uma feature desligada por falhas (kill-switch vale para a sessão).
  - Contador de falhas por feature, não zerado por `ResetRunState()`; `Fail()` desliga uma única vez com um `Warning`.
  - `ResetRunStateSafe()` (internal) envolve `ResetRunState()` em try/catch e conta em `Fail`, para o `Plugin` não ter acesso a `Fail` protegido.
  - `Cfg` **não** usa `AcceptableValueRange` em `MaxFailures`: a validação é explícita para garantir o `Warning`. Se `MaxFailures < 50`, corrige para 50 e grava no `ConfigEntry`.
  - `Plugin` referencia `RoR2.Run` apenas para os dois eventos de run (única exceção à regra "só `Features/*` e `Debug/` tocam em tipos do RoR2"), em um método único `OnRunBoundary`.
  - Feature temporária `SelfTest` em `Features/SelfTest/`, desligada por padrão (`SelfTest.Enabled=false`): hook em `On.RoR2.Run.FixedUpdate` que lança e chama `Fail`, sempre com `orig`.
- Itens adiados da 1.1 resolvidos: `GameDir` agora é usado (`RoR2.dll`); `BepInEx.dll` não é copiado para a saída. `HintPath` do R2API.Core e do MMHOOK ainda dependem do perfil r2modman (`$(R2modmanProfile)`, sobrescrevível por `-p:`).

### File List

- src/Ror2Meeeee/Ror2Meeeee.csproj (alterado: `Reference` a `RoR2` e `MMHOOK_RoR2`)
- src/Ror2Meeeee/Plugin.cs (alterado: `Cfg`, lista de features, eventos de run, `OnDestroy`)
- src/Ror2Meeeee/Common/Cfg.cs (novo; ajustado no code review: descrição do `Enabled`)
- src/Ror2Meeeee/Common/Feature.cs (novo; ajustado no code review: `Fail` idempotente após o desligamento, reset de feature desligada não conta)

## Change Log

- 2026-10-11: code review: `Fail` idempotente após o desligamento; falha de `ResetRunState` em feature desligada só é logada; descrição do `Enabled` informa que requer reinício.
- 2026-10-11: `Cfg`, `Feature` e ligação no `Plugin` (kill-switch, falha isolada, reset por run); referências a `RoR2.dll`/MMHOOK; validado em jogo com uma feature `SelfTest` temporária, depois removida.
