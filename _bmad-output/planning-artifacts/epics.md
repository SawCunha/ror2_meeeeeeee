---
stepsCompleted: ['step-01-validate-prerequisites', 'step-02-design-epics', 'step-03-create-stories', 'step-04-final-validation']
inputDocuments:
  - _bmad-output/planning-artifacts/gdds/gdd-ror2-2026-10-11/gdd.md
  - _bmad-output/planning-artifacts/gdds/gdd-ror2-2026-10-11/epics.md
  - _bmad-output/planning-artifacts/prds/prd-ror2-2026-10-11/prd.md
  - _bmad-output/planning-artifacts/prds/prd-ror2-2026-10-11/addendum.md
  - _bmad-output/game-architecture.md
  - _bmad-output/project-context.md
---

# ror2 - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for ror2, decomposing the requirements from the GDD, UX Design if it exists, and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

FR-1: O autor pode medir frame time e uso de RAM no cenário de referência em três condições: sem o mod, com o mod sem multiplicação, com o mod com multiplicação (linha de base; primeiro passo do MVP).
FR-2: O custo por tick do Bustling Fungus deixa de crescer sem limite com o número de stacks, preservando o efeito de cura de forma reconhecível (limite e fórmula decididos após medir).
FR-3: O custo por interação do Bundle of Fireworks deixa de crescer sem limite com o número de stacks, preservando o dano total de forma reconhecível (manter a quantidade e otimizar, ou agregar: decidir após medir).
FR-4: Cada alteração de item pode ser ligada ou desligada individualmente.
FR-5: Novos itens podem ser adicionados à lista de itens com cálculo limitado sem reescrever a lógica central (lista inicial de 2 itens).
FR-6: A cada pickup, o jogador recebe um multiplicador de cópias definido em campo de configuração aberto (inteiro positivo, sem teto imposto pelo mod); o valor 1 desliga a mecânica (padrão x2).
FR-7: Itens sem cálculo limitado não são multiplicados acima de um limite configurável de stacks (por item e por jogador; padrão: sem teto).
FR-8: O multiplicador é aplicado só aos pickups elegíveis conforme a tabela de elegibilidade (branco/verde/vermelho/boss via baú, drop ou impressora: sim; lunar e void: configurável, padrão não; temporário/scrap/equipamento: não; jogador morto ou desconectado: não recebe, sem acúmulo retroativo).
FR-9 [v2]: Todo pickup elegível de um jogador é entregue também aos demais jogadores, na quantidade integral recebida (incluindo cópias da multiplicação).
FR-10 [v2]: O compartilhamento pode ser desligado, e um item recebido por compartilhamento não é compartilhado de novo.
FR-11: A dificuldade da partida aumenta por um fator configurável, decidido no host; um único fator escala vida, dano, quantidade de inimigos e ritmo da escalada (valores e curva calibrados em playtest).
FR-12: O jogador pode desabilitar ou encurtar a duração de partículas de uma lista configurável de efeitos de itens.
FR-13: Desabilitar ou encurtar partículas não altera dano, cura nem colisão.

### NonFunctional Requirements

