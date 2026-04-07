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
