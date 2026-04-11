## ADDED Requirements

### Requirement: Boundary attachments act as industrial-standard boundary interfaces
The game SHALL treat mobile-factory boundary attachments as explicit interfaces between world-standard logistics and interior-standard logistics, including validation of the cargo forms and conversion flow expected at the boundary.

#### Scenario: Attachment reports boundary-standard expectations during preview
- **WHEN** the player previews or inspects a boundary attachment in the mobile-factory editor
- **THEN** the game indicates whether the attachment expects inbound world cargo, outbound world cargo, or an attached conversion flow before the opposite-side standard becomes valid

### Requirement: Boundary attachments do not imply universal same-form passthrough
The game SHALL not assume that every item entering or leaving a boundary attachment preserves the same cargo form across the hull boundary.

#### Scenario: Direct pass-through is rejected when destination standard is incompatible
- **WHEN** a boundary attachment would hand an item directly into a destination chain that does not accept the current cargo form
- **THEN** the boundary flow remains blocked or requires a configured conversion structure instead of silently forwarding the incompatible item
