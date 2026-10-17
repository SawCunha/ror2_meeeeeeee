---
baseline_commit: 1afd157
---

# Story 2.4: Sincronização do limite para clientes (condicional)

Status: done (desnecessária)

## Decisão

O relatório da Story 1.3 (`docs/relatorio-ror2-dll.md`, seção 5) concluiu **A8 = host-only suficiente**: Fungus (`useOnServer = true, useOnClient = false`, `NetworkServer.active`) e Fireworks (`FireworkLauncher.FixedUpdate` retorna sem `NetworkServer.active`) executam só no servidor. Pelo AC final da story, ela é marcada como desnecessária e **não é implementada**.

## Reabrir se

A validação com cliente real (Story 2.3) mostrar efeito ou custo no cliente.