NFR-1 (Desempenho): no cenário de referência, FPS e RAM com o mod (incluindo multiplicação ligada) ficam melhores que na linha de base; metas numéricas definidas após a linha de base.
NFR-2 (Multiplayer): funciona com todos os jogadores com o mod, mesma versão, sem dessincronização; multiplicação, compartilhamento e configs de gameplay são decididos no host e valem as do host; sem tráfego de rede por tick.
NFR-3 (Observabilidade): log verboso desligado por padrão; medição de frame time e contadores (projéteis, procs) disponível por configuração.
NFR-4 (Alocações): nos caminhos alterados, evitar alocação de memória por tick quando viável.
NFR-5 (Compatibilidade de versão): alvo é a versão mais recente do jogo (RoR2 1.5.x, build #1210).
NFR-6 (Plataformas): desenvolvimento em Linux; execução e testes em Windows (ou Proton nesta máquina).

### Additional Requirements

**Starter / setup (impacta Épico 1, Story 1)**
- Ponto de partida: template `risk-of-thunder/ExamplePlugin` (`netstandard2.1`, `LangVersion preview`, `BepInEx.Core 5.4.21`, `UnityEngine.Modules 2021.3.45`, NÃO 2021.3.33).
- Primeiro marco: "hello world" compilado no Linux e carregado no jogo; nenhuma feature é escrita antes disso.
- Feed NuGet extra `https://nuget.bepinex.dev/v3/index.json` (`RestoreAdditionalProjectSources`) para `BepInEx.Core`.
- Referenciar DLLs locais de `Risk of Rain 2_Data/Managed` via `GameDir` em `Directory.Build.props`; NÃO usar `RiskOfRain2.GameLibs`.
- Estrutura: `src/Ror2Meeeee.Core/` (netstandard2.1, sem referência ao jogo), `src/Ror2Meeeee/` (plugin), `tests/Ror2Meeeee.Core.Tests/` (net8.0, xunit.v3). Namespace `Ror2Meeeee`, GUID `com.sawcunha.ror2meeeee`.
- .NET SDK 8+ não instalado nesta máquina; build cruzado não testado; `git init` pendente (repositório ainda não é git).
- Perfil r2modman de dev; `MiscFixes` e `SeekersPatcher` desativados antes da linha de base; HookGenPatcher obrigatório (gera `MMHOOK_RoR2.dll`).
- `ilspycmd` (ou ILSpy) para descompilar `RoR2.dll`.

**Arquitetura e padrões**
- Uma classe `Feature` por mecânica (`Enable`/`Disable`/`ResetRunState`), kill-switch individual, uma feature não referencia outra.
- Todo hook: try/catch, `orig` sempre, `Fail()` com contador e auto-desligamento (`MaxFailures` padrão 50, mínimo 50); registro de sucesso/falha de cada hook no `Awake`.
- Lógica de gameplay só com `NetworkServer.active`; config "Host" lida só no host; sem sync de config na v1 (ADR-1; contingência `INetMessage` se A8 falhar).
- `Probe` próprio: Stopwatch por hook, contadores, p95, CSV por fase, buffers pré-alocados, overlay opcional, mede o hook vazio; sem custo com `ProbeEnabled = false`.
- Cálculo limitado: estratégia por item `Off` / `Clamp` / `Aggregate` (`IStackPolicy` no Core); só implementar os modos que o Épico 1 justificar; sem reimplementar o item.
- Pipeline de pickup puro no Core (`Resolve(PickupContext, MultiplierRules)`), concessão só via `GiveRaw` (`GiveItemPermanent`) com guarda `_granting`; itens do compartilhamento não são multiplicados nem compartilhados de novo.
- Config via `Cfg` (`ConfigFile` do BepInEx, uma seção por feature, valores em cache, atualizados em `SettingChanged`); valor inválido corrigido para o padrão com `Warning`; `Particles` e `General` são locais, demais são do host.
- Multiplicador sem teto: aritmética com saturação em `int.MaxValue` e `Warning`; teto de stacks checado depois de multiplicar.
- `ResetRunState()` chamado no início e no fim da run (sem vazamento entre partidas).
- ConCommand `ror2meeeee_status` (lista features, falhas e hooks); verificação de versão do jogo e do mod no `Awake` (`Warning` na divergência).
- `[NetworkCompatibility(EveryoneMustHaveMod, EveryoneNeedSameModVersion)]` no plugin (confirmar nomes de enum).
- Caminho quente sem `new`, LINQ, boxing, lambdas capturando, log não guardado, `GetComponent`/`Find` por tick; sem `?.`/`??`/`== null` em objetos Unity.
- Não consumir RNGs do `Run.instance` nem `UnityEngine.Random` (seed fixo do cenário).
- Testes: só `Ror2Meeeee.Core` com `dotnet test`; casos de borda obrigatórios (multiplicador 1 e muito alto, stack 0/1/teto/acima, morto/desconectado, lunar/void, temporário/scrap/equipamento, não recursão). Código novo no Core sem teste não fecha a story.
- Itens em aberto a validar no Épico 1/2: A2 (causa real dos gargalos), A6 (custo do hook vazio), A8 (efeitos de M1 rodam só no servidor), ponto ideal de interceptação do pickup (`GiveItemPermanent` pega toda concessão permanente, não só pickups), API do `ConCommand`.
- Hipóteses do addendum NÃO são fatos: descompilar e medir antes de implementar.
- Fora de escopo: Thunderstore, jogadores sem o mod, mod só-host, modo exponencial, conteúdo novo, compatibilidade com outros mods de drops e com patches futuros.

### UX Design Requirements

N/A. O projeto é um mod sem interface própria (UI, arte e áudio vêm do jogo base). Não há documento de UX. Menu in-game via Risk of Options é dependência suave, adiada.

### FR Coverage Map

FR-1: Epic 1 e Epic 3 - Medição e linha de base; condição 3 (com multiplicação) fecha no Epic 3
FR-2: Epic 2 - Limite do Bustling Fungus
FR-3: Epic 2 - Limite do Bundle of Fireworks
FR-4: Epic 1 e Epic 2 - Mecanismo de kill-switch (Epic 1); flag por item (Epic 2)
FR-5: Epic 2 - Lista extensível de itens limitados
FR-6: Epic 3 - Multiplicador configurável
FR-7: Epic 3 - Teto de stacks
FR-8: Epic 3 - Tabela de elegibilidade
FR-9: Epic 6 - Replicação aos demais jogadores (v2)
FR-10: Epic 6 - Desligar e não recursão (v2)
FR-11: Epic 4 - Fator de dificuldade
FR-12: Epic 5 - Lista configurável de partículas
FR-13: Epic 5 - Corte sem alterar gameplay

## Epic List

### Epic 1: Ambiente, medição e linha de base
O autor compila e carrega o plugin no jogo, mede frame time e RAM no cenário de referência e conhece a causa real do custo. Inclui a infraestrutura transversal (`Feature` base, `Fail()`, `Cfg`, `Probe`, `ror2meeeee_status`, verificação de versão).
**FRs covered:** FR-1 (condições 1 e 2), FR-4 (mecanismo)

### Epic 2: Cálculo limitado de itens
Bustling Fungus e Bundle of Fireworks deixam de crescer sem limite, com efeito reconhecível, ligáveis individualmente e extensíveis por nova entrada na lista.
**FRs covered:** FR-2, FR-3, FR-4, FR-5

### Epic 3: Multiplicação de itens
Cada pickup elegível rende N cópias (padrão x2), decidido no host, com tabela de elegibilidade e teto de stacks. Fecha a condição 3 da linha de base.
**FRs covered:** FR-1 (condição 3), FR-6, FR-7, FR-8

### Epic 4: Dificuldade aumentada
Um fator no host escala vida, dano, quantidade de inimigos e ritmo para compensar o poder extra.
**FRs covered:** FR-11

### Epic 5: Corte de partículas
Lista configurável de efeitos de itens desabilitados ou encurtados, sem alterar gameplay (config local).
**FRs covered:** FR-12, FR-13

### Epic 6: Compartilhamento de itens (v2)
Pickup replicado integralmente aos demais jogadores, sem recursão, reaproveitando o pipeline do Epic 3.
**FRs covered:** FR-9, FR-10

## Epic 1: Ambiente, medição e linha de base

O autor compila e carrega o plugin no jogo, tem infraestrutura segura de hooks, mede frame time e RAM no cenário de referência e conhece a causa real do custo. Cobre FR-1 (condições 1 e 2), FR-4 (mecanismo), NFR-3, NFR-4, NFR-5, NFR-6.

### Story 1.1: Plugin "hello world" carregando no jogo

As a autor do mod,
I want compilar o plugin no Linux e vê-lo carregar no Risk of Rain 2,
So that o build cruzado e a carga do BepInEx estão validados antes de qualquer feature.

**Acceptance Criteria:**

**Given** o .NET SDK 8+ instalado e a solução `Ror2Meeeee.sln` com `src/Ror2Meeeee.Core` e `src/Ror2Meeeee` (`netstandard2.1`), `GameDir` definido em `Directory.Build.props` e o feed `nuget.bepinex.dev` configurado
**When** executo `dotnet build -c Debug`
**Then** o build conclui sem erro e gera `.dll` e `.pdb`
**And** nenhuma DLL do jogo, `MMHOOK_RoR2.dll` nem a pasta `BepInEx` entram no repositório.

**Given** a `.dll` e o `.pdb` copiados para `BepInEx/plugins/Ror2Meeeee/` do perfil r2modman de dev (com `MiscFixes` e `SeekersPatcher` desativados)
**When** o jogo inicia até o menu principal
**Then** o `LogOutput.log` registra o GUID `com.sawcunha.ror2meeeee`, a versão do mod e a versão do jogo (1.5.0#1210)
**And** a classe do plugin declara `[NetworkCompatibility(EveryoneMustHaveMod, EveryoneNeedSameModVersion)]`.

**Given** uma versão do jogo diferente da esperada
**When** o plugin inicia
**Then** um `Warning` de divergência é logado e o plugin segue carregado (NFR-5).

**And** `git init` foi executado e o `.gitignore` cobre DLLs do jogo, `bin/`, `obj/` e a pasta `BepInEx`.

### Story 1.2: Base `Feature` com kill-switch e falha isolada

As a autor do mod,
I want uma base de feature com liga/desliga e falha isolada,
So that um hook ruim nunca derruba a partida e cada alteração pode ser desligada (FR-4).

**Acceptance Criteria:**

**Given** a classe abstrata `Feature` (`Enable`, `Disable`, `ResetRunState`, `Fail`) e `Cfg` com a seção `General` (`VerboseLogging=false`, `MaxFailures=50`, mínimo 50)
**When** o plugin inicia
**Then** cada feature só é habilitada se o seu `ConfigEntry<bool>` estiver ligado
**And** `Disable()` remove todos os hooks registrados por `Enable()`.

**Given** uma feature de teste com um hook que lança exceção
**When** o hook é disparado
**Then** a exceção é logada e contada em `Fail()`, e `orig` é chamado mesmo assim
**And** ao atingir `MaxFailures` a feature é desligada com um único `Warning`.

**Given** um valor de config inválido (por exemplo `MaxFailures=3`)
**When** a config é carregada
**Then** o valor é corrigido para o padrão com `Warning`.

**Given** o início e o fim de uma run
**When** os eventos da run disparam
**Then** `ResetRunState()` de todas as features é chamado em um único ponto
**And** cada hook registra sucesso ou falha no `Awake`.

### Story 1.3: Investigação do `RoR2.dll`

As a autor do mod,
I want um relatório do que o `RoR2.dll` realmente faz nos itens e no pickup,
So that as próximas stories se apoiem em fatos, não nas hipóteses do addendum.

**Acceptance Criteria:**

**Given** `RoR2.dll` descompilado com `ilspycmd` (versão da ferramenta registrada)
**When** analiso o Bustling Fungus e o Bundle of Fireworks
**Then** `docs/` registra, para cada item: tipo e método que executam o efeito, se rodam só no servidor (fecha A8), o que escala por stack e a frequência de execução
**And** a hipótese do addendum é marcada como confirmada ou refutada.

**Given** o fluxo de pickup e concessão de itens
**When** comparo `GiveItemPermanent` com o ponto de pickup do jogador
**Then** o relatório recomenda o ponto de interceptação para a multiplicação, indicando o que cada alternativa também captura (itens iniciais, conversões)
**And** lista os nomes reais de tipos e métodos para os hooks `On.*` das próximas stories.

**Given** o fluxo de pickup e concessão de itens
**When** levanto todas as origens de concessão (pickup de baú, drop, impressora, itens iniciais, conversões, void, lunar, scrapper, Command)
**Then** o relatório lista cada origem e o método do jogo que a executa, para a regra de elegibilidade da Story 3.3.

**Given** os efeitos do Fungus e do Fireworks
**When** concluo se rodam apenas no servidor (A8)
**Then** o relatório registra a decisão "lógica host-only é suficiente" (sim ou não)
**And** se não, a Story 2.4 (condicional) passa a ser obrigatória no Épico 2.

**Given** o relatório concluído
**When** as stories dos Épicos 2 e 3 forem detalhadas
**Then** elas referenciam este relatório como fonte (sem código de produção alterado nesta story).

### Story 1.4: `Probe` de medição

As a autor do mod,
I want medir frame time, RAM e contadores por fase com custo desprezível,
So that eu compare as condições da linha de base com números confiáveis (FR-1, NFR-3, NFR-4).

**Acceptance Criteria:**

**Given** o projeto `Ror2Meeeee.Core.Tests` (`net8.0`, xunit.v3) e o cálculo de p95/média em `Core`
**When** executo `dotnet test`
**Then** os testes cobrem buffer vazio, 1 amostra, buffer cheio e valores repetidos, e passam.

**Given** `General.ProbeEnabled = false` (padrão)
**When** o jogo roda
**Then** não há chamada a `Stopwatch` nem alocação do `Probe` nos hooks
**And** `VerboseLogging` segue `false`.

**Given** `ProbeEnabled = true` e uma fase nomeada
**When** a fase termina
**Then** um CSV é escrito com frame time médio e p95, RAM, e contadores (projéteis, procs)
**And** nenhum CSV é escrito durante a fase e o `Probe` usa buffers pré-alocados.

**Given** um hook vazio registrado só para medição
**When** a fase roda
**Then** o custo do hook vazio aparece separado no CSV (A6).

### Story 1.5: Comando `ror2meeeee_status`

As a autor do mod,
I want listar features, falhas e hooks registrados pelo console do jogo,
So that eu veja o estado do mod sem abrir o log.

**Acceptance Criteria:**

**Given** o jogo com o console aberto
**When** executo `ror2meeeee_status`
**Then** a saída lista cada feature (ligada ou desligada), contagem de falhas e o resultado de registro de cada hook
**And** mostra a versão do mod e do jogo.

**Given** uma feature auto-desligada por `MaxFailures`
**When** executo o comando
**Then** ela aparece como desligada por falha, com a contagem.

**And** o comando não aloca nem registra nada por tick (só executa sob demanda).

### Story 1.6: Cenário de referência e linha de base

As a autor do mod,
I want um procedimento reproduzível do cenário de referência com as condições 1 e 2 medidas,
So that existam números reais para definir as metas de FPS e RAM (FR-1, NFR-1).

**Acceptance Criteria:**

**Given** 3 jogadores, seed fixo e `give_item` para Fungus 50+ e Fireworks ~200
**When** documento o procedimento em `docs/`
**Then** outra pessoa consegue reproduzir o cenário passo a passo
**And** o mod não consome RNG do `Run.instance`.

**Given** a condição 1 (jogo sem o plugin) e a condição 2 (plugin com `Probe`, sem multiplicação)
**When** meço cada uma por pelo menos uma fase de duração fixa
**Then** os CSVs de ambas ficam em `docs/` com data e versão do jogo
**And** o custo do próprio `Probe` está registrado separadamente.

**Given** os resultados
**When** reviso com você
**Then** as metas numéricas de FPS e RAM (NFR-1) ficam registradas, ou explicitamente adiadas até a condição 3 (Épico 3).

## Epic 2: Cálculo limitado de itens

Bustling Fungus e Bundle of Fireworks deixam de crescer sem limite, com efeito reconhecível, ligáveis individualmente e extensíveis por nova entrada na lista. Cobre FR-2, FR-3, FR-4 (flag por item), FR-5, NFR-1, NFR-4. Fórmula e modo saem do relatório da Story 1.3 e da linha de base da 1.6.

### Story 2.1: Bustling Fungus com custo limitado

As a jogador do grupo,
I want que o Bustling Fungus deixe de custar mais a cada stack sem perder a cura,
So that partidas com 50+ stacks continuem jogáveis (FR-2).

**Acceptance Criteria:**

**Given** a interface `IStackPolicy` no `Core` com os modos `Off` e `Clamp` (e `Aggregate` somente se o relatório 1.3 e a linha de base 1.6 o justificarem) e testes `dotnet test`
**When** executo os testes
**Then** cobrem stack 0, 1, no limite, acima do limite e `int.MaxValue`, sem estouro
**And** `Off` devolve o stack original.

**Given** `LimitedCalcFeature` com uma lista de itens, cada entrada com política e flag própria (`LimitedCalc.BustlingFungus.Mode`)
**When** o jogo inicia
**Then** o hook do Fungus (nomes do relatório 1.3) é registrado e logado como sucesso ou falha
**And** adicionar um item novo exige só uma nova entrada na lista, sem mudar `Feature` nem o `Core` (FR-5).

**Given** o Fungus com 50+ stacks e o modo `Clamp` ligado, só no host (`NetworkServer.active`)
**When** o efeito é processado a cada tick
**Then** o trabalho por tick deixa de crescer com os stacks acima do limite
**And** a cura continua ocorrendo e `orig` é sempre chamado
**And** o p95 do custo do hook com 50+ stacks fica em no máximo X% do p95 com o stack no limite, com X definido a partir da linha de base da Story 1.6 e registrado em `docs/`
**And** não há alocação por tick no hook.

**Given** a flag do item em `Off`
**When** a partida roda
**Then** o comportamento é o do jogo base (FR-4)
**And** o limite escolhido e o modo estão registrados com a justificativa (dado da linha de base).

### Story 2.2: Bundle of Fireworks com custo limitado

As a jogador do grupo,
I want que o Bundle of Fireworks não gere custo ilimitado com ~200 stacks,
So that ativar interagíveis não derrube o FPS (FR-3).

**Acceptance Criteria:**

**Given** a `LimitedCalcFeature` da Story 2.1
**When** adiciono a entrada do Fireworks com sua política e flag (`LimitedCalc.BundleOfFireworks.Mode`)
**Then** nenhuma alteração é feita no `Core` nem na base `Feature`
**And** o hook é registrado e logado como sucesso ou falha.

**Given** um jogador com ~200 stacks e o modo escolhido pelo relatório 1.3 (manter a quantidade e otimizar, ou agregar)
**When** ele ativa um interagível
**Then** o custo por interação deixa de crescer sem limite
**And** o p95 do custo da interação com ~200 stacks fica em no máximo X% do p95 com o stack no limite, com X definido a partir da linha de base da Story 1.6 e registrado em `docs/`
**And** o dano total por interação medido é registrado em `docs/` para o julgamento de "efeito reconhecível" na Story 2.3
**And** `orig` é chamado e a lógica só roda no host.

**Given** a opção `Aggregate` escolhida
**When** vários projéteis seriam criados
**Then** o número de objetos criados é menor que o do jogo base para o mesmo stack
**And** não há alocação por tick no caminho alterado.

**Given** a flag do item em `Off`
**When** a partida roda
**Then** o comportamento é o do jogo base.

### Story 2.3: Validação de desempenho e de efeito reconhecível

As a autor do mod,
I want comparar o cenário de referência com o cálculo limitado ligado e desligado,
So that eu prove o ganho e confirme que os itens seguem úteis (NFR-1, contra-métrica do PRD).

**Acceptance Criteria:**

**Given** o cenário de referência da Story 1.6 e o `Probe` ligado
**When** meço com `LimitedCalc` desligado e ligado (condição 2, sem multiplicação)
**Then** os CSVs registram frame time médio e p95 e RAM nas duas situações
**And** o resultado é comparado com a linha de base.

**Given** o efeito de cada item
**When** defino o critério de "efeito reconhecível" (por exemplo, cura ou dano total por segundo em relação ao jogo base)
**Then** o critério e o valor medido ficam registrados em `docs/`
**And** se o critério for violado, o limite é revisto antes de fechar a story.

**Given** uma partida de 3 jogadores com a feature ligada
**When** jogo uma fase completa
**Then** não há dessincronização nem exceções em `Fail()` no log (`ror2meeeee_status` limpo).

### Story 2.4: Sincronização do limite para clientes (condicional)

Só entra no sprint se o relatório da Story 1.3 concluir que o efeito do Fungus ou do Fireworks roda também no cliente (A8 = não).

As a jogador cliente do grupo,
I want que o limite de cálculo também valha para os efeitos executados no meu jogo,
So that o ganho de desempenho não dependa de o efeito rodar só no host (NFR-2, ADR-1).

**Acceptance Criteria:**

**Given** o relatório 1.3 indicando que o efeito roda no cliente
**When** o host inicia a run
**Then** o modo e o limite do item limitado são enviados uma única vez aos clientes via `INetMessage` (R2API.Networking, declarada em `[BepInDependency]`), sem tráfego por tick
**And** o cliente aplica a mesma política da `LimitedCalcFeature` com os valores do host.

**Given** um cliente que entra ou reconecta
**When** a run já está em andamento
**Then** ele recebe a configuração atual do host antes de aplicar o limite
**And** se não recebê-la, o limite fica desligado, com `Warning`.

**Given** o cenário de referência com host e cliente reais
**When** jogo uma fase completa
**Then** não há dessincronização e o custo no cliente também respeita o limiar da Story 2.1 ou 2.2
**And** a story é marcada como desnecessária e removida se o relatório 1.3 concluir "host-only suficiente".

## Epic 3: Multiplicação de itens

Cada pickup elegível rende N cópias (padrão x2), decidido no host, respeitando a tabela de elegibilidade e o teto de stacks. Fecha a condição 3 da linha de base. Cobre FR-1 (condição 3), FR-6, FR-7, FR-8, NFR-1, NFR-2.

### Story 3.1: Pipeline puro de multiplicação no Core

As a autor do mod,
I want a regra de multiplicação e elegibilidade como funções puras testadas,
So that a lógica seja correta antes de tocar no jogo (FR-6, FR-7, FR-8).

**Acceptance Criteria:**

**Given** `Core/Pipeline` com `Resolve(PickupContext, MultiplierRules)` que devolve a lista de concessões, e `Core/Eligibility` com a tabela de elegibilidade
**When** executo `dotnet test`
**Then** os testes cobrem multiplicador 1 (sem concessão extra), 2, e um valor muito alto com saturação em `int.MaxValue` sem estouro
**And** cobrem multiplicador 0 ou negativo corrigido para o padrão.

**Given** os tipos de item (branco, verde, vermelho, boss, lunar, void, temporário, scrap, equipamento)
**When** `Resolve` é chamado com `IncludeLunar` e `IncludeVoid` em `false` e `true`
**Then** lunar e void só multiplicam quando ligados
**And** temporário, scrap e equipamento nunca multiplicam.

**Given** um jogador marcado como morto ou desconectado
**When** `Resolve` é chamado
**Then** nenhuma concessão é devolvida.

**And** o projeto `Ror2Meeeee.Core` continua sem referência ao jogo, BepInEx ou Unity.

### Story 3.2: Multiplicação no pickup, no host

As a jogador do grupo,
I want receber cópias extras a cada pickup elegível,
So that eu tenha mais itens e poder de build (FR-6).

**Acceptance Criteria:**

**Given** `MultiplierFeature` com a config `Multiplier.Value` (padrão 2) e o hook no ponto indicado pelo relatório 1.3
**When** o jogo inicia
**Then** o hook é registrado e logado como sucesso ou falha
**And** `Multiplier.Value = 1` desliga a mecânica sem custo mensurável.

**Given** o host e um pickup elegível de 1 cópia com `Value = 2`
**When** o pickup acontece
**Then** o inventário do jogador recebe 2 cópias no total
**And** a concessão extra usa `GiveRaw` (`GiveItemPermanent`) com a guarda `_granting`, sem recursão ou multiplicação em cascata
**And** `orig` é chamado e a lógica só roda com `NetworkServer.active`.

**Given** um cliente (não host) com a feature habilitada
**When** o pickup é processado
**Then** o cliente não concede nada e usa a config do host (NFR-2).

**Given** um evento que gera exceção na concessão
**When** o hook falha
**Then** `Fail()` registra e `_granting` volta a `false`.

### Story 3.3: Elegibilidade e jogadores mortos

As a jogador do grupo,
I want que só os pickups certos sejam multiplicados e que mortos não recebam,
So that a regra seja previsível e justa (FR-8).

**Acceptance Criteria:**

**Given** a seção `Multiplier` com `IncludeLunar` e `IncludeVoid` (padrão `false`)
**When** um item lunar ou void é coletado
**Then** só é multiplicado se a respectiva opção estiver ligada.

**Given** itens comuns vindos de baú, drop de inimigo e impressora
**When** coletados
**Then** são multiplicados
**And** itens temporários, scrap e equipamento não são.

**Given** um jogador morto ou desconectado
**When** um pickup ocorre
**Then** ele não recebe cópias extras
**And** não há acúmulo retroativo ao reviver ou reconectar.

**Given** concessões que não são pickup (itens iniciais da run, conversões de itens, trocas de void ou lunar, scrapper, Artifact of Command), conforme a lista de origens do relatório 1.3
**When** o hook as intercepta
**Then** elas não são multiplicadas, ou só o são se a regra de elegibilidade as listar explicitamente como pickup
**And** o comportamento por origem fica registrado em `docs/`.

**And** a tabela real do jogo (tipos e tiers) foi conferida com o relatório 1.3 e as divergências estão registradas.

### Story 3.4: Teto de stacks configurável

As a autor do mod,
I want um teto de stacks por item e por jogador, opcional,
So that a multiplicação não agrave o desempenho de itens sem cálculo limitado (FR-7).

**Acceptance Criteria:**

**Given** `Multiplier.StackCap` com padrão desligado (sem teto)
**When** a partida roda com o padrão
**Then** nenhum teto é aplicado.

**Given** `StackCap` ligado com um valor N e um item sem cálculo limitado
**When** o pickup multiplicado levaria o stack acima de N
**Then** o total concedido é reduzido para não passar de N
**And** o teto é checado depois da multiplicação.

**Given** um item que já está no `LimitedCalc` (Épico 2)
**When** o pickup é multiplicado
**Then** o teto não é aplicado a ele.

**And** os testes do `Core` cobrem stack abaixo, no teto e acima do teto.

### Story 3.5: Condição 3 da linha de base

As a autor do mod,
I want medir o cenário de referência com a multiplicação ligada,
So that eu confirme que o mod, com multiplicação, está melhor que o jogo sem mod (FR-1, NFR-1).

**Acceptance Criteria:**

**Given** o cenário de referência da Story 1.6 e o `Probe` ligado
**When** meço a condição 3 (mod com `LimitedCalc` e `Multiplier` ligados)
**Then** o CSV é guardado em `docs/` ao lado das condições 1 e 2
**And** a comparação das três condições está documentada (frame time médio, p95, RAM).

**Given** as metas de FPS e RAM adiadas na Story 1.6 (se for o caso)
**When** reviso os resultados
**Then** as metas numéricas ficam registradas
**And** se a condição 3 for pior que a condição 1, o limite do Épico 2 ou o teto do 3.4 é revisto antes de fechar.

**Given** uma partida de 3 jogadores com host e clientes reais
**When** jogo uma fase completa
**Then** não há dessincronização nem falhas em `ror2meeeee_status`.

## Epic 4: Dificuldade aumentada

Um fator configurável no host escala vida, dano, quantidade de inimigos e ritmo da escalada, para compensar o poder extra. Cobre FR-11, NFR-2, NFR-4. Valores por eixo e curva são calibrados em playtest (Story 4.4).

### Story 4.1: Fator de dificuldade e curva no Core

As a autor do mod,
I want a conversão do fator único em ganhos por eixo como função pura testada,
So that a curva seja previsível e ajustável sem tocar no jogo (FR-11).

**Acceptance Criteria:**

**Given** `Difficulty.Factor` (padrão 1.0, que não altera nada) e ganhos configuráveis por eixo (vida, dano, quantidade, ritmo) no `Core`
**When** executo `dotnet test`
**Then** os testes cobrem fator 1.0 (multiplicadores 1.0 em todos os eixos), fator maior, fator inválido (≤ 0 ou NaN) corrigido para o padrão e valores extremos sem estouro
**And** a curva (linear no MVP) é uma função única e isolada.

**Given** a seção `Difficulty` na config (escopo Host)
**When** o jogo lê a configuração
**Then** os valores são lidos uma vez para campos em cache e atualizados só em `SettingChanged`
**And** valor inválido gera `Warning` e volta ao padrão.

### Story 4.2: Vida e dano dos inimigos escalados

As a jogador do grupo,
I want inimigos com mais vida e dano conforme o fator,
So that a partida continue desafiadora com mais itens (FR-11, UJ-3).

**Acceptance Criteria:**

**Given** `RoR2.dll` descompilado para os pontos de stats, orçamento de spawn e coeficiente de dificuldade
**When** localizo tipos e métodos e os registro em `docs/` (confirmando se rodam só no servidor)
**Then** `DifficultyFeature` registra os hooks correspondentes no `Awake`, logados como sucesso ou falha.

**Given** `DifficultyFeature` e o hook de stats dos inimigos
**When** o jogo inicia
**Then** o hook é registrado e logado como sucesso ou falha
**And** `orig` é sempre chamado e a lógica só roda com `NetworkServer.active`.

**Given** `Factor > 1`
**When** um inimigo é gerado ou tem os stats recalculados
**Then** vida máxima e dano são multiplicados pelos ganhos do `Core`
**And** jogadores e aliados não são afetados.

**Given** `Factor = 1.0` ou a feature desligada
**When** a partida roda
**Then** o comportamento é o do jogo base
**And** não há alocação por tick no caminho alterado.

### Story 4.3: Quantidade e ritmo escalados

As a jogador do grupo,
I want mais inimigos e uma escalada mais rápida conforme o fator,
So that o desafio acompanhe o poder extra dos itens (FR-11).

**Acceptance Criteria:**

**Given** hooks do orçamento de spawn e do coeficiente de dificuldade (nomes confirmados por investigação do `RoR2.dll`)
**When** `Factor > 1`
**Then** o orçamento de spawn e a velocidade da escalada são multiplicados pelos ganhos do `Core`
**And** `orig` é chamado e a lógica só roda no host.

**Given** `Factor = 1.0` ou a feature desligada
**When** a partida roda
**Then** o comportamento é o do jogo base.

**Given** o cenário de referência com `Probe`
**When** `Factor > 1`
**Then** o custo adicional dos hooks de dificuldade aparece separado no CSV
**And** o aumento de inimigos é registrado, porque também aumenta o custo de IA (hipótese 4 do addendum) e deve ser considerado na meta de FPS.

**Given** uma exceção em qualquer hook de dificuldade
**When** o hook falha
**Then** `Fail()` conta e `orig` é chamado.

### Story 4.4: Calibração em playtest

As a autor do mod,
I want calibrar o fator e os ganhos por eixo jogando com o grupo,
So that a partida fique desafiadora sem deixar de ser divertida (UJ-3, contra-métrica do PRD).

**Acceptance Criteria:**

**Given** uma partida de 3 jogadores com multiplicação x2 e `LimitedCalc` ligados
**When** jogo pelo menos uma partida longa com alguns valores de `Factor`
**Then** o `docs/` registra o fator e os ganhos por eixo testados e a impressão do grupo (muito fácil, adequado, injogável)
**And** os valores padrão finais ficam definidos na config.

**Given** os valores finais
**When** reviso o desempenho
**Then** a medição com `Probe` mostra se a dificuldade aumentada mantém FPS e RAM dentro das metas de NFR-1
**And** se não mantiver, a decisão (reduzir a quantidade, por exemplo) fica registrada.

**Given** uma partida com host e clientes reais
**When** jogo uma fase completa
**Then** não há dessincronização nem falhas em `ror2meeeee_status`.

## Epic 5: Corte de partículas

O jogador desabilita ou encurta partículas de efeitos de itens por uma lista configurável, sem alterar dano, cura nem colisão. Cobre FR-12, FR-13, NFR-4. Config local (por cliente), desvio consciente registrado na arquitetura.

### Story 5.1: Levantamento e medição das partículas

As a autor do mod,
I want saber quais partículas de itens pesam mais e quanto o corte ganha,
So that eu escolha a lista inicial com base em dado, não em palpite (FR-12, PRD §9.3).

**Acceptance Criteria:**

**Given** o cenário de referência e o `Probe` com contador de partículas ativas
**When** meço a partida com os efeitos dos itens do cenário
**Then** `docs/` registra, por efeito, a contagem de sistemas de partículas ativos e o impacto em frame time
**And** a lista é ordenada por custo.

**Given** o `Probe` da Story 1.4 sem contador de partículas
**When** estendo o `Probe` com um contador de sistemas de partículas ativos por efeito de item (buffers pré-alocados, sem custo com `ProbeEnabled = false`)
**Then** o CSV passa a incluir essa coluna sem alterar as colunas existentes.

**Given** `RoR2.dll` descompilado
**When** localizo onde os efeitos visuais dos itens são instanciados e como são identificados (índice, prefab ou nome resolvido uma vez)
**Then** `docs/` registra tipos e métodos candidatos a hook, para a Story 5.2
**And** registra se o corte pode ser feito sem tocar em dano, cura ou colisão.

**Given** a lista ordenada
**When** escolho candidatos
**Then** a lista inicial de efeitos a cortar fica registrada com a justificativa
**And** a medição do ganho potencial (corte total dos candidatos) fica anexada.

**Given** o ganho medido ser desprezível
**When** reviso com você
**Then** a decisão de seguir ou adiar o Épico 5 fica registrada antes das stories 5.2 e 5.3.

### Story 5.2: Corte configurável de partículas

As a jogador do grupo,
I want desabilitar ou encurtar efeitos visuais de itens por uma lista na config,
So that o jogo fique menos poluído e mais leve (FR-12, JTBD-3).

**Acceptance Criteria:**

**Given** `ParticlesFeature` e a seção `Particles` (`Enabled`, lista de efeitos, ação `Disable` ou `Shorten`), com escopo local (por cliente)
**When** o jogo inicia
**Then** o hook de criação do efeito é registrado e logado como sucesso ou falha
**And** `orig` é sempre chamado e a identificação do efeito é feita por índice ou referência resolvida uma vez, nunca por string a cada ocorrência.

**Given** um efeito da lista com ação `Disable`
**When** o efeito seria exibido
**Then** não aparece para aquele jogador
**And** os demais jogadores não são afetados.

**Given** um efeito da lista com ação `Shorten`
**When** é exibido
**Then** sua duração é reduzida pelo fator configurado.

**Given** a feature desligada ou a lista vazia
**When** a partida roda
**Then** o comportamento visual é o do jogo base
**And** não há alocação por tick no caminho alterado.

### Story 5.3: Validação de gameplay inalterado

As a autor do mod,
I want comprovar que o corte visual não muda dano, cura nem colisão,
So that a mecânica continue idêntica com ou sem o corte (FR-13).

**Acceptance Criteria:**

**Given** o cenário de referência com um item de cada efeito da lista
**When** comparo dano total, cura total e hits com o corte ligado e desligado em seed fixo
**Then** os valores são iguais dentro do ruído esperado, registrado em `docs/`.

**Given** os efeitos cortados que têm colisão ou dano associado (por exemplo, projéteis com partícula)
**When** reviso o relatório
**Then** os efeitos em que o corte altera gameplay são removidos da lista padrão e anotados.

**Given** o `Probe` ligado
**When** meço com o corte ligado
**Then** a redução de partículas ativas e o ganho de frame time ficam registrados em relação à linha de base (JTBD-3)
**And** uma partida com host e clientes reais não mostra dessincronização.

## Epic 6: Compartilhamento de itens (v2)

Todo pickup elegível é replicado integralmente aos demais jogadores, incluindo as cópias da multiplicação, sem recursão e com a ordem pickup → multiplicação → compartilhamento. Fora do MVP. Cobre FR-9, FR-10, NFR-1, NFR-2.

### Story 6.1: Compartilhamento no pipeline puro

As a autor do mod,
I want estender o pipeline puro para devolver também as concessões aos demais jogadores,
So that a ordem e a não recursão sejam garantidas por testes antes de tocar no jogo (FR-9, FR-10).

**Acceptance Criteria:**

**Given** `Resolve(PickupContext, MultiplierRules, SharingRules)` no `Core/Pipeline` e `dotnet test`
**When** executo os testes
**Then** um pickup de 1 cópia com multiplicador 2 e 3 jogadores vivos devolve 2 cópias ao dono e 2 a cada um dos outros dois
**And** a ordem é pickup → multiplicação → compartilhamento.

**Given** uma concessão originada por compartilhamento
**When** ela é processada de novo pelo `Resolve`
**Then** não é multiplicada nem compartilhada (FR-10).

**Given** `Sharing.Enabled = false`
**When** `Resolve` é chamado
**Then** o resultado é igual ao do Épico 3.

**Given** jogador morto ou desconectado, e itens inelegíveis (temporário, scrap, equipamento; lunar e void conforme config)
**When** `Resolve` é chamado
**Then** nenhuma concessão compartilhada é devolvida para eles ou para o item
**And** os totais usam saturação em `int.MaxValue` sem estouro.

**And** o `Core` continua sem referência ao jogo, BepInEx ou Unity.

### Story 6.2: Replicação no host

As a jogador do grupo,
I want que o item que eu pego chegue integralmente aos meus amigos,
So that o grupo cresça junto (FR-9, UJ-2).

**Acceptance Criteria:**

**Given** `SharingFeature` com `Sharing.Enabled` (escopo Host, padrão desligado) e o mesmo ponto de interceptação da `MultiplierFeature`
**When** o jogo inicia
**Then** o hook é registrado e logado como sucesso ou falha
**And** a feature não referencia a `MultiplierFeature` (usa só `Common` e `Core`).

**Given** o host, `Sharing.Enabled = true` e um pickup elegível
**When** o pickup acontece
**Then** cada jogador vivo recebe a quantidade integral definida pelo `Resolve`
**And** a concessão usa `GiveRaw` com a guarda `_granting`, sem recursão
**And** `orig` é chamado e a lógica só roda com `NetworkServer.active`.

**Given** `Sharing.Enabled = false`
**When** a partida roda
**Then** o compartilhamento não ocorre e o comportamento é o do Épico 3 (FR-10).

**Given** uma exceção na concessão
**When** o hook falha
**Then** `Fail()` registra e `_granting` volta a `false`.

### Story 6.3: Validação de desempenho e rede

As a autor do mod,
I want medir o impacto do compartilhamento e confirmar a sincronia com 3 jogadores,
So that o total de stacks maior não quebre o orçamento de desempenho (NFR-1, NFR-2).

**Acceptance Criteria:**

**Given** o cenário de referência e o `Probe` ligado
**When** meço com `Sharing` desligado e ligado (mod com multiplicação)
**Then** os CSVs registram frame time médio e p95 e RAM nas duas situações
**And** a comparação com a linha de base está em `docs/`.

**Given** o resultado pior que o orçamento definido no Épico 3
**When** reviso com você
**Then** a decisão (teto de stacks da Story 3.4, limite do Épico 2 ou manter desligado) fica registrada.

**Given** uma partida com host e clientes reais
**When** jogo uma fase completa com o compartilhamento ligado
**Then** não há dessincronização, duplicação de concessão nem falhas em `ror2meeeee_status`
**And** a avaliação do modelo alternativo (baús instanciados, do addendum) fica registrada como não adotada ou adotada com motivo.
