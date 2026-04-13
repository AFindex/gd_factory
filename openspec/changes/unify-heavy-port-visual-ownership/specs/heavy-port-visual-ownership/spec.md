## ADDED Requirements

### Requirement: Heavy cargo uses a single active presentation owner
The system SHALL ensure that each full-size world cargo participating in a mobile-factory heavy handoff chain is rendered by exactly one active presentation owner at any moment.

#### Scenario: Inbound cargo never appears in multiple handoff stages at once
- **WHEN** a full-size cargo moves from a world route into the input heavy port, across the bridge path, and toward the unpacker handoff
- **THEN** the scene shows one continuous cargo body and does not render simultaneous copies on the world-side attachment root, bridge stage, cabin-side attachment root, or unpacker chamber

#### Scenario: Outbound cargo transfers ownership without duplicate flashes
- **WHEN** a packed full-size cargo leaves the packer chamber and progresses through the heavy output chain toward the world route
- **THEN** visual ownership changes between hosts without spawning a duplicate flash or leaving the same cargo visible on both the sender and receiver in the same transfer stage

### Requirement: Heavy cargo ownership transitions align with logical custody
The system SHALL synchronize presentation-owner changes with the logical accept and release edges for world routes, heavy ports, and converters.

#### Scenario: World-route consumption matches input-port acceptance
- **WHEN** an input heavy port accepts a full-size cargo from the connected world route into its inbound handoff chain
- **THEN** the route consumes that cargo at the same transition that assigns presentation ownership to the handoff chain instead of after a delayed duplicate animation

#### Scenario: World-route release matches outbound handoff completion
- **WHEN** an output heavy port releases a packed full-size cargo to the connected world route
- **THEN** the handoff chain relinquishes presentation ownership only when the route actually accepts the cargo instead of before or after a visually disconnected delay

### Requirement: Static heavy-port geometry is independent from cargo-body ownership
The system SHALL keep heavy-port connector, bridge, and shell geometry visible independently from the active cargo body so the scene does not require multiple cargo instances to preserve handoff readability.

#### Scenario: Empty heavy port still shows connector geometry
- **WHEN** a heavy input or output attachment is deployed without currently owning a cargo body
- **THEN** the world-side and cabin-side connector geometry remains visible as static structure presentation without spawning a placeholder cargo body

#### Scenario: Converter-owned cargo does not force a duplicate attachment payload
- **WHEN** a converter chamber owns the active full-size cargo during unpacking or packing
- **THEN** the heavy-port attachment keeps its connector and staged-path geometry visible but does not render a second cargo body on the attachment
