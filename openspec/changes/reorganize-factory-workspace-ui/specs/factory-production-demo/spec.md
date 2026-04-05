## ADDED Requirements

### Requirement: Static sandbox HUD uses categorized workspaces
The game SHALL reorganize the static factory sandbox HUD into categorized workspace panels so build, blueprint, telemetry, combat, and testing content are reachable from the workspace menu without occupying one always-expanded stack.

#### Scenario: Sandbox starts with a compact default workspace
- **WHEN** the player enters the static factory demo from the launcher
- **THEN** the HUD shows the workspace menu and a compact default workspace while the build palette, blueprint workflow, telemetry, and test content remain hidden until their workspace is selected

### Requirement: Sandbox test tools live in a dedicated panel
The game SHALL place sandbox-specific test and verification controls into a dedicated workspace panel separate from the core build workspace so experimental tools do not crowd the main construction UI.

#### Scenario: Build test panel opens independently from build tools
- **WHEN** the player selects the sandbox testing workspace
- **THEN** the HUD shows the build-test and verification controls in their own panel without automatically expanding the normal construction workspace at the same time
