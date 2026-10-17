# Changelog

## 0.10.0

- **Bazar Maior** (`[Bazaar]`, novo, ligado; reescrita do BiggerBazaar 1.14.5): o Bazar entre Tempos ganha até 6 baús
  com o item à mostra, comprados com o ouro que sobrou do estágio (o ouro não vira XP ao entrar no portal do bazar) ou
  com moedas lunares (`PayWithLunarCoins`), e um casulo ao lado do Newt que troca moeda lunar por ouro. Peso, preço,
  preço lunar e estoque por tier (10 compras por baú), sem limite de compras por jogador; limite de trocas no casulo;
  10% de chance de baú de itens temporários.
  Em relação ao original: preço = baú pequeno do estágio anterior × multiplicador (o original escalava duas vezes),
  sorteio próprio (não mexe no RNG da run), item comprado cai no chão e passa pelo Multiplicador/Comunismo, a Sale Star
  não é gasta à toa, o casulo não dispara Fogos, o Reciclador não troca o item à mostra, o Comando não troca o item
  comprado, itens únicos do mod ficam fora da loja e o hack do Captain não abre os baús. Sem ShareSuite, sem preço dos
  terminais originais do bazar e sem compra direto no inventário.
- **Elites novos** (`[Elites]`, novo, ligado; reescrita do MoreElites 0.9.5 e do BlightedElites 1.2.2): Volátil,
  Frenético e Fortalecedor (T1, com versão de Honra), Arruinado e Eco (T2). Só assets do jogo: o Frenético é o elite
  amarelo do jogo e o Eco é o edEcho do jogo, que o diretor nunca sorteava. Em relação aos originais: mísseis e
  teleporte só no host (não duplicam nos clientes), o aspecto Frenético usado por jogador não teleporta para (0,0,0),
  o elite de Honra tem os stats de Honra do jogo, o Volátil não dá um Brilliant Behemoth que nunca sai, o Fortalecedor
  não apaga um Warbanner de verdade, o projétil do Eco é registrado (chegava aos clientes sem catálogo) e os tiers vêm
  do R2API.Elites, não de índices fixos. Nova dependência: **R2API_Elites 1.1.3**.
- **Elite Dourado em IA** (`[GameFixes]`): drones, aliados e monstros Dourados soltavam pepitas de ouro a cada golpe
  em quem não é jogador, sem o cooldown nem a chance que o jogador tem, até travar a partida. Agora seguem a regra do
  jogador (mesma correção do GildedChunkSpamFix; não instale os dois).
- **Artefato do Enxame** (`[GameFixes]`): as cópias do Enxame nasciam em cima do jogador ou não nasciam, porque o
  jogo deixava zeradas as distâncias de spawn delas nos spawns normais do diretor. Agora recebem as distâncias do
  spawn original (mesmo bug que o SwarmsFix corrige; não instale os dois).
- **Tela de soltar itens** (F7): parou de criar estilos de texto novos a cada desenho (vários por frame, um por item)
  e só procura ícones no HUD quando falta algum, em vez de a cada 0,5 s. Menos lixo de memória com a tela aberta.
- **Chamado da Horda**: a descrição dizia +25% por cópia; o padrão é +50% (`Horde.QuantityPerStack = 0.5`).
- **Progressão**: fechar o jogo logo depois do fim da run não perde mais o nível nem a linha do CSV (a gravação roda na
  hora quando o jogo está fechando).
- **Teto de projéteis** (`[ProjectileBudget]`): projéteis fora da lista `Prefabs` cortados pelo teto global perdiam o
  dano; agora ele vai para o próximo disparo do mesmo tipo, como já acontecia com os da lista (dano total mantido).

## 0.9.6

- **Teto de projéteis** (`[ProjectileBudget]`), novos padrões medidos numa run de 3 jogadores (~8 mil itens, FPS
  caindo para ~11 nas lutas): `DaggerProjectile`, `EngiGrenadeProjectile` e `ShurikenProjectile` entram na lista
  `Prefabs`; `PerTypeMaxPerSecond` **15** (antes 30) e `GlobalMaxPerSecond` **80** (antes 300, que nunca era
  atingido). O dano total continua o mesmo: menos projéteis, cada um mais forte.
