## ADDED Requirements

### Requirement: Belt placement supports midspan merge authoring
The build system SHALL allow players to author a belt whose forward endpoint targets the occupied cell of another belt when the new belt's own footprint is valid, so T-shaped logistics merges can be built directly on the grid.

#### Scenario: Feeder belt can point into another belt's occupied cell
- **WHEN** the player previews or places a belt on an empty valid cell whose forward endpoint lands on a neighboring belt's occupied cell rather than that belt's input endpoint
- **THEN** the preview remains buildable and the placement succeeds instead of being rejected as a disconnected transport endpoint

#### Scenario: Occupied target still blocks non-merge overlap
- **WHEN** the player attempts to place a structure whose own occupied footprint would overlap an existing structure while trying to create a midspan merge
- **THEN** the build system still rejects the placement according to the normal footprint occupancy rules

### Requirement: Merger preview communicates three-input topology
The build preview SHALL present the merger as a three-input, one-output transport node so players can align rear and side feeder belts without relying on legacy two-input assumptions.

#### Scenario: Merger preview shows rear and side intake directions
- **WHEN** the player selects the merger build tool and moves the preview across the grid
- **THEN** the preview communicates that the merger accepts input from its rear, left, and right faces and emits output through its forward face
