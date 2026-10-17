---
title: 'Game Architecture'
project: 'ror2'
date: '2026-10-11'
author: 'Sawcunha'
version: '1.0'
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8, 9]
status: 'complete'
engine: 'Unity 2021.3.45f2 (embutida no RoR2) + BepInEx 5 / RoR2BepInExPack 1.45.0'
platform: 'RoR2 1.5.0 (#1210); desenvolvimento Linux, execução Windows/Proton'

# Source Documents
gdd: '_bmad-output/planning-artifacts/gdds/gdd-ror2-2026-10-11/gdd.md'
epics: '_bmad-output/planning-artifacts/gdds/gdd-ror2-2026-10-11/epics.md'
brief: null
---

# Game Architecture

## Document Status

Documento criado pelo workflow GDS Architecture. Status: **completo**.

**Steps Completed:** 9 of 9 (Complete)

---

## Executive Summary

**ror2 (Ror2Meeeee)** é um mod C# para Risk of Rain 2, construído sobre
**BepInEx 5 + RoR2BepInExPack 1.45.0 + R2API** na Unity do jogo (2021.3.45f2,
jogo 1.5.x, build Steam 25475991). Desenvolvimento em Linux, execução em Windows
(ou Proton, nesta máquina).

**Key Architectural Decisions:**

- **Uma `Feature` por mecânica** (M1 a M5), com kill-switch individual e falha
  isolada (try/catch, `orig` sempre, auto-desligamento a partir de 50 falhas)
- **Host autoritativo, sem sincronização de config na v1**: a lógica de
  gameplay roda só no host
- **Lib `Core` pura e testável** (`netstandard2.1`, sem referência ao jogo),
  separada do plugin
- **Medição antes de otimizar**: `Probe` próprio com baseline em 3 condições
- **DLLs locais do jogo**, porque o GameLibs do NuGet está defasado

**Project Structure:** organização por feature com 9 sistemas centrais.

**Implementation Patterns:** 9 padrões definidos (6 padrão + 3 novos: pipeline
de pickup sem recursão, efeito limitado por item, estado por run).

**Open Items:** `Inventory.GiveItem` server-only, causa real dos gargalos,
custo do hook, fórmula de M1 (todos validados no Épico 1).

**Ready for:** criação de épicos e stories; o Épico 1 é o portão das decisões
em aberto.

---

## Project Context

### Game Overview

**ror2** - Mod C# para Risk of Rain 2 que torna partidas longas em grupo jogáveis:
limita o cálculo de efeitos por stack, multiplica itens coletados e compensa com
dificuldade maior.

### Technical Scope

**Plataforma:** RoR2 versão 1.5.0 (log do jogo: `ver. 1.5.0#1210*`; Steam build
25475991, out/2026; o "1.21" do addendum provavelmente é o "#1210" do build); jogo instalado nesta máquina
Linux (build Windows, Proton); execução alvo em Windows (r2modman),
desenvolvimento em Linux
**Gênero:** Roguelike co-op (mod; sem conteúdo novo)
**Nível do projeto:** Baixo/médio em volume de código, alto em risco técnico
(hooks em código de terceiros sem fonte pública)

### Core Systems

| Sistema                                       | Complexidade | Referência GDD   |
| --------------------------------------------- | ------------ | ---------------- |
| Medição (frame time, RAM, contadores)         | Média        | Épico 1 / FR-1   |
| Cálculo limitado (Fungus, Fireworks)          | Alta         | M1 / FR-2, FR-3  |
| Multiplicação de itens + elegibilidade        | Média        | M2 / FR-6/7/8    |
| Dificuldade (4 eixos, fator único)            | Média        | M3 / FR-11       |
| Corte de partículas                           | Baixa/média  | M4 / FR-12/13    |
| Configuração (host autoritativo)              | Média        | NFR-2 / FR-4     |
| Autoridade do host e sincronização de config  | Média/alta   | NFR-2            |
| Compartilhamento (v2)                         | Média        | M5 / FR-9/10     |
| Registro extensível de itens limitados        | Baixa        | FR-5             |

### Technical Requirements

- Stack: BepInEx + RoR2BepInExPack 1.45.0, R2API (Items/Language/Networking),
  HookGenPatcher/MMHOOK, Risk of Options opcional
