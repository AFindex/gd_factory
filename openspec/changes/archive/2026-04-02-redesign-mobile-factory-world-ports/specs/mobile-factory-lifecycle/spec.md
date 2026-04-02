## MODIFIED Requirements

### Requirement: Mobile factories deploy only onto a valid footprint
The game SHALL allow each mobile factory to enter the deployed state only when every cell in that factory's size-specific footprint and every required world-facing attachment projection cell for the chosen deployment facing is inside bounds and unreserved.

#### Scenario: Valid deployment reserves footprint and active attachment projections
- **WHEN** the player or scripted scenario logic confirms a deployment target whose full factory-specific footprint and every active boundary attachment projection cell are valid for the chosen facing
- **THEN** the game completes deployment, reserves the entire size-specific footprint, reserves the correctly oriented world-facing attachment cells, activates those attachments, and marks that factory as deployed

#### Scenario: Invalid deployment is blocked for attachment projection conflicts
- **WHEN** any required footprint cell or active attachment projection cell for a factory's chosen facing would overlap an occupied, reserved, or out-of-bounds location
- **THEN** that deployment does not occur and the game reports that the target location is invalid for that factory's current size, facing, and installed attachments

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
