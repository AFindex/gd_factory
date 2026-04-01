## 1. Shared Scenario Runtime

- [x] 1.1 Introduce typed scenario authoring models for mobile factory profiles, actor definitions, and interior presets so large test-map content is not hard-coded as one-off setup logic
- [x] 1.2 Refactor `MobileFactoryInstance` to accept profile-driven interior bounds, deployment footprint cells, port cells, and spawn/parking metadata instead of assuming one fixed factory size
- [x] 1.3 Update deployment validation, preview rendering, hull sizing, and editor camera framing so every size-sensitive path reads from the same mobile factory profile data

## 2. Large Test Scenario Scene

- [x] 2.1 Create a new large-scale mobile factory test scenario scene that coexists with the focused mobile factory demo and the original static factory demo
- [x] 2.2 Author the larger world map bounds, landmarks, and logistics zones needed to make the scenario feel denser and easier to read during observation
- [x] 2.3 Populate the scenario with multiple mobile factory actors in mixed starting states, including one player-controlled factory plus deployed and in-transit background factories with varied size profiles and initial poses

## 3. Interior Case Library And Long-Run Flow

- [x] 3.1 Build a library of named interior presets that cover distinct logistics patterns such as branching, merging, recirculation, relay transfer, and dense balancing cases using the existing structure set
- [x] 3.2 Assign different interior presets to the authored mobile factories so opening multiple factories reveals genuinely different test layouts rather than one repeated template
- [x] 3.3 Add sink, recycler, or equivalent recovery paths to every long-running preset and world loop so extended unattended simulation does not depend on permanent belt blockage

## 4. Background Factory Behavior And Readability

- [x] 4.1 Implement simple scripted behavior loops for non-player mobile factories so some remain deployed while others move, turn, deploy, recall, and redeploy on deterministic schedules
- [x] 4.2 Add world-side readability cues for factory state and size, such as distinct markers, labels, accent colors, or other lightweight visuals that distinguish deployed, moving, and player-controlled factories
- [x] 4.3 Keep the player-controlled factory wired into the existing mobile factory command, deploy-preview, recall, and interior-editing interactions while background factories continue operating independently

## 5. Verification And Documentation

- [x] 5.1 Extend smoke coverage to verify the large scenario loads independently, includes one player-controlled factory, and starts with multiple background factories in mixed lifecycle states and varied sizes
- [x] 5.2 Add focused validation for profile-driven deployment footprints, independent reservation ownership, and long-run recovery behavior across the authored interior presets
- [x] 5.3 Update project documentation to explain the new large-scale test scenario, the purpose of each interior case category, and the expected difference between the focused mobile demo and the regression-oriented map