- Multiplayer: todos com o mod; host autoritativo; sem tráfego por tick;
  sem dessincronização
- Lógica autoritativa de itens (multiplicação, compartilhamento, dificuldade)
  executa só no host (`NetworkServer.active`) e lê o config do host; sem
  sincronização de config na v1 (contingência: `INetMessage` se um efeito de M1
  rodar no cliente); configs de efeito local (partículas, log, overlay) ficam
  por cliente
- Todo hook preserva a chamada a `orig`, tem kill-switch individual (FR-4) e
  falha de forma isolada (try/catch com auto-desligamento)
- Verificação de versão do jogo e do mod no início da run
- Performance: sem alocação por tick nos caminhos alterados; log verboso
  desligado; metas numéricas só após a linha de base
- Build cruzado Linux → Windows (.dll + .pdb copiados para o perfil r2modman)

### Complexity Drivers

- Hooks em código de jogo sem fonte pública (precisa descompilar RoR2.dll)
- Autoridade do host e sincronização (`Inventory.GiveItem` server-only?)
- Ordem de operações pickup → multiplicação → compartilhamento, sem recursão
- Itens multiplicados interagindo com o teto de stacks e o cálculo limitado
- Troca de abordagem possível (manter quantidade vs agregar) só definida
  depois da medição
- O próprio hook no caminho quente pode custar mais que o ganho; o custo do
  hook vazio entra na linha de base

### Technical Risks

- Suposições de baixa confiança e alto impacto, a validar nos Épicos 1 e 2:
  causa real dos gargalos (A2), `Inventory.GiveItem` server-only (A1), custo
  do hook (A6), efeitos de M1 rodarem só no servidor (A8)
- Hooks podem deixar de disparar em silêncio após patch (mitigação: registro
  de sucesso/falha por hook no `Awake`)
- Critério de "efeito reconhecível" sem métrica (definir após a linha de base)
- `dotnet build` no Linux: SDK .NET **não instalado** nesta máquina e build
  cruzado não testado
- Sem profiler externo confirmado para o RoR2 retail
- Quebra por patches futuros e conflito com outros mods (aceito, fora de escopo)
- Primeiro mod do autor, sem C#

## Engine & Framework

### Selected Engine

**Unity (embutida no Risk of Rain 2)**, alvo de modding **BepInEx 5** via
**RoR2BepInExPack 1.45.0**, **R2API** (Items, Language, Networking) e
**HookGenPatcher/MMHOOK**.

**Rationale:** o jogo base é Unity e o mod se acopla a ele em tempo de execução.
Não há escolha de engine. O framework segue o ecossistema Risk of Thunder, que é
o suporte dominante e documentado para RoR2.

### Project Initialization

Ponto de partida: template `risk-of-thunder/ExamplePlugin`
(`netstandard2.1`, `LangVersion preview`, `BepInEx.Core 5.4.21`,
`UnityEngine.Modules 2021.3.45`; o template fixa 2021.3.33, mas o
RoR2BepInExPack 1.45.0 exige >= 2021.3.45, senão o restore falha com NU1605).
`BepInEx.Core` vem do feed `https://nuget.bepinex.dev/v3/index.json`
(`RestoreAdditionalProjectSources`); do nuget.org ele é incompatível.

```bash
dotnet build -c Debug   # no Linux; copiar .dll + .pdb para o perfil r2modman (Windows)
```

Primeiro marco: "hello world" compilado no Linux e carregado no Windows.
`RiskOfRain2.GameLibs` (NuGet 1.3.1.275-r.0, 9/2/2024) está defasada em
relação à versão 1.5.x: **usar referências locais** às DLLs de
`~/.steam/debian-installation/steamapps/common/Risk of Rain 2/Risk of Rain 2_Data/Managed`
(Unity **2021.3.45f2**). `MMHOOK_RoR2.dll` deve ser
gerado localmente pelo HookGenPatcher, já que o NuGet `MMHOOK.RoR2` tende a
estar defasado também.

### Engine-Provided Architecture

