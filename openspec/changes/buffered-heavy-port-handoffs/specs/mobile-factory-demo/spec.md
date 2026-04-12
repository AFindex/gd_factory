## ADDED Requirements

### Requirement: Focused mobile demo shows the full buffered heavy-cargo handoff chain
The focused mobile-factory demo SHALL demonstrate a complete world-to-cabin-to-world heavy-cargo chain in which world cargo enters through a staged input handoff, waits for unpacking, becomes cabin-feed logistics, is repacked, and leaves through a staged output handoff.

#### Scenario: Player can observe inbound heavy cargo waiting for unpacking
- **WHEN** the focused mobile demo is running and the input side has already accepted a full-size world cargo before the unpacker is ready
- **THEN** the player can observe that cargo buffered on the handoff instead of seeing it disappear or instantly convert into cabin-feed flow

#### Scenario: Player can observe outbound heavy cargo waiting for world pickup
- **WHEN** the focused mobile demo is running and the output side has a packed world cargo ready before the world route can accept it
- **THEN** the player can observe that cargo buffered on the outbound handoff before it is released to the world route

### Requirement: Focused mobile demo preserves heavy-cargo continuity across the hull
The focused mobile-factory demo SHALL present heavy-cargo transfer across the hull as a continuous staged movement rather than a discontinuous spawn/despawn shortcut.

#### Scenario: Inbound cargo moves continuously from world route into the factory
- **WHEN** a full-size world cargo is imported into the factory in the focused mobile demo
- **THEN** the observed path reads as a continuous movement from the world route into the handoff and onward to the unpacker staging chain

#### Scenario: Outbound cargo moves continuously from packer to world route
- **WHEN** a packed world cargo is exported from the factory in the focused mobile demo
- **THEN** the observed path reads as a continuous movement from the packer to the output handoff and onward to the world route
