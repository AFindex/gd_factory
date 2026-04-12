## ADDED Requirements

### Requirement: Mobile factory overview panel owns workspace tabs and collapse control
The game SHALL render the focused mobile factory demo's workspace tabs as part of the mobile factory overview panel header, and that same header SHALL expose a control that collapses the overview panel leftward and restores it rightward without changing the active workspace.

#### Scenario: Workspace tabs render inside the overview panel header
- **WHEN** the player opens the focused mobile factory demo
- **THEN** the workspace tab strip appears in the mobile factory overview panel header instead of as a separate floating top panel above it

#### Scenario: Overview panel can slide closed and reopen
- **WHEN** the player presses the overview panel's right-side collapse control
- **THEN** the overview panel slides left to a hidden or mostly hidden state while preserving the current workspace selection and offering a visible control to slide it back open

### Requirement: Focused mobile demo exposes world-side debug build tools
The game SHALL expose the shared world debug source structures and permanent test generator from the focused mobile factory demo's world-side construction flow.

#### Scenario: World-side construction shows debug entries
- **WHEN** the player opens the focused mobile factory demo's world build category or equivalent construction panel
- **THEN** the build entries include clearly labeled debug item-source structures and a permanent test generator alongside the normal world construction tools
