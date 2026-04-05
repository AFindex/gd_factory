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
The game SHALL use the dedicated mobile-factory demo scene to demonstrate the core mobile-factory loop of maneuvering in transit, issuing a placement-style deployment command, connecting to the world through active boundary attachments, recalling, and redeploying the same factory instance.

#### Scenario: Confirmed deployment activates visible inbound and outbound attachments
- **WHEN** the player confirms a valid deployment target while the mobile factory is in transit
- **THEN** the factory automatically moves to the selected anchor, aligns to the chosen facing, deploys, shows visible connectors from its active boundary attachments to the world grid, and exchanges items with observable world-side logistics through those attachments

#### Scenario: Recalled factory no longer silently disposes outbound items
- **WHEN** the player recalls a deployed mobile factory after its internal output line has been feeding an external route
- **THEN** the world-side attachment connection is removed and the recalled factory's inactive output boundary no longer silently consumes items that attempt to leave the interior

#### Scenario: Redeployment restores the world interaction loop after recall
- **WHEN** the player recalls the deployed mobile factory, repositions it in transit, and confirms another valid deployment target
- **THEN** the same mobile factory can deploy again and restore its world-side inbound and outbound interaction loops without recreating its internal setup

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
