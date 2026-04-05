## MODIFIED Requirements

### Requirement: Ammo-fed defenses depend on factory logistics
The game SHALL provide ammo-fed defensive structures that consume ammunition items supplied through the factory logistics network, including a large-footprint heavy turret that fires independent projectiles instead of only instant attacks.

#### Scenario: Stocked turret spends ammo to attack enemies
- **WHEN** an ammo-fed turret has valid ammunition buffered and an enemy enters its firing range
- **THEN** the turret consumes ammunition over time and applies damage to enemy targets according to its attack cadence

#### Scenario: Stocked heavy turret launches a projectile attack
- **WHEN** a stocked heavy turret has valid ammunition buffered and a valid enemy enters range
- **THEN** the turret consumes compatible ammunition, spawns an independent projectile from the turret toward the chosen target, and only deals damage when that projectile resolves a hit

#### Scenario: Empty turret stops firing until resupplied
- **WHEN** an ammo-fed turret has no valid ammunition remaining in its internal buffer
- **THEN** the turret stops attacking and does not resume firing until compatible ammunition reaches it through the sandbox logistics rules

### Requirement: Defensive roster includes both attrition and support pieces
The game SHALL provide a small tower-defense building set suitable for the factory sandbox, including a perimeter blocker, an ammunition-supply building, a standard ammo-fed turret, and a large-footprint heavy turret variant.

#### Scenario: Wall-like structure buys time without consuming ammo
- **WHEN** a hostile wave reaches a defended choke point
- **THEN** the perimeter blocker absorbs attacks or delays enemy movement without requiring ammunition of its own

#### Scenario: Ammunition producer feeds the defense network
- **WHEN** the sandbox contains a connected ammunition-producing structure, logistics path, and ammo-fed turret
- **THEN** ammunition items can be produced, transported, buffered, and accepted by the turret through the same deterministic logistics layer used elsewhere in the demo

#### Scenario: Heavy turret occupies multiple cells in a defense lane
- **WHEN** the player previews or places the heavy turret in the sandbox
- **THEN** the turret reserves its full footprint, shows its larger attack presence clearly, and participates in the same defense network as other ammo-fed structures

### Requirement: Factory Sandbox ships with authored tower-defense case studies
The game SHALL expand the default Factory Sandbox scene with authored tower-defense use cases that demonstrate both successful defense and logistics failure, including at least one lane that uses the heavy turret footprint.

#### Scenario: Stocked defense lane survives expected early pressure
- **WHEN** the default Factory Sandbox scene starts
- **THEN** at least one authored lane demonstrates a turret supplied by factory logistics holding back early enemies behind perimeter defenses

#### Scenario: Large-footprint defense lane showcases the heavy turret
- **WHEN** the default Factory Sandbox scene starts
- **THEN** at least one authored lane includes a heavy turret placed through the new multi-cell footprint rules so the player can observe projectile fire and occupied-space constraints

#### Scenario: Ammo-starved lane demonstrates breach behavior
- **WHEN** the default Factory Sandbox scene starts or runs for a short interval
- **THEN** at least one authored lane demonstrates that a defense which is not resupplied runs dry and allows enemy pressure to damage or breach structures

## ADDED Requirements

### Requirement: Heavy-turret projectiles resolve as independent combat entities
The game SHALL simulate heavy-turret shots as independent projectile entities with readable travel and deterministic hit or expiry outcomes.

#### Scenario: Projectile hits the first valid target on its path
- **WHEN** a heavy turret fires at a valid enemy and the projectile reaches that enemy before expiring
- **THEN** the projectile deals its configured damage once, shows readable hit feedback, and is removed from the active combat simulation

#### Scenario: Projectile expires without a hit
- **WHEN** a heavy-turret projectile loses its valid target or reaches its maximum travel without a hit
- **THEN** the projectile expires cleanly without applying damage and no orphaned combat entity remains active
