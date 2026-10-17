# Addendum — PRD Mod Risk of Rain 2

## Pesquisa: gargalos de desempenho (2026-10-11, confiança baixa/média; sem benchmarks independentes)

Gargalos prováveis, priorizados:
1. Proc chains / on-hit multiplicativos (Pocket ICBM, Molten Perforator, ATG, Polylute) — fonte: autor de mod, não medição.
2. Projéteis/mísseis numerosos e de longa duração (inferência).
3. Recálculo de stats por buff/debuff (bleed/poison stackados forçam RecalculateStats a cada tick).
4. IA/pathfinding com muitos monstros (custo no host).
5. VFX/partículas/GPU/vsync — só relatos anedóticos.
6. Rede/NetworkServer em multiplayer — hipótese NÃO verificada.

Mods existentes:
- LateGamePerformance (Scorn_Dogs): 18 patches (BuffStatsFilter, NavPathBudget); pouca validação. https://thunderstore.io/c/riskofrain2/p/Scorn_Dogs/LateGamePerformance/
- LordsItemEdits (LordVGames, deprecated): reequilibra itens para gerar menos proc chains. https://thunderstore.io/c/riskofrain2/p/LordVGames/LordsItemEdits/
- FasterProcChains: citado, não verificado.
- Runtime Unity Editor (ManlyMarco): inspeção, não profiler.

Medição (a validar): overlay próprio com Time.unscaledDeltaTime; Stopwatch/Profiler.BeginSample em hooks MonoMod com média e p95; contadores de projéteis/orbs/ProcChainMask/mensagens de rede por segundo; runs reproduzíveis (seed fixa, give_item), com/sem mod, host e cliente isolados. Attach de profiler externo ao RoR2 retail: não confirmado.

## Pesquisa: mods de referência (2026-10-11; sem código-fonte lido, só páginas)

**ShareItemsWithMoreFun (littlewolf)** v0.2.3, 1.9K downloads, categoria "AI Generated". Baús instanciados por jogador com choice orb privado (3 itens), cobre Shrine of Chance, Hexes, boss loot, multishops, Void Potentials; congela timer pós-teleporter e revive mortos. ~17 configs. Todos os jogadores precisam do mod (mesma versão). Sem repositório. Conflita com ThreeChoiceChests, ItemShare, ShareSuite, InstancedLoot, Artifact of Potential. https://thunderstore.io/c/riskofrain2/p/littlewolf/ShareItemsWithMoreFun/

**Squeaky's Item Multiplier (SqueakySquad)** v1.1.0, 16.6K downloads, server-side (só o host instala). Multiplica itens ao pegar (padrão x5; exponencial opcional; opções para lunar/void/temporário). Debug logging ligado por padrão. Código AGPL-3.0: https://github.com/SqueakySquared/SqueakysItemMultiplier (tem testes unitários de escala). Autor testou só até alguns milhares; impacto "mínimo" com ressalva para valores altos. https://thunderstore.io/c/riskofrain2/p/SqueakySquad/Squeakys_Item_Multiplier/

**Lições (inferidas das páginas):** server-side puro evita barreira de instalação; pilhas enormes tornam recálculo de stats e procs por stack o gargalo; log verboso desligado por padrão; instanciar por jogador custa jogadores x baús; prever incompatibilidades com kill-switch.

## Pesquisa: versão do jogo e ecossistema (2026-10-11; fetch sem datas, possível cache — confirmar no site)

