## ADDED Requirements

### Requirement: Edit mode opens an independent operation panel
The game SHALL open a dedicated editor-operation panel when the player enters mobile factory edit mode, separate from the top workspace chrome, the world summary HUD, and standalone detail windows.

#### Scenario: Entering edit mode reveals the panel
- **WHEN** the player enters edit mode from the focused mobile factory demo
- **THEN** the scene shows the interior editor viewport together with a dedicated editor-operation panel while keeping the shared workspace shell active

#### Scenario: Leaving edit mode dismisses the panel
- **WHEN** the player exits edit mode
- **THEN** the editor-operation panel closes and the HUD returns to a world-focused summary layout without resetting the current mobile factory state

### Requirement: The operation panel centralizes editor tool state and actions
The game SHALL present the active interior interaction mode, selected build tool, facing, rotate/delete actions, blueprint shortcuts, focus hints, and selection summary in the independent editor-operation panel instead of scattering those controls across the world summary HUD.

#### Scenario: Tool changes update the operation panel
- **WHEN** the player selects a build tool, rotates the current facing, or toggles interaction/delete mode while edit mode is active
- **THEN** the editor-operation panel reflects the updated tool state and the world summary HUD remains high level

#### Scenario: Structure details remain independent from the operation panel
- **WHEN** the player opens a structure detail window while edit mode is active
- **THEN** the detail window appears independently and the editor-operation panel remains available for continued editing commands
