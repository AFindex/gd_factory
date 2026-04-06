## ADDED Requirements

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
