## 1. Stand Up Site Catalog And Rule Separation

- [ ] 1.1 Introduce explicit `World` and `Interior` site-kind or rule-set identifiers that can be resolved from existing world-grid and mobile-interior sites
- [ ] 1.2 Add build-catalog configuration for `WorldBuildCatalog` and `InteriorBuildCatalog` and route build palette population through the active site catalog
- [ ] 1.3 Add placement and authored-layout validation that rejects structures not allowed by the active site kind

## 2. Add Resource And Cargo-Form Layering

- [ ] 2.1 Introduce data definitions that separate resource identity from cargo form for transportable goods
- [ ] 2.2 Update item creation, transport state, and structure acceptance checks to read cargo-form-aware contracts instead of assuming one universal item presentation
- [ ] 2.3 Add a minimal first-pass cargo-form set for the new flow, including `WorldBulk`, `WorldPacked`, and `InteriorFeed`

## 3. Implement Standard-Conversion Flow

- [ ] 3.1 Add first-class conversion structures or equivalent runtime contracts for unpacking, packing, and transfer buffering
- [ ] 3.2 Update manufacturing and transfer rules so conversion structures consume and emit the configured cargo forms through the normal logistics layer
- [ ] 3.3 Update boundary attachment behavior so world-to-interior and interior-to-world flow validates against industrial-standard and cargo-form expectations

## 4. Rework Mobile Interior Editing Around Interior Standards

- [ ] 4.1 Scope mobile-factory interior editing palettes, preview text, and placement flow to the interior-only build catalog
- [ ] 4.2 Update interior-side structure presentation contracts so interior logistics and machines read as embedded modules rather than scaled-down world equipment
- [ ] 4.3 Define and integrate the first maintenance-space presentation pass so player-scale traversal, maintenance routes, and cargo-routing layers can coexist in the interior

## 5. Add Visual And Scenario Validation For The New Standards

- [ ] 5.1 Update transport visual profile resolution so world and interior cargo forms of the same resource can render with distinct deterministic payload visuals
- [ ] 5.2 Author one end-to-end demo chain that moves resources from world extraction through unpacking, interior processing, packing, and world delivery
- [ ] 5.3 Extend map, runtime, and smoke validation to cover site-catalog compatibility, cargo-form conversion, and the new world-to-interior industrial-standard boundary flow
