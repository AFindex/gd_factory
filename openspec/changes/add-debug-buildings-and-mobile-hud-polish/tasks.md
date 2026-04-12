## 1. Debug Structures And Catalogs

- [x] 1.1 Add new debug build prototype kinds, labels, accent colors, and site-aware presentation text for world and interior debug source structures plus the permanent test generator
- [x] 1.2 Implement the debug source structures and permanent test generator behavior so they produce site-compatible items or stable power without upstream inputs or fuel
- [x] 1.3 Extend `FactoryIndustrialStandards`, build palette categories, and compatibility rules so world and interior catalogs expose clearly labeled debug construction entries

## 2. Static Factory Demo Integration

- [x] 2.1 Update the static factory demo build workspace and related HUD/catalog rendering so the new debug world structures appear in the build categories
- [x] 2.2 Ensure the static demo's default authored layout remains unchanged and does not rely on debug structures for its normal production or power loops

## 3. Mobile Factory Demo And Editor UI

- [x] 3.1 Update the focused mobile factory demo's world-side construction flow so the shared debug world structures are available from its build category or equivalent construction entry point
- [x] 3.2 Simplify the mobile factory editor operation panel to keep only build-focused controls and construction categories, removing duplicated overview-style information from the top of that panel
- [x] 3.3 Rework `MobileFactoryHud` layout so the workspace tab chrome lives inside the mobile factory overview panel header instead of a separate top panel
- [x] 3.4 Add the overview panel's right-side collapse/expand button and horizontal slide behavior, and rebalance layout sizing with the editor viewport and editor operation panel
- [x] 3.5 Expose interior debug source modules and the permanent test generator through the mobile factory editor's construction categories

## 4. Verification And Regression Coverage

- [x] 4.1 Add or update smoke coverage for debug source structures and permanent test generators in world and interior contexts
- [x] 4.2 Add regression checks for the focused mobile factory HUD covering overview tab embedding, overview collapse/restore behavior, and the slimmed-down editor operation panel
- [x] 4.3 Verify that workspace selection, editor session state, and authored default demo loops still behave correctly when no debug structures are placed