| Component        | Solution                                  | Notes                                           |
| ---------------- | ----------------------------------------- | ----------------------------------------------- |
| Rendering/VFX    | Unity + sistema de partículas do jogo     | O mod só desliga ou encurta (M4)                |
| Physics          | Unity + RoR2 (projéteis, colisão)         | Não alterado (FR-13)                            |
| Networking       | UNet legado do RoR2 + R2API.Networking    | Host autoritativo; mensagens `INetMessage`      |
| Itens/Inventário | `RoR2.Inventory`, `ItemCatalog`           | Concessão pelo host; `GiveItem` a verificar     |
| Config           | `ConfigFile` do BepInEx; Risk of Options  | Menu in-game opcional                           |
| Hooking          | MonoMod `On.*` / IL via MMHOOK            | Sempre chamar `orig`                            |
| Build            | `dotnet build` (netstandard2.1)           | Cruzado Linux → Windows, a validar; SDK a instalar |

### Development Tooling

- MCPs de Unity Editor: **não aplicáveis** (não há projeto nem editor Unity)
- Opcionais: Context7 (documentação atual de BepInEx/R2API) e descompilador
  (ILSpy/`ilspycmd`) para ler `RoR2.dll`; versões não verificadas

### Remaining Architectural Decisions

- Estratégia de hooks e de falha isolada (kill-switch, auto-desligamento)
- Modelo de medição (overlay, Stopwatch por hook, contadores)
- Estrutura do projeto e módulos por feature
- Modelo de configuração (host-autoritativa vs local) e sua sincronização
- Estratégia de cálculo limitado (manter quantidade vs agregar), após o Épico 1
- Estratégia de testes (o que dá para testar fora do jogo)
- Compatibilidade de versão e verificação em runtime

## Architectural Decisions

### Decision Summary

| Categoria            | Decisão                                                           | Versão / Fonte                         | Rationale                                         |
| -------------------- | ----------------------------------------------------------------- | -------------------------------------- | ------------------------------------------------- |
| Estrutura            | Uma classe `Feature` por mecânica (`Enable()`/`Disable()`)        | n/a                                    | Kill-switch individual (FR-4), extensível (FR-5)  |
| Autoridade / rede    | Lógica no host, config do host, **sem sync de config na v1**      | R2API.Networking só como contingência  | Sem tráfego nem dessincronização (NFR-2)          |
| Medição              | Classe `Probe` própria: Stopwatch por hook, p95, CSV por fase     | n/a                                    | Mede o hook vazio; profiler externo não confirmado |
| Cálculo limitado     | Estratégia por item: `Off` / `Clamp` / `Aggregate`                | n/a                                    | Testa as 2 hipóteses sem refatorar                |
| Falha e log          | try/catch, sempre `orig`, auto-desligamento, `ManualLogSource`    | BepInEx.Core 5.4.21                    | Um hook ruim não derruba a run                    |
| Testes               | Lógica pura em lib separada + `dotnet test`; resto em jogo        | xunit.v3 3.2.2*, runner 3.1.5*         | Roda sem o jogo                                   |
| Referências do jogo  | DLLs locais de `Managed`, MMHOOK gerado pelo HookGenPatcher       | Unity 2021.3.45f2, jogo 1.5.x          | GameLibs NuGet defasado                           |
| Persistência         | `ConfigFile` do BepInEx; Risk of Options opcional                 | RoR2BepInExPack 1.45.0                 | O jogo salva; o mod só tem config                 |
| N/A                  | Save system, asset loading, cenas, UI própria, áudio              | n/a                                    | Fornecidos pelo jogo                              |

*versões de espelho de terceiros; confirmar no nuget.org antes de fixar.

### Feature Structure

**Approach:** `abstract class Feature { Enable(); Disable(); }`, uma por mecânica
(M1 a M5), mantidas em uma lista no plugin e ligadas por `ConfigEntry<bool>`.
Os itens de M1 são entradas de uma lista dentro da feature, não classes novas.

### Authority & Networking

**Approach:** toda lógica de gameplay roda com `NetworkServer.active` e lê o
config do host. Sem `INetMessage` na v1. Contingência: se a descompilação
mostrar que um efeito de M1 roda no cliente, sincronizar só esse valor.
Verificação de versão do jogo e do mod ao entrar na partida.

### Measurement

