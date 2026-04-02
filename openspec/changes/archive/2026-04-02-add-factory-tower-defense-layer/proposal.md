## Why

The current `Factory Sandbox` proves logistics and placement, but it still lacks the kind of hostile pressure that makes factory layout, buffering, and replenishment matter moment to moment. The requested tower-defense layer is a good next step because it lets the prototype borrow some of Factorio's most transferable combat ideas, especially ammo-fed turrets, disposable perimeter structures, and visible attrition, without needing a full pollution or evolution simulation in the first pass.

## What Changes

- Add a tower-defense combat layer to the static factory sandbox, centered on waves or lane-based enemy pressure that forces the player to protect production and transport lines.
- Add a small defensive building roster, including ammo-consuming turrets plus at least one non-offensive perimeter structure such as walls or barricades.
- Add an ammo supply loop so defensive buildings depend on factory logistics instead of firing infinitely; belts, storage, inserters, and dedicated ammo-producing structures should be able to keep defenses stocked.
- Give every placeable building a health model, damage intake behavior, destruction handling, and an in-world health bar or equivalent visual feedback when hovered, selected, or under attack.
- Add simple enemy units with at least one melee threat and one ranged or special-case threat so defensive coverage, walling, and replenishment matter.
- Expand the authored `Factory Sandbox` starter layout with combat-focused validation setups, such as stocked turret lanes, wall-backed choke points, and deliberate failure cases where defenses run dry.
- Keep the first version inspired by Factorio's combat loop rather than cloning it exactly: use ammo logistics, kill corridors, and structure attrition as the core design language, but scope enemy spawning and targeting to a deterministic sandbox-friendly prototype.

## Capabilities

### New Capabilities
- `factory-tower-defense`: Ammo-fed defensive buildings, building durability and health bars, enemy attack behavior, and authored tower-defense sandbox scenarios.

### Modified Capabilities

## Impact

- Affected systems include `FactoryTypes`, `FactoryStructure`, `FactoryStructureFactory`, `SimulationController`, `FactoryHud`, and the build/selection flow in `FactoryDemo`.
- The change will likely introduce new combat-oriented structure scripts, enemy unit scripts, shared health/damage data, ammo item typing, and simple targeting or lane-navigation helpers.
- `factory_demo.tscn` will need a richer scripted startup layout so the default sandbox demonstrates both successful defensive logistics and failure cases when turrets are starved or structures are overwhelmed.
