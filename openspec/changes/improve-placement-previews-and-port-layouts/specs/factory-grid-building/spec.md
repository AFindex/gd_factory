## MODIFIED Requirements

### Requirement: Placement preview communicates build state
The game SHALL show a live preview for the currently selected structure or deployment action so the player can understand the target footprint, true facing direction, current validity tier, and any contextual logistics hints before placement.

#### Scenario: Preview follows hovered cell during build mode
- **WHEN** the player has explicitly selected a build or deployment tool and moves the cursor across the factory floor
- **THEN** a preview moves to the hovered snapped anchor and updates to reflect the full occupied footprint, current orientation, facing marker, and validity state

#### Scenario: Belt preview shows nearby multi-port logistics hints
- **WHEN** the player is previewing a belt placement near a structure that exposes multiple input or output cells
- **THEN** the preview shows contextual input and output markers for the nearby port cells so the player can line belts up before placement

#### Scenario: Non-belt preview hides contextual port markers
- **WHEN** the player previews any non-belt buildable or exits build mode
- **THEN** the contextual input and output port markers are hidden instead of remaining visible

#### Scenario: Mobile factory preview shows footprint and ports
- **WHEN** the player is targeting a mobile factory deployment in the dedicated mobile-factory demo
- **THEN** the preview highlights the full footprint, world-facing port cells, and a true arrow-shaped facing indicator so blocked deployment cells remain visible before confirmation

#### Scenario: Deployable mining preview shows a warning state
- **WHEN** the player previews a mobile factory deployment whose footprint and required world cells are valid but whose mining-input projection does not currently overlap any deposits
- **THEN** the preview remains deployable, uses a distinct warning state instead of the blocked state, and shows that the mining input is not yet connected

## ADDED Requirements

### Requirement: Build mode supports continuous placement gestures
The game SHALL keep the currently selected build tool armed after a successful world placement and support primary-button drag placement across additional valid cells.

#### Scenario: Successful placement keeps the build tool active
- **WHEN** the player successfully places a structure in world build mode
- **THEN** the current build selection remains active so the player can immediately place the same structure again

#### Scenario: Holding the primary button places structures while dragging
- **WHEN** the player holds the primary mouse button in world build mode and drags across empty valid cells
- **THEN** the game places the selected structure on each newly hovered valid cell without requiring a separate click for every cell

#### Scenario: Drag placement skips invalid cells without disarming build mode
- **WHEN** the player drags the primary mouse button across a mix of valid and invalid cells in world build mode
- **THEN** the game places structures only on the valid cells and keeps the current build tool armed after crossing blocked cells
