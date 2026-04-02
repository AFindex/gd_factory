## MODIFIED Requirements

### Requirement: Demo includes an observable automation loop
The game SHALL include a playable static factory demo whose default startup layout contains multiple preauthored logistics topology segments, including storage buffers and inserter-driven exchanges, so resource flow can be observed under sustained throughput, buffering, and localized congestion instead of only fixed one-direction chains.

#### Scenario: Source items reach sinks through mixed logistics
- **WHEN** the demo scene runs with its authored starter layout
- **THEN** items are spawned, moved through belts, storage, and inserter-assisted transport chains, and registered as delivered by at least one destination structure in the scene

#### Scenario: Startup layout exercises storage and inserter patterns
- **WHEN** the player starts the default static factory demo
- **THEN** the scene already contains distinct topology clusters that showcase storage fill-and-drain behavior, inserter-fed handoff between different structure classes, and the existing splitter, merger, bridge, or loader/unloader patterns without requiring manual building
