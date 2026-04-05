## ADDED Requirements

### Requirement: Large mobile factory scenario uses categorized workspace panels
The game SHALL reorganize the large mobile factory test scenario HUD behind categorized workspace panels so sandbox tools, scenario diagnostics, and observation content are no longer always shown together.

#### Scenario: Scenario load shows menu-first HUD organization
- **WHEN** the player opens the large mobile factory test scenario
- **THEN** the HUD shows a workspace menu with the scenario's available panel categories and keeps non-active tool groups collapsed until selected

### Requirement: Scenario sandbox tooling is split into dedicated panels
The game SHALL separate scenario-facing sandbox tooling into dedicated panels, including a build test panel, so each testing workflow can be opened without crowding unrelated scenario information.

#### Scenario: Build test panel is distinct from scenario diagnostics
- **WHEN** the player selects the build test workspace inside the large mobile factory test scenario
- **THEN** the HUD shows build-oriented test controls in their own panel while unrelated scenario diagnostics remain in their separate workspace panels until explicitly selected
