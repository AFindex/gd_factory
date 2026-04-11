## MODIFIED Requirements

### Requirement: Interior editing reuses factory-style build controls
The game SHALL provide interior building controls that mirror the existing factory-building interaction style for selecting, rotating, previewing, placing, and removing structures on the internal grid, including boundary attachments that span the factory edge and multi-cell structures that occupy several interior cells, while ensuring the preview language reflects cabin-native silhouettes and maintenance-space layering.

#### Scenario: Boundary attachment placement uses familiar controls
- **WHEN** the player is inside the mobile factory editor and chooses a boundary attachment, rotates it, and clicks a valid boundary mount
- **THEN** the attachment is previewed and placed using the same style of build controls used for ordinary internal structures while still respecting its cross-boundary shape rules and showing cabin-side hull-interface presentation cues

#### Scenario: Multi-cell interior structure preview shows the full footprint
- **WHEN** the player selects a compatible multi-cell interior structure and moves the cursor across the mobile-factory grid
- **THEN** the editor preview shows the full occupied footprint, current facing, cabin-oriented silhouette, and validity of every required interior cell before placement

#### Scenario: Removing any occupied cell clears the interior structure
- **WHEN** the player removes a placed multi-cell interior structure by targeting any occupied cell in its footprint
- **THEN** the editor resolves the owning structure and clears the full occupied footprint from the interior layout

### Requirement: Interior editor shows ports and their external state
The game SHALL display mobile factory boundary attachments inside the editor along with their direction, cross-boundary shape, external connection state, and cabin-side interface role so the player can distinguish hull interfaces from ordinary internal logistics pieces.

#### Scenario: Editor shows attachment role and connection state
- **WHEN** the player views a boundary attachment in the mobile factory editor
- **THEN** the editor indicates whether it is an input or output attachment, which cells belong inside versus outside the hull, whether it is currently connected, disconnected, or blocked at the world boundary, and where the cabin-side interface meets the embedded logistics layer

## ADDED Requirements

### Requirement: Interior previews preserve maintenance-route readability
The game SHALL keep interior previews, ghost placements, and palette-facing labels readable as maintenance-space layouts by distinguishing maintenance access surfaces from embedded cargo-routing hardware.

#### Scenario: Preview distinguishes maintenance route from cargo route
- **WHEN** the player previews a cabin-native logistics piece or machine inside the interior editor
- **THEN** the preview communicates which parts belong to embedded cargo hardware and which faces remain readable as maintenance access or service frontage rather than presenting the whole object as a tiny freestanding factory building
