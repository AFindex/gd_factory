## MODIFIED Requirements

### Requirement: Factory structures expose configurable visual profiles
The game SHALL let each factory structure resolve a visual profile that is separate from simulation logic and that can branch by site context or presentation role so a world structure and its interior-standard counterpart may share behavior while presenting different silhouettes, materials, and support geometry.

#### Scenario: A structure uses a procedural visual profile
- **WHEN** a structure kind defines only a procedural visual builder in its visual profile for the active site context
- **THEN** the structure instantiates that context-aware presentation without requiring an authored external scene asset and without falling back to the wrong world-side silhouette

#### Scenario: A structure uses an authored scene visual profile
- **WHEN** a structure kind defines a loadable authored scene or model hierarchy for the current world or interior context
- **THEN** the structure instantiates that authored scene as its presentation for the active site instead of reusing a generic or world-only placeholder

## ADDED Requirements

### Requirement: Interior-standard structures render as cabin equipment
The game SHALL render interior-standard structures as cabin equipment with embedded channels, service housings, cabinets, hardpoints, or recessed interfaces rather than as scaled-down outdoor facilities.

#### Scenario: Interior logistics structure reads as embedded hardware
- **WHEN** the player previews or inspects an interior logistics piece such as a belt, splitter, merger, bridge, or transfer buffer
- **THEN** its presentation emphasizes embedded rails, channels, trays, or routing hardware instead of outdoor conveyor frames and exposed yard machinery

#### Scenario: Interior machine reads as a service module
- **WHEN** the player previews or inspects an interior smelter, assembler, ammo module, generator, storage unit, or turret mount
- **THEN** its presentation emphasizes module housings, maintenance faces, drawer-like buffering, or hardpoint geometry instead of the silhouette of a world furnace, factory shed, depot, pole, or ground turret
