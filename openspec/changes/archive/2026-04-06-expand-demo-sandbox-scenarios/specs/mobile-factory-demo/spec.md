## MODIFIED Requirements

### Requirement: Mobile demo demonstrates deploy-recall-redeploy gameplay
The game SHALL use the dedicated mobile-factory demo scene to demonstrate the core mobile-factory loop of maneuvering in transit, issuing a placement-style deployment command, connecting to the world through active boundary attachments, exchanging real mined or processed goods with map-side deposits and receiving stations, recalling, and redeploying the same factory instance.

#### Scenario: Confirmed deployment activates visible mining or station exchange
- **WHEN** the player confirms a valid deployment target while the mobile factory is in transit
- **THEN** the factory automatically moves to the selected anchor, aligns to the chosen facing, deploys, shows visible connectors from its active boundary attachments to the world grid, and exchanges real cargo with deposits or receiving stations through those attachments

#### Scenario: Redeployment restores a real exchange loop after recall
- **WHEN** the player recalls the deployed mobile factory, repositions it in transit, and confirms another valid deployment target near a different authored resource or station cluster
- **THEN** the same mobile factory can deploy again and restore its world-side exchange loop without recreating its internal setup

## ADDED Requirements

### Requirement: Focused mobile demo centers on dense mineral fields and receiving stations
The game SHALL author the focused mobile-factory demo around a mineral-rich world map with multiple receiving-station or depot-style endpoints so the player can observe how one mobile factory moves goods between field extraction and world-side logistics.

#### Scenario: Focused mobile demo loads with rich resource and station landmarks
- **WHEN** the player opens the focused mobile-factory demo scene directly
- **THEN** the world includes multiple dense mineral clusters and multiple receiving-station-style landmarks that are legible as intended deployment targets or logistics destinations

#### Scenario: Player can observe field-to-station transfer through ports
- **WHEN** the player deploys the mobile factory into an authored exchange lane between a resource field and a receiving station
- **THEN** the player can observe goods entering or leaving the factory through the active ports as part of that lane's normal operation

### Requirement: Focused mobile factory interiors use real production layouts
The game SHALL populate the focused mobile factory demo with interior layouts built from real transport, storage, refining, assembly, ammo, and port structures instead of placeholder producer-driven lines.

#### Scenario: Interior layout uses recipe-capable machines
- **WHEN** the player inspects the mobile factory interior in the focused demo
- **THEN** the authored layout includes real recipe-capable machines and logistics structures that transform or route goods instead of relying on a producer structure as the main source

#### Scenario: Interior output contributes to the world-side sandbox role
- **WHEN** the focused demo's authored interior loop is running
- **THEN** the interior's output contributes to a world-side receiving, support, or defense role through the active boundary attachments
