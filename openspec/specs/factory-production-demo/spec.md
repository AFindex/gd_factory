# factory-production-demo Specification

## Purpose
TBD - created by archiving change add-3d-factory-core-demo. Update Purpose after archive.
## Requirements
### Requirement: Demo includes an observable automation loop
The game SHALL include a playable static factory demo whose default startup layout contains multiple preauthored logistics topology segments, so resource flow can be observed under sustained throughput and localized congestion instead of only a minimal single-line example.

#### Scenario: Source items reach the sink
- **WHEN** the demo scene runs with its authored starter layout
- **THEN** items are spawned, moved through the authored transport chains, and registered as delivered by at least one destination structure in the scene

#### Scenario: Startup layout exercises multiple topology patterns
- **WHEN** the player starts the default static factory demo
- **THEN** the scene already contains several distinct topology patterns such as branching, merging, crossing, long transport runs, or loader/unloader relays that continue operating without manual building

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
The project SHALL boot into a playable factory demo scene without requiring manual scene switching in the editor.

#### Scenario: Project startup enters gameplay slice
- **WHEN** the player starts the project normally
- **THEN** the main scene loads the factory demo and exposes the camera, building, and automation loop defined by this change

### Requirement: Splitter preserves available output under asymmetric blockage
The game SHALL allow a splitter in the static factory simulation to keep forwarding items through any output that is currently able to receive them, even if the other output is blocked.

#### Scenario: One blocked branch does not stall the free branch
- **WHEN** one splitter output is blocked by downstream congestion while the other output path can still accept items
- **THEN** the splitter continues dispatching items through the available output instead of stalling both branches

#### Scenario: Splitter waits only when both branches are blocked
- **WHEN** both splitter outputs are unable to accept the next item
- **THEN** the item remains buffered at the splitter until one of the outputs becomes available