- **Lâminas Fantasmas** (`[GhostBlades]`): `MaxPerSecond` **15** (antes 30, que ficava sempre no teto).
- **Partículas** (`[Particles]`): explosões (`OmniExplosionVFXQuick`) saem da isenção total e ganham teto próprio
  maior: nova lista `Raised` (padrão `OmniExplosionVFXQuick`) com `RaisedMaxPerSecond` **60**. Sem teto, eram o
  efeito mais frequente da run (190 mil+). `Exempt` passa a ter só os golpes do Mercenary.
- **Correções de bugs do jogo** (`[GameFixes]`, novo, ligado): erros que se repetiam centenas de vezes por partida
  (com pilha no log, custando FPS) deixam de acontecer:
  - **Runic Lens**: se quem acertou morre antes do meteoro cair, o jogo repetia o erro a cada tick; o meteoro desse
    golpe é cancelado.
  - **Unstable Transmitter**: corpo com o item e sem inventário dava erro a cada frame; orbe cujo dono já morreu dava
    erro ao tocar inimigos; inimigo sem modelo não era teleportado e perdia também sangramento, atordoamento e debuff
    (agora fica onde está e recebe o resto do efeito).
  - **War Bonds**: torreta do Engi com o item dava erro ao morrer.
  - **Equipamento**: corpo sem inventário com pedido de uso de equipamento dava erro a cada tick.
- **Log**: as linhas por uso do Ghor's Tome e do Bundle of Fireworks (`[LimitedCalc]`, centenas por run) só aparecem
  com `General.VerboseLogging`.
- Configs existentes mantêm os valores salvos: para pegar os novos padrões, apague as chaves (ou o `.cfg`) ou ajuste
  na tela de mods.

## 0.9.5

- **Partículas**: teto padrão reduzido para **10/s por tipo de efeito** (antes 20/s), com exceções padrão para os
  golpes do Mercenary (`BladeComboSwingEffect`) e explosões (`OmniExplosionVFXQuick`), que seguem completos.
  Configs existentes mantêm os valores salvos de `MaxPerSecond` e `Exempt`.

## 0.9.4

- Atualizado para o **Risk of Rain 2 1.5.0#1224** (Steam build 25921972): sem o aviso de versão divergente no log.
- **Égide Vampírica** e **Égide do Devorador** ganharam modelo 3D próprio (escudo redondo com umbo: vermelho e
  dourado / roxo com brilho do Vazio), no chão e no logbook, no lugar do Rose Buckler e do Safer Spaces.
- **Pregos Enferrujados / Espinhos do Vazio**: corrigidas 6 chaves de assets do jogo que nunca carregavam (ícone da
  marca do Estilhaço, rastro dos pregos, efeito da detonação e modelo reserva). Some o `InvalidKeyException` no início.
- Métrica de carga: o log ganha `[Carga] Awake em X ms` (registro de itens/artefatos e features/hooks separados).
- **Partículas**: o corte local (`Disable`) virou **teto por segundo para todos os efeitos** (`MaxPerSecond`, padrão
  20/s por tipo de efeito), menos os listados em `Exempt`. Vale também para o que cada jogador **envia** aos outros,
  host incluído: quem tem o mod gera e transmite menos efeitos. Na troca de fase o log diz quantos efeitos o teto
  segurou. A chave `Disable` antiga deixa de ser usada.
- **Multiplicador**: `Multiplier.StackCap` passa a ter padrão **150** e vale para qualquer item multiplicado ou
  compartilhado (antes os itens do Cálculo limitado eram isentos). Teto rígido: item pego com o stack já no teto
  some, sem entrar no inventário, com aviso no chat; o compartilhamento não dá nada a quem já está no teto.
  Impressora, caldeirão e `give_item` não passam por esse teto. Configs existentes mantêm o valor salvo.
- **zzz cervo <3** ganhou modelo 3D próprio (cervo marrom de olhos esbugalhados, boca aberta e patas finas), no
  lugar do Paul's Goat Hoof.
- **Bateria do Portal** ganhou modelo 3D próprio (bateria deitada de metal gasto, anéis escuros e faixa/terminal
  ciano), no lugar da Fuel Cell do jogo.

