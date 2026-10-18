# Ror2Meeeee

**Idioma:** [English](https://github.com/SawCunha/ror2_meeeeeeee/blob/main/package/README.md) · Português (Brasil)

Mod para **Risk of Rain 2 (1.5.0)** que deixa a run mais caótica: cada item vale por dez, os inimigos ficam mais
fortes e numerosos, e entram artefatos, itens, um equipamento e elites novos. Também traz ferramentas para o FPS
aguentar quando o inventário explode.

**Multiplayer:** todos os jogadores precisam do mod, na **mesma versão**. Quem manda é o **host**: as regras de jogo
rodam na máquina dele e valem as opções do `.cfg` dele.

**Código-fonte, bugs e sugestões:** [https://github.com/SawCunha/ror2_meeeeeeee](https://github.com/SawCunha/ror2_meeeeeeee)

## Instalação

1. Instale pelo **r2modman** ou **Thunderstore Mod Manager** (botão *Install with Mod Manager* nesta página). As
   dependências (BepInEx, R2API, HookGenPatcher) vêm junto.
2. Abra o jogo pelo gerenciador. Os padrões já vêm prontos para jogar; nada precisa ser configurado.
3. Opcional: instale o **Risk of Options** para mudar as opções na tela de mods, dentro do jogo.

## Idiomas

Os textos do mod (itens, artefatos, elites, chat, telas F7/F8 e opções da tela de mods) seguem o idioma escolhido no
jogo: **português** com o jogo em Português (Brasil) e **inglês** em qualquer outro idioma. Cada jogador vê o chat no
próprio idioma. "Taxade" é um termo do mod e fica igual nos dois. O arquivo `.cfg` traz as descrições em inglês.

## Resumo

| Tipo | Conteúdo |
|---|---|
| Artefatos | Comunismo, Dízimo, Taxade, Ressurreição, Vazio |
| Itens brancos | Chamado da Horda, Capitalismo (branco), Pilha de Pregos Enferrujados |
| Itens verdes | Capitalismo (verde), Fungo Egoísta, Égide Vampírica, Bateria do Portal |
| Itens vermelhos | Capitalismo (vermelho), Cano Extra, Contrato da I.M.P., Bastião |
| Itens do Vazio | zzz cervo <3, Fungo Egoísta do Vazio, Égide do Devorador, Pilha de Espinhos do Vazio |
| Item lunar | Coração Pesado |
| Equipamento lunar | Grimório do Stolas |
| Elites | Volátil, Frenético, Fortalecedor, Arruinado, Eco |
| Sistemas | Multiplicador de itens, Dificuldade, Progressão entre runs, Bazar Maior, Cálculo limitado |
| Telas | Soltar itens (F7), Status da run (F8) |
| Desempenho | Teto de partículas, de projéteis e de Lâminas Fantasmas, limpeza de cadáveres, correções do jogo |

Os nomes entre colchetes, como `[Multiplier]`, são as seções do arquivo de configuração (veja *Configuração* no fim).

---

## Artefatos

Artefatos são ligados no **lobby**, na tela de seleção de artefatos, antes de começar a run. Desligados, não mudam nada.

### Artefato do Comunismo

**O que faz:** todo item que alguém pega também vai para os outros jogadores vivos. Vale para itens de todos os tipos e
expansões, inclusive sucata, temporários e alugados. Equipamentos e os itens únicos do mod (Capitalismo, Cano Extra e
Chamado da Horda) não são compartilhados.

**Como usar:** ligue o artefato no lobby. Não tem opções.

**Exemplo:** você abre um baú e pega um Soldier's Syringe. Cada aliado vivo também recebe o Syringe, sem precisar
dividir o baú.

### Artefato do Dízimo (`[Gold]`)

**O que faz:** parte de todo ouro que os jogadores ganham vira crédito para o diretor criar mais inimigos. De tempos
em tempos o diretor agradece no chat o quanto recebeu.

**Como usar:** ligue o artefato no lobby. Opções:
- `Ratio`: quanto do ouro vira crédito (padrão `0.1`, ou seja, 10%).
- `ThankIntervalSeconds`: intervalo das mensagens no chat (padrão 60 s; `0` = sem mensagens).
- `MaxCreditPerSecond` (150) e `MaxMonsters` (80): travas para a partida não virar lag; `0` desliga cada uma.

**Exemplo:** um inimigo dá 50 de ouro. Com `Ratio = 0.1`, o diretor ganha 5 de crédito e gasta em mais inimigos. Com
80 monstros vivos ou mais, o Dízimo para de dar crédito até o número cair.

### Artefato Taxade (`[Tax]`)

**O que faz:** toda compra paga com **ouro** (baú, santuário, drone, impressora...) custa vida. Compras com moedas
lunares ou com itens não são taxadas. Os itens **Capitalismo** reduzem essa taxa.

**Como usar:** ligue o artefato no lobby. Opções:
- `Percent`: vida perdida por compra, em % da vida máxima (padrão 30).
- `Lethal`: desligado (padrão), a taxa nunca mata e deixa você com 1 de vida; ligado, comprar com pouca vida pode matar.

**Exemplo:** com 500 de vida máxima, abrir um baú tira 150 de vida. Com o Capitalismo branco, a taxa cai 20% e o
mesmo baú tira 120.

### Artefato da Ressurreição (`[Revive]`)

**O que faz:** quem morre deixa uma lápide no chão. Um aliado que fique perto dela por alguns segundos revive o
jogador com metade da vida e paga com metade da própria vida. A lápide aparece na tela com marcador e aviso. Só faz
sentido em multiplayer.

**Como usar:** ligue o artefato no lobby. Opções:
- `Radius`: distância da lápide, em metros (padrão 4).
- `RequiredSeconds`: tempo seguido perto dela (padrão 3 s). Sair de perto zera a contagem.

**Exemplo:** seu amigo morre no meio da luta. Você corre até a lápide, fica 3 s a menos de 4 m dela e ele volta com
metade da vida, enquanto você perde metade da sua. O comando `ror2meeeee_demo` mostra como a lápide aparece.

### Artefato do Vazio (`[VoidBounty]`)

**O que faz:** cada estágio ganha baús do Vazio a mais, e todos eles sorteiam entre **todos** os itens do Vazio. Em
troca, os inimigos têm +50% de vida.

**Como usar:** ligue o artefato no lobby. Opção `ExtraChests`: quantos baús a mais por estágio (padrão 5).

**Exemplo:** no primeiro estágio aparecem 5 baús do Vazio além dos normais. Um deles pode dar um Lysate Cell, outro um
Plasma Shrimp, e assim por diante.

---

## Itens

Os itens aparecem nos baús e drops como os do jogo. Cada um tem uma chave `Enabled` na sua seção do `.cfg`: desligada,
o item continua existindo mas não faz nada. Os comandos `give_item` / `give_equipment` dos exemplos precisam do mod
**DebugToolkit** e servem só para testar.

### Chamado da Horda (branco)

**O que faz:** cada cópia que o time tiver aumenta a quantidade de inimigos. Precisa da *Dificuldade* ligada. Não é
multiplicado nem compartilhado.

**Como usar:** pegue o item. Opções em `[Horde]`: `QuantityPerStack` (padrão `0.5`, +50% por cópia) e
`MaxMultiplier` (teto, padrão 20×).

**Exemplo:** o time tem 4 cópias somadas: 1 + 4 × 0,5 = **3× mais inimigos**.
Teste: `give_item Ror2MeeeeeHorde 1`.

### Capitalismo (branco, verde e vermelho)

**O que faz:** cada um reduz a taxa do Taxade e aumenta todo ouro que você ganha, mesmo sem o artefato:

| Item | Reduz a taxa em | Ouro extra |
|---|---|---|
| Branco | 20% | +10% |
| Verde | 30% | +20% |
| Vermelho | 50% | +30% |

No máximo 1 de cada por jogador: não multiplicam e não saem mais nos baús de quem já tem.

**Como usar:** pegue o item. Precisa de `[Tax] Enabled` ligado.

**Exemplo:** com os três, a taxa some (20 + 30 + 50 = 100%) e você ganha +60% de ouro: um inimigo que dava 100 de ouro
passa a dar 160.
Teste: `give_item Ror2MeeeeeTaxReliefWhite 1` (ou `...Green`, `...Red`).

### Pilha de Pregos Enferrujados (branco)

**O que faz:** cada golpe tem chance de disparar um prego que causa sangramento.

**Como usar:** pegue o item. Chance de 2% com 1 cópia (+0,1% por cópia extra). O prego causa 10% de dano base
(+0,1% por cópia extra).

**Exemplo:** com 11 cópias, a chance é de 3% por golpe. Uma arma que acerta muito rápido, como a do Commando, dispara
pregos com frequência.
Teste: `give_item Ror2MeeeeeRustyNailStack 1`.

### Pilha de Espinhos do Vazio (Vazio, branco)

**O que faz:** versão do Vazio dos Pregos. Cada golpe tem chance de disparar um espinho que **marca** o alvo. Depois de
3 s, o alvo marcado sofre uma detonação e solta espinhos em volta (raio de 12 m), que marcam os inimigos próximos.
Corrompe todas as Pilhas de Pregos Enferrujados.

**Como usar:** pegue o item. Chance de 2% (+0,5% por cópia extra); espinho de 10% de dano base (+0,5% por cópia
extra); detonação de 250%; chuva de espinhos de 150% (+50% por cópia extra). Opção `[RustyNail] MaxChain`: quantas
vezes a chuva pode marcar de novo em cadeia (padrão 2; `0` = a chuva causa dano, mas não marca).

**Exemplo:** um espinho marca um inimigo no meio de um grupo; 3 s depois ele explode e os espinhos marcam outros 5
inimigos, que também explodem em seguida.
Teste: `give_item Ror2MeeeeeVoidSpikeStack 1`.

### Fungo Egoísta (verde)

**O que faz:** regenera 5 de vida por segundo, **sempre** (parado ou não), só em você e nos seus drones, torretas e
ajudantes. A cura sobe com o nível, como a regeneração do jogo.

**Como usar:** pegue o item. Cada cópia extra soma +0,01% da vida máxima por segundo.

**Exemplo:** diferente do Bustling Fungus, você não precisa ficar parado: a cura continua enquanto você corre.
Teste: `give_item Ror2MeeeeeSelfishFungus 1`.

### Fungo Egoísta do Vazio (Vazio, verde)

**O que faz:** como o Fungo Egoísta, mas com 15 de vida por segundo e +0,5% da vida máxima por segundo por cópia
extra. Corrompe todos os Fungos Egoístas.

**Exemplo:** com 10 cópias e 1.000 de vida máxima: 15 + 9 × 0,5% × 1.000 = **60 de vida por segundo**, no nível 1.
Teste: `give_item Ror2MeeeeeSelfishFungusVoid 1`.

### Égide Vampírica (verde)

**O que faz:** 5% de chance de bloquear um golpe. Ao bloquear, rouba 8% da vida atual do agressor por cópia (até 50%)
e cura você com isso.

**Como usar:** pegue o item. A chance de bloqueio é fixa; as cópias aumentam o roubo.

**Exemplo:** com 3 cópias, um bloqueio contra um golem com 2.000 de vida tira 24% dela (480) e cura você em 480.
Teste: `give_item Ror2MeeeeeVampiricAegis 1`.

### Égide do Devorador (Vazio, verde)

**O que faz:** 5% de chance de bloquear. Ao bloquear, solta uma onda do Vazio que rouba 3% da vida atual de **cada**
inimigo num raio de 13 m (+3% e +2 m por cópia extra, até 20% e 30 m) e cura você pela soma. Corrompe todas as Égides
Vampíricas.

**Exemplo:** cercado por 10 inimigos, um bloqueio drena um pouco de cada um e devolve a soma como vida.
Teste: `give_item Ror2MeeeeeDevourerAegis 1`.

### Bateria do Portal (verde)

**O que faz:** o teleportador e as outras zonas de carga carregam 10% mais rápido por cópia. As cópias de todo o time
somam.

**Exemplo:** o time tem 5 cópias no total: o teleportador carrega **50% mais rápido** para todos.
Teste: `give_item Ror2MeeeeePortalBattery 1`.

### Cano Extra (vermelho)

**O que faz:** cada cópia faz a arma principal disparar **+1 tiro** em leque. Vale para balas, projéteis e flechas;
ataques corpo a corpo não mudam. Não é multiplicado.

**Como usar:** pegue o item. Opções em `[CanoExtra]`: `MaxExtraShots` (teto de tiros extras, padrão 5, protege o FPS)
e `FanDegrees` (abertura do leque, padrão 5°).

**Exemplo:** com 3 cópias, cada tiro do Huntress vira 4 flechas: a original e outras a ±5° e ±10°.
Teste: `give_item Ror2MeeeeeCanoExtra 1`.

### Contrato da I.M.P. (vermelho)

**O que faz:** no início de cada estágio, o primeiro elite ou chefe vira o alvo do contrato e fica marcado. Matá-lo
paga ouro em dobro (+100% por cópia) e solta um item verde.

**Como usar:** pegue o item e procure o alvo marcado. Opção `[ImpContract] MaxGold`: teto do ouro extra (padrão
20.000; `0` = sem teto).

**Exemplo:** o alvo valeria 300 de ouro. Com 1 cópia, você recebe 600 e um item verde cai no chão.
Teste: `give_item Ror2MeeeeeImpContract 1`.

### Bastião (vermelho)

**O que faz:** dá escudo igual a 40% da vida máxima por cópia, e o escudo começa a recarregar **2 s** depois do último
golpe (no jogo são 7 s).

**Exemplo:** com 1.000 de vida e 2 cópias, você ganha 800 de escudo, que volta logo depois de sair do fogo cruzado.
Teste: `give_item Ror2MeeeeeBastion 1`.

### zzz cervo <3 (Vazio, verde)

**O que faz:** *A pressa é inimiga da perfeição.* Toda velocidade de movimento **extra** vira dano: +1% de velocidade
vira +1% de dano. Você fica na velocidade normal; lentidões continuam valendo. Cada cópia extra soma +50% de conversão.

**Exemplo:** com +40% de velocidade de outros itens e 1 cópia, você anda na velocidade normal e ganha +40% de dano. Com
2 cópias, ganha +60%.
Teste: `give_item Ror2MeeeeeCervo 1`.

### Coração Pesado (lunar)

**O que faz:** cada cópia multiplica a vida máxima por 1,5 e o dano recebido de inimigos por 1,3. As cópias se
multiplicam entre si.

**Exemplo:** com 2 cópias, a vida fica 2,25× maior e o dano recebido 1,69× maior.
Teste: `give_item Ror2MeeeeeHeavyHeart 1`.

---

## Equipamento

### Grimório do Stolas (lunar)

**O que faz:** teleporta você para perto do primeiro aliado vivo. Sozinho, leva até o teleportador do estágio. Em
troca, cada uso dá crédito ao diretor, que manda uma onda de inimigos. Recarga de 60 s.

**Como usar:** ative o equipamento. Opções em `[Grimoire]`:
- `WaveCredit`: crédito por uso, multiplicado pela dificuldade (padrão 150, uma onda média).
- `MaxCreditPerUse`: teto por uso (padrão 600).
- `MaxMonsters`: com tantos monstros vivos, o teleporte funciona mas não chama onda (padrão 80).

**Exemplo:** você ficou para trás no mapa. Usa o Grimório, aparece ao lado do amigo que está no teleportador, e uma
onda de inimigos chega logo depois.
Teste: `give_equipment Ror2MeeeeeStolasGrimoire`.

---

## Elites (`[Elites]`)

O diretor passa a sortear cinco elites novos. Os de nível 1 aparecem desde o começo; os de nível 2, a partir do loop
(depois do estágio 5). Cada um tem a sua chave no `.cfg` (`Volatile`, `Frenzied`, `Empowering`, `Blighted`, `Echo`).
Matar um elite pode soltar o aspecto dele, como no jogo.

| Elite | Nível | O que faz |
|---|---|---|
| **Volátil** | 1 | Todo golpe explode em 4 m (60% do dano) e dispara um míssil a cada 1-5 s. |
| **Frenético** | 1 | Mais velocidade de movimento e de ataque; se teleporta para perto do alvo a cada 10 s. |
| **Fortalecedor** | 1 | Aura de Warbanner de 25 m: +30% de velocidade de ataque e de movimento para os aliados dele. |
| **Arruinado** | 2 | Recebe dois afixos de elite aleatórios ao mesmo tempo. |
| **Eco** | 2 | Invoca 2 ecos com 10% da vida, que atiram projéteis teleguiados e somem quando ele morre. |

**Exemplo:** um Lemuriano Fortalecedor no meio de um grupo deixa todos em volta mais rápidos. Mate ele primeiro.

---

## Sistemas de jogo

### Multiplicador de itens (`[Multiplier]`)

**O que faz:** cada item que um jogador pega rende várias cópias. Não multiplica itens iniciais, itens de monstros,
sucata, temporários, conversões do Vazio nem os itens únicos do mod.

**Como usar:** já vem ligado. Opções:
- `Value`: total de cópias por item coletado (padrão 10).
- `IncludeLunar` / `IncludeVoid`: também multiplicar lunares e do Vazio (padrão: sim).
- `StackCap`: teto de cópias de um mesmo item por jogador (padrão 150; `0` = sem teto). Item pego com o stack no teto
  some, com aviso no chat.

**Exemplo:** você pega 1 Lens-Maker's Glasses e fica com 10. Mais 15 pegas e chega a 150: a próxima some.

### Dificuldade (`[Difficulty]`)

**O que faz:** inimigos mais fortes, mais numerosos e uma escalada de dificuldade mais rápida. Também é o que faz o
*Chamado da Horda* funcionar. Só o host precisa.

**Como usar:** já vem ligada. Opções:
- `Factor`: multiplicador geral (1 = sem mudança; 2 = o dobro, repartido pelos ganhos abaixo).
- `HealthGain`, `DamageGain`, `QuantityGain`, `PaceGain`: quanto do `Factor` vai para vida, dano, quantidade e ritmo.
- `MonsterHealthBonus` (padrão 75) e `MonsterDamageBonus` (padrão 50): vida e dano extras fixos, em %.

**Exemplo:** com os padrões, todo inimigo tem +75% de vida e +50% de dano. Com `Factor = 2`, além disso: +100% de vida,
+50% de dano, 3× mais inimigos e dificuldade subindo 50% mais rápido.

### Progressão entre runs (`[Progression]`)

**O que faz:** no fim de cada run, o grupo recebe uma nota (velocidade, mortes, dano sofrido e abates) e o resumo
aparece no chat. Vitória com nota boa sobe um nível, e cada nível dá mais inimigos na próxima run. Derrota desce um
nível.

**Como usar:** já vem ligada. Opções:
- `Step`: inimigos a mais por nível (padrão `0.15`, +15%).
- `MaxMultiplier`: teto (padrão 3×).
- `RaiseAt`: nota mínima para subir (padrão `0.5`).
- `Par...`: as referências da nota (tempo para carregar o teleportador, mortes, dano e abates).

Comandos: `ror2meeeee_progress` mostra o nível; `ror2meeeee_progress_reset` zera.

**Exemplo:** vocês vencem rápido e sem morrer: nível 1 → 2, e a próxima run começa com +30% de inimigos. Run
abandonada no meio não conta.

### Bazar Maior (`[Bazaar]`)

**O que faz:** o Bazar entre Tempos ganha até 6 baús com o item à mostra, comprados com o ouro que sobrou do estágio.
Ao lado do Newt aparece um casulo que troca moeda lunar por ouro. O item comprado cai no chão e passa pelo
Multiplicador e pelo Comunismo. Com o **Artefato do Comando**, cai o cubo de escolha do mesmo tier, como num baú do
jogo. O item que flutua sobre o baú é só vitrine: não dá para pegar.

**Como usar:** entre no portal azul do bazar. Com `KeepMoney` (padrão ligado), seu ouro **não** vira XP ao entrar:
fica para gastar, e o que sobrar vira XP ao sair. Opções:
- `Chests`: número de baús (1 a 6).
- `PayWithLunarCoins`: baús custam moedas lunares em vez de ouro.
- `LunarCoinGold` e `MaxExchangesPerPlayer`: ouro por moeda no casulo e trocas por jogador.
- Para cada tier: peso, preço, preço lunar e estoque (`WhiteWeight`, `WhiteCost`, `WhiteStock`...).

**Exemplo:** você chega no bazar com 400 de ouro. Os baús mostram dois verdes e um vermelho; um verde custa o preço de
2 baús pequenos do estágio anterior. Sem ouro, você troca 1 moeda lunar no casulo pelo valor de um baú médio.

### Cálculo limitado (`[LimitedCalc]`)

**O que faz:** itens que pesam no FPS com muitas cópias passam a contar até um limite, sem perder o efeito. Vale para
Bustling Fungus, Bundle of Fireworks e Ghor's Tome, cada um com sua seção (`[LimitedCalc.BundleOfFireworks]`...).

**Como usar:** escolha o modo de cada item:
- `Off`: como no jogo.
- `Clamp`: funciona como se tivesse no máximo `Limit` cópias.
- `Aggregate`: menos efeitos, cada um mais forte, com o mesmo resultado total. Fireworks solta menos mísseis com mais
  dano cada; Ghor's Tome solta menos pacotes de ouro, cada um valendo mais.

Padrões: Fireworks `Aggregate`, até 20; Ghor's Tome `Aggregate`, até 5; Fungus `Off`.

**Exemplo:** com 200 Fireworks, abrir um baú soltaria 804 mísseis. Em `Aggregate` com limite 20, saem 84 mísseis, cada
um com quase 10× o dano. O total é o mesmo e o FPS não cai.

---

## Telas

### Soltar itens (F7, `[Drop]`)

**O que faz:** solta itens do seu inventário no chão, para dar a um aliado.

**Como usar:** aperte **F7**, clique no ícone do item, ajuste a quantidade e clique em **SOLTAR** (ou use as setas,
`+`/`-` e Enter). Também pelo console:
- `ror2meeeee_drop`: lista o que dá para soltar.
- `ror2meeeee_drop <nome> <quantidade>`: solta direto.
- `ror2meeeee_drop_menu <nome>`: abre a tela com o item já escolhido.

Opções: `MenuKey` (tecla, padrão `F7`) e `MaxPerCommand` (máximo por vez, padrão 10).

**Exemplo:** você tem 30 Syringes e seu amigo nenhum: `ror2meeeee_drop Syringe 10` solta 10 no chão para ele pegar.

### Status da run (F8, `[Stats]`)

**O que faz:** mostra seus atributos atuais e, para todos os jogadores, o total da run de dano causado, cura e dano
recebido.

**Como usar:** aperte **F8** para abrir e fechar. A tecla muda em `MenuKey`.

**Exemplo:** no fim do estágio, abra o F8 para ver quem causou mais dano e quem tomou mais.

---

## Desempenho

Ligados por padrão, para a partida aguentar inventários enormes. Nenhum muda o dano total.

| Seção | O que faz | Opções principais |
|---|---|---|
| `[Particles]` | Limita quantas vezes por segundo cada tipo de efeito visual aparece e é enviado aos outros. | `MaxPerSecond` (10), `Raised`/`RaisedMaxPerSecond` (explosões, 60), `Exempt` (nunca limitados; padrão golpes do Mercenary) |
| `[ProjectileBudget]` | Itens que disparam em massa (Sticky Bomb, Molten Perforator, estacas, Fireworks, mísseis...) soltam menos projéteis, cada um mais forte. Só projéteis de jogadores. | `Prefabs`, `PerTypeMaxPerSecond` (15), `GlobalMaxPerSecond` (40) |
| `[GhostBlades]` | Limita as Lâminas Fantasmas do item Curse (DLC4), que chegavam a ~700 por segundo. | `MaxPerSecond` (10) |
| `[Cleanup]` | Cadáveres somem rápido, mesmo à vista, e corrige um erro do jogo no Totem dos Lynx. | `MaxCorpses` (10), `CorpseLifetime` (2 s) |
| `[GameFixes]` | Corrige erros do jogo que se repetem centenas de vezes por partida (Runic Lens, Unstable Transmitter, War Bonds), os erros de spawn da Craca do Vazio e de morte da Defective Unit e do Dunestrider, do Baú Mímico, o artefato do Enxame e drones/monstros Dourados que travavam com pepitas. | só `Enabled` |

**Exemplo:** com 100 Sticky Bombs, em vez de 100 bombas por segundo saem 15, cada uma com o dano somado das outras.

---

## Configuração

- **Arquivo:** `BepInEx/config/com.sawcunha.ror2meeeee.cfg`. No r2modman: *Config editor* →
  `com.sawcunha.ror2meeeee`.
- **Na tela de mods:** com o **Risk of Options** instalado, as opções aparecem dentro do jogo.
- **Quem precisa configurar:** só o host.
- **Quando vale:** `Enabled` liga e desliga na hora (entre partidas). Os outros valores são relidos com
  `ror2meeeee_reload`.

## Comandos de console

Abra o console com **Ctrl + Alt + \`** (ou F2, conforme o seu teclado).

| Comando | Para que serve |
|---|---|
| `ror2meeeee_status` | Lista as features, falhas e hooks do mod. |
| `ror2meeeee_reload` | Relê o `.cfg`. |
| `ror2meeeee_drop [nome] [qtd]` | Solta itens (veja *Soltar itens*). |
| `ror2meeeee_drop_menu [nome]` | Abre a tela de soltar itens. |
| `ror2meeeee_progress` / `ror2meeeee_progress_reset` | Nível da progressão entre runs / zerar. |
| `ror2meeeee_spend` | Compras e ouro gasto por jogador na run. |
| `ror2meeeee_effects [top]` | Efeitos visuais mais frequentes (com `[Particles] CountSpawns` ligado). |
| `ror2meeeee_demo` | Mostra a lápide e o marcador de alvo à sua frente por 15 s. |

## Perguntas frequentes

**Meu amigo não consegue entrar.** Todos precisam da mesma versão do mod. Atualize todos pelo gerenciador.

**Peguei um item e ele sumiu.** Você chegou ao teto `[Multiplier] StackCap` (150 cópias). O chat avisa.

**Algo deu errado.** O `BepInEx/LogOutput.log` tem linhas `[Ror2Meeeee]`, e `ror2meeeee_status` mostra se alguma parte
se desligou sozinha depois de erros. Mande o log junto com o relato.

**O jogo travou e tive que fechar.** Abra o jogo de novo e mande o `LogOutput.log` dessa nova abertura: a linha
`[Vigia] a sessão anterior não fechou normalmente` mostra como a partida estava nos últimos segundos antes de travar.

## Créditos

Desenvolvido e testado por:

- **SawCunha**
- **Sleeping_tv_**
- **Póker.the.wolf**
- **Artie_saito**

Projeto no GitHub: [https://github.com/SawCunha/ror2_meeeeeeee](https://github.com/SawCunha/ror2_meeeeeeee)
