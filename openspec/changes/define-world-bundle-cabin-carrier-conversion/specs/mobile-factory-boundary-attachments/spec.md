## MODIFIED Requirements

### Requirement: Boundary attachments support directional item exchange
The game SHALL support both outbound and inbound item attachments using the same boundary-attachment system, but world bundles crossing the mobile-factory boundary SHALL exchange only through heavy handoff semantics instead of being treated as direct cabin-belt payloads.

#### Scenario: Input attachment hands a world bundle into the heavy conversion path
- **WHEN** a deployed mobile factory has an active input attachment connected to a valid world-side route carrying a world bundle inward
- **THEN** that bundle enters the factory through the attachment's heavy handoff path and remains destined for heavy nodes such as unpackers or heavy buffers instead of being injected directly into an ordinary cabin belt

#### Scenario: Output attachment exports only packed world bundles
- **WHEN** a deployed mobile factory has an active output attachment connected to a valid world-side route
- **THEN** the attachment exports only dispatch-ready packed world bundles from heavy handoff nodes and does not treat ordinary cabin carriers as world-exportable payloads

## ADDED Requirements

### Requirement: Boundary attachments present heavy handoff cradles and bundle state
The game SHALL render mobile-factory cargo boundary attachments as heavy handoff cradles that can visibly stage a world bundle at the hull boundary and communicate whether the port is waiting, blocked, or transferring a heavy payload.

#### Scenario: Connected cargo port stages a visible heavy bundle at the hull boundary
- **WHEN** a cargo boundary attachment is active and currently holding or transferring a world bundle
- **THEN** the port presentation shows that heavy bundle in its boundary handoff cradle instead of implying that the world payload has already become a cabin-belt item
