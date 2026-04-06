## MODIFIED Requirements

### Requirement: Mobile factory demo exposes explicit control modes
The mobile-factory demo SHALL expose explicit control modes so the player can tell whether movement input currently controls the player character, the mobile factory, deployment preview, or the world camera.

#### Scenario: Demo starts in player control mode
- **WHEN** the player opens the dedicated mobile-factory demo scene
- **THEN** the HUD shows that player control mode is active and movement input is routed to the player character instead of the mobile factory or the world camera

#### Scenario: Player toggles factory or observer context
- **WHEN** the player activates the factory-command button, deploy workflow, or observer-mode button
- **THEN** the HUD indicates the newly active control mode and provides a clear way to return to player control mode

### Requirement: Command mode routes WASD to the mobile factory while in transit
The mobile-factory demo SHALL route WASD input to the mobile factory's world-side movement controls whenever the player has explicitly entered factory command mode and the factory is not currently deployed.

#### Scenario: In-transit factory responds to command movement
- **WHEN** the player presses movement keys while the mobile factory is in transit and factory command mode is active
- **THEN** the mobile factory moves and/or turns in the world instead of moving the player character or panning the camera

## ADDED Requirements

### Requirement: Player mode routes WASD to the player character by default
The mobile-factory demo SHALL treat player-character movement as the default owner of WASD until the player explicitly enters another mobile-factory control context.

#### Scenario: Default movement input moves the player
- **WHEN** the focused mobile-factory demo is in player control mode and the player presses WASD
- **THEN** the player character moves through the world and the mobile factory remains unchanged unless separately commanded

#### Scenario: Leaving factory context restores player control
- **WHEN** the player exits factory command mode, deploy preview, or observer mode
- **THEN** subsequent WASD input returns to moving the player character
