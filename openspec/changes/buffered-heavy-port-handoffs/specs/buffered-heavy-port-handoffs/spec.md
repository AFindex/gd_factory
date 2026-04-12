## ADDED Requirements

### Requirement: Heavy boundary handoffs use dual-side cargo buffering
The system SHALL model each heavy cargo input or output handoff as a staged transfer with one world-side buffer slot, one cabin-side buffer slot, and at most one bridge-transfer slot for full-size world cargo.

#### Scenario: Input handoff buffers one world cargo on each side
- **WHEN** an input handoff has already accepted one full-size world cargo into its outer buffer and a second cargo finishes bridging into its inner buffer
- **THEN** the handoff keeps at most one cargo in the outer buffer, one cargo in the inner buffer, and does not admit an additional bridge transfer until one of those slots is released

#### Scenario: Output handoff stages one packed cargo before world release
- **WHEN** a packer finishes a full-size world cargo while the world-side route is temporarily unable to receive it
- **THEN** the output handoff stores that cargo in its cabin-side or world-side buffer chain and does not discard, shrink, or reroute it onto cabin feed rails

### Requirement: Heavy handoffs transfer full-size world cargo along a continuous path
The system SHALL move full-size world cargo between world routes, heavy handoff buffers, bridge-transfer space, and converter intake/output points as one continuous staged transfer rather than by spawning a new scaled copy at each segment.

#### Scenario: Input cargo keeps world scale through the hull handoff
- **WHEN** a world cargo travels from a world conveyor into the heavy input handoff and across the bridge toward the cabin-side buffer
- **THEN** the presentation keeps the same world-scale cargo identity across those segments and does not replace it with a pre-shrunk cabin cargo representation

#### Scenario: Output cargo reaches the world route without a visual snap
- **WHEN** a packed world cargo leaves the cabin-side output buffer, crosses the bridge, and is released to the world-side route
- **THEN** the cargo follows a continuous release path from the handoff to the world route instead of disappearing at the hull boundary and reappearing on the belt

### Requirement: Heavy handoffs use converter-ready handshakes
Heavy input and output handoffs SHALL exchange full-size world cargo with unpackers and packers only through explicit ready/accept/release handshakes.

#### Scenario: Input handoff waits for an idle unpacker
- **WHEN** a heavy input handoff already holds a full-size world cargo in its inner buffer but the unpacker is still busy processing a previous cargo
- **THEN** the handoff keeps the cargo buffered in the inner slot until the unpacker is ready to accept it

#### Scenario: Output handoff waits for world-side availability
- **WHEN** a heavy output handoff already holds a packed world cargo but the connected world-side route cannot accept it yet
- **THEN** the handoff remains in a buffered outbound state until the world route is ready instead of forcing the cargo into a blocked transit list or deleting it

### Requirement: Converter processing owns the full-size cargo until the transition point
Unpackers and packers SHALL visually own the full-size world cargo during processing and only transition away from that cargo at an explicit completion point.

#### Scenario: Unpacker keeps the full-size cargo until unpack completion
- **WHEN** an unpacker has accepted a full-size world cargo from an input handoff
- **THEN** the unpacker keeps that full-size cargo visible in its processing chamber until the unpack-complete transition, after which cabin feed items may begin to emit

#### Scenario: Packer emits a full-size cargo only after packing completes
- **WHEN** a packer has accumulated the required cabin feed items for its configured bundle template
- **THEN** it presents packing as an in-chamber process and only hands a full-size packed cargo to the output handoff after the pack-complete transition