**Approach:** `Probe` estático com buffers pré-alocados (sem alocação por tick):
Stopwatch por hook, contadores (projéteis, procs), p95. Saída em CSV por fase
e overlay opcional. Desligada por padrão (NFR-3). Inclui medir o **hook vazio**.

**Condições da linha de base (FR-1):**
1. Sem o mod: jogo sem o plugin, medido pelo FPS/contadores nativos ou overlay do BepInEx
2. Mod sem multiplicação: plugin com `Probe` ligado e `Multiplier.Enabled = false`
3. Mod com multiplicação: tudo ligado

O custo do próprio `Probe` (hook vazio) é registrado à parte.

### Limited Calculation

**Approach:** estratégia por item com modo configurável. Só serão implementados
os modos que o Épico 1 justificar; fórmula e limite ficam para depois da
linha de base.

### Failure Handling & Logging

Todo hook em try/catch, chama `orig` sempre, conta falhas e desliga a própria
feature ao passar de um limite. Cada hook registra sucesso ou falha no `Awake`.
Log verboso desligado por padrão.

### Testing

Lógica pura (fórmula de limite, tabela de elegibilidade, matemática do
multiplicador) em biblioteca `netstandard2.1` sem referência ao jogo, testada
por projeto `net8.0`. O restante é validado em jogo com cenário de seed fixo.

### Deferred

- Verificação de versão em runtime (detalhes)
- Menu in-game via Risk of Options (dependência suave)

### Architecture Decision Records

- **ADR-1:** host-only sem sync de config. Substitui a ideia inicial de enviar
  config no início da run; reabrir se A8 falhar.
- **ADR-2:** DLLs locais em vez de GameLibs NuGet, por defasagem (1.3.1 vs 1.5.x).
- **ADR-3:** `Probe` próprio, porque anexar profiler externo ao RoR2 retail não
  foi confirmado.

## Cross-cutting Concerns

Estas regras valem para TODO o código do mod e são obrigatórias para qualquer
agente que implemente uma story.

### Error Handling

**Strategy:** try/catch na borda de cada hook, com contador por feature e
auto-desligamento. Result objects apenas na lib de lógica pura.

**Regras:**
- Erro nunca pausa o jogo nem aparece ao jogador; vai ao log
- Exceção em hook: registra, incrementa o contador e **chama `orig` mesmo assim**
- Ao atingir `MaxFailures` (padrão 50, mínimo 50, configurável): `Disable()` da
  feature e um único `Warning`
- Nunca engolir exceção sem contar

**Example:**

```csharp
// Tipo e método ilustrativos; confirmar na descompilação
private void OnHook(On.RoR2.SomeComponent.orig_FixedUpdate orig, SomeComponent self)
{
    if (Enabled)
    {
        try { /* lógica da feature */ }
        catch (Exception e) { Fail(e); }   // conta, loga, auto-desliga
    }
    orig(self);                            // sempre
}
```

`Feature.Fail(Exception)` é o único ponto de contagem e desligamento.

### Logging

**Format:** texto simples via `ManualLogSource` do BepInEx.
**Destination:** console do BepInEx e `LogOutput.log`.

| Nível   | Uso                                                              |
| ------- | ---------------------------------------------------------------- |
| Error   | Feature desligada por falhas; hook que não registrou             |
| Warning | Divergência de versão; config inválida corrigida                 |
| Info    | Marcos: plugin carregado, versão do jogo, features ativas        |
| Debug   | Somente com `VerboseLogging = true` (padrão `false`)             |

**Regra de caminho quente:** nenhum log por tick sem guarda; a mensagem só é
montada **dentro** da guarda (sem alocação, NFR-4).

```csharp
if (Cfg.Verbose) Log.LogDebug($"{Name}: stacks={stacks} -> {effective}");
```

### Configuration

**Approach:** `ConfigFile` do BepInEx, uma seção por feature.

**Structure:**

