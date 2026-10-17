---
project_name: 'ror2'
user_name: 'Sawcunha'
date: '2026-10-11'
sections_completed:
  ['technology_stack', 'engine_rules', 'performance_rules', 'code_organization', 'testing_rules', 'platform_rules', 'critical_rules']
status: 'complete'
rule_count: 91
optimized_for_llm: true
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing game code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

- Jogo: Risk of Rain 2 1.5.0 (log do jogo: `ver. 1.5.0#1210*`; Steam build 25475991), Unity 2021.3.45f2 (Mono)
- BepInEx 5.4.21 (NÃO BepInEx 6); RoR2BepInExPack 1.45.0
- HookGenPatcher é obrigatório (gera `MMHOOK_RoR2.dll`; o RoR2BepInExPack NÃO o inclui)
- R2API: só os submódulos realmente usados, declarados via `[BepInDependency]`. `Items` e `Language`
  NÃO são necessários (o mod não cria itens nem textos); `Networking` só se a contingência da ADR-1 for acionada
- Plugin e Core: C# `preview`, `netstandard2.1`; testes: `net8.0` + xunit.v3
- Referências do jogo: DLLs locais de `Managed` via `GameDir` em
  `Directory.Build.props`; NÃO usar `RiskOfRain2.GameLibs` (NuGet, defasado)
- `MMHOOK_RoR2.dll` é gerado pelo HookGenPatcher; nunca commitar DLLs do jogo
- `UnityEngine.Modules` 2021.3.45 (NÃO 2021.3.33 do template: conflita com o RoR2BepInExPack, NU1605)
- Feed NuGet extra obrigatório: `https://nuget.bepinex.dev/v3/index.json` (via
  `RestoreAdditionalProjectSources`); `BepInEx.Core` não restaura do nuget.org
- Não adicionar dependência NuGet sem aprovação; versões de pacotes de teste
  confirmadas no nuget.org antes de fixar
- Identificadores: namespace `Ror2Meeeee`, GUID `com.sawcunha.ror2meeeee`

## Critical Implementation Rules

### Engine-Specific Rules

**Plugin e carga**
- A classe do plugin leva `[BepInPlugin(GUID, Name, Version)]`, um
  `[BepInDependency]` por submódulo R2API usado e
  `[NetworkCompatibility(EveryoneMustHaveMod, EveryoneNeedSameModVersion)]`
  (confirmar nomes de enum no R2API)
- Catálogos (`ItemCatalog`, etc.) NÃO estão prontos em `Awake`. Resolver
  `ItemIndex` quando o catálogo ficar disponível (por exemplo
  `ItemCatalog.availability.CallWhenAvailable`, confirmar) e guardar o índice;
  nunca buscar item por nome de string a cada tick

**Hooks (MonoMod / MMHOOK)**
- `On.*` vale para TODAS as instâncias do tipo; chamar `orig` sempre
- Remover todo hook em `Disable()`; a ordem de hooks é LIFO e outros mods
  também hookam os mesmos métodos
- IL hooks (`IL.*`) só quando `On.*` não alcança; falha de IL deve ser
  detectada no `Awake` (corpo do método mudou entre patches) e logada

**Unity**
- NUNCA usar `?.`, `??` ou `== null` em `UnityEngine.Object`: um objeto
  destruído é "fake null". Usar `if (obj)` / `if (!obj)`
- NUNCA `GameObject.Find`, `FindObjectOfType` ou `GetComponent` por tick/frame;
  obter uma vez e guardar em cache (e limpar em `ResetRunState`)
- Não criar `MonoBehaviour` só para polling de `Update`; preferir hooks e
  eventos do jogo
- Runtime é Mono (Unity 2021.3): evitar APIs do .NET moderno que o `netstandard2.1`
  referenciado não garante em runtime

**Autoridade e rede (UNet legado)**
- Lógica de gameplay só com `NetworkServer.active`
- Métodos `[Server]` do jogo chamados no cliente falham em silêncio com aviso;
  `Inventory.GiveItemPermanent(ItemIndex, int)` é `[Server]` (confirmado); `GiveItem` é obsoleto
  e só encaminha para ele. Existem `GiveItemTemp`/`Channeled`/`Rented`, que NÃO são multiplicados
- Nunca enviar mensagem de rede por tick

**Determinismo**
- O cenário de referência usa seed fixo: NÃO consumir `Run.instance` RNGs nem
  alterar `UnityEngine.Random` em caminhos de gameplay do mod; se precisar de
  sorteio, usar um `Xoroshiro128Plus` próprio

**Ciclo da run**
- `ResetRunState()` das features ligado ao início e ao fim da run
  (`Run.onRunStartGlobal` / `Run.onRunDestroyGlobal`, confirmar nomes)

### Performance Rules

**Metas e disciplina**
- Sem metas numéricas de FPS/RAM até o Épico 1 (linha de base). NÃO inventar
  limites nem otimizar item algum antes de medir
