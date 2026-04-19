## ADDED Requirements

### Requirement: Boundary attachments resolve heavy converter handoffs from shared contract anchors
Mobile-factory boundary attachments SHALL resolve heavy-cargo handoffs against shared contract-facing anchors exposed by compatible interior converters instead of identifying those converters only by concrete structure class.

#### Scenario: Output attachment finds a compatible converter by contract anchor
- **WHEN** an output-side boundary attachment evaluates an adjacent interior converter that exposes a contract anchor capable of accepting its heavy outbound handoff
- **THEN** the attachment treats that converter as a valid handoff target without requiring a hard-coded structure-type check

#### Scenario: Attachment behavior follows converter contract changes
- **WHEN** a compatible converter's effective heavy handoff anchor changes because of a rotated or otherwise re-resolved structure contract
- **THEN** the boundary attachment follows the updated contract anchor when determining whether handoff is possible
