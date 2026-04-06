## MODIFIED Requirements

### Requirement: Starter manufacturing catalog includes branching multi-stage production paths
The game SHALL define an authored starter manufacturing catalog that keeps the baseline coal, iron, and copper branches and adds at least two additional mineable mineral families with downstream intermediates so the demo can sustain manufacturing loops for defense supply, power maintenance, and station logistics without relying on placeholder cargo.

#### Scenario: Expanded catalog includes more than the baseline mineral trio
- **WHEN** the player inspects the starter manufacturing catalog
- **THEN** it includes the existing coal, iron, and copper branches plus at least two additional mineable mineral families that participate in downstream recipes instead of existing only as unused map resources

#### Scenario: At least one new mineral branch contributes to support gameplay
- **WHEN** the player inspects the authored recipes enabled by the expanded catalog
- **THEN** at least one newly added mineral family feeds a defense-supply, maintenance, or power-support output that is used by a sandbox scenario

### Requirement: Manufacturing machines use declared recipe inputs and outputs
The game SHALL allow manufacturing structures to run recipes that declare required input items, produced output items, cycle time, power demand, and balance-ready item relationships across the expanded mineral catalog, and the authored demo loops SHALL use those declared recipes instead of producer-style direct spawning for their main products.

#### Scenario: Expanded refining recipes convert newly mined resources into dedicated intermediates
- **WHEN** a refining or smelting machine receives the required raw resource for one of the newly introduced mineral branches and has power
- **THEN** it consumes the declared input and produces that branch's configured intermediate output after the configured cycle time

#### Scenario: Support or defense recipe consumes ingredients from multiple real branches
- **WHEN** an assembler-capable machine receives the full declared ingredient set for a defense-supply or maintenance-oriented recipe and has power
- **THEN** it consumes those real inputs and produces the configured output without bypassing the recipe system

## ADDED Requirements

### Requirement: Manufacturing catalog supports authored non-placeholder sandbox loops
The game SHALL provide enough real recipe variety for authored sandbox cases to cover extraction-to-delivery, defense resupply, and power-support loops without requiring placeholder cargo producers in the normal demo path.

#### Scenario: Authored defense lane is fed by recipe outputs
- **WHEN** the player inspects the authored defense-supporting production lane in the sandbox
- **THEN** the lane's ammunition or support goods originate from declared machine recipes rather than from a placeholder source building

#### Scenario: Authored maintenance lane is fed by recipe outputs
- **WHEN** the player inspects the authored power-maintenance or depot-support lane in the sandbox
- **THEN** its delivered goods originate from declared machine recipes built from mined or refined resources