| Seção        | Escopo | Exemplos de chaves                                         |
| ------------ | ------ | ---------------------------------------------------------- |
| `General`    | Local  | `VerboseLogging`, `ProbeEnabled`, `OverlayEnabled`, `MaxFailures` (>= 50) |
| `LimitedCalc`| Host   | `Enabled`, modo por item (`Off`/`Clamp`/`Aggregate`)       |
| `Multiplier` | Host   | `Value` (padrão 2), `IncludeLunar`, `IncludeVoid`, `StackCap` |
| `Difficulty` | Host   | `Factor`, ganhos por eixo                                  |
| `Particles`  | Local  | `Enabled`, lista de efeitos (desvio consciente do GDD: o corte visual não altera gameplay, FR-13, então cada jogador decide) |
| `Sharing`    | Host   | `Enabled` (v2)                                             |

**Regras:**
- Configs "Host" são lidas só quando `NetworkServer.active` (ADR-1)
- Lidas **uma vez** para campos em cache; atualizadas só em `SettingChanged`,
  nunca por tick
- Valor inválido: corrigido para o padrão com `Warning`

```csharp
MaxFailures = cfg.Bind("General", "MaxFailures", 50,
    new ConfigDescription("Falhas antes de desligar uma feature",
        new AcceptableValueRange<int>(50, int.MaxValue)));
```

### Event System

**Pattern:** sem barramento próprio. Features se inscrevem direto em hooks e
eventos do jogo; apenas o `Plugin` cria e destrói features.

**Regras:**
- Uma feature **não chama outra**
- A ordem pickup → multiplicação → compartilhamento é uma **função pura de
  pipeline** no ponto do pickup, não uma cadeia de eventos
- Todo hook é removido em `Disable()` (simetria com `Enable()`)

**Event Naming:** não há eventos próprios; métodos de hook seguem
`On<Tipo><Método>` (por exemplo `OnInventoryGiveItem`).

### Debug Tools

| Ferramenta        | Ativação                          | Observação                                    |
| ----------------- | --------------------------------- | --------------------------------------------- |
| `Probe` (CSV)     | `General.ProbeEnabled`            | CSV por fase, p95, mede o hook vazio          |
| Overlay           | `General.OverlayEnabled`          | Frame time e contadores                       |
| `ror2meeeee_status`  | Console do jogo                   | Lista features, falhas e hooks registrados    |
| Cenário de teste  | `give_item`, seed fixo            | Procedimento do Épico 1                       |

```csharp
// API do ConCommand a verificar na descompilação
[ConCommand(commandName = "ror2meeeee_status", flags = ConVarFlags.None,
    helpText = "Lista features, falhas e hooks do mod")]
private static void CCStatus(ConCommandArgs args) { /* ... */ }
```

Tudo desligado por padrão e sem custo no caminho quente quando desligado.

## Project Structure

### Organization Pattern

**Pattern:** por feature, com lib de lógica pura separada.

**Rationale:** casa com a decisão de uma `Feature` por mecânica (M1 a M5), permite
trabalho isolado por agente e mantém a lógica testável fora do jogo.

### Directory Structure

```
ror2/
├── Ror2Meeeee.sln
├── Directory.Build.props          # GameDir, LangVersion, versões comuns
├── src/
│   ├── Ror2Meeeee.Core/           # netstandard2.1, SEM referência ao jogo
│   │   ├── Limits/                # fórmulas Clamp/Aggregate
│   │   ├── Eligibility/           # tabela de elegibilidade
│   │   └── Pipeline/              # pickup -> multiplicação -> compartilhamento
│   └── Ror2Meeeee/                # plugin BepInEx, netstandard2.1
│       ├── Plugin.cs              # cria/destrói features, carrega config
│       ├── Common/                # Feature, Probe, Cfg, Log
│       ├── Features/
│       │   ├── LimitedCalc/       # M1
│       │   ├── Multiplier/        # M2
│       │   ├── Difficulty/        # M3
│       │   ├── Particles/         # M4
│       │   └── Sharing/           # M5 (v2)
│       └── Debug/                 # overlay, ConCommand ror2meeeee_status
├── tests/
│   └── Ror2Meeeee.Core.Tests/     # net8.0, xunit.v3
├── docs/
└── _bmad-output/
```

**Identificadores:** namespace `Ror2Meeeee`; GUID `com.sawcunha.ror2meeeee`;
nome do plugin `Ror2Meeeee`.

### System Location Mapping

