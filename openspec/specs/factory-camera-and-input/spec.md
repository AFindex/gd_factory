# factory-camera-and-input Specification

## Purpose
TBD - created by archiving change add-3d-factory-core-demo. Update Purpose after archive.
## Requirements
### Requirement: Fixed-angle camera navigation
The game SHALL provide a 3D factory camera that keeps a fixed downward pitch and prevents unrestricted free-look while allowing the player to navigate the playable area.

#### Scenario: Demo scene opens with a readable build view
- **WHEN** the player launches the project into the factory demo
- **THEN** the camera is positioned above the build area at a fixed gameplay angle and the factory floor is visible without manual setup

#### Scenario: Camera movement stays within usable bounds
- **WHEN** the player pans or zooms the camera
- **THEN** the camera target remains inside configured map limits and zoom remains inside configured minimum and maximum distances

### Requirement: Camera rotation preserves grid readability
The camera SHALL preserve grid readability by keeping its angle constrained and only allowing orientation changes that maintain an aligned factory-building perspective.

#### Scenario: Rotation does not enter free orbit
- **WHEN** the player triggers camera rotation
- **THEN** the camera changes to an allowed facing without changing to arbitrary pitch or roll values

### Requirement: Mouse targeting resolves to the build surface
The game SHALL translate mouse position into a world target on the factory floor so building tools can act on grid cells instead of raw screen positions.

#### Scenario: Hovering the world highlights a target cell
- **WHEN** the cursor is over a buildable part of the factory floor
- **THEN** the game resolves the hovered grid coordinate and presents visible hover feedback at that location

#### Scenario: UI interaction does not place into the world
- **WHEN** the cursor is interacting with an on-screen UI control
- **THEN** world placement and removal actions are not triggered for the same input

### Requirement: Observer mode gates camera panning in the mobile-factory demo
The game SHALL reserve WASD camera panning in the mobile-factory demo for an explicit observer mode so factory command mode can reuse the same keys for unit control.

#### Scenario: Factory command mode keeps the camera fixed
- **WHEN** the player is in the mobile-factory demo's factory command mode and presses WASD
- **THEN** the camera does not pan in response to those keys and the input remains available to the mobile factory controls

#### Scenario: Observer mode restores world camera panning
- **WHEN** the player switches the mobile-factory demo into observer mode and presses WASD
- **THEN** the world camera pans using the existing fixed-angle camera behavior until the player exits observer mode

### Requirement: Player-controlled demos use a follow camera
The game SHALL keep the factory camera readable while following the player character in demos where the player avatar is the default interaction anchor.

#### Scenario: Static sandbox camera follows the player
- **WHEN** the player moves through the static factory sandbox
- **THEN** the camera continues to frame the player from the existing fixed gameplay angle without requiring manual panning after every movement input

#### Scenario: Mobile demo camera follows the player outside explicit factory contexts
- **WHEN** the focused mobile-factory demo is in its default player-control context
- **THEN** the camera tracks the player character instead of defaulting to free panning or direct mobile-factory control

### Requirement: Player-facing UI blocks world control input
The game SHALL prevent player panels, hotbar interactions, and active inventory drags from leaking the same input into world movement, structure placement, or structure selection.

#### Scenario: Dragging inventory items does not place into the world
- **WHEN** the player is dragging an item between hotbar, backpack, or structure inventory windows
- **THEN** left-click release resolves the inventory interaction only and does not also place a structure into the world

#### Scenario: Clicking player panels does not move the character
- **WHEN** the pointer is interacting with a player-facing panel or hotbar control
- **THEN** that input does not trigger player movement, world targeting, or camera mode changes for the same action
