## MODIFIED Requirements

### Requirement: Placement snaps to valid cells
The game SHALL place prototype factory structures and deployable mobile factories on snapped grid cells only when every cell required by the target footprint is valid and available.

#### Scenario: Valid placement creates a structure
- **WHEN** the player selects a buildable static structure and clicks an empty valid anchor whose full resolved footprint is buildable
- **THEN** the structure is instantiated at the snapped anchor with its intended orientation and occupancy recorded for every cell in its footprint

#### Scenario: Valid mobile factory deployment reserves its footprint
- **WHEN** the player uses the dedicated mobile-factory demo to target a clear valid deployment anchor
- **THEN** the mobile factory is deployed, all cells in its footprint and active port reservations are recorded, and overlapping placements become invalid

#### Scenario: Invalid placement is blocked
- **WHEN** the player attempts to place a structure or deploy a mobile factory outside the build bounds or on any occupied, reserved, or otherwise invalid required cell in the resolved footprint
- **THEN** the structure is not created, the mobile factory is not deployed, and the preview indicates that the placement is invalid

### Requirement: Placement preview communicates build state
The game SHALL show a live preview for the currently selected structure or deployment action so the player can understand the target footprint, true facing direction, and current validity tier before placement.

#### Scenario: Preview follows hovered cell during build mode
- **WHEN** the player has explicitly selected a build or deployment tool and moves the cursor across the factory floor
- **THEN** a preview moves to the hovered snapped anchor and updates to reflect the full occupied footprint, current orientation, facing marker, and validity state

#### Scenario: Mobile factory preview shows footprint and ports
- **WHEN** the player is targeting a mobile factory deployment in the dedicated mobile-factory demo
- **THEN** the preview highlights the full footprint, world-facing port cells, and a true arrow-shaped facing indicator so blocked deployment cells remain visible before confirmation

#### Scenario: Deployable mining preview shows a warning state
- **WHEN** the player previews a mobile factory deployment whose footprint and required world cells are valid but whose mining-input projection does not currently overlap any deposits
- **THEN** the preview remains deployable, uses a distinct warning state instead of the blocked state, and shows that the mining input is not yet connected

### Requirement: Structures can be removed for iteration
The game SHALL allow the player to remove placed prototype structures during the demo so layout iteration is possible without restarting the scene.

#### Scenario: Removing a structure clears occupancy
- **WHEN** the player uses the remove action on any occupied cell belonging to an existing structure
- **THEN** the structure is deleted from the scene and every cell in its occupied footprint becomes buildable again
