# factory-map-runtime-loading Specification

## Purpose
Provide shared validation and reconstruction entry points that load authored factory map documents through the existing runtime placement flows for world and interior maps.

## Requirements
### Requirement: Runtime validates factory map documents before reconstruction
The system SHALL validate a factory map document before any runtime structures or deposits are spawned so malformed authored data cannot partially build a broken map.

#### Scenario: Invalid map fails before partial reconstruction
- **WHEN** a factory map file contains duplicate occupancy, unsupported kinds, invalid facings, or missing required sections
- **THEN** the runtime reports the validation failure and does not partially reconstruct the map

#### Scenario: Map-kind-specific rules are enforced
- **WHEN** the runtime validates a world map versus an interior map
- **THEN** it applies the correct map-kind-specific rules for deposits, anchors, ports, bounds, and other authored content relevant to that map type

### Requirement: Runtime reconstructs maps through shared factory placement flows
The system SHALL rebuild authored maps by driving the existing shared factory placement and registration flows instead of bypassing them with direct low-level state mutation.

#### Scenario: Loaded structure behaves like authored placement
- **WHEN** the runtime reconstructs a structure from a validated map file
- **THEN** the structure enters the simulation through the same core placement and setup path used by current authored demo bootstrap logic

#### Scenario: Loaded deposits and anchors participate in normal runtime behavior
- **WHEN** the runtime reconstructs deposits, anchors, or equivalent authored map-side entities
- **THEN** the resulting runtime state participates in the same logistics, deployment, and interaction systems used by the demos today

### Requirement: Runtime loader exposes clear map-level entry points
The system SHALL provide shared loading entry points for authored world maps and authored interior maps so demo controllers can request reconstruction without embedding raw layout payloads directly in controller code.

#### Scenario: Static demo requests a world map load
- **WHEN** the static factory demo initializes its authored sandbox
- **THEN** it can invoke a shared map-loading entry point with a selected authored world map file instead of hardcoding the entire layout inline

#### Scenario: Mobile demo requests both world and interior map loads
- **WHEN** the focused mobile-factory demo initializes its authored content
- **THEN** it can invoke shared map-loading entry points for both its world-side layout and its interior layout while keeping demo-only orchestration separate

### Requirement: Runtime map validation can run without scene reconstruction
The system SHALL expose reusable map-validation entry points that analyze authored map documents through the same core runtime rules used by reconstruction, without requiring a caller to boot a full demo scene first.

#### Scenario: Headless preflight reuses runtime validation rules
- **WHEN** a headless validation workflow asks the runtime layer to analyze an authored world map
- **THEN** the runtime layer validates that map through the same document and placement rules used by shared runtime loading instead of a separate handwritten rule set

#### Scenario: Preflight failure blocks reconstruction
- **WHEN** runtime map analysis reports one or more error-level findings for a requested map load
- **THEN** the shared loader fails before partially reconstructing deposits, structures, or runtime state

### Requirement: Runtime interior validation supports mobile factory profiles
The system SHALL allow shared interior-map validation to run against a selected mobile factory profile so boundary attachments and other mobile-only constraints are checked with the correct factory context before reconstruction proceeds.

#### Scenario: Profile-aware validation rejects incompatible attachments
- **WHEN** a mobile-factory interior map is analyzed against a profile whose attachment mounts do not allow one of the authored attachment entries
- **THEN** validation fails with a diagnostic that identifies the authored attachment and the incompatible profile rule

#### Scenario: Profile-aware validation preserves valid focused interior loading
- **WHEN** the focused mobile-factory interior map is analyzed against its authored mobile factory profile
- **THEN** the shared runtime validation accepts the authored attachment layout and allows the existing reconstruction flow to proceed
