## 1. Expand shared content catalogs

- [x] 1.1 Add the new mineable mineral kinds and matching output item kinds in `FactoryTypes.cs` and `FactoryResources.cs`.
- [x] 1.2 Add names, colors, and transport visual profiles for the expanded mineral and intermediate item roster in `FactoryItemVisuals.cs`.
- [x] 1.3 Extend `FactoryStructureRecipes.cs` with mining, refining, assembly, defense-supply, and maintenance-support recipes needed by the new sandbox loops.

## 2. Replace legacy producer-driven static demo lanes

- [x] 2.1 Audit `FactoryDemo.cs` and remove authored mainline districts that currently depend on `BuildPrototypeKind.Producer` as their primary throughput source.
- [x] 2.2 Rebuild the static sandbox districts around real loops for mining, refining, assembly, receiving/depot transfer, and recycling.
- [x] 2.3 Add dense mineral fields and map-scale receiving-station/depot case studies to the static sandbox layout.

## 3. Add power-maintenance and defense closed loops

- [x] 3.1 Author at least one power-maintenance district that shows fuel delivery, outage, and recovery using the real power network.
- [x] 3.2 Rework the static defense lanes so turrets are supplied by real upstream mining and manufacturing chains instead of placeholder cargo sources.
- [x] 3.3 Ensure at least one authored defense lane succeeds under sustained supply and at least one authored lane fails after a real upstream interruption.

## 4. Rebuild focused mobile-factory sandbox cases

- [x] 4.1 Rework `MobileFactoryDemo.cs` so the focused mobile demo world contains dense mineral clusters, multiple receiving-station landmarks, and clear deployment/exchange lanes.
- [x] 4.2 Replace producer-led interior templates in the focused mobile demo with real transport, storage, refining, assembly, ammo, and port layouts.
- [x] 4.3 Ensure the focused mobile demo can demonstrate a complete field-to-interior-to-station loop through `InputPort`, `OutputPort`, or `MiningInputPort`.

## 5. Upgrade the large mobile-factory scenario

- [x] 5.1 Update `MobileFactoryScenarioLibrary.cs` presets so each large-scenario factory has a distinct authored role such as extraction, processing, station transfer, defense support, or maintenance support.
- [x] 5.2 Replace producer-based large-scenario interior templates with real machine layouts that match each factory's world-side role.
- [x] 5.3 Expand the large scenario world with many mineral regions and multiple receiving-station/depot hubs so different factories can be observed serving different loops.

## 6. Align player-facing tools and presets

- [x] 6.1 Remove or demote `Producer` from the default authored sandbox build paths, HUD categories, and mobile/static scenario presets that are meant to showcase normal gameplay.
- [x] 6.2 Update workspace text and sandbox helper panels so they describe scenario verification and loop observation instead of encouraging test-building mainlines.
- [x] 6.3 Review starter inventory, hotbar, and preset structure kits so the default player-facing tools emphasize real buildings used by the new sandbox loops.

## 7. Refresh smoke coverage and scenario verification

- [x] 7.1 Update static demo smoke checks to validate real extraction, recipe progression, receiving-station throughput, power maintenance, and defense resupply without counting producer shortcuts as success.
- [x] 7.2 Update focused and large mobile-factory verification flows to assert real port exchange, interior processing, and long-running loop stability.
- [x] 7.3 Run the static demo, focused mobile demo, and large mobile-factory scenario manually and through existing smoke paths to confirm the new closed-loop sandbox cases remain readable and stable.