| System                | Location                                  | Responsibility                          |
| --------------------- | ----------------------------------------- | --------------------------------------- |
| Medição               | `Ror2Meeeee/Common/Probe.cs`              | Stopwatch por hook, p95, CSV            |
| Cálculo limitado (M1) | `Features/LimitedCalc/` + `Core/Limits/`  | Hooks no jogo; fórmula pura no Core     |
| Multiplicação (M2)    | `Features/Multiplier/` + `Core/Eligibility/` | Hook no pickup; regras puras no Core |
| Dificuldade (M3)      | `Features/Difficulty/`                    | Hooks de spawn e stats                  |
| Partículas (M4)       | `Features/Particles/`                     | Lista configurável                      |
| Compartilhamento (M5) | `Features/Sharing/` + `Core/Pipeline/`    | Usa o pipeline puro                     |
| Config                | `Common/Cfg.cs`                           | `ConfigFile`, cache, `SettingChanged`   |
| Falha e log           | `Common/Feature.cs`, `Common/Log.cs`      | `Fail()`, contador, desligamento        |
| Debug                 | `Ror2Meeeee/Debug/`                       | Overlay e ConCommand                    |

### Naming Conventions

#### Files
`PascalCase.cs`, um tipo principal por arquivo (`MultiplierFeature.cs`).

#### Code Elements

| Element                 | Convention                       | Example                          |
| ----------------------- | -------------------------------- | -------------------------------- |
| Classes e métodos       | PascalCase                       | `StackLimiter.Apply`             |
| Campos privados         | `_camelCase`                     | `_failureCount`                  |
| Constantes              | PascalCase                       | `DefaultMultiplier`              |
| Hooks                   | `On<Tipo><Método>`               | `OnInventoryGiveItem`            |
| Seções/chaves de config | PascalCase                       | `Multiplier.IncludeLunar`        |
| Testes                  | `<Método>_<Cenário>_<Resultado>` | `Apply_AboveCap_ReturnsCap`      |

#### Game Assets
Não há assets, cenas nem prefabs; o mod não adiciona conteúdo.

### Architectural Boundaries

1. `Ror2Meeeee.Core` nunca referencia o jogo, BepInEx nem Unity.
2. Uma feature não referencia outra feature; só `Common` e `Core`.
3. Só `Features/*` e `Debug/` tocam em tipos do RoR2.
4. Toda configuração passa por `Cfg`; ninguém chama `ConfigFile.Bind` solto.
5. `GameDir` fica em `Directory.Build.props`, nunca como caminho absoluto em `.csproj`.
6. Repositório de terceiros (`yekoc`, GPL/LGPL) é só referência de estudo; não copiar código.

## Implementation Patterns

Estes padrões garantem consistência entre todos os agentes de IA.

### Novel Patterns

#### N1. Pipeline de pickup (M2 + M5)

**Purpose:** multiplicar e compartilhar itens sem recursão (o hook não reage às
concessões do próprio mod).

**Components:**
- `Core/Pipeline`: função pura `Resolve(PickupContext, MultiplierRules)` que
  devolve a lista de concessões (`Grant`)
- `Features/Multiplier` e `Features/Sharing`: aplicam as concessões, com a guarda
  de reentrância

**Data Flow:** pickup original → `orig` → `Resolve` (multiplicação, depois
compartilhamento) → `GiveRaw` por concessão, com `_granting = true`.
Itens recebidos por compartilhamento não são multiplicados nem compartilhados
de novo.

**Implementation Guide:**

```csharp
// Core: puro e testável
IReadOnlyList<Grant> Resolve(PickupContext ctx, MultiplierRules rules);

// Feature (host). `GiveRaw` deve chamar `Inventory.GiveItemPermanent` (GiveItem é obsoleto)
if (_granting || !NetworkServer.active) { orig(self, itemIndex, count); return; }
orig(self, itemIndex, count);
_granting = true;
try { foreach (var g in Pipeline.Resolve(ctx, _rules)) GiveRaw(g); }
catch (Exception e) { Fail(e); }
finally { _granting = false; }
```

**Usage:** toda concessão feita pelo mod passa por `GiveRaw` com a guarda ativa.
Assinaturas do RoR2 acima são ilustrativas, exceto `Inventory.GiveItemPermanent(ItemIndex, int)`,
confirmado na descompilação.

#### N2. Efeito limitado por item (M1)

