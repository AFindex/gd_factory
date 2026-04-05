## MODIFIED Requirements

### Requirement: Placement preview communicates build state
The game SHALL show a live preview for the currently selected structure or deployment action so the player can understand the target footprint, true facing direction, and current validity tier before placement.

#### Scenario: Preview follows hovered cell during build mode
- **WHEN** the player has explicitly selected a build or deployment tool and moves the cursor across the factory floor
- **THEN** a preview moves to the hovered snapped cell and updates to reflect the current orientation, facing marker, and validity state

#### Scenario: Mobile factory preview shows footprint and ports
- **WHEN** the player is targeting a mobile factory deployment in the dedicated mobile-factory demo
- **THEN** the preview highlights the full footprint, world-facing port cells, and a true arrow-shaped facing indicator so blocked deployment cells remain visible before confirmation

#### Scenario: Deployable mining preview shows a warning state
- **WHEN** the player previews a mobile factory deployment whose footprint and required world cells are valid but whose mining-input projection does not currently overlap any deposits
- **THEN** the preview remains deployable, uses a distinct warning state instead of the blocked state, and shows that the mining input is not yet connected
