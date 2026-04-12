## MODIFIED Requirements

### Requirement: Mobile demo demonstrates deploy-recall-redeploy gameplay
The game SHALL use the dedicated mobile-factory demo scene to demonstrate the core mobile-factory loop of maneuvering in transit, issuing a placement-style deployment command, connecting to the world through active boundary attachments, exchanging real mined or processed goods with map-side deposits and receiving stations, recalling, and redeploying the same factory instance. That loop MUST visibly include the world-to-cabin conversion boundary so players can observe large world payloads entering or leaving through conversion structures instead of appearing directly on cabin rails.

#### Scenario: Deployment shows large-payload handoff into the interior
- **WHEN** the player confirms a valid deployment target while the mobile factory is in transit
- **THEN** the factory moves to the selected anchor, deploys, shows visible connectors from its active boundary attachments to world logistics cells, and presents the first cargo handoff as a world-standard payload entering a conversion or staging structure before cabin carriers appear inside

#### Scenario: Redeployment restores the same conversion loop
- **WHEN** the player recalls the deployed mobile factory, repositions it in transit, and confirms another valid deployment target near a different authored resource or station cluster
- **THEN** the same mobile factory deploys again and restores a readable loop of world payload exchange, interior carrier flow, and world payload export without recreating its internal setup

### Requirement: Focused mobile demo centers on dense mineral fields and receiving stations
The game SHALL author the focused mobile-factory demo around a mineral-rich world map with multiple receiving-station or depot-style endpoints so the player can observe how one mobile factory moves goods between field extraction and world-side logistics, including the visible transition from world payloads to cabin carriers and back.

#### Scenario: Demo landmarks support readable conversion loops
- **WHEN** the player opens the focused mobile-factory demo scene directly
- **THEN** the world includes dense mineral clusters, receiving stations, and cargo lanes positioned so the player can clearly observe at least one inbound unpacking chain and one outbound packing chain

#### Scenario: Player can watch the full scale-change route
- **WHEN** the player deploys the mobile factory into an authored exchange lane between a resource field and a receiving station
- **THEN** the player can observe large cargo arriving from the world, compact carriers circulating inside, and repacked large cargo leaving for the world-side destination

### Requirement: Focused mobile factory interiors use real production layouts
The game SHALL populate the focused mobile factory demo with interior layouts built from real transport, storage, refining, assembly, ammo, and port structures instead of placeholder producer-driven lines, and those layouts MUST be staged around real unpacking, buffering, processing, and packing structures that honor the scale difference between world payloads and cabin carriers.

#### Scenario: Interior layout starts with unpacking before internal flow
- **WHEN** the player inspects the mobile factory interior in the focused demo
- **THEN** the authored layout shows world cargo first entering a conversion/staging area and only then feeding compact carriers into interior processing structures

#### Scenario: Interior output repacks before world export
- **WHEN** the focused demo's authored interior loop is running
- **THEN** the interior output reaches a packing/conversion exit that emits world-standard cargo to the world-side receiving, support, or defense role instead of exposing cabin carriers directly outside
