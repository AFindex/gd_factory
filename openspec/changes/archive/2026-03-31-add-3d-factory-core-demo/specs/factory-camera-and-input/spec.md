## ADDED Requirements

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
