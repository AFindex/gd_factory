# factory-production-demo Specification

## Purpose
TBD - created by archiving change add-3d-factory-core-demo. Update Purpose after archive.
## Requirements
### Requirement: Demo includes an observable automation loop
The game SHALL include a playable static factory demo whose default startup layout contains multiple preauthored logistics topology segments, including storage buffers and inserter-driven exchanges, so resource flow can be observed under sustained throughput, buffering, and localized congestion instead of only fixed one-direction chains.

#### Scenario: Source items reach sinks through mixed logistics
- **WHEN** the demo scene runs with its authored starter layout
- **THEN** items are spawned, moved through belts, storage, and inserter-assisted transport chains, and registered as delivered by at least one destination structure in the scene

#### Scenario: Startup layout exercises storage and inserter patterns
- **WHEN** the player starts the default static factory demo
- **THEN** the scene already contains distinct topology clusters that showcase storage fill-and-drain behavior, inserter-fed handoff between different structure classes, and the existing splitter, merger, bridge, or loader/unloader patterns without requiring manual building

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
