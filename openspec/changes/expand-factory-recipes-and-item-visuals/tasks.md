## 1. Expand Item And Recipe Data

- [x] 1.1 Extend the shared item and resource definitions to cover the new starter catalog, including coal, iron ore, copper ore, iron plate, copper plate, steel plate, gear, copper wire, circuit board, machine part, ammo magazine, and high-velocity ammo.
- [x] 1.2 Rework the shared recipe catalogs so smelting and assembly machines can use the expanded branching chain with explicit inputs, outputs, cycle times, and power demand values.
- [x] 1.3 Update recipe-capable structures and inspection summaries so the new recipe families expose correct accepted ingredients, produced outputs, and readable detail text.

## 2. Add Configurable Item Visual Profiles

- [x] 2.1 Introduce item visual profile definitions and lookup helpers that support per-item tint, optional texture, optional 3D model, and optional billboard sprite fallback.
- [x] 2.2 Refactor shared transport payload rendering so moving items use a visual factory with deterministic fallback order instead of a single hard-coded cube mesh.
- [x] 2.3 Apply an immediate first-pass color pass to the existing item set and hook the new profiles into belt and other moving-item presentation paths.

## 3. Rebuild The Demo Showcase

- [x] 3.1 Re-author the relevant `factory_demo` starter lines so iron and copper branches feed shared intermediate manufacturing and at least one final multi-input crafted output.
- [x] 3.2 Configure at least one authored starter item to use a billboard sprite or 3D model transport profile so the richer item presentation is visible in normal play.
- [x] 3.3 Update any HUD, structure-detail, or recipe-summary surfaces that need to explain the richer production chain and differentiated items.

## 4. Verify Production And Readability

- [x] 4.1 Extend sandbox smoke coverage to verify that a crafted output depending on more than one resource branch is produced and delivered on the default layout.
- [x] 4.2 Add or update regression checks so mixed placeholder, billboarded, and modeled item visuals remain visible without changing transport behavior.
- [x] 4.3 Run the relevant demo or smoke validation path and resolve any issues in authored layout, recipe wiring, or item readability before closing the change.
