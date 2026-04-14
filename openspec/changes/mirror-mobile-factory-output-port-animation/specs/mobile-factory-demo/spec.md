## ADDED Requirements

### Requirement: Focused mobile demo shows the staged outbound mirror animation
The focused mobile-factory demo SHALL let a player observe the output-port outbound chain with the same staged readability as the rebuilt input-port import chain.

#### Scenario: Player can follow one packed cargo from packer to world route
- **WHEN** the focused mobile demo runs an active outbound heavy-cargo exchange
- **THEN** the player can follow one packed cargo from the `CargoPacker` through the output port's inner buffer, bridge-out path, outer buffer, and visible world release edge without duplicate flashes

#### Scenario: Demo exposes the waiting-for-world-pickup stage
- **WHEN** the focused mobile demo reaches a moment where the output port has a packed cargo ready but the world route is not yet able to receive it
- **THEN** the player can observe that cargo waiting on the output side instead of seeing it disappear or release immediately
