## ADDED Requirements

### Requirement: Observer mode gates camera panning in the mobile-factory demo
The game SHALL reserve WASD camera panning in the mobile-factory demo for an explicit observer mode so factory command mode can reuse the same keys for unit control.

#### Scenario: Factory command mode keeps the camera fixed
- **WHEN** the player is in the mobile-factory demo's factory command mode and presses WASD
- **THEN** the camera does not pan in response to those keys and the input remains available to the mobile factory controls

#### Scenario: Observer mode restores world camera panning
- **WHEN** the player switches the mobile-factory demo into observer mode and presses WASD
- **THEN** the world camera pans using the existing fixed-angle camera behavior until the player exits observer mode
