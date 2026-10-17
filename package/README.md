# Ror2Meeeee

Mod experimental para Risk of Rain 2 (1.5.0). **Todos os jogadores precisam do mesmo mod e da mesma versão.**
A lógica de gameplay roda só no host: as opções valem a partir do `.cfg` de quem hospeda.

## O que faz

- **Multiplicador de pickup** (`[Multiplier]`): cada item elegível coletado rende N cópias.
- **Artefato do Comunismo** (sem opção de config, só o artefato): com ele ativo no lobby, todo item pego também vai para os outros jogadores vivos.
  Todos os tipos, de todas as expansões (inclusive sucata, temporários e alugados); equipamentos e itens únicos do mod, não.
- **Cálculo limitado** (`[LimitedCalc]`): limita Bustling Fungus, Bundle of Fireworks e Ghor's Tome (`Mode = Clamp` ou `Aggregate`).
- **Dificuldade** (`[Difficulty]`): fator único, bônus fixo de vida e dano dos inimigos, mais inimigos e escalada mais rápida.
- **Progressão entre runs** (`[Progression]`, novo): no fim de cada run o mod dá uma nota ao grupo (velocidade, mortes,
  dano sofrido, abates), mostra o resumo no chat e ajusta o orçamento de spawn do diretor para a próxima run.
  Vitória com nota boa sobe um nível; derrota desce um. O cálculo roda fora da thread principal.
- **Artefato do Dízimo** (`[Gold]`): todo ouro ganho também vira crédito de spawn do diretor, e o diretor
  agradece no chat o crédito acumulado (`ThankIntervalSeconds`, padrão 60 s; 0 desliga). Teto de crédito por segundo
  (`MaxCreditPerSecond`, 150) e trava com muitos monstros vivos (`MaxMonsters`, 80); 0 desliga cada um.
- **Artefato Taxade** (`[Tax]`): toda compra paga com ouro (baú, santuário, drone...) custa vida (30% da vida máxima
  por padrão). Compras com outras moedas (moedas lunares, itens) não são taxadas.
- **Item "Chamado da Horda"** (branco): cada stack aumenta o orçamento de spawn de inimigos.
- **Itens "Capitalismo"** (novo; branco, verde e vermelho): reduzem a taxa do Taxade (20% / 30% / 50%) e aumentam
  todo ouro ganho (+10% / +20% / +30%). No máximo 1 de cada por jogador; não duplicam e não saem mais em baús
  de quem já tem.
- **Item "zzz cervo <3"** (novo; void verde): *A pressa é inimiga da perfeição.* Toda velocidade de movimento
  extra vira dano (1:1; +50% de conversão por cópia extra). Console: `give_item Ror2MeeeeeCervo 1`.
- **Item "Cano Extra"** (novo; vermelho): cada cópia faz a arma principal disparar +1 tiro em leque (balas,
  projéteis e flechas; corpo a corpo não). Até 5 extras (`[CanoExtra] MaxExtraShots`). Não é multiplicado.
  Console: `give_item Ror2MeeeeeCanoExtra 1`.
- **Grimório do Stolas** (novo; equipamento lunar): teleporta para perto do primeiro aliado vivo (sozinho: para o teleportador do estágio); cada uso dá crédito ao
  diretor (onda de inimigos). Console: `give_equipment Ror2MeeeeeStolasGrimoire`.
- **Contrato da I.M.P.** (novo; vermelho): o primeiro elite ou chefe do estágio vira alvo marcado; matá-lo paga ouro extra
  (+100% por cópia) e solta um item verde. Console: `give_item Ror2MeeeeeImpContract 1`.
- **Fungo Egoísta** (novo; verde, 5 de vida/s) e **Fungo Egoísta do Vazio** (void, 15/s; % da vida por cópia extra; corrompe o primeiro): curam sempre,
  só você e seus drones/torretas/ajudantes. Console: `give_item Ror2MeeeeeSelfishFungus 1` / `give_item Ror2MeeeeeSelfishFungusVoid 1`.
- **Égide Vampírica** (novo; verde) e **Égide do Devorador** (void): 5% de chance de bloquear; ao bloquear, roubam vida do
  agressor (verde) ou de todos os inimigos próximos (void). Console: `give_item Ror2MeeeeeVampiricAegis 1`.
