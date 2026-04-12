## MODIFIED Requirements

### Requirement: Mobile factory editing uses a split-view workspace
The game SHALL open mobile factory interior editing as a split-view workspace that keeps the world visible, dedicates the main visual area to the interior editor, and adds an independent editor-operation panel for tools and interaction state instead of using the world summary HUD as the primary editing surface.

#### Scenario: Opening the editor preserves world context and opens edit tools
- **WHEN** the player opens a mobile factory for interior editing
- **THEN** the screen keeps the world context visible, shows the interior editor as the main editing surface, and opens the dedicated editor-operation panel for editing tools

#### Scenario: Closing the editor restores the world-focused HUD layout
- **WHEN** the player exits the mobile factory interior editor
- **THEN** the editor-operation panel closes and the HUD returns to its world-focused summary presentation without discarding the current mobile factory state

### Requirement: Mouse input follows hover ownership between panes
The game SHALL route mouse interactions according to whether the pointer is over the interior editor viewport, the editor-operation panel, or the world view instead of requiring an explicit focus toggle.

#### Scenario: Hovering the editor viewport controls internal building
- **WHEN** the pointer is over the interior editor viewport
- **THEN** mouse actions affect the internal building view and do not manipulate the world view or trigger operation-panel buttons

#### Scenario: Hovering the operation panel consumes editor UI input
- **WHEN** the pointer is over the editor-operation panel
- **THEN** mouse actions interact with panel controls and do not place, remove, or inspect world or interior structures behind the panel

#### Scenario: Hovering the world controls the world view
- **WHEN** the pointer is over the world view while the editor session is open
- **THEN** mouse actions affect world interaction and do not place or remove internal structures
