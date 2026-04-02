# mobile-factory-lifecycle Specification

## Purpose
TBD - updated by change add-complex-mobile-factory-test-scenario. Refine Purpose after archive.
## Requirements
### Requirement: Mobile factories keep a persistent identity across relocation
The game SHALL model each mobile factory as a persistent gameplay entity whose internal layout, buffered state, and identity survive deploy, recall, and redeploy transitions.

#### Scenario: Recall and redeploy preserve the same factory
- **WHEN** a mobile factory is recalled from one valid deployment site and later redeployed to another valid deployment site
- **THEN** the same mobile factory instance is reused and its internal structures, inventory, and configuration remain intact instead of being rebuilt from scratch

### Requirement: Mobile factories deploy only onto a valid footprint
The game SHALL allow each mobile factory to enter the deployed state only when every cell in that factory's size-specific footprint and every required world-facing attachment projection cell for the chosen deployment facing is inside bounds and unreserved.

#### Scenario: Valid deployment reserves footprint and active attachment projections
- **WHEN** the player or scripted scenario logic confirms a deployment target whose full factory-specific footprint and every active boundary attachment projection cell are valid for the chosen facing
- **THEN** the game completes deployment, reserves the entire size-specific footprint, reserves the correctly oriented world-facing attachment cells, activates those attachments, and marks that factory as deployed

#### Scenario: Invalid deployment is blocked for attachment projection conflicts
- **WHEN** any required footprint cell or active attachment projection cell for a factory's chosen facing would overlap an occupied, reserved, or out-of-bounds location
- **THEN** that deployment does not occur and the game reports that the target location is invalid for that factory's current size, facing, and installed attachments

### Requirement: Deployed mobile factories must be recalled before moving
The game SHALL prevent direct movement, turning, or redeploy commands from relocating a mobile factory while it still owns an active deployed footprint in the world.

#### Scenario: Move command is rejected while deployed
- **WHEN** the player issues a move, turn, or redeploy command to a currently deployed mobile factory without recalling it first
- **THEN** the factory remains deployed in place and the game requires the active deployment to be released before relocation

### Requirement: Deployment ports define the only world connection boundary
The game SHALL allow items to cross between a mobile factory interior and the world grid only through the mobile factory's active deployed boundary attachments.

#### Scenario: Inactive output attachment blocks instead of consuming items
- **WHEN** a mobile factory is in transport, recalled, or otherwise lacks an active world binding for an output attachment
- **THEN** that attachment does not delete or recycle outgoing items and instead blocks further outward transfer until a valid deployment reconnects it

#### Scenario: Inactive input attachment cannot import from the world
- **WHEN** a mobile factory is in transport, recalled, or otherwise lacks an active world binding for an input attachment
- **THEN** items cannot enter the factory interior from the world through that attachment until the factory is deployed again

### Requirement: Interior changes persist across lifecycle transitions
The game SHALL preserve a mobile factory's interior layout, installed boundary attachments, and presentation across deploy, recall, and redeploy transitions.

#### Scenario: Attachment layout survives deployment state changes
- **WHEN** the player installs or reconfigures boundary attachments in a mobile factory and later deploys, recalls, or redeploys that same factory
- **THEN** the factory keeps the same attachment arrangement and presents it consistently in both the editor and the world representation

### Requirement: In-transit mobile factories can be maneuvered before deployment
The game SHALL allow a mobile factory that is not currently deployed to move and turn in the world as a persistent unit before the player commits to a deployment target.

#### Scenario: Player maneuvers the factory while in transit
- **WHEN** the mobile factory is in transit and the player issues world movement input in factory command mode
- **THEN** the factory updates its world position and heading without entering the deployed state

### Requirement: Auto-deploy finishes only after movement and facing alignment
The game SHALL treat deployment confirmation as a command to move into place and align to the chosen facing before the mobile factory actually enters the deployed state.

#### Scenario: Factory reaches the target before deployment finalizes
- **WHEN** the player confirms a valid deployment target
- **THEN** the mobile factory remains non-deployed while it travels toward that target and only becomes deployed after it reaches the target anchor and aligns to the selected facing

### Requirement: Multiple mobile factories can hold independent lifecycle states
The game SHALL allow multiple mobile factories to exist at the same time with independent lifecycle state, transform, and reservation ownership.

#### Scenario: One factory changes state without disturbing another
- **WHEN** one mobile factory deploys, recalls, or auto-deploys while another mobile factory remains deployed or in transit elsewhere in the world
- **THEN** each factory keeps its own state, pose, reservations, and logistics connections without being reset or re-authored by the other factory's lifecycle transition

