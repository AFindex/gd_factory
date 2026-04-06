## MODIFIED Requirements

### Requirement: Demo includes an observable automation loop
The game SHALL include a playable static factory demo whose default startup layout is built from real mining, refining, assembly, power, logistics, and defense structures, and the authored districts SHALL form multiple observable loops without relying on legacy producer structures as the primary source of throughput.

#### Scenario: Startup layout uses real structures for the core loop
- **WHEN** the player starts the default static factory demo
- **THEN** at least one authored loop mines resources, refines or assembles them with powered machines, and delivers the result through normal logistics to a receiving endpoint without requiring a producer shortcut

#### Scenario: Defense resupply comes from the same factory economy
- **WHEN** the static sandbox runs with its authored starter layout
- **THEN** at least one defensive lane receives ammunition that originates from mined and manufactured inputs inside the sandbox instead of directly spawned test cargo

### Requirement: Demo sandbox spans multiple authored districts
The game SHALL expand the static sandbox world to include visibly separated authored districts for dense mining fields, receiving stations, manufacturing, power maintenance, defense, and regression-friendly logistics observation so material movement across map-scale roles is readable on one map.

#### Scenario: Expanded sandbox loads mining fields and receiving districts together
- **WHEN** the player enters the static factory demo from the launcher
- **THEN** the playable world includes authored mining regions with more than the baseline three resource families plus separate receiving or depot-style logistics districts connected to the wider sandbox

#### Scenario: Camera and interaction remain usable across the richer sandbox
- **WHEN** the player pans, zooms, and builds across the expanded sandbox
- **THEN** camera bounds, hover targeting, and build previews continue to function across the enlarged world that now includes additional mining and station districts

### Requirement: Starter use cases verify the new core factory systems
The game SHALL include authored starter-layout use cases and smoke coverage that verify real resource extraction, expanded recipe progression, power maintenance, and defense resupply on the default sandbox, and producer-only lanes SHALL NOT count as satisfying those startup use cases.

#### Scenario: Smoke flow validates a real powered production chain
- **WHEN** the static sandbox smoke test runs against the default starter layout
- **THEN** it verifies that mined resources flow through powered manufacturing and reach a receiving endpoint without depending on a producer structure to create the main goods

#### Scenario: Smoke flow validates real defense resupply and power upkeep
- **WHEN** the static sandbox smoke test runs against the default starter layout
- **THEN** it verifies that at least one authored defense lane is resupplied by the sandbox's real production chain while at least one authored powered district remains sensitive to fuel or network maintenance state

## ADDED Requirements

### Requirement: Static sandbox includes authored receiving-station case studies
The game SHALL include authored receiving-station or depot-style logistics case studies in the static sandbox so mined and manufactured goods visibly accumulate, transfer, or recycle through map buildings instead of ending only at isolated sinks.

#### Scenario: Receiving station accepts output from a remote production district
- **WHEN** a remote mining or manufacturing district is running in the authored sandbox
- **THEN** at least one receiving-station-style node accepts its output through loaders, unloaders, storage, depots, or equivalent logistics buildings on the shared world map

#### Scenario: Receiving district participates in a closed loop
- **WHEN** the authored sandbox runs for an extended observation period
- **THEN** at least one receiving district forwards, buffers, recycles, or consumes incoming goods in a way that keeps the wider loop readable without depending on permanent blockage
