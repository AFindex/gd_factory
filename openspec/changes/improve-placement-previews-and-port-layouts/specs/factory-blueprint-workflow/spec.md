## ADDED Requirements

### Requirement: Blueprint application preview matches committed multi-cell placement
The game SHALL render blueprint application previews for multi-cell structures using the same resolved footprint center, rotated bounds, and facing that the committed placement will use.

#### Scenario: Multi-cell blueprint preview aligns to final committed position
- **WHEN** the player previews a blueprint that contains a multi-cell structure on a valid world-grid anchor
- **THEN** the preview for that structure is centered over the same resolved footprint that will be occupied after the blueprint is applied

#### Scenario: Rotated multi-cell blueprint preview preserves true occupied cells
- **WHEN** the player rotates a blueprint apply preview that contains one or more multi-cell structures
- **THEN** each previewed structure rotates its footprint and visual center consistently so the previewed occupied cells match the final rotated placement