- NÃO "otimizar" neutralizando o efeito de um item: o efeito deve seguir
  reconhecível e útil (contra-métrica do PRD)
- O custo do próprio hook entra na medição; todo hook novo é medido com `Probe`

**Caminho quente (hooks por tick/frame)**
- Sem `new` de classes, sem LINQ, sem boxing, sem `params`, sem interpolação
  de string fora de guarda de log
- Sem lambdas que capturam variáveis nem `delegate` criado por chamada
  (alocam); usar método de instância ou campo `static` em cache
- `foreach` apenas sobre `List<T>` ou array (enumerador de struct); nunca sobre
  `IEnumerable<T>` (boxing)
- Coleções temporárias são campos reutilizados (`Clear()` e reuso), nunca
  criadas por chamada; `GetComponents<T>(List<T>)` em vez da versão que aloca
- Valores de config lidos uma vez para campo em cache, atualizados só em
  `SettingChanged`

**Medição**
- `Probe` com buffers pré-alocados; com `ProbeEnabled = false` não pode haver
  chamada a `Stopwatch` nem custo mensurável
- CSV só é escrito no fim da fase, nunca por tick

**Memória**
- Todo cache por jogador, por objeto ou por run é limpo em `ResetRunState()`;
  vazamento entre partidas é exatamente o problema que o mod resolve
- Não criar objetos, projéteis ou efeitos adicionais no caminho quente:
  reduzir ou agregar trabalho, nunca multiplicá-lo
- Sem pooling até a medição mostrar a alocação como causa (YAGNI)

### Code Organization Rules

**Onde cada coisa vive**
- `src/Ror2Meeeee.Core/`: lógica pura (`Limits/`, `Eligibility/`, `Pipeline/`).
  Sem referência ao jogo, BepInEx nem Unity
- `src/Ror2Meeeee/Features/<Feature>/`: uma pasta por mecânica
  (`LimitedCalc`, `Multiplier`, `Difficulty`, `Particles`, `Sharing`)
- `src/Ror2Meeeee/Common/`: `Feature`, `Probe`, `Cfg`, `Log`
- `src/Ror2Meeeee/Debug/`: overlay e `ror2meeeee_status`
- `tests/Ror2Meeeee.Core.Tests/`: só testa o `Core`

**Fronteiras**
- Uma feature NÃO referencia outra feature; só `Common` e `Core`
- Só `Features/*` e `Debug/` tocam em tipos do RoR2
- Toda config via `Cfg`; ninguém chama `ConfigFile.Bind` solto
- Concessão de itens só por `GiveRaw` com guarda `_granting`
- Único singleton permitido: `Plugin.Instance`; features recebem `Cfg` e `Log`
  por construtor
- Lógica com fórmula, tabela ou regra vai para o `Core` (testável) e o hook só a chama

**Nomes**
- Arquivos `PascalCase.cs`, um tipo principal por arquivo
- Campos privados `_camelCase`; constantes `PascalCase`
- Hooks `On<Tipo><Método>` (`OnInventoryGiveItem`)
- Config: seção e chave em PascalCase (`Multiplier.IncludeLunar`)
- Testes `<Método>_<Cenário>_<Resultado>`

**Adicionar item ao cálculo limitado (FR-5)**
- É uma nova entrada na lista da feature `LimitedCalc` com sua `IStackPolicy`,
  NÃO uma nova classe de feature nem mudança no núcleo

### Testing Rules

**O que é teste automatizado**
- Só o `Ror2Meeeee.Core` é testado com `dotnet test` (projeto `net8.0`,
  xunit.v3). Não há mock do RoR2: se algo precisa de tipo do jogo, não é
  teste unitário, é validação em jogo
- Um teste por regra de negócio: fórmula de limite, tabela de elegibilidade,
  matemática do multiplicador, pipeline de pickup (incluindo não recursão e
  ordem pickup → multiplicação → compartilhamento)
- Nome do teste `<Método>_<Cenário>_<Resultado>`; sem fixtures nem frameworks
  de mock; dados de teste inline

**Casos obrigatórios (bordas)**
- Multiplicador 1 (desliga), multiplicador muito alto (sem estouro de `int`)
- Stack 0, stack 1, stack no teto, stack acima do teto
- Jogador morto ou desconectado não recebe
- Lunar e void respeitam a config; temporário, scrap e equipamento nunca multiplicam
- Item recebido por compartilhamento não é multiplicado nem compartilhado de novo

**O que é validação em jogo (não automatizável)**
- Cenário de referência: 3 jogadores, seed fixo, Fungus 50+, Fireworks ~200,
  via `give_item`
- Três condições de linha de base (sem o mod, sem multiplicação, com
  multiplicação), sempre com `Probe` e CSV
- Multiplayer sempre com host e cliente reais (ou o procedimento do wiki
  "Testing Multiplayer Alone"); nunca assumir sincronização sem testar
- Cada story de hook termina com uma checagem de "hook registrou" no log do
  `Awake` e uma validação em jogo descrita na story