**Jogo:** patch 1.21 (1.005.001), 8/out/2026, com a expansão Hallowed Concepts (fonte única, não oficial: https://mp1st.com/?p=316355). Patch anterior 1.20 (9/dez/2025). Lista de DLCs e plataformas: não confirmada.

**Ecossistema:**
- Usar RoR2BepInExPack 1.45.0 (o BepInExPack do bbepis está deprecated): https://thunderstore.io/c/riskofrain2/p/RiskofThunder/RoR2BepInExPack/. Versão do BepInEx incluída: não confirmada (mods recentes citam 5.4.2117).
- R2API modular; submódulos precisam ser declarados explicitamente. Versão do Core inconsistente entre fontes (5.1.x vs 5.3.0): confirmar.
- HookGenPatcher 1.2.9 (ou 1.3.x): gera MMHOOK_*.
- Alvo de framework segundo o wiki: netstandard2.0 (confirmar no template). Unity: não confirmado.
- Quebras de mods após 1.21: não confirmado; verificar páginas dos mods e Discord do Risk of Thunder.

**Fluxo do primeiro mod (R2Wiki):** template ExamplePlugin (GitHub risk-of-thunder); Visual Studio Community com ".NET desktop development"; r2modman com perfil separado de desenvolvimento; instalar no perfil BepInExPack, HookGenPatcher, R2API Items e Language; build Debug; copiar .dll e .pdb para BepInEx/plugins/; testar com console do BepInEx e F2 (item de exemplo). Linux: não coberto pelo wiki (ambiente do usuário é Linux — risco).

**Publicação (Thunderstore):** zip com icon.png 256x256, README.md, manifest.json (name, description ≤250, version_number, dependencies, website_url) na raiz; dependências no formato Time-Pacote-Versão. https://wiki.thunderstore.io/mods/creating-a-package

## Caso observado: Bustling Fungus (relato do usuário, 2026-10-11)
Hipótese NÃO verificada (conhecimento geral, sem leitura do código): o item cria uma zona de cura em torno do jogador parado, processada por tick, com raio e cura crescendo por stack. Com muitos stacks e vários jogadores/aliados, o custo por tick (busca de alvos na área, aplicação de cura, possível alocação de memória) pode crescer. Verificar no código do jogo (HealingWard / BuffWard) e medir antes de qualquer otimização.

## Caso observado: Bundle of Fireworks
Relato: um jogador chegou a ~200 stacks. Pelo conhecimento geral (confirmar no código/wiki): ao ativar interagíveis lança fogos de artifício, com quantidade crescendo por stack, cada um como projétil/dano separado. 200 stacks por interação implicam centenas de projéteis simultâneos (candidato a gargalo de CPU e alocação).

**Atualização (usuário, 2026-10-11):** versão-alvo = a mais recente, liberada na semana anterior. Desenvolvimento em Linux, jogo executado em Windows (build no Linux, cópia da .dll para o perfil do r2modman no Windows).

## Referência: artigo "Developing a Risk of Rain 2 mod" (cjcocokrisp, Medium) — colado pelo usuário, trecho parcial

O Medium bloqueou o acesso automático (403); o usuário colou o trecho inicial. Conteúdo:
- RoR2 é feito em Unity; mods em C# com o framework de plugins BepInEx.
- A comunidade criou a R2API, que expõe objetos e métodos do jogo; o autor também usa o **Risk of Options** (pacote para criar menus de configuração in-game).
- Processo do autor: dividir o mod em passos (exibir algo no painel de objetivos; extrair stats dos jogadores; ordenar e exibir; opções de configuração); estudar mods existentes e a documentação antes de começar.
- **Armadilha relevante ao nosso mod:** ao atrelar a exibição ao método de update do painel (executa todo frame), o objeto era adicionado repetidamente e travava o jogo; a correção foi obter o objeto uma vez e **guardá-lo em cache**. Lição: não recriar nem buscar objetos a cada tick/frame; reforça a hipótese de que parte do custo do jogo pode vir de trabalho repetido por tick (a verificar por medição).
- Trecho recebido não cobre setup de build, hooks de itens nem testes; nada sobre Linux ou versões.

## Referência: artigo cjcocokrisp, parte 2 (colado pelo usuário)
- Estatísticas dos jogadores vêm de `PlayerCharacterMasterController` (instâncias dos jogadores no lobby). **Só o host enxerga o estado completo** de todos; clientes não.
- Solução do autor: `NetMessage` da R2API para sincronizar valores host → clientes (stats + nomes). Mod dividido em: hooks de exibição + NetMessage.
- Fluxo: a cada frame o host envia as estatísticas a cada cliente; cada cliente compara com o que exibe e **só reordena/atualiza a string quando há diferença** (evita custo por frame).
- Configuração via Risk of Options (fácil, boa aparência); a opção de ranking valia a do host.
- **Implicações para o nosso mod (hipóteses/lições):** (1) lógica autoritativa de itens (multiplicação, compartilhamento) deve rodar no host; clientes recebem o resultado; (2) enviar dados todo frame é custo — preferir enviar só mudanças; (3) padrão "comparar antes de recalcular" é aplicável aos efeitos por tick; (4) com todos instalando o mod, `NetMessage` é viável mas aumenta superfície de dessincronização.

## Pesquisa: organização risk-of-thunder no GitHub (2026-10-11; org tem 24 repos, 10 vistos; nada testado)

**Repositórios:** R2API (https://github.com/risk-of-thunder/R2API, ~40 submódulos, release 4.4.0 em 5/jun, último push 10/out/2026); ExamplePlugin (https://github.com/risk-of-thunder/ExamplePlugin, atualizado 5/abr/2026); RoR2BepInExPack (atualizado 10/out/2026); R2Wiki (605 commits, atualizado 11/out/2026); R2Boilerplate; ferramentas Unity/versão. HookGenPatcher não está na org (repositório e versão não confirmados; pacote do Thunderstore gera `MMHOOK_RoR2.dll`).

**Networking (R2API.Networking):** `[BepInDependency(NetworkingAPI.PluginGUID)]`; classe `INetMessage` (`Serialize`, `Deserialize`, `OnReceived`, construtor vazio); `NetworkingAPI.RegisterMessageType<T>()` no `Awake`; envio `new Msg(...).Send(NetworkDestination.Clients)` (destinos `Server` e `Clients`); `OnReceived` roda em cada máquina e a checagem de host (`NetworkServer.active`) é do mod. Guia: R2Wiki `docs/Mod-Creation/C#-Programming/Networking/R2API.NetworkingAPI.md`. Wiki também tem `Server-side-and-client-side-mods.md` (atributo `NetworkCompatibility`) e `UNet.md`.

**Conceder itens:** R2API.Items só **cria** itens custom. Conceder usa `Inventory.GiveItem` do jogo, que provavelmente é `[Server]` (host chama; inventário sincroniza via UNet) — **não verificado**. Jogadores via `PlayerCharacterMasterController.instances`.

**ExamplePlugin:** `ExamplePlugin.cs`, `.csproj` SDK-style, **netstandard2.1**, `LangVersion preview`, unsafe; referências NuGet: BepInEx.Core 5.4.21, R2API.Items, R2API.Language, UnityEngine.Modules 2021.3.33, RiskOfRain2.GameLibs, MMHOOK.RoR2, RoR2BepInExPack — sem DLL local do jogo. Por ser SDK-style com NuGet, `dotnet build` deve funcionar no Linux (**não testado**; discrepância: o R2Wiki citava `netstandard2.0` na saída).

**Hooks/debug (R2Wiki `docs/Mod-Creation/C#-Programming`):** `Hooking.md` (On.* roda antes/depois de `orig`, vale para todas as instâncias; sempre chamar `orig`; desinscrever em `OnDisable`; cadeia LIFO), `IL-Hooking.md`, `Debugging-Your-Mods.md` (VS2022/dnSpy/Rider, depurador em 127.0.0.1:55555 via `doorstop_config.ini`, `.pdb` ao lado da DLL), `Testing/Testing-Multiplayer-Alone.md`. Profiling dedicado: não coberto.

**Código do jogo:** não há repositório público da org com o código descompilado. Referência prática: descompilar `RoR2.dll` com ILSpy/dnSpy (dump via `DumpAssemblies` do BepInEx). Hipótese de causa de lentidão (HealingWard/BuffWard/Bustling Fungus/FireworkLauncher) **continua não verificada**.

## Tecnologia movida do PRD (2026-10-11)
- Configuração: arquivo de config do BepInEx; menu in-game opcional via Risk of Options.
- Dependências: RoR2BepInExPack, R2API (módulos Items/Language/Networking conforme necessário), HookGenPatcher/MMHOOK.
- Build: compilar no Linux (dotnet CLI/Rider) e copiar `.dll` + `.pdb` para o perfil do r2modman no Windows; validar no primeiro "hello world". Wiki e template divergem em netstandard2.0 vs 2.1.
- Sincronização: `NetworkingAPI`/`INetMessage` do R2API; concessão pelo host via `Inventory.GiveItem` (não verificado).
- Versão do jogo segundo a pesquisa: patch 1.21 (Hallowed Concepts, 8/out/2026), fonte não oficial.
- Opção de design a avaliar em v2: compartilhamento por baús instanciados com choice orb (modelo do ShareItemsWithMoreFun) em vez de replicação.