## 0.9.3

- **Partículas**: com `Particles.CountSpawns` ligado, a lista dos 20 efeitos mais frequentes da run (a mesma do
  `ror2meeeee_effects`) sai sozinha no console e no `LogOutput.log` a cada fase nova (`[Particles] efeitos mais
  frequentes após N fase(s)`). Roda em cada jogador, não só no host.

## 0.9.2

- **Ghor's Tome** no cálculo limitado (`[LimitedCalc.GhorsTome]`, padrão `Aggregate`, `Limit = 5`): a chance de soltar
  pacote de ouro fica em até 4% × `Limit` (20%) e cada pacote vale proporcionalmente mais (até 5×), mantendo o ouro
  esperado por kill com menos pacotes no mapa. `Clamp` só corta a chance; `Off` = como no jogo. Vale para todos os
  jogadores, pela config do host. Log: `[LimitedCalc] GhorsTome Aggregate: pacote X -> Y`.

## 0.9.1

- **Pilha de Pregos Enferrujados** e **Pilha de Espinhos do Vazio** ganharam modelo 3D próprio (pilha de pregos
  enferrujados / roxos), no chão e no logbook, no lugar da Tri-Tip do jogo.
- Com `General.VerboseLogging` ligado, o `LogOutput.log` registra cada prego, espinho do Vazio e detonação
  (`RustyNail: ...`).
- Ícones próprios do **Artefato do Comunismo** no lobby.

## 0.9.0

- Novo item **Pilha de Pregos Enferrujados** (branco): 2% de chance por golpe (+0,1% por cópia extra) de disparar um
  prego de 10% do dano base (+0,1% por cópia extra) que aplica sangramento. Console: `give_item Ror2MeeeeeRustyNailStack 1`.
- Novo item **Pilha de Espinhos do Vazio** (void; corrompe a anterior): 2% de chance (+0,5% por cópia extra) de disparar
  um espinho de 10% do dano base (+0,5% por cópia extra) que marca o alvo com Estilhaço do Vazio por 3 s. Ao fim, detonação
  de 250% e chuva de espinhos num raio de 12 m (150%, +50% por cópia extra) que marca os atingidos. A cadeia é limitada
  por `RustyNail.MaxChain` (padrão 2). Console: `give_item Ror2MeeeeeVoidSpikeStack 1`.
- Tela de soltar itens (F7): a quantidade agora respeita `Drop.MaxPerCommand` (antes a tela deixava escolher 50, o
  servidor soltava 10 e o valor não batia) e a contagem cai na hora. Novo campo para digitar a quantidade e botões
  −1, +1, +5 e +10.
- Compartilhamento virou o **Artefato do Comunismo**: só funciona com o artefato ativo no lobby (a opção
  `Sharing.Enabled` foi removida) e agora compartilha todo
  tipo de item (lunar e void mesmo sem multiplicar, sucata, tiers das expansões, temporários e alugados no mesmo modo).

## 0.8.6

- Probe: três colunas novas por janela de 30 s, para achar o que pesa em runs longas: `heal_events` (quantos eventos de
  cura, não só quanto), `minions` (drones, torretas e outros ajudantes dos jogadores) e `player_items` (total de cópias de
  itens dos jogadores). Também no log `[Probe] Fase ...` e no `tools/probe-report.py`. O CSV antigo é arquivado sozinho.
- Grimório: quando não há diretor de combate ativo (Bazar, fim de estágio), o chat diz "Sem diretor ativo: nenhuma onda
  desta vez" em vez de dizer que o contrato foi cobrado. Nomes no chat e no log sem os títulos de elite.
- Novo `[ProjectileBudget]`: teto de projéteis dos jogadores e ajudantes, no host. Por tipo (`Prefabs`, padrão
  StickyBomb, FireMeatBall (Molten Perforator) e LightningStake; `PerTypeMaxPerSecond` 30) e global
  (`GlobalMaxPerSecond` 300). Acima do teto o projétil não sai e o dano dele vai para o próximo: menos projéteis, cada um
  mais forte, mesmo dano total. Em runs longas esses itens passavam de 11 mil projéteis em 30 s (~1 FPS) e o lixo gerado
  causava travadas de vários segundos. Projéteis de monstros não mudam.
