# Ror2Meeeee

Mod para **Risk of Rain 2** (1.5.0#1224, build Steam 25921972) feito com BepInEx 5 e R2API. Deixa o jogo mais caótico:
multiplica os itens coletados, deixa os inimigos mais fortes e numerosos, adiciona itens, um equipamento e artefatos
novos, e inclui ferramentas para manter o FPS de pé quando o inventário explode.

- **Versão atual:** 0.10.0 (`src/Ror2Meeeee.Core/ModInfo.cs`; histórico em [`package/CHANGELOG.md`](package/CHANGELOG.md))
- **GUID:** `com.sawcunha.ror2meeeee` · **Pacote:** `Sawcunha-Ror2Meeeee`
- **Multiplayer:** todos os jogadores precisam do mesmo mod na mesma versão. A lógica de gameplay roda **só no host**,
  então valem as opções do `.cfg` de quem hospeda.

---

## Instalação

### Pelo r2modman (jogadores)

1. Gere ou baixe `Sawcunha-Ror2Meeeee-<versão>.zip` (veja [Build](#build-e-desenvolvimento)).
2. No r2modman: **Settings → Import local mod** e escolha o zip.
3. As dependências (`package/manifest.json`) são instaladas junto:
   BepInExPack, HookGenPatcher, RoR2BepInExPack 1.45.0 e os módulos R2API Core, ContentManagement, Elites, Items,
   Language e Networking.

Opcional: **Risk of Options** mostra todas as opções numa tela de mods dentro do jogo.

### Instalação manual

Copie `Ror2Meeeee.dll` e `Ror2Meeeee.Core.dll` para `BepInEx/plugins/Sawcunha-Ror2Meeeee/`. Use **só uma pasta**
com o plugin: duas cópias com o mesmo GUID fazem o BepInEx escolher uma delas, e o `Core.dll` pode vir da errada.

---

## Itens

Todos os itens podem ser criados pelo console com `give_item <nome interno> <quantidade>`.
Desligar a seção do item no config mantém o item no jogo, mas sem efeito.

| Item | Raridade | Efeito | Nome interno |
|---|---|---|---|
| **Chamado da Horda** | Branco | +25% no orçamento de spawn de inimigos por cópia (teto em `Horde.MaxMultiplier`) | `Ror2MeeeeeHorde` |
| **Capitalismo** | Branco | Taxa do Taxade −20% e +10% de todo ouro ganho. Máximo 1 por jogador | `Ror2MeeeeeTaxReliefWhite` |
| **Pilha de Pregos Enferrujados** | Branco | 2% de chance por golpe (+0,1% por cópia) de disparar um prego de 10% do dano base (+0,1% por cópia) que aplica sangramento | `Ror2MeeeeeRustyNailStack` |
| **Capitalismo** | Verde | Taxa −30% e +20% de ouro. Máximo 1 por jogador | `Ror2MeeeeeTaxReliefGreen` |
| **Fungo Egoísta** | Verde | Cura 5 de vida/s sempre (+% da vida por cópia extra), só em você e nos seus drones, torretas e ajudantes | `Ror2MeeeeeSelfishFungus` |
| **Égide Vampírica** | Verde | 5% de chance de bloquear um golpe; ao bloquear, rouba vida do agressor | `Ror2MeeeeeVampiricAegis` |
| **Bateria do Portal** | Verde | Teleportador e outras zonas de carga carregam +10% mais rápido por cópia do time | `Ror2MeeeeePortalBattery` |
| **Capitalismo** | Vermelho | Taxa −50% e +30% de ouro. Máximo 1 por jogador | `Ror2MeeeeeTaxReliefRed` |
| **Cano Extra** | Vermelho | Cada cópia faz a arma principal disparar +1 tiro em leque (balas, projéteis e flechas; corpo a corpo não). Até 5 extras. Não é multiplicado | `Ror2MeeeeeCanoExtra` |
| **Contrato da I.M.P.** | Vermelho | O primeiro elite ou chefe do estágio vira alvo marcado; matá-lo paga ouro extra (+100% por cópia, até `ImpContract.MaxGold`) e solta um item verde | `Ror2MeeeeeImpContract` |
| **Bastião** | Vermelho | Escudo de 40% da vida máxima por cópia, que começa a recarregar 2 s depois do último golpe (no jogo são 7 s) | `Ror2MeeeeeBastion` |
| **Coração Pesado** | Lunar | Cada cópia multiplica a vida máxima por 1,5 e o dano recebido de inimigos por 1,3 (composto) | `Ror2MeeeeeHeavyHeart` |
| **Pilha de Espinhos do Vazio** | Vazio (branco) | Corrompe os Pregos. 2% de chance (+0,5% por cópia) de um espinho de 10% do dano base (+0,5% por cópia) que marca o alvo por 3 s. Ao fim: detonação de 250% e chuva de espinhos num raio de 12 m (150%, +50% por cópia) que marca os atingidos, em cadeia até `RustyNail.MaxChain` | `Ror2MeeeeeVoidSpikeStack` |
| **Fungo Egoísta do Vazio** | Vazio (verde) | Corrompe o Fungo Egoísta. Cura 15 de vida/s (+% da vida por cópia extra), só em você e nos seus ajudantes | `Ror2MeeeeeSelfishFungusVoid` |
| **Égide do Devorador** | Vazio (verde) | Corrompe a Égide Vampírica. 5% de chance de bloquear; ao bloquear, rouba vida de todos os inimigos próximos | `Ror2MeeeeeDevourerAegis` |
| **zzz cervo <3** | Vazio (verde) | *A pressa é inimiga da perfeição.* Toda velocidade de movimento extra vira dano (1:1; +50% de conversão por cópia extra). Não corrompe nenhum item; sai em Void Cradles e Potentials | `Ror2MeeeeeCervo` |

Os itens Capitalismo não duplicam e deixam de sair em baús para quem já tem o da mesma cor.

### Equipamento

| Equipamento | Tipo | Efeito | Nome interno |
|---|---|---|---|
| **Grimório do Stolas** | Lunar (recarga 60 s) | Teleporta para perto do primeiro aliado vivo (sozinho: para o teleportador do estágio). Cada uso dá crédito ao diretor, que manda uma onda de inimigos | `Ror2MeeeeeStolasGrimoire` |

Console: `give_equipment Ror2MeeeeeStolasGrimoire`.

---

## Artefatos

Só têm efeito se forem ativados no lobby.

| Artefato | Efeito |
|---|---|
| **Comunismo** | Todo item que um jogador pega também vai para os outros jogadores vivos: todos os tipos, de todas as expansões, inclusive sucata, temporários e alugados. Equipamentos e itens únicos do mod não são compartilhados. Não tem opção no config: o artefato é o interruptor |
| **Dízimo** | Parte de todo ouro ganho (`Gold.Ratio`, 10%) vira crédito para o diretor gerar mais inimigos, e o diretor agradece no chat a cada `ThankIntervalSeconds` (60 s). Limites: `MaxCreditPerSecond` (150) e `MaxMonsters` (80 vivos) |
| **Taxade** | Toda compra paga com ouro (baú, santuário, drone...) custa vida: 30% da vida máxima por padrão (`Tax.Percent`). Compras com outras moedas não pagam. Com `Tax.Lethal = false` (padrão), a taxa nunca mata: deixa 1 de vida. Os itens Capitalismo reduzem a taxa |
| **Vazio** | +5 baús do Vazio por estágio (`VoidBounty.ExtraChests`), cada um sorteando entre todos os itens do Vazio. Inimigos com +50% de vida |
| **Ressurreição** | Quem morre deixa uma lápide. Um aliado que fica a até 4 m dela por 3 s revive o jogador com metade da vida, pagando metade da própria vida. Só faz sentido em multiplayer; `ror2meeeee_demo` mostra os marcadores na tela |

---

## Sistemas de gameplay

### Multiplicador de pickup (`[Multiplier]`)

Cada item elegível coletado rende **N cópias** (`Value`, padrão **10**).

- Lunares e itens do Vazio também multiplicam (`IncludeLunar` e `IncludeVoid`, ligados por padrão).
- `StackCap` (padrão **150**) é o teto de cópias de um mesmo item por jogador, para qualquer item: a multiplicação e o
  compartilhamento param no teto, e um item pego com o stack já no teto **some** (aviso no chat). Impressora,
  caldeirão e `give_item` não passam por esse teto. 0 = sem teto.
- Não multiplicam: itens iniciais, conversões (Vazio, sucata), itens de monstros, temporários e o Cano Extra.
- Jogadores mortos não recebem as cópias extras.

### Dificuldade (`[Difficulty]`)

Deixa os inimigos mais fortes e numerosos. Tudo roda no host.

- **Bônus fixo:** `MonsterHealthBonus` (+75% de vida) e `MonsterDamageBonus` (+50% de dano), aplicados com os itens
  `BoostHp`/`BoostDamage` do próprio jogo, uma vez por inimigo.
- **Fator único** (`Factor`, 1,0 = sem mudança), distribuído por ganhos separados:
  - `QuantityGain` (2): mais créditos para o diretor, ou seja, mais inimigos;
  - `PaceGain` (0,5): a dificuldade sobe mais rápido com o tempo;
  - `HealthGain` (1) e `DamageGain` (0,5): vida e dano.
- Também aplica o efeito do item **Chamado da Horda**.

### Progressão entre runs (`[Progression]`)

No fim de cada run, o grupo recebe uma nota baseada em velocidade, mortes, dano sofrido e abates. As metas de
referência ficam nas opções `Par*`. O resumo aparece no chat (`ShowInChat`).

- Vitória com nota a partir de `RaiseAt` (0,5) sobe um nível; derrota desce um.
- Cada nível muda o orçamento de spawn do diretor da próxima run em `Step` (15%), até `MaxMultiplier` (3×).
- Run abandonada no meio não conta.
- O nível fica em `BepInEx/config/ror2meeeee-progress.txt` e o histórico em `BepInEx/ror2meeeee-runs.csv`.
- O cálculo roda fora da thread principal.

### Bazar Maior (`[Bazaar]`)

O Bazar entre Tempos vira loja. Reescrita do BiggerBazaar 1.14.5 (MagnusMagnuson). Tudo roda no host.

- Ao entrar no portal do bazar, o ouro **não** vira XP (`KeepMoney`): fica para gastar. O que sobrar vira XP ao sair.
- Até 6 baús (`Chests`) com o item à mostra. O tier de cada baú é sorteado por peso (`<Tier>Weight`; padrão 55% branco,
  30% verde, 15% vermelho; chefe, lunar e equipamentos desligados). Preço em ouro = `<Tier>Cost` baús pequenos do
  estágio anterior (1 / 2 / 16); com `PayWithLunarCoins`, `<Tier>LunarCost` moedas lunares.
- Cada baú vende `<Tier>Stock` vezes (10 em todos os tiers; -1 = sem limite); sem limite de compras por jogador.
- `TemporaryChance` (10%): o baú vende `TemporaryAmount` (5) cópias temporárias por compra.
- Casulo ao lado do Newt: 1 moeda lunar = `LunarCoinGold` (50, escala com a dificuldade), até `MaxExchangesPerPlayer`
  (3) vezes.
- O item comprado cai no chão e passa pelo Multiplicador e pelo Comunismo como qualquer pickup. Itens únicos do mod
  não aparecem na loja; o hack do Captain não abre os baús.

### Elites novos (`[Elites]`)

Cinco elites a mais no sorteio do diretor. Reescrita do MoreElites 0.9.5 (Nuxlar) e do BlightedElites 1.2.2 (Moffein),
só com assets do jogo. Tudo roda no host; cada elite tem sua chave (`Volatile`, `Frenzied`, `Empowering`, `Blighted`,
`Echo`), que vale para os próximos spawns.

- **Volátil** (T1): todo golpe explode (como um Brilliant Behemoth) e dispara um míssil a cada 1 a 5 s.
- **Frenético** (T1): +2 de velocidade base e +50% de velocidade de ataque (o elite amarelo que o jogo tem e não usa);
  a cada 10 s se teleporta para perto do alvo. O aspecto usado por um jogador teleporta 25 m na direção do movimento.
- **Fortalecedor** (T1): Warbanner de 25 m para os aliados. Nos clientes, o anel aparece com o visual do da Celestine.
- **Arruinado** (T2): dois afixos T1 aleatórios e diferentes (Flamejante, Glacial, Sobrecarregado, Mending). 16× vida, 4× dano.
- **Eco** (T2): o elite Eco que já existe no jogo, mas que ele não sorteia: 2 ecos com 10% da vida que atiram projéteis
  caçadores.

Stats iguais aos dos elites do jogo (T1, T1 de Honra e Malaquita). Os aspectos caem com a chance do jogo (1 em 4000).

### Soltar itens (`[Drop]`)

Solta itens do seu inventário no chão para dar a um aliado.

- **Menu:** tecla **F7** (`Drop.MenuKey`). Clique no ícone, ajuste a quantidade (campo, −1/+1/+5/+10, setas ou +/−) e
  confirme em **SOLTAR** ou com Enter.
- **Console:** `ror2meeeee_drop [nome] [quantidade]` (sem argumentos, lista o que dá para soltar) e
  `ror2meeeee_drop_menu [nome]`.
- No máximo `MaxPerCommand` (10) por vez.

### Tela de status (F8)

Mostra seus atributos atuais e, de todos os jogadores, o total da run de dano causado, cura e dano recebido. Tecla em
`Stats.MenuKey`. Precisa do R2API Networking.

---

## Desempenho

Com o multiplicador em 10×, o inventário cresce muito rápido. Estes sistemas seguram o FPS sem tirar o efeito dos itens:

| Seção | O que faz | Padrão |
|---|---|---|
| `[LimitedCalc.BustlingFungus]` | Trata o Bustling Fungus como se tivesse no máximo `Limit` cópias (`Mode = Clamp`) | `Off`, limite 10 |
| `[LimitedCalc.BundleOfFireworks]` | `Aggregate`: menos mísseis, cada um mais forte, mesmo dano total | `Aggregate`, limite 20 |
| `[LimitedCalc.GhorsTome]` | `Aggregate`: menos pacotes de ouro no chão, cada um vale mais, mesmo ouro esperado | `Aggregate`, limite 5 |
| `[ProjectileBudget]` | Teto de projéteis dos jogadores e ajudantes (Sticky Bomb, Molten Perforator, estacas...): menos projéteis, cada um mais forte, mesmo dano total. Projéteis de monstros não mudam | 15/s por tipo, 80/s no total |
| `[GhostBlades]` | Limita quantas Lâminas Fantasmas (item Curse do DLC4) o jogo gera ou renova por segundo | 15/s |
| `[Cleanup]` | Cadáveres somem rápido, mesmo à vista, e corrige um erro do jogo no Totem dos Lynx que gerava centenas de exceções | 10 corpos, 2 s |
| `[GameFixes]` | Corrige erros do jogo que se repetiam centenas de vezes por partida e custavam FPS: Runic Lens, Unstable Transmitter, War Bonds e equipamento de corpos sem inventário; também conserta o artefato do Enxame e os drones/monstros Dourados que travavam a partida soltando pepitas de ouro (mesma correção do GildedChunkSpamFix) | ligado |
| `[Particles]` | Teto por segundo (`MaxPerSecond`) para **todo** tipo de efeito, menos os da lista `Exempt`; os da lista `Raised` têm teto próprio maior (`RaisedMaxPerSecond`). Vale para o que você mostra **e para o que você envia** aos outros (host e clientes com o mod). Não muda dano | 10/s por tipo; explosões 60/s; exceção: golpes do Mercenary |

---

## Configuração

O arquivo é `BepInEx/config/com.sawcunha.ror2meeeee.cfg` (só o host precisa ajustar), ou a tela do Risk of Options.

- Os padrões já vêm prontos para jogar: quase tudo ligado. Só o Probe (medição) vem desligado.
- Cada seção tem um `Enabled` com a explicação do que ela faz. `Enabled` vale entre partidas.
- Os outros valores podem ser relidos com `ror2meeeee_reload`.

### `[General]`

| Opção | Padrão | Uso |
|---|---|---|
| `VerboseLogging` | `false` | Logs extras de depuração no `LogOutput.log` (ex.: cada prego, espinho e detonação do RustyNail). Custa FPS |
| `MaxFailures` | 50 (mínimo) | Quantos erros uma feature pode ter antes de se desligar sozinha |
| `ProbeEnabled` | `false` | Liga o Probe de medição |
| `ProbeAutoWindowSeconds` | 30 | Tamanho da janela automática do Probe |

---

## Comandos de console

Abra o console com **Ctrl+Alt+`** (ou F2, conforme o teclado).

| Comando | O que faz |
|---|---|
| `ror2meeeee_status` | Lista as features, falhas e hooks do mod |
| `ror2meeeee_reload` | Relê o `.cfg` (`Enabled` ainda exige reiniciar a partida) |
| `ror2meeeee_drop [nome] [qtd]` | Solta itens do seu inventário |
| `ror2meeeee_drop_menu [nome]` | Abre a tela de soltar itens |
| `ror2meeeee_demo` | Mostra por 15 s os marcadores de teste (lápide e alvo) |
| `ror2meeeee_progress` / `ror2meeeee_progress_reset` | Mostra ou zera o nível da progressão entre runs |
| `ror2meeeee_spend` | Compras e ouro gasto por jogador na run |
| `ror2meeeee_effects [top]` | Efeitos visuais mais frequentes (precisa de `Particles.CountSpawns`); com ele ligado, a lista também sai sozinha no console/log a cada fase nova |
| `ror2meeeee_probe_label <nome>` | Rotula as janelas automáticas seguintes do Probe |
| `ror2meeeee_probe_begin <nome>` / `ror2meeeee_probe_end` | Inicia e encerra uma fase de medição (grava CSV) |

---

## Diagnóstico e medição

- **Sempre ligado:** linhas `[Identidade]`, `[Carga]` (tempo do `Awake` do mod), `[Config]`, `[Estágio]`, `[Vigia]` e erros novos do jogo no `LogOutput.log`.
- **Falha isolada:** cada feature tem um kill-switch. Se der `MaxFailures` erros, ela se desliga sozinha e o resto do mod
  continua. `ror2meeeee_status` mostra quem caiu.
- **Gasto por jogador** (`[Spend]`): conta compras e ouro gasto e grava `BepInEx/ror2meeeee-spend.csv` no fim da run.
- **Probe** (`[General] ProbeEnabled`): mede FPS, monstros, eventos de cura, ajudantes e itens por janela de 30 s,
  com alertas de degradação. Grava CSV em `BepInEx/`; `tools/probe-report.py` resume o arquivo.
- Linhas úteis no log: `[Multiplier]`, `[Difficulty]`, `[Progression]`, `[Tax]`, `[Probe]`, `[ProjectileBudget]`, `[LimitedCalc]`.

---

## Build e desenvolvimento

### Requisitos

- .NET SDK 8 (o plugin compila para `netstandard2.1`; os testes rodam em `net8.0`).
- Risk of Rain 2 instalado. O projeto referencia as DLLs locais de `Risk of Rain 2_Data/Managed`, não o NuGet
  `RiskOfRain2.GameLibs`.
- Um perfil do r2modman com as dependências instaladas (é de lá que vêm as DLLs do R2API e do MMHOOK).

Os caminhos ficam em [`Directory.Build.props`](Directory.Build.props) e podem ser sobrescritos com `-p:` ou variável de
ambiente:

| Propriedade | Padrão |
|---|---|
| `GameDir` | `~/.steam/debian-installation/steamapps/common/Risk of Rain 2` |
| `R2modmanProfile` | `~/.config/r2modmanPlus-local/RiskOfRain2/profiles/Default` |

### Comandos

```bash
dotnet build -c Debug     # compila o plugin e o Core
dotnet test               # testes do Ror2Meeeee.Core (xunit.v3)
tools/pack.sh             # build Release + dist/Sawcunha-Ror2Meeeee-<versão>.zip para o r2modman
```

O `pack.sh` confere se a versão de `ModInfo.cs` bate com `package/manifest.json` antes de empacotar.

### Lançar uma versão

1. Atualize a versão em `src/Ror2Meeeee.Core/ModInfo.cs` e em `package/manifest.json`.
2. Adicione a entrada em `package/CHANGELOG.md` e, se algo mudou para o jogador, em `package/README.md`.
3. Rode `dotnet test` e `tools/pack.sh`.

### Estrutura

```
src/
  Ror2Meeeee.Core/     lógica pura (regras, curvas, pipeline do multiplicador). Nunca referencia o jogo; 100% testável
  Ror2Meeeee/          o plugin BepInEx
    Plugin.cs          ponto de entrada: registra itens, artefatos e features
    Common/            base Feature (kill-switch + falha isolada), Cfg, ModItem, diagnóstico, telas F7/F8
    Features/<Nome>/   uma pasta por mecânica (hooks On.* do MMHOOK)
    Debug/             comandos de console e Probe
    Assets/            ícones embutidos no DLL (artifacts/ = ícones dos artefatos, 128×128)
tests/Ror2Meeeee.Core.Tests/
package/               manifest.json, README.md e CHANGELOG.md do pacote Thunderstore/r2modman
tools/                 pack.sh, probe-report.py
docs/                  artes originais e relatórios de teste e medição
_bmad-output/          planejamento (PRD, GDD, arquitetura, stories)
```

### Regras do código

As regras completas estão em [`_bmad-output/project-context.md`](_bmad-output/project-context.md) e
[`_bmad-output/game-architecture.md`](_bmad-output/game-architecture.md). As que mais quebram:

- Todo hook `On.*` chama `orig`.
- Lógica de gameplay só com `NetworkServer.active` (host).
- Nada de alocação por tick no caminho quente.
- `Ror2Meeeee.Core` nunca referencia o jogo.
- Comportamento do jogo se confirma descompilando o `RoR2.dll` (ex.: `ilspycmd`) e medindo, não por suposição.
- Itens e artefatos novos usam `ModItem.Register` e `TitheArtifact.LoadIcon` (ícones embutidos como `EmbeddedResource`).
