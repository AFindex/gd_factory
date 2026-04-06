## MODIFIED Requirements

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

## ADDED Requirements

### Requirement: Large scenario world contains dense mineral fields and receiving stations
The large-scale mobile factory test scenario SHALL load with a world map that contains many mineral deposits across the expanded resource roster plus multiple receiving-station or depot-style logistics hubs that mobile factories can connect to.

#### Scenario: World map exposes many mineral and station targets on load
- **WHEN** the large-scale mobile factory test scenario finishes loading
- **THEN** the player can identify multiple distinct mineral regions and multiple receiving-station-style world hubs without leaving the scene

#### Scenario: Different factories can specialize on different field-to-station loops
- **WHEN** the large scenario is running with its authored mobile factories
- **THEN** different factories can be observed serving different combinations of mineral fields, processing roles, and receiving-station destinations
