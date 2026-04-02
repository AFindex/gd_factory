## ADDED Requirements

### Requirement: Launcher scene lists available demo experiences
The project SHALL provide a launcher scene that serves as the default startup hub and presents the launcher-managed demo scenes with clear labels and scene-switch actions.

#### Scenario: Project startup opens launcher
- **WHEN** the player starts the project normally
- **THEN** the main scene loads the launcher instead of entering one specific demo directly

#### Scenario: Launcher exposes the current demos
- **WHEN** the launcher scene is shown
- **THEN** it displays launch actions for the static factory demo, the focused mobile factory demo, the large mobile factory test scenario, and the UI showcase

### Requirement: Launcher can open the selected demo scene
The launcher SHALL route the player into the selected demo scene without requiring editor-side scene changes.

#### Scenario: Player opens a demo from the launcher
- **WHEN** the player activates one of the launcher demo entries
- **THEN** the project changes to that entry's target scene and starts the selected demo experience

### Requirement: Launcher-managed demos provide a return path
Each launcher-managed demo scene SHALL expose a visible action that returns the player to the launcher scene.

#### Scenario: Returning from the static factory demo
- **WHEN** the player uses the demo return action from the static factory demo
- **THEN** the project changes back to the launcher scene

#### Scenario: Returning from the focused mobile factory demo
- **WHEN** the player uses the demo return action from the focused mobile factory demo
- **THEN** the project changes back to the launcher scene

#### Scenario: Returning from the large mobile factory test scenario
- **WHEN** the player uses the demo return action from the large mobile factory test scenario
- **THEN** the project changes back to the launcher scene

#### Scenario: Returning from the UI showcase
- **WHEN** the player uses the demo return action from the UI showcase
- **THEN** the project changes back to the launcher scene
