## MODIFIED Requirements

### Requirement: Placement snaps to valid cells
The game SHALL place prototype factory structures and deployable mobile factories on snapped grid cells only when every cell required by the target footprint is valid and available.

#### Scenario: Valid placement creates a structure
- **WHEN** the player selects a buildable static structure and clicks an empty valid cell
- **THEN** the structure is instantiated at the snapped cell with its intended orientation and occupancy recorded

#### Scenario: Valid mobile factory deployment reserves its footprint
- **WHEN** the player uses the dedicated mobile-factory demo to target a clear valid deployment anchor
- **THEN** the mobile factory is deployed, all cells in its footprint and active port reservations are recorded, and overlapping placements become invalid

#### Scenario: Invalid placement is blocked
- **WHEN** the player attempts to place a structure or deploy a mobile factory outside the build bounds or on any occupied or reserved required cell
- **THEN** the structure is not created, the mobile factory is not deployed, and the preview indicates that the placement is invalid

### Requirement: Placement preview communicates build state
The game SHALL show a live preview for the currently selected structure or deployment action so the player can understand what will happen before placement.

#### Scenario: Preview follows hovered cell
- **WHEN** the player has a build or deployment tool active and moves the cursor across the factory floor
- **THEN** a preview moves to the hovered snapped cell and updates to reflect current orientation and validity

#### Scenario: Mobile factory preview shows footprint and ports
- **WHEN** the player is targeting a mobile factory deployment in the dedicated mobile-factory demo
- **THEN** the preview highlights the full footprint and world-facing port cells so blocked deployment cells are visible before confirmation