**Purpose:** impedir que o custo cresça com os stacks, mantendo o efeito.

**Components:** `IStackPolicy` no Core (`Off`, `Clamp`, `Aggregate`); a feature só
troca o número de stacks efetivo ou agrupa o trabalho, **sem reimplementar o item**.

**Status:** fórmula e limite pendentes do Épico 1.

#### N3. Estado por run

**Purpose:** evitar vazamento de memória entre partidas.

**Rule:** toda feature implementa `ResetRunState()`, chamado em um único ponto no
início e no fim da run. Caches por jogador ou por objeto são limpos ali.

### Communication Patterns

**Pattern:** hooks diretos e funções puras; sem barramento de eventos.

```csharp
// Plugin cria, features não se conhecem
_features.Add(new MultiplierFeature(cfg, log));
_features.Add(new LimitedCalcFeature(cfg, log));
foreach (var f in _features) if (f.Enabled) f.Enable();
```

### Entity Patterns

**Creation:** N/A. O mod não cria inimigos, itens nem objetos; concede itens pela
API do jogo.

### State Patterns

**Pattern:** ciclo `Enable`/`Disable` da feature + `ResetRunState()` por run.
Sem máquinas de estado por entidade.

```csharp
public abstract class Feature
{
    public bool Enabled { get; private set; }
    public void Enable()  { if (Enabled) return; OnEnable();  Enabled = true;  }
    public void Disable() { if (!Enabled) return; OnDisable(); Enabled = false; }
    protected abstract void OnEnable();
    protected abstract void OnDisable();      // remove TODOS os hooks
    public virtual void ResetRunState() { }
    protected void Fail(Exception e) { /* conta, loga, auto-desliga */ }
}
```

### Data Patterns

**Access:** somente via `Cfg` (config) e `Probe` (medição). Sem arquivos próprios
além do CSV do Probe. Único singleton permitido: `Plugin.Instance`; features
recebem `Cfg` e `Log` por construtor.

```csharp
public sealed class MultiplierFeature : Feature
{
    private readonly Cfg _cfg;
    public MultiplierFeature(Cfg cfg, ManualLogSource log) { _cfg = cfg; }
}
```

### Consistency Rules

| Pattern        | Convention                                         | Enforcement                       |
| -------------- | -------------------------------------------------- | --------------------------------- |
| Hook           | try/catch + `orig` sempre + `Fail()`               | Revisão de código                 |
| Host-only      | `NetworkServer.active` na primeira linha           | Revisão de código                 |
| Concessão      | Só via `GiveRaw` com guarda `_granting`            | Revisão de código                 |
| Estado         | `ResetRunState()` por feature                      | Método virtual na base            |
| Caminho quente | Sem `new`, sem LINQ, sem log não guardado          | Revisão + Probe                   |
| Singleton      | Apenas `Plugin.Instance`                           | Revisão de código                 |

## Architecture Validation

### Validation Summary

| Check                  | Result              | Notes                                              |
| ---------------------- | ------------------- | -------------------------------------------------- |
| Decision Compatibility | PASS                | `Particles` local é desvio consciente do GDD       |
| GDD Coverage           | PASS com ressalvas  | Fórmula de M1 pendente do Épico 1; NFR-5 detalhes adiados |
| Pattern Completeness   | PASS                | Entity creation N/A, justificado                   |
| Epic Mapping           | PASS                | 6/6 épicos mapeados                                |
| Document Completeness  | PASS                | Sem placeholders                                   |

### Coverage Report

**Systems Covered:** 9/9
**Patterns Defined:** 9 (6 padrão + 3 novos)
**Decisions Made:** 8 decisões + 3 ADRs

### Issues Resolved

- Comando de console renomeado de `ror2mod_status` para `ror2meeeee_status`
- G1: condições da linha de base (FR-1) definidas na seção Measurement
- G2: `Particles` mantido como config local, registrado como desvio do GDD
- G3: verificação de versão em runtime adiada, pendência da story do Épico 1

### Open Items (verificar na implementação)

