## MODIFIED Requirements

### Requirement: Focused mobile factory interiors use real production layouts
The game SHALL populate the focused mobile factory demo with interior layouts built from real transport, storage, refining, assembly, ammo, port, unpacking, packing, and heavy handoff structures instead of placeholder producer-driven lines, and the authored loop SHALL explicitly demonstrate world bundles being unpacked into multiple cabin carriers before later being repacked into world bundles.

#### Scenario: Focused demo interior uses a world-bundle conversion chain
- **WHEN** the player inspects the mobile factory interior in the focused demo
- **THEN** the authored layout includes at least one inbound heavy cargo attachment, an unpacker, cabin-carrier logistics and processing structures, a packer, and an outbound heavy cargo attachment as part of the demonstrated loop

#### Scenario: Focused demo output returns through a packed world bundle
- **WHEN** the focused demo's authored interior loop is running
- **THEN** the interior contributes to the world-side sandbox role by repacking cabin carriers into a world bundle before export instead of sending cabin carriers directly to the world route

## ADDED Requirements

### Requirement: Focused demo makes the scale break between world bundles and cabin carriers legible
The focused mobile factory demo SHALL make the difference between world-bundle heavy handoff and cabin-carrier belt flow legible through authored visuals, staging positions, and player-observable processing beats.

#### Scenario: Player can observe one world bundle becoming multiple cabin carriers
- **WHEN** the player watches an unpacker chain in the focused mobile demo
- **THEN** the player can observe a visible world bundle entering the heavy handoff path and later see multiple cabin carriers emerge into the interior belt layer from that conversion
