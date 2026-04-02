## 1. Combat Foundations

- [x] 1.1 Extend shared factory data and simulation primitives with item typing, structure health/damage state, and deterministic combat update hooks.
- [x] 1.2 Add shared destruction/removal handling so damaged world structures cleanly leave the grid, simulation, and visuals when their health is depleted.
- [x] 1.3 Add reusable world-space combat feedback helpers for structure health bars, under-attack state, and combat inspection text.

## 2. Defensive Buildings And Ammo Flow

- [x] 2.1 Add the defensive build prototypes, labels, factory registration entries, and placeholder visuals for a wall, an ammunition producer, and an ammo-fed turret.
- [x] 2.2 Implement ammunition production, transport compatibility, turret reload behavior, and turret-side ammo buffering using the existing logistics network.
- [x] 2.3 Update `FactoryDemo` build selection, HUD copy, and structure inspection so the new defensive buildings can be placed, observed, and debugged in the sandbox.

## 3. Hostile Pressure Layer

- [x] 3.1 Implement lightweight enemy actor scripts and combat data for at least one melee hostile and one ranged hostile.
- [x] 3.2 Add deterministic lane or waypoint-based spawning and targeting rules so enemies can enter the sandbox, advance toward factory assets, and attack valid structures.
- [x] 3.3 Wire turret targeting and damage resolution against hostile units, including enemy death or cleanup behavior once a target is defeated.

## 4. Factory Sandbox Use Cases

- [x] 4.1 Expand the scripted `Factory Sandbox` starter layout with an ammo-fed success lane, a wall-backed choke point, and an intentionally ammo-starved breach case.
- [x] 4.2 Add any sandbox-side combat counters or notes needed to make enemy pressure, turret ammo state, and structure losses legible during normal play.
- [x] 4.3 Keep the original non-combat logistics showcase clusters playable while isolating new combat lanes so they remain useful as regression beds.

## 5. Verification

- [x] 5.1 Add or update smoke/regression coverage for turret ammo consumption, structure health loss and destruction, and enemy pressure on authored sandbox lanes.
- [x] 5.2 Run the relevant validation path for the Factory Sandbox and confirm the default scene boots with active logistics, visible combat feedback, and at least one working defense scenario plus one breach scenario.
