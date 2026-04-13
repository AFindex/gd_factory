## ADDED Requirements

### Requirement: Heavy boundary attachments expose one ordered cargo-handoff path
Active heavy input and output boundary attachments SHALL expose one ordered handoff path for world approach, outer staging, bridge alignment, bridge crossing, inner staging, and converter handoff so a single cargo presenter can traverse the entire boundary exchange.

#### Scenario: Inbound handoff path covers belt-to-unpacker transfer
- **WHEN** a full-size cargo travels from a connected world route into an active heavy input attachment
- **THEN** the attachment provides a continuous staged path from the route handoff point through outer staging, bridge stages, and inner staging up to the unpacker handoff point

#### Scenario: Outbound handoff path covers packer-to-belt transfer
- **WHEN** a packed full-size cargo leaves a packer and enters an active heavy output attachment
- **THEN** the attachment provides a continuous staged path from the packer handoff point through inner staging, bridge stages, outer staging, and world release point

### Requirement: Heavy boundary attachments separate static structure roots from cargo-body ownership
Heavy boundary attachments SHALL keep their world-side and cabin-side structure roots limited to static connector geometry, anchors, and state feedback instead of independently deciding to spawn or retain a full-size cargo body.

#### Scenario: World-side attachment root does not duplicate an inner-stage cargo
- **WHEN** the active full-size cargo has already advanced to the bridge, inner staging, or converter handoff stage
- **THEN** the world-side attachment root keeps only its static connector presentation and does not continue rendering a stale copy of that cargo

#### Scenario: Cabin-side attachment root does not pre-spawn cargo before acceptance
- **WHEN** the connected world route has not yet transferred logical ownership of a full-size cargo to the heavy input handoff
- **THEN** the cabin-side attachment root does not show that cargo early and waits until the shared handoff presenter owns it
