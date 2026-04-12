## MODIFIED Requirements

### Requirement: Buildable structures define shared rotated footprints
The game SHALL let a buildable structure declare a footprint as a set of local occupied cells relative to an anchor, and eligible structures SHALL also be able to switch between authored footprint variants based on a configured size tier or chamber profile. The resolved occupied cells, preview bounds, preview centers, and directional markers SHALL continue to rotate with the structure's facing in the world sandbox, mobile-factory interiors, and blueprint application previews.

#### Scenario: Conversion chamber size tier changes its occupied cells
- **WHEN** the player previews or loads an unpacker or packer variant that is configured for a different bundle size tier
- **THEN** the game resolves that chamber's authored occupied-cell set, preview bounds, and center from the matching footprint variant instead of assuming the default single-cell footprint

#### Scenario: Rotating a tiered chamber still updates every occupied cell
- **WHEN** the player previews or places a multi-cell conversion chamber and changes its facing
- **THEN** the game recomputes the full occupied-cell set, preview bounds, and directional markers from the rotated footprint variant selected for that chamber tier

#### Scenario: Legacy single-cell structures keep the default footprint
- **WHEN** the player previews or places an unchanged `1x1` structure that has no size-tier footprint variants
- **THEN** the game resolves it through the same shared footprint system with a single occupied anchor cell and unchanged behavior
