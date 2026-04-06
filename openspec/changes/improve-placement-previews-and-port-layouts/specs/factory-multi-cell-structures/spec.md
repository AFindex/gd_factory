## MODIFIED Requirements

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