- ~~A1: `Inventory.GiveItem` ser server-only~~ **RESOLVIDO (descompilação, 2026-10-11):** `GiveItem` é `[Server]`
  e `[Obsolete]` na 1.5.x; encaminha para `GiveItemPermanent(ItemIndex, int)`, também `[Server]`.
  Existem variantes `GiveItemTemp`, `GiveItemChanneled` e `GiveItemRented`. No cliente só
  loga aviso e retorna. Ponto de interceptação ideal (inventário vs. pickup) ainda a decidir:
  hookar `GiveItemPermanent` pegaria toda concessão permanente (itens iniciais, conversões),
  não só pickups.
- A2: causa real dos gargalos (descompilar e medir)
- A6: custo do hook vazio
- A8: efeitos de M1 rodarem só no servidor
- API do `ConCommand` e nomes de tipo/método do RoR2 nos exemplos
- SDK .NET não instalado; build cruzado não testado
- Versões do xunit.v3 de espelho de terceiros; confirmar no nuget.org

### Validation Date

2026-10-11

## Development Environment

### Prerequisites

- **.NET SDK 8 ou superior** (para `netstandard2.1` e testes `net8.0`);
  **não instalado** nesta máquina, a versão exata deve ser confirmada
- Risk of Rain 2 instalado (já está em
  `~/.steam/debian-installation/steamapps/common/Risk of Rain 2`)
- **r2modman** com perfil separado de desenvolvimento: RoR2BepInExPack,
  HookGenPatcher (gera `MMHOOK_RoR2.dll`), R2API Items/Language/Networking
- Descompilador: ILSpy (`ilspycmd`) para ler `RoR2.dll`
- Execução nesta máquina via Proton: **confirmado** em 2026-10-11 (BepInEx 5.4.21 sob
  Unity 2021.3.45, RoR2BepInExPack 1.45.0 carregado, 3 plugins). Perfil r2modman `Default`
  em `~/.config/r2modmanPlus-local/RiskOfRain2/profiles/Default`
- Confirmado em 2026-10-11: HookGenPatcher gerou `BepInEx/plugins/MMHOOK/MMHOOK_RoR2.dll`
  (27,7 MB); 9 plugins carregados; jogo chegou ao menu principal; R2API 5.3.0 avisa que foi
  construído para 1.4.1 e o jogo é 1.5.0.
- HookGenPatcher: **resolvido** (obrigatório para hooks `On.*`). R2API Items/Language
  não são necessários (o mod não cria itens nem textos); Networking só na contingência. Nada
  disso vem com o pacote obrigatório. O r2modman instala `bbepis-BepInExPack` como metapacote, que traz
  `RoR2BepInExPack`, `MiscFixes`, `SeekersPatcher`, `BepInEx_GUI` e `FixPluginTypesSerialization`
  (dependências lidas do `manifest.json` em cache). `MiscFixes` e `SeekersPatcher` devem ser
  desativados antes da linha de base (o `SeekersPatcher` altera a `RoR2.dll`)

### AI Tooling (MCP Servers)

Nenhum MCP de engine selecionado: os MCPs de Unity controlam o Unity Editor, e
este projeto não tem editor. Opcionais: **Context7** (documentação atual de
BepInEx/R2API) e descompilador como ferramenta de desenvolvimento. MCPs podem
ser adicionados depois.

### Setup Commands

```bash
git init                                              # repositório ainda não é git
dotnet new sln -n Ror2Meeeee
dotnet new classlib -n Ror2Meeeee.Core -f netstandard2.1 -o src/Ror2Meeeee.Core
dotnet new classlib -n Ror2Meeeee      -f netstandard2.1 -o src/Ror2Meeeee
# projeto de testes: net8.0 + xunit.v3 (confirmar versão no nuget.org)
dotnet tool install -g ilspycmd                       # descompilar RoR2.dll
```

`Directory.Build.props` define `GameDir` apontando para a pasta do jogo.

### First Steps

1. Instalar o .NET SDK e criar a solução; compilar um "hello world" que
   carregue no jogo (valida o build cruzado e o NetworkCompatibility)
2. Rodar o jogo uma vez com BepInEx + HookGenPatcher para gerar o `MMHOOK_RoR2.dll`
3. Descompilar `RoR2.dll` e verificar A1 (`GiveItem` server-only) e A8
4. Implementar o Épico 1 (`Probe` e o cenário de referência) antes de qualquer
   otimização
