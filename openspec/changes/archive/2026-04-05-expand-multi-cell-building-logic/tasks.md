## 1. Shared Footprint Foundation

- [x] 1.1 Add a shared footprint definition and rotation helper for build prototypes/structures, with a default `1x1` fallback for unchanged content.
- [x] 1.2 Update site reservation and lookup helpers so every occupied cell of a multi-cell structure resolves to the same owning structure and can be validated as one footprint.
- [x] 1.3 Refactor shared preview/placement helper paths to compute occupied cells, directional markers, and blocking reasons from the resolved footprint contract.

## 2. World And Mobile Build Flows

- [x] 2.1 Update static sandbox placement, hover, inspection, and removal flows to work from any occupied cell of a multi-cell structure.
- [x] 2.2 Update mobile-factory interior build validation, preview rendering, placement, and removal to use the same resolved footprint pipeline.
- [x] 2.3 Ensure compatible authored build menus, presets, and layout helpers expose multi-cell structures only where the active site bounds allow them.

## 3. Example Structures And Combat

- [x] 3.1 Implement a `2x2` large storage depot prototype/structure that preserves current storage semantics while exercising the new footprint system in static and mobile build surfaces.
- [x] 3.2 Implement a `2x2` heavy gun turret prototype/structure that consumes ammo through existing logistics and reserves its full footprint.
- [x] 3.3 Add deterministic heavy-turret projectile entities and hook their spawn, update, hit, and expiry behavior into the combat simulation.
- [x] 3.4 Author or refresh demo layouts so the factory sandbox includes a heavy-turret defense example and mobile-factory content includes a valid multi-cell utility placement example.

## 4. Verification And Regression Coverage

- [x] 4.1 Add or extend smoke/regression coverage for multi-cell placement validity, occupied-cell deletion, and unchanged `1x1` placement behavior in the static sandbox.
- [x] 4.2 Add or extend mobile-factory regression coverage for interior multi-cell placement/removal and compatibility gating by interior bounds.
- [x] 4.3 Add or extend combat verification for heavy-turret ammo consumption, projectile resolution, and authored example lanes without breaking existing tower-defense cases.
