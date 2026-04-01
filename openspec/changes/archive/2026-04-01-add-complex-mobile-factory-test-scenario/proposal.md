## Why

The current mobile factory demo proves the basic deploy, recall, and redeploy loop, but it is still a relatively small and curated scene with limited logistics diversity. We now need a larger, denser, longer-running test scenario so we can validate multiple mobile factories, richer world layouts, varied factory sizes, and complex interior conveyor topologies under sustained simulation instead of only short concept demos.

## What Changes

- Add a new large-scale mobile factory test scenario scene dedicated to stress-testing mobile factory behavior rather than replacing the existing focused demo.
- Expand the world map footprint and environmental content so the scenario contains more deployment space, more landmarks, and more active logistics context across the map.
- Populate the map with multiple simultaneously active mobile factories, including both deployed factories that output into the world and in-transit factories that visibly move with their own poses and orientations.
- Give each world mobile factory a distinct size profile so the scenario covers a range from smaller to larger mobile platforms instead of assuming one standard footprint.
- Build out each mobile factory interior with multiple conveyor and structure case studies, covering branching, merging, loops, crossings, buffers, bottlenecks, and other complex topologies useful for regression testing.
- Add recycler-backed sink paths or equivalent recovery loops inside the scenario so long-running automated operation does not permanently clog belts during unattended testing.
- Preserve a player-controlled mobile factory inside the same test map so manual driving, deployment, recall, redeploy, and interior editing can be tested alongside autonomous world activity.
- Extend scenario presentation and test documentation so the richer map clearly communicates which factories are deployed, which are moving, and which interior layouts are intended as specific logistics test cases.

## Capabilities

### New Capabilities
- `mobile-factory-test-scenario`: A dedicated large-scale scenario with multiple mobile factories, varied factory sizes, richer world dressing, and long-running logistics test loops for regression and observation.

### Modified Capabilities
- `mobile-factory-demo`: Expands demo coverage so the project offers not only the focused interaction demo but also a broader scenario that exercises many mobile factories and more demanding world conditions.
- `mobile-factory-lifecycle`: Broadens lifecycle expectations from single-factory validation to concurrent deployed and in-transit factories with visible state, pose, and footprint variation.
- `mobile-factory-interior-editing`: Extends interior expectations so test scenarios can showcase multiple prebuilt interior layouts with more complex conveyor topologies and sustained flow management.

## Impact

- Affects scene content under `scenes/`, especially the mobile factory demo flow and any new or derived large test-map scene.
- Likely touches `scripts/factory/MobileFactoryDemo.cs`, `scripts/factory/MobileFactoryInstance.cs`, `scripts/factory/MobileFactorySite.cs`, `scripts/factory/FactoryStructureFactory.cs`, `scripts/factory/GridManager.cs`, and `scripts/factory/SimulationController.cs` to support richer scenario setup and concurrent runtime behavior.
- Requires new authored world content, mobile factory presets, and long-running logistics arrangements that are stable enough for repeated manual testing and smoke validation.
