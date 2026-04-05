## ADDED Requirements

### Requirement: Focused mobile factory demo organizes tools into workspaces
The game SHALL expose the focused mobile factory demo's command controls, editor tools, blueprint workflow, and factory reference information through menu-selected workspaces instead of one default-expanded overlay.

#### Scenario: Mobile demo shows categorized workspace entries
- **WHEN** the player opens the focused mobile factory demo
- **THEN** the HUD presents a workspace menu that includes the core mobile demo categories needed for command, editing, blueprints, and factory information

### Requirement: Mobile factory details can be opened without interrupting play
The game SHALL let the player open a dedicated mobile factory detail workspace that reports lifecycle, attachment, and layout-oriented information without cancelling the current demo control mode or closing the split-view session.

#### Scenario: Opening factory details preserves the current mobile demo state
- **WHEN** the player selects the mobile factory detail workspace while the focused mobile factory demo is active
- **THEN** the HUD shows mobile factory detail content while the current deployment state, control mode, and editor session remain active
