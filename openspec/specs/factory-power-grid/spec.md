# factory-power-grid Specification

## Purpose
Define deterministic electrical-network behavior for powered factory structures.

## Requirements
### Requirement: Power producers, relays, and consumers form connected networks
The game SHALL support power-producing, power-relay, and power-consuming structures in the world sandbox, and connected power structures SHALL be treated as one deterministic electrical network.

#### Scenario: Generator and relays energize distant consumers
- **WHEN** a generator is connected to a mining drill or manufacturing machine through valid relay coverage
- **THEN** the connected consumer joins the same power network and can receive electrical supply from that generator

#### Scenario: Isolated consumers remain disconnected
- **WHEN** a mining drill or manufacturing machine has no connected generator path
- **THEN** the structure remains unpowered and does not behave as if electricity were available

### Requirement: Fuel-fed generators convert delivered items into electrical supply
The game SHALL allow a generator to consume compatible delivered fuel items and convert them into electrical output for its connected network.

#### Scenario: Fueled generator powers the network
- **WHEN** a generator receives compatible fuel through the logistics chain and a connected consumer requests power
- **THEN** the generator contributes electrical supply that can satisfy connected machine demand

#### Scenario: Empty generator stops supplying power
- **WHEN** a generator has no valid fuel available
- **THEN** it contributes no electrical supply until compatible fuel is delivered again

### Requirement: Consumer throughput follows network power satisfaction
The game SHALL compute a deterministic power-satisfaction state for each connected network from available supply versus active demand, and powered consumers SHALL use that state to determine whether they operate at full speed, reduced speed, or idle.

#### Scenario: Fully supplied network runs machines at normal speed
- **WHEN** total supply on a connected network meets or exceeds the demand of its active consumers
- **THEN** connected drills and manufacturing machines advance their work at their normal configured rates

#### Scenario: Underpowered network reduces machine effectiveness deterministically
- **WHEN** total demand on a connected network exceeds available supply
- **THEN** connected powered consumers enter an underpowered state and their production progress reflects the network's deterministic satisfaction result instead of continuing at full speed

### Requirement: Power state is visible through structure inspection
The game SHALL expose whether a powered structure is disconnected, underpowered, or fully supplied through existing structure-inspection surfaces.

#### Scenario: Player inspects an unpowered machine
- **WHEN** the player opens the detail window for a mining drill, generator, or manufacturing machine that lacks sufficient electrical supply
- **THEN** the inspection UI shows that structure's current power status rather than implying normal operation
