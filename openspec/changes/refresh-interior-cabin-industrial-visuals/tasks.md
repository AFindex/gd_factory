## 1. Establish Cabin Presentation Foundations

- [x] 1.1 Introduce a reusable interior-cabin presentation role/style layer for embedded logistics, service modules, buffer cabinets, power nodes, hardpoints, and hull interfaces
- [x] 1.2 Update structure visual-profile resolution so interior-context structures can select cabin-native builders/scenes without duplicating simulation classes
- [x] 1.3 Add shared naming, trim, and labeling helpers so cabin-native previews and UI text can resolve consistently across structures, cargo, and interfaces

## 2. Rebuild Interior Structure Presentation

- [x] 2.1 Replace interior logistics-piece visuals such as belts, splitters, mergers, bridges, inserters, and transfer buffers with embedded channel / tray / routing-hardware presentation
- [x] 2.2 Rework interior machines, storage, power nodes, and hardpoints so they read as cabin modules, cabinets, bus nodes, and turret wells instead of miniaturized world facilities
- [x] 2.3 Rebuild input/output/mining boundary attachments so their interior side reads as hull-standard interfaces with cabin-side adapters, hatches, and transition geometry

## 3. Rebuild Interior Cargo Presentation

- [x] 3.1 Add cabin-native carrier families for `InteriorFeed` and other interior cargo contexts, including descriptor data for cassettes, canisters, trays, magazines, or equivalent cabin carriers
- [x] 3.2 Update transport visual resolution and fallback/batching paths so interior cargo forms no longer reuse world bulk or world packed silhouettes at smaller scale
- [x] 3.3 Preserve cross-context resource readability by keeping shared color/motif cues between world and interior forms of the same resource

## 4. Align Editor And Demo Presentation

- [x] 4.1 Update mobile-factory interior previews, ghost placements, palette labels, and port overlays so they reflect cabin-native silhouettes and maintenance-route readability
- [x] 4.2 Refresh the mobile-factory demo and relevant interior case-study maps so key interior structures, interfaces, and cargo flows visibly demonstrate the new cabin language

## 5. Add Visual Regression Coverage

- [x] 5.1 Extend smoke and headless validation to assert cabin-native cargo descriptors, site-aware structure presentation branches, and interior boundary-interface presentation cues
- [x] 5.2 Add or update targeted visual/demo checks so future changes cannot silently reintroduce world-side silhouettes into interior-only objects
