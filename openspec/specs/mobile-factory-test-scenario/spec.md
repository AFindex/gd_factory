# mobile-factory-test-scenario Specification

## Purpose
TBD - created by archiving change add-complex-mobile-factory-test-scenario. Update Purpose after archive.
## Requirements
### Requirement: Large-scale mobile factory test scenario ships as a separate scene
The game SHALL provide a separate large-scale mobile factory test scenario scene for regression and observation without replacing the focused mobile factory demo scene.

#### Scenario: Large scenario can be opened independently
- **WHEN** the project content is inspected after the change is implemented
- **THEN** a dedicated large-scale mobile factory test scenario exists as its own scene entry point alongside the focused mobile factory demo

### Requirement: Large scenario starts with mixed mobile factory activity
The large-scale mobile factory test scenario SHALL load with multiple mobile factories already present in mixed lifecycle states, with varied size profiles, and with distinct authored roles such as mineral extraction, intermediate processing, station transfer, defense support, or maintenance logistics.

#### Scenario: Deployed and moving factories coexist with different sandbox roles
- **WHEN** the large-scale mobile factory test scenario finishes loading
- **THEN** the map contains multiple mobile factories, with at least one already deployed, at least one still in transit, and the participating factories covering more than one world-logistics role

### Requirement: Scenario interiors cover diverse logistics case studies
The large-scale mobile factory test scenario SHALL assign distinct pre-authored interior layouts to its mobile factories so the scene exercises varied conveyor and transfer topologies using real factory structures and recipe chains rather than repeating producer-based shortcut lines.

#### Scenario: Factories showcase different real production categories
- **WHEN** a tester inspects the interior layouts of the mobile factories included in the large-scale scenario
- **THEN** the set of layouts includes multiple named categories such as mineral intake buffering, refining, assembly, defense resupply, recirculation, relay transfer, or receiving support built from real structures

#### Scenario: Interiors avoid placeholder-source mainlines
- **WHEN** a tester inspects the authored interior layouts used by the large-scale scenario
- **THEN** the main sandbox loops do not depend on a producer structure as the primary source of goods needed for that factory's role

### Requirement: Scenario supports long unattended operation
The large-scale mobile factory test scenario SHALL provide sink, recycler, or equivalent recovery paths so its active logistics loops can continue running without permanent belt blockage during extended observation.

#### Scenario: Recovery paths prevent permanent belt lockup
- **WHEN** the large-scale mobile factory test scenario runs unattended for an extended period
- **THEN** the authored interior and world loops continue to consume, recycle, or discharge produced items in a way that prevents permanent congestion from halting every active conveyor route

### Requirement: Scenario includes a player-controlled factory alongside autonomous actors
The large-scale mobile factory test scenario SHALL keep one mobile factory available for direct player control while background mobile factories continue executing their own authored behavior.

#### Scenario: Player control coexists with background activity
- **WHEN** the player drives, deploys, recalls, or edits the designated player-controlled mobile factory in the large-scale scenario
- **THEN** the other scenario mobile factories continue to move, stay deployed, or run their logistics loops according to their authored roles instead of pausing with the player's actions

### Requirement: Large mobile factory scenario uses categorized workspace panels
The game SHALL reorganize the large mobile factory test scenario HUD behind categorized workspace panels so sandbox tools, scenario diagnostics, and observation content are no longer always shown together.

#### Scenario: Scenario load shows menu-first HUD organization
- **WHEN** the player opens the large mobile factory test scenario
- **THEN** the HUD shows a workspace menu with the scenario's available panel categories and keeps non-active tool groups collapsed until selected

### Requirement: Scenario sandbox tooling is split into dedicated panels
The game SHALL separate scenario-facing sandbox tooling into dedicated panels, including a build test panel, so each testing workflow can be opened without crowding unrelated scenario information.

#### Scenario: Build test panel is distinct from scenario diagnostics
- **WHEN** the player selects the build test workspace inside the large mobile factory test scenario
- **THEN** the HUD shows build-oriented test controls in their own panel while unrelated scenario diagnostics remain in their separate workspace panels until explicitly selected

### Requirement: Large scenario world contains dense mineral fields and receiving stations
The large-scale mobile factory test scenario SHALL load with a world map that contains many mineral deposits across the expanded resource roster plus multiple receiving-station or depot-style logistics hubs that mobile factories can connect to.

#### Scenario: World map exposes many mineral and station targets on load
- **WHEN** the large-scale mobile factory test scenario finishes loading
- **THEN** the player can identify multiple distinct mineral regions and multiple receiving-station-style world hubs without leaving the scene

#### Scenario: Different factories can specialize on different field-to-station loops
- **WHEN** the large scenario is running with its authored mobile factories
- **THEN** different factories can be observed serving different combinations of mineral fields, processing roles, and receiving-station destinations

