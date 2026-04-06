## MODIFIED Requirements

### Requirement: Power producers, relays, and consumers form connected networks
The game SHALL support power-producing, power-relay, and power-consuming structures in the richer world sandbox, and connected power structures SHALL be treated as one deterministic electrical network even when authored districts are separated by longer mining, receiving, or defense lanes.

#### Scenario: Generator and relays energize distant authored districts
- **WHEN** a generator is connected to a mining, manufacturing, receiving, or defense-support district through valid relay coverage
- **THEN** the distant consumer district joins the same power network and can receive electrical supply from that generator

#### Scenario: Isolated maintenance district remains disconnected
- **WHEN** a mining, manufacturing, or support district has no connected generator path after a relay break or missing maintenance link
- **THEN** the affected structures remain unpowered and do not behave as if electricity were available

### Requirement: Fuel-fed generators convert delivered items into electrical supply
The game SHALL allow a generator to consume compatible delivered fuel items and convert them into electrical output for its connected network, and authored demo cases SHALL source that fuel through normal sandbox logistics instead of direct placeholder spawning on the main path.

#### Scenario: Fueled generator powers the network from authored logistics
- **WHEN** a generator receives compatible fuel through the sandbox's real logistics chain and a connected consumer requests power
- **THEN** the generator contributes electrical supply that can satisfy connected machine demand

#### Scenario: Empty generator stops supplying power until the fuel chain recovers
- **WHEN** a generator's authored fuel line is interrupted and no valid fuel remains buffered
- **THEN** it contributes no electrical supply until compatible fuel is delivered again through the sandbox logistics rules

## ADDED Requirements

### Requirement: Sandbox includes authored power-maintenance case studies
The game SHALL include authored sandbox cases that make power maintenance and recovery observable, including at least one case where production slows or stops under a broken fuel or relay chain and later resumes after that support path is restored.

#### Scenario: Broken support path causes a visible production outage
- **WHEN** an authored power-maintenance case loses its required fuel delivery or relay coverage
- **THEN** the dependent district visibly enters an unpowered or underpowered state and its throughput drops according to the deterministic power rules

#### Scenario: Restored support path restarts the affected district
- **WHEN** the authored maintenance case regains valid fuel delivery or relay coverage
- **THEN** the affected district resumes its normal powered throughput without requiring manual reconstruction of the machines
