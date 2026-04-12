## ADDED Requirements

### Requirement: World cargo and cabin cargo use different handling scales
The game SHALL treat world cargo and cabin cargo as different industrial handling standards. World cargo MUST read as a large external payload that cannot directly traverse cabin feed rails, while cabin cargo MUST read as a compact internal carrier that only appears after conversion.

#### Scenario: World payload cannot be represented as a cabin rail item
- **WHEN** the game presents a world-facing cargo item at a boundary attachment, depot, or world belt
- **THEN** that payload is shown as a large transfer load and is not rendered or described as something that fits directly on a cabin feed rail

#### Scenario: Cabin carrier appears only after conversion
- **WHEN** a converted item enters an interior rail, slot conveyor, or cabin module
- **THEN** the visible payload is presented as a compact cabin-standard carrier rather than as the original world payload scaled down

#### Scenario: World payload keeps its size inside the cabin conversion zone
- **WHEN** a world payload is shown inside an interior-side handoff bay, unpacker, packer, or large-payload staging position
- **THEN** it remains presented at the same world-payload size class instead of being rescaled to match cabin cargo

### Requirement: Unpackers convert one world payload at a time into cabin carriers
The game SHALL model the unpacker as a one-input, one-output conversion chamber that accepts one world payload at a time and emits cabin-standard carriers into the interior logistics network.

#### Scenario: Unpacker accepts a single world payload into its chamber
- **WHEN** a world payload reaches an active unpacker
- **THEN** the unpacker reserves one world-side input load, shows that load occupying its conversion chamber, and does not present multiple simultaneous world payloads in the same chamber

#### Scenario: Unpacker output begins on the cabin side only after conversion
- **WHEN** the unpacker completes processing its current world payload
- **THEN** the interior side begins emitting cabin-standard carriers through its single cabin output instead of forwarding the original world payload through the rail

### Requirement: Packers assemble one world payload at a time from cabin carriers
The game SHALL model the packer as a one-input, one-output conversion chamber that accumulates cabin-standard carriers and assembles them into one world payload for export.

#### Scenario: Packer accumulates cabin carriers before exporting
- **WHEN** cabin carriers reach an active packer
- **THEN** the packer consumes them through its interior-side input, stages them toward one outgoing world payload, and does not expose multiple simultaneous world export loads from the same chamber

#### Scenario: Packer releases a world payload on completion
- **WHEN** the packer completes its current assembly cycle
- **THEN** it emits one visible world-standard payload through its world-facing output instead of sending cabin carriers directly into the world route

### Requirement: Conversion structures visibly show the operated payload
The game SHALL make unpackers, packers, and any directly related conversion staging structures show the payload currently being handled so the player can read the scale change in-world.

#### Scenario: Unpacker visibly shows the world payload during processing
- **WHEN** an unpacker is actively converting a world payload
- **THEN** the structure presentation shows the large incoming payload on or inside the machine rather than hiding the conversion behind a generic idle shell

#### Scenario: Packer visibly shows staged export cargo during processing
- **WHEN** a packer is assembling a world payload from cabin carriers
- **THEN** the structure presentation shows an in-progress export load, loading cradle, or chamber contents that communicate the outgoing world payload state
