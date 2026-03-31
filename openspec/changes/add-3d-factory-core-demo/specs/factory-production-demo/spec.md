## ADDED Requirements

### Requirement: Demo includes an observable automation loop
The game SHALL include a playable demo setup where resources move from a producer through transport elements into a destination structure.

#### Scenario: Source items reach the sink
- **WHEN** the demo scene runs with a valid connection from source to transport to sink
- **THEN** items are spawned, moved through the chain, and registered as delivered by the destination structure

### Requirement: Prototype logistics update on a deterministic simulation step
The game SHALL advance factory entities on a controlled simulation cadence so prototype production behavior is predictable and testable.

#### Scenario: Unconnected output does not teleport items
- **WHEN** a producer has no valid downstream transport path
- **THEN** produced items remain blocked or buffered according to the prototype rules instead of skipping to a destination

### Requirement: Demo feedback exposes loop success
The game SHALL expose lightweight feedback that confirms whether the automation loop is functioning in the demo scene.

#### Scenario: Delivered item count is visible
- **WHEN** items reach the destination structure during play
- **THEN** the player can see a visible counter, status readout, or similar telemetry indicating successful delivery

### Requirement: Demo scene is immediately playable
The project SHALL boot into a playable factory demo scene without requiring manual scene switching in the editor.

#### Scenario: Project startup enters gameplay slice
- **WHEN** the player starts the project normally
- **THEN** the main scene loads the factory demo and exposes the camera, building, and automation loop defined by this change
