# factory-manufacturing-chain Specification

## Purpose
Define the recipe-driven manufacturing rules for the powered factory sandbox.
## Requirements
### Requirement: Manufacturing machines use declared recipe inputs and outputs
The game SHALL allow manufacturing structures to run recipes that declare required input items, produced output items, cycle time, power demand, and balance-ready item relationships across the expanded mineral catalog, and the authored demo loops SHALL use those declared recipes instead of producer-style direct spawning for their main products.

#### Scenario: Expanded refining recipes convert newly mined resources into dedicated intermediates
- **WHEN** a refining or smelting machine receives the required raw resource for one of the newly introduced mineral branches and has power
- **THEN** it consumes the declared input and produces that branch's configured intermediate output after the configured cycle time

#### Scenario: Support or defense recipe consumes ingredients from multiple real branches
- **WHEN** an assembler-capable machine receives the full declared ingredient set for a defense-supply or maintenance-oriented recipe and has power
- **THEN** it consumes those real inputs and produces the configured output without bypassing the recipe system

### Requirement: Recipe-capable manufacturing machines can switch among valid recipes
The game SHALL allow at least the primary assembler-capable manufacturing structure to expose multiple valid recipes and update its accepted ingredients and produced outputs when the active recipe changes.

#### Scenario: Selecting a different assembler recipe changes its contract
- **WHEN** the player selects another valid recipe for a recipe-capable manufacturing structure
- **THEN** the structure updates its active recipe, accepted input set, production summary, and future outputs to match the selected recipe

### Requirement: Multi-port assemblers expose side-separated logistics lanes
The game SHALL define the assembler and ammo assembler multi-port logistics contract from shared footprint metadata so each `3x2` machine exposes three input cells on one long side of its footprint and three output cells on the opposite long side, with rotation preserving that separation.

#### Scenario: East-facing assembler exposes three ports per long edge
- **WHEN** the player inspects or connects belts to an east-facing assembler or ammo assembler
- **THEN** the machine exposes three input cells along one `3-cell` long edge of the footprint and three output cells along the opposite `3-cell` long edge

#### Scenario: Rotating an assembler preserves separated input and output sides
- **WHEN** the player rotates an assembler or ammo assembler before placement, after placement, or through blueprint application preview
- **THEN** the six port cells rotate with the footprint and continue presenting three inputs on one side and three outputs on the opposite side

#### Scenario: Assemblers no longer show permanent port cubes outside belt preview
- **WHEN** the player is looking at an assembler or ammo assembler without an active belt placement preview
- **THEN** the machine does not show always-on port marker cubes on its model

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

### Requirement: Starter manufacturing catalog includes branching multi-stage production paths
The game SHALL define an authored starter manufacturing catalog that keeps the baseline coal, iron, and copper branches and adds at least two additional mineable mineral families with downstream intermediates so the demo can sustain manufacturing loops for defense supply, power maintenance, and station logistics without relying on placeholder cargo.

#### Scenario: Expanded catalog includes more than the baseline mineral trio
- **WHEN** the player inspects the starter manufacturing catalog
- **THEN** it includes the existing coal, iron, and copper branches plus at least two additional mineable mineral families that participate in downstream recipes instead of existing only as unused map resources

#### Scenario: At least one new mineral branch contributes to support gameplay
- **WHEN** the player inspects the authored recipes enabled by the expanded catalog
- **THEN** at least one newly added mineral family feeds a defense-supply, maintenance, or power-support output that is used by a sandbox scenario

### Requirement: Manufacturing catalog supports authored non-placeholder sandbox loops
The game SHALL provide enough real recipe variety for authored sandbox cases to cover extraction-to-delivery, defense resupply, and power-support loops without requiring placeholder cargo producers in the normal demo path.

#### Scenario: Authored defense lane is fed by recipe outputs
- **WHEN** the player inspects the authored defense-supporting production lane in the sandbox
- **THEN** the lane's ammunition or support goods originate from declared machine recipes rather than from a placeholder source building

#### Scenario: Authored maintenance lane is fed by recipe outputs
- **WHEN** the player inspects the authored power-maintenance or depot-support lane in the sandbox
- **THEN** its delivered goods originate from declared machine recipes built from mined or refined resources
