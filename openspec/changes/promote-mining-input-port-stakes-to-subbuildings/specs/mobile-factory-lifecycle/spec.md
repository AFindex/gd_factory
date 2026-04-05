## MODIFIED Requirements

### Requirement: Mobile factories deploy only onto a valid footprint
The game SHALL allow each mobile factory to enter the deployed state only when every cell in that factory's size-specific footprint and every required world-facing attachment projection cell for the chosen deployment facing is inside bounds and unreserved, while mining-input stake creation is resolved after that validation from deposit coverage and the port's built-stake stock.

#### Scenario: Valid deployment reserves footprint and active attachment projections
- **WHEN** the player or scripted scenario logic confirms a deployment target whose full factory-specific footprint and every required boundary attachment projection cell are valid for the chosen facing
- **THEN** the game completes deployment, reserves the entire size-specific footprint, reserves the correctly oriented world-facing attachment cells, activates ordinary connected attachments, deploys the subset of mining stakes that are both eligible and available, and marks that factory as deployed

#### Scenario: Invalid deployment is blocked for attachment projection conflicts
- **WHEN** any required footprint cell or required attachment projection cell for a factory's chosen facing would overlap an occupied, reserved, or out-of-bounds location
- **THEN** that deployment does not occur and the game reports that the target location is invalid for that factory's current size, facing, and installed attachments

#### Scenario: Mixed mining projection still allows deployment
- **WHEN** a mobile factory deployment target keeps the footprint and projected mining-input cells reservable but only some projected mining cells overlap compatible deposits
- **THEN** the factory still deploys successfully and only the compatible deposit cells receive mining stakes while the empty projected cells remain stake-free

#### Scenario: Stake shortage leaves the mining attachment partially deployed
- **WHEN** a mining input port has fewer built stakes than the number of compatible deposit cells in its current deployment projection
- **THEN** the mobile factory still deploys, the mining input attachment deploys only the available stakes, and the game reports the resulting partial mining deployment count
