## MODIFIED Requirements

### Requirement: Interior editing reuses factory-style build controls
The game SHALL provide interior building controls that mirror the existing factory-building interaction style for selecting, rotating, placing, and removing structures on the internal grid, including boundary attachments that span the factory edge.

#### Scenario: Boundary attachment placement uses familiar controls
- **WHEN** the player is inside the mobile factory editor and chooses a boundary attachment, rotates it, and clicks a valid boundary mount
- **THEN** the attachment is previewed and placed using the same style of build controls used for ordinary internal structures while still respecting its cross-boundary shape rules

### Requirement: Interior editor shows ports and their external state
The game SHALL display mobile factory boundary attachments inside the editor along with their direction, cross-boundary shape, and external connection state.

#### Scenario: Editor shows attachment role and connection state
- **WHEN** the player views a boundary attachment in the mobile factory editor
- **THEN** the editor indicates whether it is an input or output attachment, which cells belong inside versus outside the hull, and whether it is currently connected, disconnected, or blocked at the world boundary
