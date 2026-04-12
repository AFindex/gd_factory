## MODIFIED Requirements

### Requirement: Mobile factory demo exposes explicit control modes
The mobile-factory demo SHALL expose explicit world control modes together with an explicit edit-session state so the player can tell whether movement input currently controls the player character, the mobile factory, deployment preview, the world camera, or the mobile factory interior editor.

#### Scenario: Demo starts with readable world control state and no active edit session
- **WHEN** the player opens the dedicated mobile-factory demo scene
- **THEN** the HUD shows the current world control mode and indicates that edit mode is inactive until the player explicitly enters it

#### Scenario: Player toggles factory, observer, deploy, or edit context
- **WHEN** the player activates the factory-command button, deploy workflow, observer-mode button, or edit-mode entry
- **THEN** the HUD indicates the newly active context and provides a clear way to return to the previous world control state or exit edit mode

## ADDED Requirements

### Requirement: Editing mode preserves world control ownership
The game SHALL preserve the last active world control mode when entering and leaving edit mode, and it MUST NOT implicitly force observer mode merely because the editor surface is opened.

#### Scenario: Entering edit mode remembers the previous world owner
- **WHEN** the player enters edit mode while player control, factory command mode, deploy preview, or observer mode is active
- **THEN** the demo stores that world control owner and routes edit inputs to the editor surfaces without discarding the remembered world mode

#### Scenario: Exiting edit mode restores the remembered world owner
- **WHEN** the player closes edit mode
- **THEN** subsequent world input resumes under the world control mode that was active before edit mode was opened
