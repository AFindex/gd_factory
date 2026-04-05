# factory-production-demo Specification

## Purpose
TBD - created by archiving change add-3d-factory-core-demo. Update Purpose after archive.
## Requirements
### Requirement: Demo includes an observable automation loop
The game SHALL include a playable static factory demo whose default startup layout contains multiple authored districts that together form a powered input -> production -> output chain, including resource extraction, fuel supply, power generation and distribution, intermediate manufacturing, buffering, and final delivery, so automation can be observed under sustained throughput, localized congestion, and power-state changes instead of only fixed producer-to-sink chains.

#### Scenario: Startup layout exercises powered extraction and manufacturing
- **WHEN** the player starts the default static factory demo
- **THEN** the scene already contains authored mining, power, smelting, assembly, storage, and delivery segments that demonstrate the full chain without requiring manual building

#### Scenario: Mined resources reach outputs through the powered chain
- **WHEN** the demo scene runs with its authored starter layout
- **THEN** items extracted from resource deposits flow through powered manufacturing and are registered as delivered by at least one destination structure in the scene

### Requirement: Demo sandbox spans multiple authored districts
The game SHALL expand the static sandbox world to roughly three times its current footprint so mining, power, manufacturing, combat, and regression lanes can coexist on one readable map.

#### Scenario: Expanded sandbox loads with separated districts
- **WHEN** the player enters the static factory demo from the launcher
- **THEN** the playable world includes visibly separated authored districts for extraction, power, manufacturing, and verification lanes on one larger map

#### Scenario: Camera and interaction remain usable across the larger world
- **WHEN** the player pans, zooms, and builds across the expanded sandbox
- **THEN** camera bounds, hover targeting, and build previews continue to function across the enlarged playable area

### Requirement: Starter use cases verify the new core factory systems
The game SHALL include authored starter-layout use cases and smoke coverage that verify powered extraction, manufacturing progression, and delivery on the default sandbox.

#### Scenario: Smoke flow validates the powered factory chain
- **WHEN** the static sandbox smoke test runs against the default starter layout
- **THEN** it verifies that resource extraction, generator-powered production, recipe progression, and final delivery all succeed without manual intervention

### Requirement: Prototype logistics update on a deterministic simulation step
The game SHALL advance factory entities on a controlled simulation cadence so prototype production behavior is predictable and testable.

#### Scenario: Unconnected output does not teleport items
- **WHEN** a producer has no valid downstream transport path
- **THEN** produced items remain blocked or buffered according to the prototype rules instead of skipping to a destination

### Requirement: Demo feedback exposes loop success
The game SHALL expose compact on-screen feedback in the static factory demo that lets the player confirm both automation health and runtime performance without obscuring most of the playfield.

#### Scenario: Delivered item count is visible
- **WHEN** items reach a destination structure during play
- **THEN** the player can see a visible throughput or delivery readout indicating that the automation loop is functioning

#### Scenario: Compact HUD preserves most of the viewport
- **WHEN** the static factory demo HUD is shown during normal play
- **THEN** its primary panel remains confined to a compact region near one edge of the screen rather than expanding into a large overlay over the factory floor

#### Scenario: Runtime profiler telemetry is visible
- **WHEN** the static factory demo is running
- **THEN** the HUD shows runtime telemetry including current frame rate and at least one performance-hotspot-oriented metric relevant to the demo simulation

### Requirement: Demo scene is immediately playable
The project SHALL boot into a launcher scene that exposes the static factory demo as a selectable experience, and entering that demo from the launcher SHALL still load a playable factory slice without requiring manual scene switching in the editor.

#### Scenario: Project startup enters launcher first
- **WHEN** the player starts the project normally
- **THEN** the main scene loads the launcher and presents the static factory demo as one of the available entries

#### Scenario: Launcher opens the factory gameplay slice
- **WHEN** the player selects the static factory demo from the launcher
- **THEN** the project loads the factory demo scene with the camera, building, and automation loop defined by this capability

### Requirement: Splitter preserves available output under asymmetric blockage
The game SHALL allow a splitter in the static factory simulation to keep forwarding items through any output that is currently able to receive them, even if the other output is blocked.

#### Scenario: One blocked branch does not stall the free branch
- **WHEN** one splitter output is blocked by downstream congestion while the other output path can still accept items
- **THEN** the splitter continues dispatching items through the available output instead of stalling both branches

#### Scenario: Splitter waits only when both branches are blocked
- **WHEN** both splitter outputs are unable to accept the next item
- **THEN** the item remains buffered at the splitter until one of the outputs becomes available

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
