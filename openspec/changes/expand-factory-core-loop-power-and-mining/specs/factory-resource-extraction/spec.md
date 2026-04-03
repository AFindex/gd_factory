## ADDED Requirements

### Requirement: Sandbox supports authored typed resource deposits
The game SHALL support authored resource deposits on world-grid cells, with each deposit exposing a resource type, a visible footprint, and placement restrictions that distinguish extractable terrain from generic build floor.

#### Scenario: Starter sandbox contains multiple resource patches
- **WHEN** the static factory sandbox loads its default starter layout
- **THEN** the world contains at least one authored fuel deposit and at least one authored ore deposit that are visually distinguishable on the map

#### Scenario: Incompatible structures cannot consume deposit cells
- **WHEN** the player targets a deposit cell with a structure that is not a compatible extractor
- **THEN** placement is rejected and the preview indicates that the targeted resource cell requires a matching mining structure

### Requirement: Mining drills require compatible deposit coverage
The game SHALL only allow a mining drill to be placed when its extraction footprint covers cells from a compatible resource deposit.

#### Scenario: Compatible drill placement succeeds on matching resource cells
- **WHEN** the player places a mining drill over a deposit that matches the drill's supported resource type
- **THEN** the drill is created successfully and records the covered deposit cells as its extraction source

#### Scenario: Drill placement fails off-deposit or on the wrong resource type
- **WHEN** the player tries to place a mining drill outside any supported deposit footprint or over a deposit of the wrong type
- **THEN** the drill is not created and the placement preview remains invalid

### Requirement: Powered mining drills emit extracted items into logistics
The game SHALL allow a powered mining drill with a valid deposit source to create extracted items at a deterministic cadence and hand them off into downstream logistics using the normal item-transfer rules.

#### Scenario: Powered drill feeds an attached logistics chain
- **WHEN** a powered mining drill has valid deposit coverage and an available downstream belt, storage, or inserter handoff
- **THEN** it produces the corresponding raw resource item and sends it into the connected logistics chain

#### Scenario: Blocked drill output stalls without losing items
- **WHEN** a powered mining drill finishes an extraction cycle but its downstream output cannot currently accept the item
- **THEN** the drill keeps the extracted item buffered or waits to emit it instead of deleting or teleporting the resource

### Requirement: Prototype deposits support sustained sandbox verification
The game SHALL keep authored prototype deposits available for normal starter-layout and smoke-test durations so the expanded sandbox can run continuously without exhausting its core resource sources.

#### Scenario: Long-running starter layout continues extracting
- **WHEN** the default powered sandbox runs for the project's normal smoke-test duration
- **THEN** the starter mining lines continue producing resources without the authored deposits becoming unavailable during that run
