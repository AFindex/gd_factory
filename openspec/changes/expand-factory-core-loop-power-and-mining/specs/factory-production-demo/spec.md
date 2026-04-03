## MODIFIED Requirements

### Requirement: Demo includes an observable automation loop
The game SHALL include a playable static factory demo whose default startup layout contains multiple authored districts that together form a powered input -> production -> output chain, including resource extraction, fuel supply, power generation and distribution, intermediate manufacturing, buffering, and final delivery, so automation can be observed under sustained throughput, localized congestion, and power-state changes instead of only fixed producer-to-sink chains.

#### Scenario: Startup layout exercises powered extraction and manufacturing
- **WHEN** the player starts the default static factory demo
- **THEN** the scene already contains authored mining, power, smelting, assembly, storage, and delivery segments that demonstrate the full chain without requiring manual building

#### Scenario: Mined resources reach outputs through the powered chain
- **WHEN** the demo scene runs with its authored starter layout
- **THEN** items extracted from resource deposits flow through powered manufacturing and are registered as delivered by at least one destination structure in the scene

## ADDED Requirements

### Requirement: Demo sandbox spans multiple authored districts
The game SHALL expand the static sandbox world to roughly three times its current footprint so mining, power, manufacturing, combat, and regression lanes can coexist on one readable map.

#### Scenario: Expanded sandbox loads with separated districts
- **WHEN** the player enters the static factory demo from the launcher
- **THEN** the playable world includes visibly separated authored districts for extraction, power, manufacturing, and verification lanes on one larger map

#### Scenario: Camera and interaction remain usable across the larger world
- **WHEN** the player pans, zooms, and builds across the expanded sandbox
- **THEN** camera bounds, hover targeting, and build previews continue to function across the enlarged playable area

### Requirement: Starter use cases verify the new core factory systems
The game SHALL include authored starter-layout use cases and smoke coverage that verify powered extraction, manufacturing progression, and delivery on the default sandbox.

#### Scenario: Smoke flow validates the powered factory chain
- **WHEN** the static sandbox smoke test runs against the default starter layout
- **THEN** it verifies that resource extraction, generator-powered production, recipe progression, and final delivery all succeed without manual intervention