- Chamado da Horda não é mais multiplicado nem compartilhado (1 pickup = 1 cópia, como o Cano Extra). Com 3-4 jogadores
  ele chegava a 60 cópias e x43 inimigos, com ciclos de morte e quedas de FPS.
- Lâminas Fantasmas: teto padrão de 100 para 30 por segundo (com 100 ficava sempre no teto, ~2,8 mil projéteis por 30 s).
  Configs existentes mantêm o valor gravado; ajuste `GhostBlades.MaxPerSecond` se quiser.

## 0.8.5

- Lápide: quem morre com vida extra do jogo (Dio's Best Friend ou a versão do Vazio) não gera lápide; o jogo já o traz de
  volta. Antes a lápide aparecia e sumia 2 s depois. Log: `[Revive] X morreu com vida extra ... sem lápide`.

## 0.8.4

- Tela de status (F8): falhava ao abrir ("Expected data type ULong, got data type Double"). As estatísticas da run são
  inteiras no jogo; agora são lidas com a leitura que aceita qualquer tipo.

## 0.8.3

- Lápide sem visual e marcador do I.M.P. invisível (desde a 0.7.0): os prefabs dos efeitos do mod (modelo da lápide,
  pulso silencioso, marcador do contrato) eram criados no menu sem `DontDestroyOnLoad`; a primeira troca de cena os
  destruía e o envio era pulado em silêncio. Agora sobrevivem às trocas de cena.
- Coração Pesado/Bastião: vida máxima + escudo agora param em 1 bilhão. Acima de ~2,1 bilhões a barra de vida do jogo
  (que usa int) estourava e lançava OverflowException a cada frame (centenas por minuto no log, custo de desempenho).
- Nova tela de status (`Stats.MenuKey`, padrão **F8**): atributos atuais do seu personagem (vida, escudo, regeneração,
  dano, velocidade de ataque, crítico, armadura, velocidade, nível) e totais da run (dano causado, dano dos ajudantes, maior
  golpe, cura, dano recebido, abates, mortes, itens). Os totais vêm das estatísticas do próprio jogo (as da tela de fim de
  run). Mostra todos os jogadores: vida, dano e totais da run de cada um. O jogo só manda ao cliente os próprios totais;
  o host envia os de todos a cada 2 s (só com cliente conectado). **Nova dependência: R2API Networking.**
- Telas F7 (soltar itens) e F8 (status): fecham só pela própria tecla (Esc não fecha mais) e não abrem uma sobre a outra.

## 0.8.2

- Ordem de aplicação fixa para os itens que mexem em vida, escudo, velocidade e dano (antes, cada item tinha o seu hook e
  a ordem dependia de quem registrou primeiro). Um hook só por ponto do jogo (`BodyHooks`), ligado só com portador:
  - Status: Coração Pesado (vida) -> Bastião (escudo sobre a vida já multiplicada) -> Cervo (velocidade em dano) -> vida e
    escudo atuais reajustados uma vez, no host.
  - Dano recebido: Égide sorteia o bloqueio -> Coração Pesado aumenta o dano de inimigo (só se não bloqueou) -> jogo ->
    Égide rouba vida se bloqueou.
  Log: `[BodyHooks] hook de stats/dano ligado/desligado`.
- Taxa do Taxade é sempre o último passo: calculada sobre a vida máxima final e aplicada direto, sem bloqueio da Égide nem
  aumento do Coração Pesado. A Égide também passa a respeitar BypassBlock (como o Tougher Times do jogo).
- Compras com ordem fixa (`PurchaseHub`, um assinante só): Spend registra o gasto e o Taxade vem por último (antes, a
  ordem dependia de qual feature ligou primeiro, inclusive ao religar pela tela de mods). O custo de vida do revive também
  passa direto, como a taxa.
- Bateria do Portal: como os outros itens, os hooks das zonas de carga só existem enquanto alguém tem o item. Log:
  `[PortalBattery] hooks de carga ligados/desligados`.

## 0.8.1

- Coração Pesado: o aumento de dano recebido só vale para dano vindo de outro personagem. Antes ele também multiplicava
  cobranças em vida (Void Cradle, Santuário de Sangue): abrir um baú do Vazio deixava o jogador com 1 de vida, e o golpe
  seguinte matava. Queda e dano sem atacante também não aumentam mais.
- Artefato Taxade: abrir um baú normal podia matar (com Tax.Lethal desligado). O limite "deixa 1 de vida" era calculado
  antes de outros efeitos multiplicarem o dano (Coração Pesado, Death Mark). Agora o dano da taxa é NonLethal: o próprio
  jogo garante 1 de vida, seja qual for o multiplicador.

## 0.8.0

- Novo item lunar **Coração Pesado** (`[HeavyHeart]`): cada cópia multiplica a vida máxima por 1,5 e o dano recebido por
  1,3 (composto, sem teto: 10 cópias = vida x57,7 e dano x13,8). Ao pegar o item a vida extra já vem cheia (como os itens
  de vida do jogo); ao perder, a vida é cortada no novo máximo. Modelo: Pearl.
  Console: `give_item Ror2MeeeeeHeavyHeart 1`.
- Novo **Artefato do Vazio** (`[VoidBounty]`): com ele ativado no lobby, cada estágio ganha +5 baús do Vazio (Void Cradle;
  `VoidBounty.ExtraChests`) e todo baú do Vazio sorteia entre todos os itens do Vazio (branco 60%, verde 30%, vermelho 8%,
  boss 2%). Preço: todos os inimigos nascem com +50% de vida. Precisa do DLC Survivors of the Void na run.
- Novo item verde **Bateria do Portal** (`[PortalBattery]`): o teleportador e as outras zonas de carga carregam +10% mais
  rápido por cópia, somando as cópias de todo o time (sem teto). Modelo: Fuel Cell.
  Console: `give_item Ror2MeeeeePortalBattery 1`.
- Novo item vermelho **Bastião** (`[Bastion]`): escudo de 40% da vida máxima por cópia (sem teto) e o escudo começa a
  recarregar 2 s depois do último golpe (o jogo espera 7 s), na mesma velocidade de recarga do jogo. Modelo: Personal Shield
  Generator. Console: `give_item Ror2MeeeeeBastion 1`.
- Grimório do Stolas: em mapas com poucos nós (Bazar) o teleporte "até o aliado" caía num ponto aleatório do mapa (o jogo
  usa um ponto aleatório quando não acha nó livre perto). Agora só aceita nó a até 10 m do aliado; senão, 2 m atrás dele, no
  chão. O log mostra a distância final.

## 0.7.1

- Lápide sem visual na 0.7.0: o Overlay volta a ficar sempre ligado (a otimização que o desligava quando ocioso foi
  desfeita) e o modelo do Dio's Best Friend é medido e ajustado para ~2,5 m de altura (a escala nativa do modelo de
  pickup não é confiável). Log `[Revive] lápide visível nesta máquina: ...` na 1ª vez de cada sessão.
