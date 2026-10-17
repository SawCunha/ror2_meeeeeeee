---
title: PRD — Mod de desempenho e multiplicação de itens para Risk of Rain 2
status: final
created: 2026-10-11
updated: 2026-10-11
---

# PRD: Mod de desempenho e multiplicação de itens para Risk of Rain 2
*Título provisório — confirmar.*

## 0. Propósito do documento

PRD de um mod pessoal para Risk of Rain 2, escrito para o autor (Sawcunha) e para a sequência BMAD (arquitetura, épicos, stories). O vocabulário do §3 é fixo. Funcionalidades agrupadas, FRs numerados globalmente, suposições marcadas `[ASSUMPTION]` e indexadas no §10. O documento descreve capacidades; tecnologia, pesquisa, fontes e hipóteses técnicas ficam em `addendum.md`, e decisões em `decision-log.md`.

## 1. Visão

Em partidas com muitos itens acumulados, o Risk of Rain 2 sofre queda de FPS (o autor relatou 2–3 FPS) e alto consumo de RAM, porque muitos itens executam efeitos a cada tick e o custo cresce com o número de stacks. O mod troca o cálculo por stack desses itens por versões que escalam de forma limitada, para que partidas de late game em grupo continuem jogáveis.

Com o custo sob controle, o mod acrescenta mecânicas que aumentam o número de itens (multiplicação e compartilhamento) e compensa o poder extra com dificuldade maior. O autor aceita mudar a mecânica dos itens para isso.

