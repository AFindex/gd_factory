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
The game SHALL place prototype factory structures on snapped grid cells and reject placement requests that violate placement rules.

#### Scenario: Valid placement creates a structure
- **WHEN** the player selects a buildable structure and clicks an empty valid cell
- **THEN** the structure is instantiated at the snapped cell with its intended orientation and occupancy recorded

#### Scenario: Invalid placement is blocked
- **WHEN** the player attempts to place a structure outside the build bounds or on an occupied cell
- **THEN** the structure is not created and the preview indicates that the placement is invalid

### Requirement: Placement preview communicates build state
The game SHALL show a live preview for the currently selected structure so the player can understand what will happen before placement.

#### Scenario: Preview follows hovered cell
- **WHEN** the player has a build tool active and moves the cursor across the factory floor
- **THEN** a preview moves to the hovered snapped cell and updates to reflect current orientation and validity

### Requirement: Structures can be removed for iteration
The game SHALL allow the player to remove placed prototype structures during the demo so layout iteration is possible without restarting the scene.

#### Scenario: Removing a structure clears occupancy
- **WHEN** the player uses the remove action on an existing structure
- **THEN** the structure is deleted from the scene and its occupied cell or cells become buildable again

