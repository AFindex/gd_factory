## MODIFIED Requirements

### Requirement: Sandbox supports authored typed resource deposits
The game SHALL support authored resource deposits on world-grid cells across an expanded mineral roster, with each deposit exposing a resource type, a visible footprint, and placement restrictions that distinguish extractable terrain from generic build floor, and the authored demo maps SHALL include dense mineral regions instead of only sparse baseline patches.

#### Scenario: Starter sandbox contains multiple rich resource regions
- **WHEN** the static factory sandbox loads its default starter layout
- **THEN** the world contains more than the baseline coal, iron, and copper resource families and presents multiple dense mineral regions that are visually distinguishable on the map

#### Scenario: Incompatible structures cannot consume expanded deposit cells
- **WHEN** the player targets any deposit cell from the expanded mineral roster with a structure that is not a compatible extractor
- **THEN** placement is rejected and the preview indicates that the targeted resource cell requires a matching mining structure

### Requirement: Prototype deposits support sustained sandbox verification
The game SHALL keep authored prototype deposits available for normal starter-layout, large-scenario, and smoke-test durations so the richer sandbox can run continuously without exhausting the core resource fields that its authored loops depend on.

#### Scenario: Expanded starter layout continues extracting over normal smoke duration
- **WHEN** the default powered sandbox runs for the project's normal smoke-test duration
- **THEN** the authored mining lines across the expanded mineral roster continue producing resources without the required deposits becoming unavailable during that run

#### Scenario: Large mobile scenario retains long-running mining throughput
- **WHEN** the large mobile factory scenario runs unattended for an extended observation period
- **THEN** its authored mineral fields remain sufficient for the participating mobile factories to continue their specialized extraction roles

### Requirement: Mobile factory mining inputs extract only through surviving deployed stakes
The game SHALL allow a mining input port to mine resources from the expanded mineral roster only from compatible deposit cells that currently host a deployed, surviving mining stake child structure.

#### Scenario: Deployed stake enables extraction from an expanded mineral family
- **WHEN** a mining input port is deployed with at least one surviving mining stake on compatible deposit cells from one of the added mineral families
- **THEN** the port enters an active mining state and can feed that resource into the mobile factory's logistics flow

#### Scenario: Non-matching rich deposits do not silently produce
- **WHEN** a mining input projection overlaps nearby cells that belong to a different or incompatible mineral family
- **THEN** only the compatible deposit cells contribute output and the incompatible cells produce nothing

## ADDED Requirements

### Requirement: Resource districts support receiving-station logistics at map scale
The game SHALL author extraction districts so their output can be routed into receiving-station or depot-style logistics hubs in both the static sandbox and the large mobile-factory scenario.

#### Scenario: Static sandbox mine feeds a map-scale receiving hub
- **WHEN** an authored extraction district is running in the static sandbox
- **THEN** at least one of its outputs can be routed into a receiving or depot-style logistics hub elsewhere on the map

#### Scenario: Mobile scenario mine feeds a world-side station
- **WHEN** an authored mobile-factory mining loop is active in the large scenario
- **THEN** the mined output can be exchanged with at least one world-side receiving station or depot through the authored logistics network
