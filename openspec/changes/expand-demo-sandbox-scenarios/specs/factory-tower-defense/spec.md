## MODIFIED Requirements

### Requirement: Ammo-fed defenses depend on factory logistics
The game SHALL provide ammo-fed defensive structures that consume ammunition items supplied through the factory logistics network, and the authored demo lanes SHALL obtain that ammunition from real mining and manufacturing branches instead of producer shortcuts or permanently injected placeholder cargo.

#### Scenario: Stocked turret spends ammo produced by the sandbox economy
- **WHEN** an ammo-fed turret has valid ammunition buffered and an enemy enters its firing range
- **THEN** the turret consumes ammunition that originated from the authored sandbox production chain and attacks according to its normal cadence

#### Scenario: Broken upstream chain starves the turret
- **WHEN** the upstream mining, manufacturing, or logistics chain feeding an ammo-fed turret is interrupted
- **THEN** the turret eventually exhausts its buffered ammunition and stops firing until the real supply chain recovers

### Requirement: Factory Sandbox ships with authored tower-defense case studies
The game SHALL expand the default Factory Sandbox scene with authored tower-defense use cases that demonstrate both successful defense and logistics failure using real production, power, and resupply loops, including at least one lane that uses the heavy turret footprint.

#### Scenario: Stocked defense lane survives expected early pressure through real resupply
- **WHEN** the default Factory Sandbox scene starts
- **THEN** at least one authored lane demonstrates a turret supplied by the sandbox's real mining and manufacturing chain holding back early enemies behind perimeter defenses

#### Scenario: Ammo-starved lane demonstrates breach after real supply interruption
- **WHEN** the default Factory Sandbox scene starts or runs for a short interval
- **THEN** at least one authored lane demonstrates that a defense whose real upstream resupply path fails runs dry and allows enemy pressure to damage or breach structures