- Todos os jogadores precisam desta mesma versão (0.7.0 teve vários pacotes com conteúdo diferente).

## 0.7.0

- zzz cervo <3: o hook em `CharacterBody.RecalculateStats` (todo corpo, monstros inclusive) só fica registrado enquanto
  alguém tem o item, como já era no Cano Extra, nos Fungos e nas Égides. Log: `[Cervo] hook de stats ligado/desligado`.
- Novos itens **Égide Vampírica** (verde) e **Égide do Devorador** (void verde, corrompe a primeira), `[Aegis]`. Ambas dão 5% de
  chance de bloquear (não acumula) e reagem a qualquer bloqueio do jogo (Tougher Times, Safer Spaces, parry, armadura do
  Captain); imunidade/invencibilidade não conta. Vampírica: rouba 8% da vida atual do agressor por cópia (até 50%) e cura
  você. Devorador: onda do Vazio que rouba 3% da vida atual de cada inimigo num raio de 13 m (+3% e +2 m por cópia, até 20% e
  30 m) e cura pela soma. Modelos: Rose Buckler e Safer Spaces. Console: `give_item Ror2MeeeeeVampiricAegis 1` /
  `give_item Ror2MeeeeeDevourerAegis 1`.
- Modelos 3D no chão para os itens do mod (antes todos eram a "caixa misteriosa"): Chamado da Horda = War Horn,
  zzz cervo <3 = Paul's Goat Hoof, Cano Extra = Brilliant Behemoth, Contrato da I.M.P. = Death Mark, Grimório do Stolas =
  Ghor's Tome, Capitalismo = Roll of Pennies. Se um modelo não carregar, volta para a caixa misteriosa.
