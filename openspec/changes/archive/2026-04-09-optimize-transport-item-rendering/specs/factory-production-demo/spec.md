## ADDED Requirements

### Requirement: Static sandbox exposes transport-render telemetry during play
The game SHALL expose compact transport-render telemetry in the static factory demo so the player can confirm that high-density logistics rendering is active and observe how much of the transport population is currently being drawn.

#### Scenario: HUD shows transport-render population metrics
- **WHEN** the static factory demo is running with active logistics traffic
- **THEN** the on-screen telemetry includes transport-render metrics such as total active moving items and currently renderable or visible moving items

#### Scenario: HUD shows transport-render batching state
- **WHEN** the static factory demo is using the optimized transport-render path
- **THEN** the telemetry exposes at least one batching-oriented indicator such as active render buckets, instance batches, or an equivalent optimized-render status signal

### Requirement: Static sandbox validates optimized transport rendering under high logistics density
The game SHALL include a high-density logistics observation case and regression coverage that exercise the optimized transport-render path without sacrificing moving-item readability.

#### Scenario: Startup sandbox contains an observable high-density logistics segment
- **WHEN** the player enters the static factory demo from the launcher
- **THEN** the authored sandbox includes at least one logistics segment dense enough to make transport-render telemetry and moving-item readability meaningful during normal observation

#### Scenario: Smoke coverage verifies optimized transport-render signals
- **WHEN** the static sandbox smoke or regression checks run against the default factory demo
- **THEN** they confirm that transport-render telemetry is populated and that the optimized transport-render path remains active while the sandbox is producing moving logistics items
