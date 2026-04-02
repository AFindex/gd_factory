# factory-tower-defense Specification

## Purpose
TBD - created by archiving change add-factory-tower-defense-layer. Update Purpose after archive.
## Requirements
### Requirement: World structures participate in combat with visible durability
The game SHALL treat world-placeable factory structures as damageable entities with persistent health, visible damage feedback, and deterministic destruction when their health is depleted.

#### Scenario: Building health decreases under enemy attack
- **WHEN** an enemy attacks a structure in the factory sandbox
- **THEN** the target structure loses health, exposes visible damage feedback, and keeps its updated health state until repaired or destroyed

#### Scenario: Destroyed building leaves the active simulation
- **WHEN** a structure's health reaches zero
- **THEN** the structure is removed or disabled in a way that stops its logistics or combat behavior and frees any occupied world state that should no longer remain reserved

### Requirement: Ammo-fed defenses depend on factory logistics
The game SHALL provide at least one defensive structure that consumes ammunition items supplied through the factory logistics network instead of firing infinitely.

#### Scenario: Stocked turret spends ammo to attack enemies
- **WHEN** an ammo-fed turret has valid ammunition buffered and an enemy enters its firing range
- **THEN** the turret consumes ammunition over time and applies damage to enemy targets according to its attack cadence

#### Scenario: Empty turret stops firing until resupplied
- **WHEN** an ammo-fed turret has no valid ammunition remaining in its internal buffer
- **THEN** the turret stops attacking and does not resume firing until compatible ammunition reaches it through the sandbox logistics rules

### Requirement: Defensive roster includes both attrition and support pieces
The game SHALL provide a small tower-defense building set suitable for the factory sandbox, including a perimeter blocker and an ammunition-supply building in addition to the ammo-fed turret.

#### Scenario: Wall-like structure buys time without consuming ammo
- **WHEN** a hostile wave reaches a defended choke point
- **THEN** the perimeter blocker absorbs attacks or delays enemy movement without requiring ammunition of its own

#### Scenario: Ammunition producer feeds the defense network
- **WHEN** the sandbox contains a connected ammunition-producing structure, logistics path, and ammo-fed turret
- **THEN** ammunition items can be produced, transported, buffered, and accepted by the turret through the same deterministic logistics layer used elsewhere in the demo

### Requirement: Hostile units pressure the sandbox through readable attack patterns
The game SHALL include simple enemy units that enter the factory sandbox, advance through authored pressure lanes, and attack valid structures using distinct behaviors.

#### Scenario: Melee enemy must close distance to damage defenses
- **WHEN** a melee hostile reaches a defended lane
- **THEN** it advances toward the nearest valid blocking or target structure and only deals damage after entering melee attack range

#### Scenario: Ranged enemy pressures structures from stand-off range
- **WHEN** a ranged hostile reaches an authored firing position or attack threshold
- **THEN** it can attack a valid structure without needing to collide directly with the target

### Requirement: Combat readability exposes health and attack state in-world
The game SHALL make combat status legible in the sandbox with in-world health bars or equivalent overlays plus visible under-attack state for affected structures.

#### Scenario: Damaged or threatened building shows health information
- **WHEN** a structure is hovered, selected, damaged, or actively under attack
- **THEN** the player can see that structure's current health through an in-world bar or equivalent readable combat overlay

#### Scenario: Under-attack state is visually distinguishable
- **WHEN** a structure is receiving enemy pressure
- **THEN** the structure shows a distinct attacked-state cue such as flashing, tinting, or another visible warning separate from its idle state

### Requirement: Factory Sandbox ships with authored tower-defense case studies
The game SHALL expand the default Factory Sandbox scene with authored tower-defense use cases that demonstrate both successful defense and logistics failure.

#### Scenario: Stocked defense lane survives expected early pressure
- **WHEN** the default Factory Sandbox scene starts
- **THEN** at least one authored lane demonstrates a turret supplied by factory logistics holding back early enemies behind perimeter defenses

#### Scenario: Ammo-starved lane demonstrates breach behavior
- **WHEN** the default Factory Sandbox scene starts or runs for a short interval
- **THEN** at least one authored lane demonstrates that a defense which is not resupplied runs dry and allows enemy pressure to damage or breach structures

