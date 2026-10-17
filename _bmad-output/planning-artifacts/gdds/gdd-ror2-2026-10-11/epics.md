# Épicos - ror2 (mod RoR2)

Ordem sugerida: 1 → 2 → 3 → 4 → 5 → 6. Épico 1 destrava as decisões em aberto.

## Épico 1 - Medição e linha de base (FR-1)
Meta: números de frame time e RAM nas 3 condições (sem mod, mod sem multiplicação, mod com multiplicação) no cenário de referência.
- Histórias: instrumentar captura de frame time/RAM; script do cenário (3 jogadores, seed fixo, Fungus 50+, Fireworks ~200); registrar baseline e definir metas de FPS/RAM.
- Resolve: metas numéricas e causa real do custo.

## Épico 2 - Cálculo limitado (FR-2/FR-3)
Meta: Bustling Fungus e Bundle of Fireworks sem crescimento ilimitado, efeito reconhecível, ligável/desligável.
- Histórias: limitar Bustling Fungus; limitar Bundle of Fireworks; flag de config por item.
- Resolve: fórmula e limite (manter quantidade ou agregar), com base no épico 1.

## Épico 3 - Multiplicação de itens (FR-6/7/8)
Meta: pickup elegível multiplicado por inteiro configurável (padrão x2).
- Histórias: multiplicador no host; regras de elegibilidade (raridades, lunar/void configuráveis, exclusões); jogador morto/desconectado; teto de stacks configurável (padrão sem teto).

## Épico 4 - Dificuldade (FR-11)
Meta: fator único decidido no host escalando vida, dano, quantidade e ritmo.
- Histórias: fator no host; aplicar aos 4 eixos; calibrar em playtest.

## Épico 5 - Corte de partículas (FR-12/13)
Meta: desabilitar ou encurtar efeitos de itens por lista configurável sem alterar dano, cura ou colisão.
- Histórias: mecanismo de lista configurável; levantar partículas de maior custo (usa dados do épico 1).

## Épico 6 - Compartilhamento (FR-9/10, v2)
Meta: pickup replicado integralmente aos demais; não recursivo; ordem pickup → multiplicação → compartilhamento.
- Histórias: replicação no host; garantia de não recursão.
