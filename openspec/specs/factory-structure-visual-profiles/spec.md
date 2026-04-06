# factory-structure-visual-profiles Specification

## Purpose
Define configurable visual profiles for factory structures so simulation logic can stay separate from procedural visuals, authored model scenes, and animation-aware presentation state.

## Requirements
### Requirement: Factory structures expose configurable visual profiles
The game SHALL let each factory structure kind resolve a visual profile that is separate from its simulation logic and that can describe a procedural code-built presentation, an authored 3D scene/model presentation, or a scene presentation that includes animation-capable nodes.

#### Scenario: A structure uses a procedural visual profile
- **WHEN** a structure kind defines only a procedural visual builder in its visual profile
- **THEN** the structure instantiates its world presentation through that profile without requiring an authored external scene asset

#### Scenario: A structure uses an authored scene visual profile
- **WHEN** a structure kind defines a loadable authored scene or model hierarchy in its visual profile
- **THEN** the structure instantiates that authored scene as its world presentation instead of falling back to the generic procedural placeholder

### Requirement: Structure presentation supports deterministic visual fallback
The game SHALL resolve structure visuals through a deterministic fallback chain so missing higher-fidelity assets do not prevent a structure from rendering in the world.

#### Scenario: Missing authored asset falls back to procedural presentation
- **WHEN** a structure visual profile prefers an authored scene or model asset but that asset cannot be instantiated
- **THEN** the structure falls back to its configured procedural visual builder and remains visible in the world

#### Scenario: Missing authored asset and procedural builder falls back to a generic placeholder
- **WHEN** a structure visual profile has no usable authored asset and no usable custom procedural builder
- **THEN** the structure still renders with a generic placeholder presentation rather than disappearing or blocking placement/play

### Requirement: Structure visual updates consume runtime presentation state without altering simulation
The game SHALL drive structure presentation from a dedicated runtime visual-state update path so animation and presentation state changes do not alter recipe progression, transfer rules, power resolution, or placement behavior.

#### Scenario: Working-state animation does not change recipe completion timing
- **WHEN** a recipe-capable structure switches from idle to actively processing and its visual state changes to a working presentation
- **THEN** the structure's recipe timing, input consumption, and output production remain governed by the existing deterministic simulation rules

#### Scenario: Underpowered-state presentation does not override machine rules
- **WHEN** a structure enters an underpowered or unpowered state and its presentation switches to a dimmed or stalled visual response
- **THEN** the structure still follows the normal power-gated simulation behavior instead of receiving extra progress or forced interruption from the visual layer

### Requirement: Smelter visual profile demonstrates furnace-like procedural presentation
The game SHALL migrate the smelter to the structure visual-profile pipeline and provide a code-defined furnace presentation that communicates heating, active smelting, and cooling states more clearly than the current static box composition.

#### Scenario: Active smelting shows a hot furnace state
- **WHEN** the smelter has power, is processing a recipe, and updates its presentation state
- **THEN** its code-defined presentation shows visible furnace-like hot-state cues such as animated firebox glow, heated exhaust, or rhythmic body/emission changes instead of a mostly static machine silhouette

#### Scenario: Idle or powerless smelter cools down visibly
- **WHEN** the smelter is idle, blocked, underpowered, or unpowered after previously running
- **THEN** its presentation transitions toward a cooler, lower-activity furnace state instead of remaining locked in the active hot-state appearance
