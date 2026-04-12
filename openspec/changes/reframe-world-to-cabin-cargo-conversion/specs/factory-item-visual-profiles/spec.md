## MODIFIED Requirements

### Requirement: Factory item kinds expose configurable transport visual profiles
The game SHALL let each factory item kind resolve a transport visual profile that declares not only readable identity cues such as tint, texture, model, or billboard fallback, but also which industrial cargo standard it is presenting. Profiles MUST distinguish world-scale payload presentation from cabin-scale carrier presentation and MUST NOT rely on pure uniform scaling to imply conversion between the two.

#### Scenario: World cargo resolves as a large external payload profile
- **WHEN** an item kind is presented on a world belt, depot, attachment handoff, or other world-facing logistics surface
- **THEN** its resolved transport visual profile identifies a world-scale payload presentation that reads as a large external load rather than as a cabin carrier enlarged for effect

#### Scenario: World cargo keeps the same presentation class inside cabin-side conversion spaces
- **WHEN** an item kind is presented as a world payload inside a boundary handoff bay, unpacker, packer, or large-payload staging position within the mobile factory
- **THEN** its resolved transport visual profile remains the world-scale payload presentation rather than switching to a reduced-size cabin variant

#### Scenario: Cabin cargo resolves as a compact carrier profile
- **WHEN** an item kind is presented on an interior rail, slot conveyor, or cabin module input
- **THEN** its resolved transport visual profile identifies a cabin-scale carrier presentation that reads as a compact internal transport unit rather than as the original world payload simply scaled down

#### Scenario: Item profile still supports authored asset overrides
- **WHEN** an item kind defines a texture, a 3D model, or a billboard sprite in its visual profile for a supported cargo standard
- **THEN** transport rendering uses those configured visual assets for that standard instead of treating all items as identical payloads

### Requirement: Item visual profiles expose deterministic transport render descriptors and fallbacks
The game SHALL let each factory item kind resolve a transport render descriptor set that identifies its cargo standard, primary presentation, supported shared-batch representation, and deterministic fallback chain for lighter transport rendering tiers.

#### Scenario: Descriptor resolution preserves the selected cargo standard
- **WHEN** a factory item kind is queried for moving transport presentation in a world or cabin context
- **THEN** its visual profile resolves a stable descriptor chain that remains within that context's cargo standard instead of substituting an unrelated descriptor from the other standard

#### Scenario: Placeholder fallback keeps world and cabin profiles distinct
- **WHEN** an item kind falls back to placeholder geometry because authored assets are unavailable
- **THEN** the fallback still preserves whether the item is being shown as a world payload or a cabin carrier rather than collapsing both standards into the same generic cube

## ADDED Requirements

### Requirement: Cargo-standard presentation remains separate from transport semantics
The game SHALL keep world-payload versus cabin-carrier presentation separate from deterministic logistics semantics so the difference in size class does not alter routing, ordering, or transfer ownership by itself.

#### Scenario: Same route rules apply across different presentation standards
- **WHEN** a system converts a world payload into cabin carriers and those carriers continue through the shared transport simulation
- **THEN** the presentation-standard change affects only the visible descriptor set and allowed interfaces, while deterministic movement logic remains governed by the existing transport rules
