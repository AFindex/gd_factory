# factory-map-data-format Specification

## Purpose
Define the project-owned minimal factory map document format used to reconstruct authored world and interior maps without scene-tree noise or redundant render data.

## Requirements
### Requirement: Factory maps use a minimal semantic file format
The system SHALL define a project-owned factory map file format that stores only the semantic gameplay data required to rebuild an authored factory map.

#### Scenario: World map document contains only reconstruction-relevant data
- **WHEN** an authored static factory world map is represented in the custom format
- **THEN** the file contains only the fields required to reconstruct map bounds, authored deposits, authored anchors or landmarks, and placed structures with their runtime-relevant settings

#### Scenario: Map file does not carry scene-tree noise
- **WHEN** a factory map file is reviewed or parsed
- **THEN** it does not require Godot node paths, scene-tree hierarchy, absolute transforms, or editor-generated metadata unless a runtime reconstruction step explicitly depends on that information

### Requirement: Factory map documents declare kind and version explicitly
The system SHALL mark each map document with an explicit schema version and map kind so the runtime can validate how to interpret the data before reconstruction begins.

#### Scenario: Loader distinguishes world and interior maps
- **WHEN** the runtime opens a factory map file
- **THEN** it can determine from the document itself whether the file represents a world map or a factory interior map before applying map-kind-specific validation

#### Scenario: Unsupported schema versions are rejected
- **WHEN** the runtime encounters a factory map file with an unknown or unsupported schema version
- **THEN** loading fails with a validation error instead of attempting best-effort reconstruction

### Requirement: Factory map entries use logical grid-oriented coordinates
The system SHALL encode authored structures, deposits, anchors, and other gameplay placements using logical map coordinates and orientation data rather than render-space transforms.

#### Scenario: Structure entry identifies gameplay placement directly
- **WHEN** a structure is authored into a factory map file
- **THEN** the entry stores the structure kind, logical placement cell, facing, and only the extra authored options required by that structure's runtime behavior

#### Scenario: Derived render data is not duplicated
- **WHEN** a structure or deposit can be positioned visually from its logical map data
- **THEN** the file omits duplicate world-space transforms or other values that the runtime can derive during reconstruction
