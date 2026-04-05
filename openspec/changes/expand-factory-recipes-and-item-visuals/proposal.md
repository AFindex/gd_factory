## Why

The current factory sandbox proves that powered mining, smelting, assembly, and logistics all work, but its content depth and item readability are still prototype-level: there are only a few item kinds, recipe chains are short, and every belt payload is still rendered as the same small box. We need a more Factorio-inspired production ladder and a configurable item presentation layer now so the sandbox can support richer balancing, more readable throughput debugging, and future content growth without hard-coding visuals per item.

## What Changes

- Expand the core item economy beyond coal, iron, plates, parts, and ammo by adding more raw materials, intermediate products, and multi-step crafting routes inspired by Factorio-style early and mid-game production loops.
- Rework recipe configuration so mining, refining, smelting, assembly, and ammo production can share a broader catalog of declared inputs, outputs, cycle times, power demand, and balance values without baking those rules into structure classes.
- Author at least one longer starter production path in the static sandbox that demonstrates branching inputs, intermediate parts, and a more meaningful end product than the current compact chain.
- Introduce configurable item visual profiles that let each item kind declare its own color, optional texture, optional 3D model, and a billboard-style 2D sprite fallback for belt and transport presentation.
- Update transport-item rendering so belt payloads can instantiate item-specific visuals instead of one shared cube, while preserving deterministic logistics behavior.
- Add an immediate first-pass color pass to the existing item set so current resources and products are easier to distinguish before richer models and textures are added.

## Capabilities

### New Capabilities
- `factory-item-visual-profiles`: Declarative item presentation profiles for logistics visuals, including tint, textures, optional models, and billboard sprite fallback behavior.

### Modified Capabilities
- `factory-manufacturing-chain`: Expand the recipe and item domain to support more raw resources, intermediate tiers, and longer crafted output paths with explicit balance data.
- `factory-production-demo`: Update the default sandbox and authored starter lines so the richer production chain and clearer belt-item differentiation are visible in normal play.

## Impact

- Affected systems include `FactoryTypes`, `FactoryStructureRecipes`, `FactoryResources`, `FactoryItemTransfer`, `FlowTransportStructure`, `FactoryDemo`, and the relevant mining, smelting, assembler, storage, and transport structures under `scripts/factory/structures/`.
- The change will add new item and recipe configuration data, new authored production routes in the static demo, and a new presentation/configuration layer for transport item visuals.
- Smoke coverage, structure inspection summaries, and demo authoring will likely need updates so the new multi-stage recipes and per-item belt visuals are observable and testable.
