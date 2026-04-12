## ADDED Requirements

### Requirement: Boundary attachments expose staged heavy-cargo transfer states
Mobile-factory input and output boundary attachments SHALL expose staged heavy-cargo transfer states that distinguish outer buffering, bridge transfer, inner buffering, and blocked release/acceptance.

#### Scenario: Input attachment reports that cargo is waiting for unpacking
- **WHEN** an input attachment has already accepted a full-size world cargo into its cabin-side buffer and the unpacker is not yet ready
- **THEN** the attachment reports a state equivalent to "waiting for unpacker" instead of only a generic connected or blocked label

#### Scenario: Output attachment reports that cargo is waiting for world pickup
- **WHEN** an output attachment has moved a full-size packed cargo into its world-side buffer but the receiving world route is not ready
- **THEN** the attachment reports a state equivalent to "waiting for world pickup" instead of collapsing that case into a generic disconnected state

### Requirement: Boundary attachments preserve full-size world cargo through active handoff stages
Active input and output boundary attachments SHALL preserve full-size world cargo presentation at every heavy handoff stage instead of rescaling that cargo into cabin-feed dimensions while it remains in the handoff chain.

#### Scenario: Input attachment keeps a world cargo full-size in both buffers
- **WHEN** a player observes an active input attachment whose outer buffer and inner buffer are each holding a world cargo at different times
- **THEN** the cargo shown in both buffer locations remains at world scale and is not represented as a cabin-feed rail item

#### Scenario: Output attachment keeps the packed cargo full-size before release
- **WHEN** a packed cargo is buffered on the output attachment before it is released to the world route
- **THEN** the attachment presents that cargo as the same full-size outbound world cargo that will leave the factory
