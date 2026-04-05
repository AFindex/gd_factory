## MODIFIED Requirements

### Requirement: Manufacturing machines use declared recipe inputs and outputs
The game SHALL allow manufacturing structures to run recipes that declare required input items, produced output items, cycle time, power demand, and balance-ready item relationships across a broader multi-stage catalog, and a machine SHALL only advance a recipe when those declared conditions are met.

#### Scenario: Smelting recipes convert different raw resources into distinct intermediates
- **WHEN** a refining or smelting machine receives the required mined raw resource for an iron, copper, or steel-stage recipe and has power
- **THEN** it consumes the declared input and produces that recipe's configured intermediate output after the configured cycle time

#### Scenario: Assembly recipe consumes intermediates from more than one branch
- **WHEN** an assembler receives the full declared set of intermediate ingredients from separate production branches for its active recipe and has power
- **THEN** it consumes those declared ingredients and produces the recipe's configured higher-tier output item

### Requirement: Recipe-capable manufacturing machines can switch among valid recipes
The game SHALL allow at least the primary assembler-capable manufacturing structure to expose multiple valid recipes and update its accepted ingredients and produced outputs when the active recipe changes.

#### Scenario: Selecting a different assembler recipe changes its contract
- **WHEN** the player selects another valid recipe for a recipe-capable manufacturing structure
- **THEN** the structure updates its active recipe, accepted input set, production summary, and future outputs to match the selected recipe

### Requirement: Manufacturing outputs hand off through the existing logistics layer
The game SHALL keep recipe completion compatible with the current logistics sandbox by handing manufactured outputs into buffers, inserters, belts, storage, or sinks through the existing deterministic transfer rules.

#### Scenario: Completed recipe output enters downstream logistics
- **WHEN** a manufacturing machine completes a recipe cycle and a downstream logistics path is available
- **THEN** the completed item is emitted into the connected transfer chain without bypassing the normal logistics rules

### Requirement: Manufacturing stalls cleanly when inputs, output space, or power are missing
The game SHALL keep manufacturing deterministic when a recipe is starved of ingredients, cannot emit its output, or lacks sufficient power.

#### Scenario: Missing ingredient pauses recipe progress
- **WHEN** a manufacturing machine lacks one or more required recipe ingredients
- **THEN** it does not consume partial inputs or advance as if the recipe were satisfied

#### Scenario: Blocked output preserves the completed item
- **WHEN** a manufacturing machine has finished a product but no downstream receiver can currently accept it
- **THEN** the completed output remains buffered or blocked at the machine instead of being deleted or duplicated

## ADDED Requirements

### Requirement: Starter manufacturing catalog includes branching multi-stage production paths
The game SHALL define an authored starter manufacturing catalog that includes at least coal, iron ore, copper ore, iron plate, copper plate, steel plate, gear, copper wire, circuit board, machine part, ammo magazine, and high-velocity ammo, with recipes connecting them through more than one production branch.

#### Scenario: Iron and copper branches create separate intermediate families
- **WHEN** the player inspects the starter manufacturing catalog
- **THEN** it includes at least one iron-derived intermediate branch and one copper-derived intermediate branch before the final crafted outputs

#### Scenario: Machine part requires combined intermediate inputs
- **WHEN** the machine-part recipe is active in the starter catalog
- **THEN** it requires declared ingredients from more than one prior crafting step instead of being produced directly from a single mined raw resource

#### Scenario: Upgraded ammo requires a higher-tier follow-up recipe
- **WHEN** the player inspects the high-velocity ammo recipe in the starter catalog
- **THEN** it depends on at least one previously crafted ingredient rather than being defined as a direct raw-resource output
