# mobile-factory-demo Specification

## Purpose
TBD - updated by change add-complex-mobile-factory-test-scenario. Refine Purpose after archive.
## Requirements
### Requirement: Mobile factory concept ships in dedicated demo scenes
The game SHALL provide a focused dedicated mobile-factory demo scene and a separate large-scale mobile-factory test scenario without replacing the existing static factory demo scene.

#### Scenario: Existing factory demo remains available
- **WHEN** the project content is inspected after the change is implemented
- **THEN** the original static factory demo scene still exists and remains available as a separate experience

#### Scenario: Focused mobile demo can be opened independently
- **WHEN** the player or developer opens the focused mobile-factory demo scene directly
- **THEN** the scene loads the controls, world content, split-view editing UI, and overlays needed to demonstrate core mobile-factory deployment behavior without requiring the static demo to be modified first

#### Scenario: Large-scale mobile factory test scenario can be opened independently
- **WHEN** the player or developer opens the large-scale mobile factory test scenario directly
- **THEN** the project loads the larger world map, multi-factory activity, and observation content for regression-style testing without replacing the focused mobile-factory demo

### Requirement: Mobile demo demonstrates deploy-recall-redeploy gameplay
The game SHALL use the dedicated mobile-factory demo scene to demonstrate the core mobile-factory loop of maneuvering in transit, issuing a placement-style deployment command, connecting to the world through active boundary attachments, exchanging real mined or processed goods with map-side deposits and receiving stations, recalling, and redeploying the same factory instance.

#### Scenario: Confirmed deployment activates visible mining or station exchange
- **WHEN** the player confirms a valid deployment target while the mobile factory is in transit
- **THEN** the factory automatically moves to the selected anchor, aligns to the chosen facing, deploys, shows visible connectors from its active boundary attachments to the world grid, and exchanges real cargo with deposits or receiving stations through those attachments

#### Scenario: Redeployment restores a real exchange loop after recall
- **WHEN** the player recalls the deployed mobile factory, repositions it in transit, and confirms another valid deployment target near a different authored resource or station cluster
- **THEN** the same mobile factory can deploy again and restore its world-side exchange loop without recreating its internal setup

### Requirement: Focused mobile factory demo organizes tools into workspaces
The game SHALL expose the focused mobile factory demo's command controls, editor tools, blueprint workflow, and factory reference information through menu-selected workspaces instead of one default-expanded overlay.

#### Scenario: Mobile demo shows categorized workspace entries
- **WHEN** the player opens the focused mobile factory demo
- **THEN** the HUD presents a workspace menu that includes the core mobile demo categories needed for command, editing, blueprints, and factory information

### Requirement: Mobile factory details can be opened without interrupting play
The game SHALL let the player open a dedicated mobile factory detail workspace that reports lifecycle, attachment, and layout-oriented information without cancelling the current demo control mode or closing the split-view session.

#### Scenario: Opening factory details preserves the current mobile demo state
- **WHEN** the player selects the mobile factory detail workspace while the focused mobile factory demo is active
- **THEN** the HUD shows mobile factory detail content while the current deployment state, control mode, and editor session remain active

### Requirement: Focused mobile demo starts from a player-anchored world view
The game SHALL present the focused mobile-factory demo as a player-anchored experience first, with the mobile factory remaining an important interactive system rather than the only initial control target.

#### Scenario: Mobile demo loads with player HUD and avatar
- **WHEN** the player opens the focused mobile-factory demo scene directly
- **THEN** the scene loads the player avatar, hotbar, and player-facing panels needed to begin interacting from the character perspective while keeping the mobile-factory systems available

#### Scenario: Returning from factory interaction restores player-first flow
- **WHEN** the player finishes inspecting or commanding the mobile factory and returns to the default world interaction state
- **THEN** the demo continues from the player-controlled viewpoint without restarting the scene or discarding the current mobile-factory state

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

