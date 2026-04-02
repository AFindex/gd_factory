# factory-grid-building Specification

## Purpose
TBD - created by archiving change add-3d-factory-core-demo. Update Purpose after archive.
## Requirements
### Requirement: Demo world provides a bounded build grid
The game SHALL provide a bounded grid-aligned build surface in the demo scene that can be addressed by integer cell coordinates.

#### Scenario: Build grid exists on demo load
- **WHEN** the factory demo scene is loaded
- **THEN** the player can target cells on a defined buildable floor within the playable area

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

#### Scenario: Preview follows hovered cell during build mode
- **WHEN** the player has explicitly selected a build or deployment tool and moves the cursor across the factory floor
- **THEN** a preview moves to the hovered snapped cell and updates to reflect current orientation and validity

#### Scenario: Mobile factory preview shows footprint and ports
- **WHEN** the player is targeting a mobile factory deployment in the dedicated mobile-factory demo
- **THEN** the preview highlights the full footprint and world-facing port cells so blocked deployment cells are visible before confirmation

### Requirement: Placement requires explicit build selection
The game SHALL default the static factory demo to a non-build interaction mode and only place structures when the player has explicitly selected a buildable prototype or deployment action.

#### Scenario: Default clicks interact instead of placing
- **WHEN** the player has not selected a buildable prototype and clicks on the factory floor or an existing structure
- **THEN** the game stays in interaction mode and does not place a new structure

#### Scenario: Selecting a prototype enables placement
- **WHEN** the player explicitly selects a buildable prototype and targets a valid empty cell
- **THEN** the game enters build mode and allows the selected structure to be placed according to the normal snapped placement rules

### Requirement: Structures can be removed for iteration
The game SHALL allow the player to remove placed prototype structures during the demo so layout iteration is possible without restarting the scene.

#### Scenario: Removing a structure clears occupancy
- **WHEN** the player uses the remove action on an existing structure
- **THEN** the structure is deleted from the scene and its occupied cell or cells become buildable again
