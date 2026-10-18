# Changelog

**Idioma:** [English](https://github.com/SawCunha/ror2_meeeeeeee/blob/main/package/CHANGELOG.md) · Português (Brasil)

## 0.11.3

- **Tormenta:** proteção dos inimigos novos (e a cada revive) de 10 s para **5 s**. Configs com o padrão antigo (3 ou 10)
  passam a 5 sozinhas (`General.ConfigVersion = 3`); um valor escolhido à mão fica.
- **Tormenta, desempenho:** golpe em inimigo protegido agora é marcado como rejeitado. Antes o jogo descartava o dano mas
  o atacante rodava todos os efeitos ao acertar dos itens (mísseis, raios, bolhas...) no inimigo imune, e cada efeito
  acertava de novo outro imune. Num teste com ~3.300 itens foram 71 mil golpes em inimigos protegidos num só estágio e
  travadas de alguns segundos a mais de 2 minutos.
- **Tormenta:** o Void Infestor (solto por elites do Vazio ao morrer) não ganha mais Dio's. Ele se mata ao entrar no alvo
  e, com a vida extra, revivia e infestava de novo.
- Diagnóstico: o NRE do `EnableCustomRenderTextureOnLight.OnDisable` (ao fechar o jogo) passa a ser ruído conhecido.

## 0.11.2

- **Tormenta:** o reforço de vida e dano dos monstros pelo Multiplicador passa a valer só a partir do estágio 2, como a
  redução de dano. Na 0.11.1 ele já valia no minuto 0 (+220% de vida e dano com x10) e o time foi derrotado em menos de
  1 minuto no estágio 1.
- **Aparo do Enforcer** (`[GameFixes]`): o contra-ataque mira quem deu o golpe aparado; quando esse atacante já não
  existe (morreu ou, com o Dio's da Tormenta, renasceu em outro corpo), o jogo estourava `NullReferenceException` a
  cada tick da janela de aparo e o aparo não disparava. Agora só o revide desse golpe é pulado.
- Diagnóstico: o NRE do `HurtBox.SimpleColliderBlockFilter` (raio da torreta do Engenheiro atravessando a bolha de
  bloqueio com o alvo já destruído; só aquele tiro se perde) passa a ser ruído conhecido.

## 0.11.1

- **Tormenta mais dura:**
  - Proteção dos inimigos novos de 3 s para **10 s**, com o brilho dourado do jogo para dar para ver quem está protegido.
    Configs existentes com o valor antigo (3) passam a 10 sozinhas (`General.ConfigVersion = 2`).
  - Cada inimigo normal ganha 1 **Dio's Best Friend** e cada elite ganha 2 (`ExtraLivesNormal`, `ExtraLivesElite`).
    Chefes, invocações (minas, totens, ouriços) e temporários não ganham. A cada vez que revive, a proteção de 10 s vale de
    novo.
  - Com o **Multiplicador** ligado (x M), o dano que passa pela redução é dividido por `M^MultiplierReductionExponent`
    (config nova, padrão 0,5): com x10, o estágio 2 vai de 20% para 75% e o 3, de 40% para 81% (teto 99%). A vida e o dano
    das curvas são multiplicados pelo mesmo fator (x3,16 com x10). O valor do Multiplicador é lido no início de cada
    estágio.
- **Tela de status (F8):** os fundos (painel, cartões, moldura do retrato) sumiam depois do 1º estágio, porque a troca de
  cena descarregava as imagens do mod; o mesmo valia para a tela F7. A vida não cabe mais cortada: na barra e no painel,
  as colunas têm larguras diferentes e o valor encolhe se precisar. Em inglês, os números curtos saem como `12.3K`/`4.5M`
  (antes, sempre `12,3 mil`).
- **Correções do jogo no cliente** (`[GameFixes]`), vistas no log de um cliente da run de teste:
  - Projéteis teleguiados e minas (`ProjectileSteerTowardTarget`, `ProjectileSphereTargetFinder`): o jogo religa no
    cliente um componente que só roda no host (Retorno do Cyclone da Huntress, objetos soltos pelo Drifter), e cada tick
    estourava `NullReferenceException`: cerca de 10.700 erros numa run, com custo de FPS. Agora o mod desliga de novo, como
    o próprio jogo faria.
  - Caldeirão que drena itens (`HallowedCauldron.DrainingItems`): no cliente, sem o jogador que ativou, o estado estourava
    a cada tick enquanto drenava (251 erros). O tick do cliente é pulado; quem conduz é o host.
- Diagnóstico da Tormenta: o resumo do estágio passa a sair na troca de cena (antes, os monstros iniciais caíam num
  "resumo" falso e sumiam do estágio certo) e conta cada troca de fase uma vez só (contava uma por diretor).
- Diagnóstico: passam a ser ruído conhecido o NRE do `AcolyteBodySwapController` e o do `PlaySoundOnEvent.OnDestroy` (os
  dois na troca de cena), o do `SendP2PPacket` (ao sair, com a Steam já fechada), o aviso `ActivityManager::CurrentSurvivor`
  e o NRE do `BurnEffectController.Start` (corpo pega fogo com uma parte do modelo já destruída; só as
  partículas não saem).

## 0.11.0

- **Dificuldade Tormenta** (`[Tempest]`, nova dependência R2API.Difficulty): acima da Monção (`ScalingValue` 3,5).
  Teto de monstros 60 (+10 por jogador extra), crédito extra do diretor com portão antes do teto e retomada gradual,
  fases de enxame (monstros baratos, ondas de 10) e pesada, 3 s de invulnerabilidade para inimigos novos, redução de
  dano e o mesmo em vida máxima por estágio (20/40/55/70/90/99%), curvas de vida, dano e elites, pressão por
  permanência, Teleportador Primordial só no estágio 10 (outros finais bloqueáveis), corte do crédito extra com FPS
  baixo, resumo na F8 e `DebugLog` para validar em jogo. Lentes do Vidente Perdido tiram 90% da vida restante em vez
  de matar. Comando novo `ror2meeeee_portals`. Nas outras dificuldades
  nada muda.
- Config: `General.ConfigVersion = 1`.
- **Tela de status (F8)** com visual novo, em ciclo: 1º toque mostra seus atributos numa barra acima das habilidades
  (sempre atualizada), 2º mostra todos os jogadores com retrato, nome e atributos (e o resumo da Tormenta no host), 3º
  fecha. Os totais da run (dano causado, cura, dano recebido...) saíram, e com eles a dependência do R2API Networking.
- **Idiomas**: o mod passa a ter base em inglês e tradução para português do Brasil. Com o jogo em Português (Brasil),
  tudo aparece em português; em qualquer outro idioma, em inglês. Vale para nomes e descrições de itens, artefatos,
  equipamento e elites, mensagens de chat, telas F7/F8, avisos e marcadores na tela, respostas dos comandos de console
  e descrições da tela de mods (Risk of Options). "Taxade" é termo do mod e fica igual nos dois idiomas.
- Chat: as mensagens do host (Dízimo, Contrato da I.M.P., Grimório, Lápide, Drop, teto do Multiplicador, resumo da
  Progressão e Bazar Maior) chegam como texto do mod, e cada jogador as lê no próprio idioma. Nomes de jogadores e de
  itens dentro delas vêm como o host os vê.
- Config: as descrições no `.cfg` passam a ser em inglês; chaves e valores não mudam e configs existentes continuam
  valendo. A tela de mods mostra as descrições em português com o jogo em pt-BR. Com um Risk of Options antigo, sem
  suporte a tokens, a tela de mods não aparece e o log avisa (o mod segue funcionando pelo `.cfg`).
- Os logs (`LogOutput.log`) e as ferramentas de diagnóstico (Probe, `ror2meeeee_status`, `ror2meeeee_effects`,
  `ror2meeeee_spend`) continuam em português.
- README e CHANGELOG em inglês, com as versões em português (`README.pt-BR.md` e `CHANGELOG.pt-BR.md`) e um link no
  topo para escolher o idioma.

## 0.10.4

- Desempenho: **mísseis** (`MissileProjectile`: ATG, Disposable Missile Launcher, aspecto Volátil...) entram na lista
  padrão do `[ProjectileBudget]` (15 por segundo, dano dos que não saem somado ao próximo). Numa run de 4 jogadores, o
  Bazar, sem nenhum inimigo, tinha 675 mísseis a cada 30 s. Em uma config existente, acrescente `MissileProjectile` a
  `Prefabs` à mão.
- Desempenho: teto global de projéteis de jogadores (`[ProjectileBudget] GlobalMaxPerSecond`) de 80 para **40** por
  segundo. Na mesma run, mesmo com os tetos por tipo, a soma passava de 50/s com 1 a 3 monstros vivos e o frame médio
  chegava a 34 ms (p95 54 ms). O dano dos projéteis que não saem é somado aos seguintes do mesmo tipo. Em uma config
  existente, mude `GlobalMaxPerSecond` à mão.
- Diagnóstico: o `NullReferenceException` do `InventorySafekeepController` ao fim da run (o jogo tenta devolver os
  inventários guardados de cada jogador depois que a run já foi desmontada; não há perda de itens numa run em
  andamento) passa a ser contado como ruído conhecido, assim como o do `SetStateOnHurt.GetModifiedDuration` (inimigo
  atingido no mesmo frame em que nasce, antes de o jogo preencher os controladores de atordoamento; corrigir exigiria
  um hook em todo golpe, caro demais para um caso raro) e o do `ParticleSystemColorFromEffectData` (efeito do jogo criado
  sem passar pelo `EffectManager`; a partícula só não recebe a cor) e o `Unable to SpawnEffect from prefab named
  'GenericHugeFootstepDust'` (efeito de pisada do jogo que não está no catálogo de efeitos; só esse nome) e o
  `BaseAchievement: localUser is null!` (conquista verificada sem usuário local num desbloqueio em multiplayer) e o do
  `MasterSuicideOnTimer.OnDestroy` (ajudante temporário destruído antes de o jogo terminar de montá-lo) e o do
  `BrotherUltUtils.EvaluatePattern` (fase 2 do Mithrix: o ataque mira quem matou o inimigo, e uma torreta ou drone sem
  direção estoura o cálculo; o ataque só não sai) e o da conquista do Captain na zona de choque
  (`CaptainSupplyDropFinaleAchievement`), que também aparece como `Failed at InvokeHandler, probably malformed packet!`.
- README: seção de créditos com quem desenvolve e testa o mod (SawCunha, Sleeping_tv_, Póker.the.wolf e
  Artie_saito) e link do projeto no GitHub (topo da página e créditos).

## 0.10.3

- Desempenho: **Fireworks** (`FireworkProjectile`) entra na lista padrão do `[ProjectileBudget]`: no máximo 15 por
  segundo, somando todos os jogadores, com o dano dos que não saem somado ao próximo (mesmo dano total). Numa run de 4
  jogadores chegaram a 902 mísseis a cada 30 s antes de o jogo travar; o `[LimitedCalc]` limita cada lançador, mas com o
  Multiplicador são muitos lançadores. Em uma config existente, acrescente `FireworkProjectile` a `Prefabs` à mão.
- Diagnóstico: o aviso de enxurrada de ouro (`[Diag] enxurrada de eventos de ouro`) passa a usar 40 eventos por
  segundo **por jogador**; com 3 jogadores, cada abate gera 3 eventos e um pico normal de abates já disparava o aviso.
- Correções do jogo (`[GameFixes]`): o **Dunestrider** que morria sem o ponto "Center" no modelo estourava
  `NullReferenceException` no efeito de morte **antes** de destruir o modelo e o corpo, e o jogo não tentava de novo: o
  corpo morto ficava no servidor até o fim do estágio. Agora o efeito usa a posição do corpo e a limpeza do jogo roda
  inteira (uma checagem por morte de Dunestrider).
  O **Baú Mímico** sem o ponto "Muzzle" no modelo estourava ao abrir e ao se enterrar; ao abrir, o estouro pulava a
  linha que marca o mímico como "ator falso", mas a saída do estado desmarcava mesmo assim: o contador ficava negativo e
  a camada de colisão do mímico, errada. Agora o mod trata só esse estouro esperado e completa a linha pulada.
- Diagnóstico: passam a ser contados como ruído conhecido do jogo o `CombatSquad has no living members. Triggering
  defeat...` (registrado como erro quando um grupo de chefe/evento é derrotado) e o `Unable to set gate state for ...`
  (mapa que referencia uma passagem ausente do grafo de navegação, ao carregar o estágio) e o `PickupDisplay bounds are
  zero!` (modelo de pickup do jogo com tamanho zero, como o do HelpWake), além dos erros de rede e da prévia de
  personagem quando alguém sai do lobby (`host id {0} invoked wrong io operation`, `host id {0} out of bound...`,
  `SurvivorMannequinSlotController.ApplyLoadoutToMannequinInstance`) e o `NullReferenceException` do Extractor Unit sem
  animator (`ExtractLunge.OnEnter`; sem animator ele não ativa a hitbox e não rouba itens, então não há o que corrigir) e o
  do Solus Amalgamator (`DetatchState.OnEnter`: o jogo dispara as separações no corpo já morto, sem modelo).

## 0.10.2

- **Diagnóstico de travamento**: a vigia grava a cada 5 s um "último sinal de vida" em
  `BepInEx/ror2meeeee-heartbeat.txt` (estágio, frame, monstros, abates, ouro, lâminas cortadas, exceções, RAM). Fechar o
  jogo normalmente apaga o arquivo; se ele existir na abertura seguinte, o log ganha `[Vigia] a sessão anterior não
  fechou normalmente...` com o estado da partida no fim. Motivo: numa run de 3 jogadores o processo inteiro congelou
  (log, Unity e a própria vigia pararam no mesmo segundo) e não sobrou nada para saber o que acontecia.
- Dificuldade: as almas do **Artefato da Alma** (uma por monstro morto, milhares por run) não recebem mais os itens de
  Boost de vida e dano; morrem num golpe e o Boost só custava um recálculo de atributos a cada uma.
- Lâminas Fantasmas (`[GhostBlades]`): teto padrão de 15 para **10** por segundo (a run que travou ficou no teto,
  ~400 lâminas a cada 30 s). Vale para configs novas; em uma existente, mude `MaxPerSecond` à mão.
- **README reescrito para o Thunderstore**: cada artefato, item, equipamento, elite e sistema em uma seção própria, com
  o que faz, como usar e um exemplo; tabela de resumo, comandos e perguntas frequentes. Descrição e site do pacote
  atualizados.
- Elite Eco: se o projétil dos ecos vier sem ghost (endereço e referência vazios), o mod usa o `EchoHunterGhost` do
  jogo para os tiros não ficarem invisíveis.
- **Bazar Maior**: o item que flutua sobre o baú podia ser pego (o `Start()` do jogo zerava a marca `consumed` que o
  mod colocava logo depois de criá-lo). Agora a vitrine usa um tempo de espera infinito, que o jogo não reinicia, e
  nunca é concedida. Com o **Artefato do Comando**, o baú volta a soltar o cubo de escolha do mesmo tier em vez do item
  direto; só a vitrine ignora o artefato.
- Correções do jogo (`[GameFixes]`): a Craca do Vazio chamava um efeito de spawn vazio a cada vez que nascia (o 1.5.0
  deixou o endereço do efeito em branco), gerando um erro com pilha no log por craca. O mod descarta o efeito vazio
  antes; sem custo por tick, uma checagem por spawn.
  A Defective Unit estourava `NullReferenceException` ao morrer já sem modelo (o jogo lia o modelo sem checar); nesse
  caso o mod pula só o trecho do efeito de morte, que não teria onde aparecer.
- `ror2meeeee_reload`: o texto de ajuda dizia que `Enabled` exigia reiniciar; ele já vale na hora, entre partidas.
- Desempenho: o crédito do Dízimo (`[Gold]`) não monta mais uma string de diagnóstico a cada ouro ganho (só quando a
  enxurrada é detectada, uma vez por estágio); o Grimório guarda o efeito de teleporte em vez de recarregá-lo a cada uso.
- Métrica de carga: a linha `[Carga]` agora lista as etapas do Awake que levaram 5 ms ou mais (registro de cada
  item/artefato/elite, interface, config, `Enable` de cada feature e tela de mods), para achar o que pesa na abertura.
- Diagnóstico: passam a ser contados como ruído conhecido do jogo (não do mod) o `IndexOutOfRangeException` do
  Facepunch na lista do lobby quando alguém entra, o `AddPlayer` duplicado ao um jogador entrar (`already a player at
  that playerControllerId` / `connection has already been set as ready`), o `NullReferenceException` do
  `SteamworksLobbyDataGenerator` no lobby, o do `ViewableTag.OnDisable` ao fechar o jogo e o `GenericPool: ReleasePools
  called on non-empty pool` da troca de cena e o `AssignClientAuthority` de ajudantes do jogo. O aviso de "erro novo"
  mostra a primeira linha da pilha e ignora números (IDs de conexão) ao decidir se o erro já foi visto.

## 0.10.1

- **Elite Eco volta a funcionar no jogo 1.5.0**: o patch moveu os assets do Eco de `RoR2/InDev/` para `RoR2/Junk/`
  (`edEcho.asset` e `EchoHunterProjectile.prefab`); na 0.10.0 o elite não era registrado e o log mostrava
  `InvalidKeyException ... Key=RoR2/InDev/edEcho.asset`.

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
