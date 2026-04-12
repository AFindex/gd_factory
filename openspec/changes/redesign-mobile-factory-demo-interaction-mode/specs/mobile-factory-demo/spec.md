## MODIFIED Requirements

### Requirement: Focused mobile factory demo organizes tools into workspaces
The game SHALL expose the focused mobile factory demo's command controls, edit entry, blueprint workflow, save workflow, testing surfaces, and factory reference information through a top workspace navigation shell that follows the same primary HUD hierarchy and one-bit interaction style used by the static factory demo, instead of permanently presenting separate world and editor control stacks as equal peers.

#### Scenario: Focused mobile demo shows a factory-demo-style workspace shell
- **WHEN** the player opens the focused mobile-factory demo
- **THEN** the HUD shows a shared top workspace menu, one primary main panel, and scene-appropriate status labels organized in the same interaction hierarchy used by the static factory demo

#### Scenario: Switching workspaces preserves the current play context
- **WHEN** the player switches between command, blueprint, save, testing, or detail workspaces while the demo is already running
- **THEN** the current lifecycle state, selected world control mode, deployment preview state, and active editor session remain intact unless the player explicitly changes or closes them

#### Scenario: Entering edit mode does not require a permanently expanded editor workspace
- **WHEN** the player chooses to edit the mobile factory interior from the focused demo
- **THEN** the demo keeps the shared workspace shell active and opens the dedicated editor viewport plus operation panel only for the active edit session
