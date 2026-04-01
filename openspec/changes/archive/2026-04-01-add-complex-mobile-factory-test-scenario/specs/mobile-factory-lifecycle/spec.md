## MODIFIED Requirements

### Requirement: Mobile factories deploy only onto a valid footprint
The game SHALL allow each mobile factory to enter the deployed state only when every cell in that factory's size-specific footprint and every required world-facing port cell for the chosen deployment facing is inside bounds and unreserved.

#### Scenario: Valid deployment reserves size-specific footprint and facing-specific ports
- **WHEN** the player or scripted scenario logic confirms a deployment target whose full factory-specific footprint and world-facing port cells are valid for the chosen facing
- **THEN** the game completes deployment, reserves the entire size-specific footprint, activates the correctly oriented world-facing ports, and marks that factory as deployed

#### Scenario: Invalid deployment is blocked for size-specific conflicts
- **WHEN** any required footprint or port cell for a factory's chosen facing would overlap an occupied, reserved, or out-of-bounds location
- **THEN** that deployment does not occur and the game reports that the target location is invalid for that factory's current size and facing

## ADDED Requirements

### Requirement: Multiple mobile factories can hold independent lifecycle states
The game SHALL allow multiple mobile factories to exist at the same time with independent lifecycle state, transform, and reservation ownership.

#### Scenario: One factory changes state without disturbing another
- **WHEN** one mobile factory deploys, recalls, or auto-deploys while another mobile factory remains deployed or in transit elsewhere in the world
- **THEN** each factory keeps its own state, pose, reservations, and logistics connections without being reset or re-authored by the other factory's lifecycle transition