**Regras**
- Código novo no `Core` sem teste não fecha a story
- Teste não pode depender do jogo, de relógio nem de rede
- Falha de teste nunca é corrigida afrouxando a asserção

### Platform & Build Rules

**Ambientes**
- Desenvolvimento em Linux; execução em Windows (r2modman) ou, nesta máquina,
  em Proton. O jogo está em
  `~/.steam/debian-installation/steamapps/common/Risk of Rain 2`
- O jogo é um build Windows. NUNCA assumir caminhos, separadores nem
  comportamentos de Linux em código do plugin: o plugin roda sob Windows ou Wine
- Usar `Path.Combine` e `BepInEx.Paths`; nada de caminho absoluto no código

**Build**
- `dotnet build -c Debug` (Debug leva o `.pdb`); copiar `.dll` + `.pdb` para
  `BepInEx/plugins/` do perfil r2modman de desenvolvimento
- `GameDir` vem de `Directory.Build.props`, com sobrescrita por variável de
  ambiente ou propriedade; NUNCA caminho do usuário dentro de `.csproj`
- Não commitar DLLs do jogo, `MMHOOK_RoR2.dll` nem a pasta `BepInEx`
- Alvo único: `netstandard2.1` (plugin e `Core`); projeto de teste `net8.0`
- Antes do primeiro "hello world" funcionar, nenhuma feature é escrita

**Compatibilidade**
- Alvo é a versão mais recente do jogo (1.5.x). O log do `Awake` registra a
  versão do jogo e do mod; divergência gera `Warning`
- Se um hook falha ao registrar (assinatura mudou), a feature fica desligada
  com `Error` no log, NÃO derruba o plugin
- Sem compatibilidade garantida com outros mods de drops/inventário; conflitos
  são esperados e documentados

**Config e arquivos**
- Config em `BepInEx/config/com.sawcunha.ror2meeeee.cfg`; o único arquivo
  extra permitido é o CSV do `Probe`
- Sem Thunderstore nesta fase: nada de `manifest.json`/ícone a manter

### Critical Don't-Miss Rules

**Anti-padrões**
- NÃO tratar as hipóteses do addendum como fatos (causa do gargalo,
  efeito de Fungus/Fireworks): estão NÃO verificadas (`GiveItem` server-only já foi confirmado).
  Descompilar `RoR2.dll` e medir antes de implementar em cima delas
- NÃO conceder item dentro do hook de concessão sem a guarda `_granting`
  (recursão infinita e multiplicação em cascata)
- NÃO "consertar" desempenho removendo ou enfraquecendo o efeito do item além
  do que o Épico 1 justificar
- NÃO copiar código de mods de terceiros (`yekoc`, GPL/LGPL; `Squeaky's Item
  Multiplier`, AGPL): só estudo
- NÃO usar `?.`, `??` ou `== null` em objetos Unity
- NÃO engolir exceção sem contar em `Fail()`

**Casos de borda**
- Multiplicador "sem teto": `count * multiplier` pode estourar `int`; usar
  aritmética com saturação (clamp em `int.MaxValue`) e logar `Warning`
- Stack acumulado por multiplicação + compartilhamento cresce com o número de
  jogadores: o teto de stacks por item e por jogador (padrão desligado) deve
  ser checado DEPOIS de multiplicar
- Jogador morto ou desconectado: sem concessão e sem acúmulo retroativo
- Troca de host ou queda do host: o estado por run é descartado, nunca
  reaproveitado
- Config inválida (multiplicador 0 ou negativo): corrigir para o padrão com
  `Warning`; `1` desliga a mecânica
- Itens temporários, scrap e equipamento nunca multiplicam nem compartilham

**Armadilhas do ecossistema**
- Dois mods hookando o mesmo método encadeiam em LIFO: nunca assumir ser o
  único ou o último; sempre chamar `orig`
- Hook que roda em todos os clientes e no host: sem `NetworkServer.active`, a
  mesma concessão acontece N vezes
- Atualizações do jogo quebram assinaturas silenciosamente: cada hook confirma
  o registro no `Awake`
- Log verboso por padrão degrada desempenho (o mod de referência cometeu esse
  erro): `VerboseLogging` padrão `false`

**Armadilhas de performance**
- Cálculo por stack que cresce linearmente com o número de stacks
- Alocação por tick (lambda, LINQ, string, `new`)
- Busca de objeto por tick (`Find`, `GetComponent`) em vez de cache

---

## Usage Guidelines

**For AI Agents:**

- Read this file before implementing any game code
- Follow ALL rules exactly as documented
- When in doubt, prefer the more restrictive option
- Update this file if new patterns emerge

**For Humans:**

- Keep this file lean and focused on agent needs
- Update when technology stack changes (e.g. after a game patch)
- Review after Epic 1: resolve the "confirmar" items marked in the rules
- Remove rules that become obvious over time

Last Updated: 2026-10-11
