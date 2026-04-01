## ADDED Requirements

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

## MODIFIED Requirements

### Requirement: Mobile factories deploy only onto a valid footprint
The game SHALL allow a mobile factory to enter the deployed state only when every cell in its footprint and every required world-facing port cell for the chosen deployment facing is inside bounds and unreserved.

#### Scenario: Valid deployment reserves footprint and facing-specific ports
- **WHEN** the player confirms a deployment target whose footprint and world-facing port cells are valid for the chosen facing
- **THEN** the game completes deployment, reserves the full footprint, activates the correctly oriented world-facing ports, and marks the factory as deployed

#### Scenario: Invalid deployment is blocked
- **WHEN** any required footprint or port cell for the chosen deployment facing would overlap an occupied, reserved, or out-of-bounds location
- **THEN** the deployment does not occur and the game reports that the target location is invalid

### Requirement: Deployed mobile factories must be recalled before moving
The game SHALL prevent direct movement, turning, or redeploy commands from relocating a mobile factory while it still owns an active deployed footprint in the world.

#### Scenario: Move command is rejected while deployed
- **WHEN** the player issues a move, turn, or redeploy command to a currently deployed mobile factory without recalling it first
- **THEN** the factory remains deployed in place and the game requires the active deployment to be released before relocation