- **Coração Pesado** (novo; lunar): cada cópia multiplica a vida máxima por 1,5 e o dano recebido por 1,3.
  Console: `give_item Ror2MeeeeeHeavyHeart 1`.
- **Artefato do Vazio** (novo): +5 baús do Vazio por estágio, sorteando entre todos os itens do Vazio; inimigos com +50% de vida.
- **Bateria do Portal** (novo; verde): o teleportador carrega +10% mais rápido por cópia do time.
  Console: `give_item Ror2MeeeeePortalBattery 1`.
- **Bastião** (novo; vermelho): escudo de 40% da vida máxima por cópia, recarga 2 s depois do último golpe.
  Console: `give_item Ror2MeeeeeBastion 1`.
- **Pilha de Pregos Enferrujados** (novo; branco) e **Pilha de Espinhos do Vazio** (void; corrompe a primeira): chance por
  golpe de disparar um prego com sangramento (branco) ou um espinho que marca o alvo e, 3 s depois, detona e espalha
  espinhos que marcam os inimigos próximos (void). Console: `give_item Ror2MeeeeeRustyNailStack 1` / `give_item Ror2MeeeeeVoidSpikeStack 1`.
- **Tela de status** (novo; tecla F8): seus atributos atuais e, de todos os jogadores, o total da run de dano causado, cura e
  dano recebido. Precisa do R2API Networking.
- **Artefato da Ressurreição** (novo): quem morre deixa uma lápide; um aliado perto dela por 3 s revive o jogador com metade
  da vida, pagando metade da própria vida. Só faz sentido em multiplayer. A lápide aparece na tela (marcador e banner);
  `ror2meeeee_demo` mostra como fica.
- **Soltar itens** (`[Drop]`, novo): `ror2meeeee_drop [nome] [quantidade]` ou o menu na tela (F7, configurável em `Drop.MenuKey`) solta
  itens do seu inventário no chão: clique no ícone do item, ajuste a quantidade e clique em SOLTAR (ou setas, +/- e Enter).
  `ror2meeeee_drop_menu [nome]` abre a tela com o item já escolhido; `ror2meeeee_drop` sem argumentos lista o que dá para soltar.
- **Diagnóstico** (sempre ligado): `[Identidade]`/`[Config]`, `[Estágio]`, `[Vigia]` e erros novos do jogo no `LogOutput.log`;
  `ror2meeeee_status` resume tudo. O Probe (opcional) acrescenta alertas de degradação e monstros por fase.
- **Partículas** (`[Particles]`): corta efeitos visuais por nome, só localmente.
- **Gasto por jogador** (`[Spend]`) e **Probe** (`[General] ProbeEnabled`): medição; gravam CSV em `BepInEx/`.

## Configuração

Os padrões já vêm prontos para jogar: quase tudo **ligado** (multiplicador x10 com lunares e void, compartilhamento,
dificuldade com +75% de vida e +50% de dano, Dízimo, Taxade, progressão, itens). Só o **Probe** (medição) vem desligado.
Ajuste no `BepInEx/config/com.sawcunha.ror2meeeee.cfg` (só o host precisa) ou na tela de mods (Risk of Options, opcional).
Cada seção tem um `Enabled` que explica o que ela faz.

`Enabled` vale na hora (entre partidas); os outros valores podem ser relidos com `ror2meeeee_reload`.

## Comandos de console (F2)

- `ror2meeeee_status`: features, falhas e hooks.
- `ror2meeeee_reload`: relê o `.cfg`.
- `ror2meeeee_progress` / `ror2meeeee_progress_reset`: nível da progressão entre runs / zerar.
- `ror2meeeee_spend`, `ror2meeeee_effects [top]`: gasto por jogador / efeitos mais frequentes.
- `ror2meeeee_probe_label <nome>`, `ror2meeeee_probe_begin <nome>`, `ror2meeeee_probe_end`: medição.

## Observações

- Itens iniciais, conversões (void) e itens de monstros não são multiplicados.
- Run abandonada no meio não conta para a progressão. O nível fica em `BepInEx/config/ror2meeeee-progress.txt`
  e o histórico em `BepInEx/ror2meeeee-runs.csv`.
- Se algo der errado, o `LogOutput.log` tem linhas `[Multiplier]`, `[Difficulty]`, `[Progression]`, `[Tax]`, `[Probe]`.
