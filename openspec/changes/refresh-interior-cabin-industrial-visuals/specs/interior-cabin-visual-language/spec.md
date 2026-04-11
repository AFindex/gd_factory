## ADDED Requirements

### Requirement: Mobile factory interiors define cabin-native presentation roles
The game SHALL classify interior-only industrial objects into cabin-native presentation roles such as embedded logistics, service modules, buffer cabinets, power nodes, hardpoints, and hull interfaces so those objects share a coherent industrial family instead of reading like unrelated miniaturized world buildings.

#### Scenario: Two interior objects in the same role family read as related equipment
- **WHEN** the player compares two interior logistics pieces or two interior service modules in the same mobile factory
- **THEN** their silhouettes, panel language, trim, and support features read as members of the same cabin equipment family rather than as separate copied world facilities

### Requirement: Cabin presentation preserves maintenance-space readability
The game SHALL keep cabin presentation readable as a maintenance space by making maintenance routes, service faces, and embedded logistics layers visually distinct from one another.

#### Scenario: Maintenance route remains legible beside active cargo hardware
- **WHEN** the player observes an active interior bay that includes both cargo flow and installed industrial equipment
- **THEN** the presentation clearly separates maintenance walkways or access zones from the embedded cargo-routing layer instead of collapsing both into a single open floor of tiny machines

### Requirement: Interior-only industrial objects present as installed equipment
The game SHALL present interior-only industrial objects as installed cabin equipment with module housings, access panels, recessed channels, or hardpoint geometry instead of as free-standing outdoor facilities scaled down for indoor use.

#### Scenario: Interior object avoids a world-facility silhouette
- **WHEN** the player previews or inspects an interior-only machine, storage unit, turret mount, or interface
- **THEN** its primary shape emphasizes installed cabin hardware such as recessed rails, cabinets, hardpoint wells, or wall-mounted interfaces rather than the silhouette of a world-side drill, pole, depot, or ground turret
