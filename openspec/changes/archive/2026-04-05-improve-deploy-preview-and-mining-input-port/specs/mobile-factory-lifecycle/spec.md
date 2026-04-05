## MODIFIED Requirements

### Requirement: Mobile factories deploy only onto a valid footprint
The game SHALL allow each mobile factory to enter the deployed state only when every cell in that factory's size-specific footprint and every hard-required world-facing attachment projection cell for the chosen deployment facing is inside bounds and unreserved, while mining-input deposit coverage may remain optional.

#### Scenario: Valid deployment reserves footprint and active attachment projections
- **WHEN** the player or scripted scenario logic confirms a deployment target whose full factory-specific footprint and every hard-required boundary attachment projection cell are valid for the chosen facing
- **THEN** the game completes deployment, reserves the entire size-specific footprint, reserves the correctly oriented world-facing attachment cells, activates every fully connected attachment, and marks that factory as deployed

#### Scenario: Invalid deployment is blocked for attachment projection conflicts
- **WHEN** any required footprint cell or hard-required attachment projection cell for a factory's chosen facing would overlap an occupied, reserved, or out-of-bounds location
- **THEN** that deployment does not occur and the game reports that the target location is invalid for that factory's current size, facing, and installed attachments

#### Scenario: Mining-input deployment can proceed without deposit coverage
- **WHEN** a mobile factory deployment target keeps the footprint and projected mining-input cells reservable but none of those projected cells overlap deposits
- **THEN** the factory still deploys, the projected mining cells remain reserved for that deployment, and the mining input attachment stays disconnected until a later deployment overlaps deposits
