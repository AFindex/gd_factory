## ADDED Requirements

### Requirement: Output boundary attachments expose a readable mirrored outbound handoff
Active mobile-factory output boundary attachments SHALL expose a readable outbound heavy-cargo handoff that mirrors the staged import contract already used on the input side.

#### Scenario: Output attachment shows inner, bridge, and outer staging in order
- **WHEN** an active output attachment exports a packed world cargo from the factory
- **THEN** the attachment presents the cargo through cabin-side staging, bridge-out movement, and world-side staging in that order instead of collapsing the exchange into a single generic transit step

#### Scenario: Output attachment reports world-pickup wait as its own visible state
- **WHEN** the output attachment is holding a packed cargo at the world-side stage while waiting for the connected world route to accept it
- **THEN** the attachment reports and presents that condition as a distinct outbound waiting state rather than hiding the cargo or treating it as a disconnected error
