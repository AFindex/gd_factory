## ADDED Requirements

### Requirement: World bundles are defined by bundle templates and manifests
The system SHALL define every world-side cargo bundle through a bundle template that declares its size tier, world presentation metadata, unpack manifest, and allowed repack template semantics instead of treating a world bundle as only a scaled copy of one cabin item kind.

#### Scenario: Fixed bundle template resolves a deterministic unpack manifest
- **WHEN** the game resolves a fixed world bundle template such as an ore crate or standard ammo crate
- **THEN** the template yields a deterministic manifest that lists the exact cabin-carrier kinds and quantities produced by unpacking that world bundle

#### Scenario: Controlled mixed bundle template declares approved mixed contents
- **WHEN** the game resolves a controlled mixed bundle template such as a forward-supply crate
- **THEN** the template explicitly lists the approved mixture of cabin-carrier kinds and quantities instead of allowing arbitrary mixed contents

### Requirement: World bundles only traverse heavy handoff nodes inside mobile factories
The system SHALL prevent world bundles from entering ordinary cabin belts, splitters, mergers, bridges, or recipe machines inside a mobile factory and SHALL restrict their interior movement to heavy handoff nodes such as boundary ports, unpackers, heavy buffers, packers, and outbound handoff points.

#### Scenario: Ordinary cabin belt rejects a world bundle
- **WHEN** a world bundle is offered directly to an ordinary interior belt or other cabin-carrier logistics structure
- **THEN** that structure rejects the bundle and the bundle remains in a heavy handoff node or blocked state instead of entering the cabin belt layer

#### Scenario: Boundary handoff can pass a world bundle to an unpacker
- **WHEN** an inbound boundary attachment is connected to a valid unpacker-side heavy handoff node
- **THEN** the world bundle can move through that heavy handoff path without being treated as a cabin-carrier belt payload

### Requirement: Unpackers convert one world bundle into multiple cabin carriers over time
An unpacker SHALL accept at most one world bundle at a time, SHALL process it as a manifest-driven conversion job, and SHALL emit the listed cabin carriers over one or more output dispatch steps until the bundle's manifest is exhausted.

#### Scenario: Ore bundle emits multiple ore carriers
- **WHEN** an unpacker finishes processing a world ore bundle whose manifest contains multiple ore carriers
- **THEN** the unpacker dispatches those cabin carriers over time to the connected cabin belt layer instead of creating only a single carrier output

#### Scenario: Mixed manifest preserves configured output sequence
- **WHEN** an unpacker processes a controlled mixed world bundle
- **THEN** the unpacker emits only the carrier kinds and quantities declared by that template's manifest and does not invent additional cabin outputs

### Requirement: Packers accumulate cabin carriers against a target bundle template
A packer SHALL accumulate inbound cabin carriers against a selected or configured bundle template and SHALL emit exactly one world bundle only after the required manifest for that template has been satisfied.

#### Scenario: Incomplete manifest does not produce a world bundle
- **WHEN** a packer has received only part of the cabin carriers required by its active bundle template
- **THEN** the packer remains in an in-progress packing state and does not output a partial or placeholder world bundle

#### Scenario: Completed manifest produces one world bundle
- **WHEN** a packer has accumulated all cabin carriers required by its active bundle template
- **THEN** the packer emits one world bundle tagged with that template and clears the consumed packing progress

### Requirement: Packing templates restrict mixed-content packing
The system SHALL limit mixed-content packing to explicitly authored bundle templates and SHALL reject inbound cabin carriers that do not satisfy the current packer's allowed template semantics.

#### Scenario: Exact bundle template rejects wrong carrier kind
- **WHEN** a packer is configured for an exact single-resource bundle template and receives a carrier kind not listed by that template
- **THEN** the packer rejects or refuses to consume that carrier for the current bundle progress

#### Scenario: Controlled mixed template accepts only its approved mixture
- **WHEN** a packer is configured for a controlled mixed bundle template
- **THEN** it accepts only the approved carrier kinds and quantities for that template and rejects extra or unrelated cabin carriers
