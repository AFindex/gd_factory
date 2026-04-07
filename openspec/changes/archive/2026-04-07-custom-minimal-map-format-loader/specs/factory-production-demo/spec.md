## ADDED Requirements

### Requirement: Static sandbox authored layout loads from factory map data
The game SHALL source the static factory sandbox's authored startup layout from the custom factory map format through the shared runtime loader instead of keeping the full authored layout embedded in demo controller code.

#### Scenario: Startup sandbox reconstructs from authored map file
- **WHEN** the player opens the static factory demo
- **THEN** the authored mining fields, logistics districts, manufacturing lines, power layout, and receiving or defense landmarks are reconstructed from the selected factory map file through the shared loader

#### Scenario: Map-driven startup preserves existing sandbox behavior
- **WHEN** the static sandbox is reconstructed from factory map data
- **THEN** the existing build, detail, blueprint, inventory, telemetry, and smoke-tested automation behavior remains functionally equivalent to the prior authored startup experience