- Novos itens **Fungo Egoísta** (verde) e **Fungo Egoísta do Vazio** (void verde, corrompe o primeiro), `[SelfishFungus]`:
  curam **sempre** (andando ou parado) só você e seus drones/torretas/ajudantes, onde estiverem; não curam outros jogadores.
  Egoísta: 5 de vida por segundo (cresce 20% por nível) +0,01% da vida máxima por cópia extra, com o brilho do Medkit
  a cada 3 s. Vazio: 15/s +0,5% por cópia extra, com a aura roxa do Weeping Fungus. A base é fixa (não em %) para não
  escalar com a vida inflada pelo multiplicador. Ninguém com vida cheia é "curado" (sem eventos
  de cura à toa). Console: `give_item Ror2MeeeeeSelfishFungus 1` / `give_item Ror2MeeeeeSelfishFungusVoid 1`.
- Grimório do Stolas: agora teleporta para perto do primeiro aliado vivo (na ordem da lista de jogadores). Sozinho, ou com
  todos os outros mortos, continua indo para o teleportador; sem aliado e sem teleportador (Bazar, Lua) o uso não é gasto.
- Contrato da I.M.P.: o aviso "novo contrato" e o marcador "ALVO DO CONTRATO" (maior, fonte 26) aparecem para **todos**
  os jogadores, não só para o host. O marcador some sozinho em ~1 s quando o alvo morre.
- Lápide (Artefato da Ressurreição): agora tem o modelo 3D do Dio's Best Friend (2,5x) no local, e o marcador
  "LÁPIDE de <nome>" ficou maior (fonte 30) e aparece para **todos** os jogadores, não só para o host. O host reenvia o
  modelo como efeito a cada 1 s; ao reviver, ele some em ~1 s. Nova dependência de compilação: `Unity.Addressables`.
- Overlay (avisos e marcadores) e tela de soltar itens ficam desligados quando não há nada na tela: o Unity deixa de
  chamar o `OnGUI`/`Update` deles a cada frame. A tecla `Drop.MenuKey` passa a ser lida no update do jogo.
- Cano Extra: os hooks de disparo (`BulletAttack.Fire`, `ProjectileManager.FireProjectile`, `OrbManager.AddOrb`) só ficam
  registrados enquanto algum corpo tem o item. Sem portador, nenhum tiro do jogo (inclusive de monstros) passa pelo mod.
  O log mostra `[CanoExtra] hooks de disparo ligados/desligados` a cada mudança.
- Ícone do pacote (r2modman) agora é a mesma arte da tela de mods do jogo (`Assets/mod_icon.png`). Até a 0.6.5 o zip
  levava um `icon.png` antigo, copiado de versão em versão.

## 0.6.5

- Novo `[GhostBlades]`: teto de gerações/renovações das Lâminas Fantasmas (item Curse do DLC4) por segundo
  (`GhostBlades.MaxPerSecond`, padrão 100; 0 = sem teto). Em runs longas o jogo chegava a ~700 por segundo
  (20 a 47 mil projéteis por 30 s), o que levava a memória nativa a mais de 3 GB e o frame a 100-300 ms.
  Só no host; `GhostBlades.Enabled` desliga. `ror2meeeee_status` mostra quantas foram puladas.

## 0.6.4

- Tela de soltar itens: a imagem agora é lida dos ícones que o próprio HUD do jogo já desenhou (`ItemIcon`), porque
  `pickupIconTexture`/`pickupIconSprite` chegavam nulos. Se ainda faltar imagem, o log lista os campos de ícone do `ItemDef`
  (`[DropMenu] campos de ícone ...`) e o quadrado mostra o nome.

