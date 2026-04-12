## ADDED Requirements

### Requirement: Heavy interface structures show staged cargo ownership without rescaling
The structure-visual-profile pipeline SHALL support heavy interface and converter structures showing full-size world cargo at staged ownership anchors such as outer buffers, inner buffers, bridge-transfer anchors, and processing chambers without shrinking that cargo into cabin-feed scale.

#### Scenario: Input interface visual switches ownership between staged anchors
- **WHEN** a full-size world cargo moves from an input interface outer buffer to its bridge anchor and then to its inner buffer
- **THEN** the presentation pipeline moves that cargo through staged heavy-ownership anchors while preserving world-scale visuals and a consistent cargo identity

#### Scenario: Converter chamber keeps the processing cargo visible
- **WHEN** an unpacker or packer is actively processing a full-size world cargo
- **THEN** the structure visual profile keeps that cargo visible in the chamber until the configured completion transition rather than hiding it for the entire processing duration

### Requirement: Heavy converter visuals show completion transitions before cargo-form change
The structure-visual-profile pipeline SHALL support unpackers and packers presenting a completion transition, such as dissolve, shell split, or release animation, before the visible cargo form changes from world cargo to cabin feed or vice versa.

#### Scenario: Unpacker transitions after processing completes
- **WHEN** an unpacker reaches the end of its processing cycle for a full-size world cargo
- **THEN** the presentation shows a completion transition on that full-size cargo before cabin-feed items begin to emerge

#### Scenario: Packer transitions before handing cargo to the output interface
- **WHEN** a packer finishes assembling a packed world cargo
- **THEN** the presentation shows the completed outbound cargo in the chamber before transferring it to the output interface staging anchor
