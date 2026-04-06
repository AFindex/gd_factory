# factory-multi-cell-structures Specification

## Purpose
Define the shared multi-cell footprint contract used by world-grid and mobile-factory build surfaces.

## Requirements
### Requirement: Buildable structures define shared rotated footprints
The game SHALL let a buildable structure declare a footprint as a set of local occupied cells relative to an anchor, and the resolved occupied cells, preview bounds, preview centers, and directional markers SHALL rotate with the structure's facing in the world sandbox, mobile-factory interiors, and blueprint application previews.

#### Scenario: Rotating a large structure updates every occupied cell
- **WHEN** the player previews or places a multi-cell structure and changes its facing
- **THEN** the game recomputes the full occupied-cell set, preview bounds, and directional markers from the same rotated footprint definition

#### Scenario: Blueprint preview uses the same footprint center as final placement
- **WHEN** the player previews a blueprint entry for a multi-cell structure
- **THEN** the structure's preview center and footprint overlay are derived from the same rotated footprint definition that the final placed structure uses

#### Scenario: Legacy single-cell structures keep the default footprint
- **WHEN** the player previews or places an unchanged `1x1` structure
- **THEN** the game resolves it through the same footprint system with a single occupied anchor cell and unchanged behavior

### Requirement: Occupied-cell interactions resolve to the owning structure
The game SHALL map every occupied cell of a multi-cell structure back to the same owning structure so overlap checks, selection, inspection, and deletion all operate on the full footprint instead of a single anchor cell.

#### Scenario: Overlap validation blocks any conflicting occupied cell
- **WHEN** the player targets a placement whose resolved footprint includes at least one out-of-bounds, reserved, or otherwise invalid cell
- **THEN** the placement is rejected and the blocking cells are reported against the attempted structure footprint

#### Scenario: Deleting from a non-anchor cell removes the whole structure
- **WHEN** the player removes or inspects a multi-cell structure by clicking any occupied cell that belongs to it
- **THEN** the game resolves the owning structure and clears or focuses the entire structure instead of only the clicked cell

### Requirement: The project ships with multi-cell example structures
The game SHALL include at least one combat and one utility structure that use the shared footprint contract so the feature is exercised by real content instead of only internal helpers.

#### Scenario: Heavy turret demonstrates large combat placement
- **WHEN** the player opens the factory sandbox build roster
- **THEN** the roster includes a large-footprint heavy turret that occupies multiple cells and can be placed through the normal build workflow

#### Scenario: Large storage depot works in static and mobile-factory build surfaces
- **WHEN** the player opens a compatible static sandbox or mobile-factory interior build roster
- **THEN** the roster includes a large storage depot that occupies multiple cells, validates against site bounds, and preserves normal storage/logistics semantics once placed