## 0.6.3

- Tela de soltar itens: os ícones continuavam em branco na 0.6.2. Nesta versão do jogo `pickupIconSprite` está obsoleto e vem nulo;
  a imagem agora vem de `ItemDef.pickupIconTexture` (com `PickupDef.iconTexture` e os sprites antigos de reserva).
  Sem imagem, o quadrado ainda mostra o nome do item.

## 0.6.2

- Tela de soltar itens: os ícones estavam em branco. Agora usam `PickupDef.iconSprite` (o mesmo da UI do jogo), com
  `ItemDef.pickupIconSprite` de reserva; sem imagem, o quadrado mostra o nome do item na cor da tier. O log registra
  `[DropMenu] ícones: N de M itens com imagem` na primeira abertura.

## 0.6.1

- Soltar itens: item solto com o `Drop` e pego de volta não é mais multiplicado nem compartilhado de novo. Antes, soltar 4 e pegar
  de volta com multiplicador x10 virava 40 (duplicação); agora volta 4. O registro de itens soltos zera a cada estágio e a cada run.
- Tela de soltar itens refeita: grade de ícones grandes (escalada pela resolução), clique no item mostra o painel dele já selecionado
  (nome, quantidade, botões grandes), cursor liberado enquanto aberta; setas movem, +/- quantidade, Enter solta.
  Novo comando `ror2meeeee_drop_menu [nome]` abre a tela já com o item escolhido.

## 0.6.0

- Novo "Soltar itens" (`[Drop]`): comando `ror2meeeee_drop [nome] [quantidade]` e menu na tela (tecla `Drop.MenuKey`, padrão F7;
  setas escolhem, Enter solta). Solta itens do próprio inventário no chão para dar a um aliado; `Drop.MaxPerCommand` (padrão 10).
- Grimório do Stolas: teto de crédito por uso (`Grimoire.MaxCreditPerUse`, 600) e trava por monstros vivos (`Grimoire.MaxMonsters`, 80);
  com a trava o uso ainda teleporta, só não dá crédito.
- Contrato da I.M.P.: teto do ouro extra por contrato (`ImpContract.MaxGold`, 20000); o log mostra quanto tempo o alvo viveu.
- Diagnóstico sempre ligado: `[Identidade]` e `[Config]` no início (versão, build da DLL e configs), resumo `[Estágio]` por estágio
  (duração, pior frame, abates, pico de monstros, ouro/crédito, exceções, erros novos do jogo), `[Vigia]` para travadas
  (jogo sem atualizar por 3 s ou mais), classificação dos erros do Unity (ruído conhecido contado, erro novo logado uma vez)
  e detector de enxurrada com pilha de chamadas (ouro e dano), uma vez por estágio.
- Probe: monstros (média/pico), eventos de ouro, crédito dado/cortado, variação de memória Unity e pior frame por fase
  (colunas novas no CSV; o antigo é guardado à parte) e `ALERTA` quando a fase degrada (frame médio mais de 3x a melhor fase
  da run, +300 MB de memória nativa ou frame acima de 1 s), com os monstros vivos por tipo.
- `ror2meeeee_status` mostra o diagnóstico (ouro, monstros, exceções, usos do Grimório e custo amostrado do hook de ouro).
- Ressurreição: o log mostra quando a posição da lápide foi ajustada, o tempo de vida de cada lápide e o tempo até o revive.

## 0.5.0

- Ressurreição: a lápide agora aparece na tela de quem roda o mod (banner e marcador com nome e distância; seta na borda quando fora de vista).
- Ressurreição: pulsos repetidos usam um clone silencioso do efeito (o som toca só uma vez, na criação da lápide); coluna de luz visível de longe.
- Ressurreição: lápide criada em posição segura (chão abaixo da morte; sem chão, no aliado vivo mais próximo), evitando lápide inalcançável fora do mapa.
- Contrato da I.M.P.: o alvo ganha marcador vermelho na tela e banner de aviso.
- Dízimo: novo teto de crédito por segundo (`Gold.MaxCreditPerSecond`, padrão 150) e trava por monstros vivos (`Gold.MaxMonsters`, padrão 80), contra a bola de neve ouro -> crédito -> inimigos -> lag.
- Dízimo: o log registra a pilha de chamadas na primeira enxurrada de eventos de ouro (diagnóstico).
- Novo comando de console `ror2meeeee_demo`: mostra por 15 s a lápide e o alvo de teste na tela.

