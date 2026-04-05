## MODIFIED Requirements

### Requirement: Interior editing reuses factory-style build controls
The game SHALL provide interior building controls that mirror the existing factory-building interaction style for selecting, rotating, previewing, placing, and removing structures on the internal grid, including boundary attachments that span the factory edge and multi-cell structures that occupy several interior cells.

#### Scenario: Boundary attachment placement uses familiar controls
- **WHEN** the player is inside the mobile factory editor and chooses a boundary attachment, rotates it, and clicks a valid boundary mount
- **THEN** the attachment is previewed and placed using the same style of build controls used for ordinary internal structures while still respecting its cross-boundary shape rules

#### Scenario: Multi-cell interior structure preview shows the full footprint
- **WHEN** the player selects a compatible multi-cell interior structure and moves the cursor across the mobile-factory grid
- **THEN** the editor preview shows the full occupied footprint, current facing, and validity of every required interior cell before placement

#### Scenario: Removing any occupied cell clears the interior structure
- **WHEN** the player removes a placed multi-cell interior structure by targeting any occupied cell in its footprint
- **THEN** the editor resolves the owning structure and clears the full occupied footprint from the interior layout