É um mod de uso pessoal e de amigos; a publicação pública não é objetivo. É também o primeiro mod do autor (experiente em Java, sem experiência prévia com mods de RoR2 nem C#).

## 2. Usuário-alvo

### 2.1 Persona principal
Sawcunha, jogador e desenvolvedor, joga em grupo de 3 jogadores; todos instalam o mod. `[ILLUSTRATIVE: o grupo de 3 é o cenário de teste citado]`

### 2.2 Jobs To Be Done
- **JTBD-1** — Jogar partidas longas em grupo sem queda de FPS nem estouro de RAM, mesmo com dezenas ou centenas de stacks (mede-se no §8).
- **JTBD-2** — Ter mais itens e poder de build sem que a partida fique trivial (§4.2–§4.4).
- **JTBD-3** — Ver menos poluição visual de partículas (§4.5).
- **JTBD-4** — Aprender a criar um mod de RoR2 fazendo algo útil para o próprio grupo (sem métrica; é o contexto do projeto).

### 2.3 Não-usuários (v1)
Jogadores que não instalam o mod; público do Thunderstore; partidas públicas com desconhecidos.

### 2.4 Jornadas-chave
- **UJ-1** — Sawcunha e dois amigos jogam com o mod; ao acumular 50+ stacks de Bustling Fungus e ~200 de Bundle of Fireworks, o jogo se mantém dentro do orçamento de desempenho. *(realizado por FR-1, FR-2, FR-4, FR-5, FR-6)*
- **UJ-2** `[v2]` — Um jogador pega um item e todas as cópias recebidas (já multiplicadas) são compartilhadas com os demais. *(realizado por FR-9, FR-10)*
- **UJ-3** — O grupo sente que a partida continua desafiadora apesar da multiplicação de itens. *(realizado por FR-11)*

## 3. Glossário

- **Item** — Objeto de inventário do jogo que concede efeitos ao jogador.
- **Stack** — Quantidade acumulada de cópias de um item no inventário de um jogador.
- **Cópia** — Uma unidade de um item; stack é a soma das cópias.
- **Pickup** — Evento em que um jogador recebe um item de qualquer fonte do jogo.
- **Tick** — Cada atualização de lógica do jogo.
- **Efeito por tick** — Lógica de um item executada a cada tick.
- **Cálculo por stack** — Como um item converte o número de stacks em magnitude do efeito (dano, raio, quantidade de projéteis).
- **Cálculo limitado** — Cálculo por stack cujo custo cresce de forma limitada ou agregada em vez de linear.
- **Host / cliente** — O host é o jogador que hospeda a partida e tem a autoridade; clientes são os demais.
- **Orçamento de desempenho** — Limites de frame time e RAM que o mod deve respeitar no cenário de referência (valores definidos após a linha de base).
- **Linha de base** — Medição do cenário de referência sem o mod.
- **Cenário de referência** — Partida com 3 jogadores, Bustling Fungus em 50+ stacks e um jogador com ~200 stacks de Bundle of Fireworks, com seed fixo e conjunto de itens reproduzível.
- **Multiplicação de itens** — Mecânica do mod que concede mais cópias de um item a cada pickup.
- **Compartilhamento de itens** — Mecânica do mod em que todo item pego por um jogador, na quantidade integral (não apenas 1 unidade), é replicado para os demais jogadores.
- **Dificuldade aumentada** — Ajuste do mod que eleva a dificuldade da partida para compensar o poder extra.
- **Partícula** — Efeito visual gerado por itens (escopo do §4.5).

## 4. Funcionalidades

### 4.1 Medição e cálculo limitado por stack (núcleo)
**Descrição:** O mod primeiro permite medir o custo de cada item no cenário de referência; só então substitui o cálculo por stack de itens caros por um cálculo limitado. O primeiro conjunto é Bustling Fungus e Bundle of Fireworks, escolhidos por observação do autor. A causa de cada gargalo é hipótese e deve ser medida antes de otimizar (ver addendum). Isso mantém o item reconhecível: o efeito continua valendo a pena, só deixa de crescer sem limite.

**Requisitos funcionais:**
- **FR-1** — O autor pode medir frame time e uso de RAM no cenário de referência em três condições: sem o mod, com o mod sem multiplicação, com o mod com multiplicação. *(linha de base; é o primeiro passo do MVP)*
- **FR-2** — O custo por tick do Bustling Fungus deixa de crescer sem limite com o número de stacks, preservando o efeito de cura de forma reconhecível. `[NOTE FOR PM: limite e fórmula a decidir após medir]`
- **FR-3** — O custo por interação do Bundle of Fireworks deixa de crescer sem limite com o número de stacks, preservando o dano total de forma reconhecível. `[NOTE FOR PM: manter a quantidade e otimizar, ou agregar? decidir após medir]`
- **FR-4** — Cada alteração de item pode ser ligada ou desligada individualmente.
- **FR-5** — Novos itens podem ser adicionados à lista de itens com cálculo limitado sem reescrever a lógica central. `[ASSUMPTION: lista inicial de 2 itens; outros entram conforme medição]`

### 4.2 Multiplicação de itens
**Descrição:** A cada pickup, o jogador recebe um número configurável de cópias adicionais, como na referência `Squeakys_Item_Multiplier` (ver addendum). A decisão de multiplicar é do host. Entra no MVP porque é com a multiplicação ligada que se valida o desempenho.

**Requisitos funcionais:**
- **FR-6** — A cada pickup, o jogador recebe um multiplicador de cópias definido em um campo de configuração aberto (qualquer valor inteiro positivo, sem teto imposto pelo mod); o valor 1 desliga a mecânica. `[ASSUMPTION: valor padrão x2]`
- **FR-7** — Itens sem cálculo limitado (§4.1) não são multiplicados acima de um limite configurável de stacks. `[ASSUMPTION: o teto é de stacks por item; evita agravar o desempenho]`
- **FR-8** — O multiplicador é aplicado só aos pickups elegíveis conforme a tabela de elegibilidade; o jogador escolhe na tela de configuração se itens lunares e itens void são multiplicados (padrão: não).

**Elegibilidade (multiplicação e, em v2, compartilhamento)** `[ASSUMPTION: valores padrão; ajustar]`

| Fonte / tipo | Multiplica? |
|---|---|
| Item comum (branco/verde/vermelho/boss) via baú, drop de inimigo, impressora | Sim |
| Itens lunares | Configurável na tela de configuração (padrão: não) |
| Itens void | Configurável na tela de configuração (padrão: não) |
| Itens temporários / scrap / equipamento | Não |
| Jogador morto ou desconectado | Não recebe; sem acúmulo retroativo |

Ordem de operações: pickup → multiplicação (§4.2) → compartilhamento (§4.3, v2), e o item recebido por compartilhamento não é multiplicado nem compartilhado de novo.

### 4.3 Compartilhamento de itens `[v2]`
**Descrição:** Todo item pego por um jogador é replicado, na quantidade integral, para os demais jogadores. Combinado com a multiplicação, o total de stacks cresce com o número de jogadores, o que torna §4.1 requisito de viabilidade. A decisão é do host.

**Requisitos funcionais:**
- **FR-9** — Todo pickup elegível de um jogador é entregue também aos demais jogadores, na quantidade integral recebida (incluindo cópias da multiplicação).
- **FR-10** — O compartilhamento pode ser desligado, e um item recebido por compartilhamento não é compartilhado de novo.

### 4.4 Dificuldade aumentada
**Descrição:** Para compensar o poder extra, o mod eleva a dificuldade da partida.

**Requisitos funcionais:**
- **FR-11** — A dificuldade da partida aumenta por um fator configurável, decidido no host. `[NOTE FOR PM: o que aumenta (vida, dano, quantidade de inimigos, ritmo) é decisão de design do autor, a fechar na story]`

### 4.5 Redução de partículas
**Descrição:** O mod reduz ou desabilita partículas geradas por itens, para diminuir carga e poluição visual, sem alterar o gameplay.

**Requisitos funcionais:**
- **FR-12** — O jogador pode desabilitar ou encurtar a duração de partículas de uma lista configurável de efeitos de itens.
- **FR-13** — Desabilitar ou encurtar partículas não altera dano, cura nem colisão. `[ASSUMPTION]`

**Notas:** `[NOTE FOR PM: quais efeitos cortar primeiro? medir o ganho antes (§9)]`

## 5. Não-objetivos

- Publicar no Thunderstore, dar suporte a jogadores sem o mod ou a partidas públicas com entrada tardia de desconhecidos `[NON-GOAL for MVP]`.
- Modo exponencial de multiplicação como na referência `Squeakys_Item_Multiplier` `[NON-GOAL]`; a escolha por categoria se limita a lunar e void, via configuração (FR-8).
- Mod só-host (server-side puro) `[NON-GOAL]`: todos instalam, mesma versão.
- Cobrir todos os itens do jogo; só os identificados por medição, e rebalancear itens fora da lista.
- Adicionar itens, personagens ou conteúdo novo.
- Compatibilidade com outros mods de drops/inventário/compartilhamento (por exemplo `ShareItemsWithMoreFun`): conflitos são esperados e documentados; a replicação deste mod difere do modelo de baús instanciados da referência.
- Garantir compatibilidade com patches futuros do jogo.

## 6. Requisitos não funcionais transversais

- **NFR-1 — Desempenho:** no cenário de referência, FPS e RAM com o mod (incluindo multiplicação ligada) ficam melhores que na linha de base. Metas numéricas definidas após a linha de base (decisão do autor).
- **NFR-2 — Multiplayer:** funciona com todos os jogadores com o mod, mesma versão, sem dessincronização. Multiplicação, compartilhamento e configurações que afetam gameplay são decididas no host e valem as do host `[ASSUMPTION]`; clientes recebem o resultado. O mod não envia dados à rede a cada tick sem necessidade.
- **NFR-3 — Observabilidade:** log verboso desligado por padrão; medição de frame time e contadores (projéteis, procs) disponível por configuração.
- **NFR-4 — Alocações:** nos caminhos alterados, o mod evita alocação de memória por tick quando viável `[ASSUMPTION]`.
- **NFR-5 — Compatibilidade de versão:** alvo é a versão mais recente do jogo, liberada na semana anterior a 2026-10-11 (confirmado pelo autor).
- **NFR-6 — Plataformas:** desenvolvimento em Linux; execução e testes em Windows (restrição). Tecnologia e verificações em `addendum.md`.

## 7. Escopo do MVP

### 7.1 Dentro
- FR-1 (medição e linha de base), FR-2, FR-3, FR-4, FR-5.
- FR-6, FR-7, FR-8 (multiplicação de itens, com a tabela de elegibilidade).
- FR-11 (dificuldade aumentada, versão mínima: um fator configurável; o que sobe é definido na story).
- FR-12, FR-13 (partículas, após medir o ganho).

### 7.2 Fora do MVP
- Compartilhamento de itens (§4.3, FR-9 e FR-10; UJ-2) `[v2 — out of MVP]` `[NOTE FOR PM: proposta; multiplica o total de stacks pelo número de jogadores, então entra depois da medição com multiplicação]`.
- Publicação no Thunderstore.

## 8. Métricas de sucesso

**Primárias**
- Frame time médio e p95 / FPS p5 no cenário de referência, nas três condições do FR-1. Meta numérica após a linha de base (JTBD-1).
- RAM e alocações por tick no mesmo cenário. Meta após a linha de base.

**Secundárias**
- Partida de 3 jogadores até o fim sem queda abaixo de um piso de FPS, a definir.
- Partículas: redução de contagem ativa em relação à linha de base (JTBD-3).

**Contra-métricas (não otimizar)**
- Remover ou neutralizar o efeito de itens só para ganhar FPS (o efeito segue reconhecível e útil).
- Dificuldade tão alta que a partida deixa de ser divertida.
- Dessincronização entre host e clientes ou crash por causa do mod.

## 9. Perguntas em aberto

Nenhuma bloqueia arquitetura ou épicos; todas são resolvidas durante a implementação ou depois da linha de base (dono: autor).

1. Limite de stacks de itens sem cálculo limitado (FR-7) — após medir.
2. Dificuldade: o que sobe e quanto (FR-11) — na story.
3. Quais partículas cortar primeiro (FR-12) — após medir o ganho.
4. FR-2 e FR-3: limite, fórmula, e se mantém a quantidade ou agrega — após medir.
5. A causa real do custo de Bustling Fungus e Bundle of Fireworks é a hipótese do addendum? — descompilar e medir.
6. Metas numéricas de FPS e RAM (NFR-1) — após a linha de base.

## 10. Índice de suposições

- §2.1 — Grupo de 3 jogadores como cenário (ILLUSTRATIVE).
- FR-5 — Lista inicial de 2 itens.
- FR-6 — Multiplicador padrão x2, campo aberto.
- FR-7 — Teto de stacks para itens sem cálculo limitado.
- Tabela §4.2 — Valores padrão de elegibilidade (lunares e void não multiplicam por padrão; configuráveis).
- FR-13 — Partículas não alteram gameplay.
- NFR-2 — Decisões e configuração de gameplay valem as do host.
- NFR-4 — Evitar alocação por tick quando viável.