## 0.4.0

- Desempenho do Cano Extra: orbs extras (flechas da Huntress) clonados sem reflexão por tiro e rotações do leque pré-calculadas.
- Ressurreição: jogador morto operando um drone (Operação Remota) agora conta como morto e a lápide revive; drone operado não revive ninguém.
- Ressurreição: logs de diagnóstico (artefato ativo por estágio, lápide criada/removida, morte sem artefato).
- Um único hook em CharacterMaster.GiveMoney para Taxade e Dízimo (antes eram dois).
- Logs de compra (Taxade) e de multiplicação (Multiplicador) amostrados: as primeiras da run e depois 1 a cada N.

## 0.3.0

- Novos itens (tema Helluva Boss):
  - Equipamento lunar "Grimório do Stolas": teleporta para o teleportador; cada uso dá crédito ao diretor (onda de inimigos).
  - Item vermelho "Contrato da I.M.P.": o primeiro elite ou chefe do estágio vira alvo marcado; matá-lo paga ouro extra
    (+100% por cópia) e solta um item verde.
  - "Artefato da Ressurreição": quem morre deixa uma lápide; um aliado perto dela por 3 s revive o jogador com metade
    da vida, pagando metade da própria vida.
  - Ícones provisórios (cor sólida).
- Novo item vermelho "Cano Extra": +1 tiro em leque na arma principal por cópia (até `MaxExtraShots`, padrão 5;
  teto global por frame). Não é multiplicado; torretas não copiam. Custo zero por tiro quando ninguém tem o item.
- Novo item void verde "zzz cervo <3": toda a velocidade de movimento extra vira dano (1:1, +50% de conversão por
  cópia extra). Sai em Void Cradles.
- Novo `[Cleanup]`: cadáveres somem rápido (máximo 10, 2 s, mesmo à vista) usando o sistema do jogo; corrige o erro
  do jogo no Totem dos Lynx (NullReferenceException a cada tick quando os aliados já morreram).
- Artefato do Dízimo: o diretor agradece no chat o ouro recebido (em R$), acumulado, no máximo 1 vez por
  `Gold.ThankIntervalSeconds` (padrão 60 s) e no fim do estágio.
- Artefato Taxade: a taxa vale para toda compra paga com ouro (baús, santuários, drones...) e nunca para outras moedas.
- Padrões novos: quase todas as features ligadas, multiplicador x10 com lunares/void, dificuldade +75% vida/+50% dano,
  Horda 0.5/20, Fireworks em Aggregate (20). Probe continua desligado.
- Descrições da tela de mods reescritas: cada `Enabled` explica o que a feature faz e do que depende.
- Probe: coluna `ram_unity_alloc_mb` (memória alocada, em vez de reservada); CSV antigo é guardado à parte.
- Correção: ao fechar o jogo no meio da run, o CSV do Spend e a última janela do Probe se perdiam; agora são gravados na hora.
- Interno: acesso aos diretores de combate centralizado em `Common/Directors` (nenhuma feature referencia outra).

## 0.2.0

- Novo: progressão de dificuldade entre runs (`[Progression]`), com resumo da run no chat e CSV de histórico.
  Cálculo e gravação em disco fora da thread principal.
- Novo: itens "Capitalismo" (branco/verde/vermelho): alívio da taxa do Taxade e bônus de ouro (+10/+20/+30%);
  1 de cada por jogador, trocados no baú de quem já tem.
- `[Tax]` passa a vir ligado por padrão.
- Desempenho: hooks de `LimitedCalc` e `Particles` só ficam registrados quando há algo a fazer; ícones sem cópia
  na RAM; menos alocação em Sharing, Spend e Probe; cache de reflexão no Difficulty.
- Correção: memória que crescia por inimigo durante a run (Difficulty).

## 0.1.0

- Primeira versão: multiplicador, cálculo limitado, dificuldade, item Chamado da Horda, Probe.
