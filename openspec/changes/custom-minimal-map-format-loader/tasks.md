## 1. Map Format Foundation

- [ ] 1.1 Define the project-owned factory map document schema, version marker, shared DTOs, and minimal per-entry payload rules for world and interior maps
- [ ] 1.2 Implement deterministic parsing/serialization support for the custom factory map files and establish where authored map assets live in the project
- [ ] 1.3 Implement validation and normalization for required sections, map kind, occupancy conflicts, unsupported kinds/facings, and map-kind-specific constraints

## 2. Shared Runtime Loading

- [ ] 2.1 Implement shared runtime loading entry points that read validated world-map documents and reconstruct deposits, anchors, and structures through existing placement flows
- [ ] 2.2 Implement shared runtime loading entry points that read validated interior-map documents and reconstruct interior structures and runtime-authored options through existing placement flows
- [ ] 2.3 Separate loader/reconstruction responsibilities from demo controllers so demo code consumes map-loading services instead of embedding raw authored layout payloads

## 3. Demo Migration

- [ ] 3.1 Move the static factory sandbox authored startup layout out of `FactoryDemo` and into the custom map format, then load it through the shared map loader
- [ ] 3.2 Move the focused mobile-factory demo's authored world layout and authored interior layout out of `MobileFactoryDemo` and into the custom map format, then load them through the shared map loader
- [ ] 3.3 Preserve demo-specific orchestration, HUD/input flows, smoke hooks, and non-map controller logic while removing the migrated authored layout payloads from the demo controllers

## 4. Verification

- [ ] 4.1 Add regression coverage or validation helpers that verify malformed map files fail cleanly and valid map files reconstruct the expected authored content
- [ ] 4.2 Verify that the static sandbox and focused mobile demo still preserve current build, deployment, detail, blueprint, inventory, and smoke-tested automation behavior after the map-driven migration
