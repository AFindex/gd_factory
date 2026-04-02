## MODIFIED Requirements

### Requirement: Demo scene is immediately playable
The project SHALL boot into a launcher scene that exposes the static factory demo as a selectable experience, and entering that demo from the launcher SHALL still load a playable factory slice without requiring manual scene switching in the editor.

#### Scenario: Project startup enters launcher first
- **WHEN** the player starts the project normally
- **THEN** the main scene loads the launcher and presents the static factory demo as one of the available entries

#### Scenario: Launcher opens the factory gameplay slice
- **WHEN** the player selects the static factory demo from the launcher
- **THEN** the project loads the factory demo scene with the camera, building, and automation loop defined by this capability
