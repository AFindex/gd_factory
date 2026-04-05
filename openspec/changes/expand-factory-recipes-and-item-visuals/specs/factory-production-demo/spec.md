## MODIFIED Requirements

### Requirement: Demo includes an observable automation loop
The game SHALL include a playable static factory demo whose default startup layout contains multiple authored districts that together form a powered input -> production -> output chain, including at least a branching iron/copper resource ladder, shared intermediate manufacturing, and a final multi-input crafted output, so automation can be observed under sustained throughput, localized congestion, and recipe-state changes instead of only a short linear chain.

#### Scenario: Startup layout exercises branching resource production
- **WHEN** the player starts the default static factory demo
- **THEN** the scene already contains authored mining, smelting, intermediate assembly, and final-output segments that demonstrate at least two raw-resource branches converging into downstream production without requiring manual building

#### Scenario: Multiple resource branches reach a shared crafted output
- **WHEN** the demo scene runs with its authored starter layout
- **THEN** items extracted from more than one raw-resource family flow through powered manufacturing and contribute to at least one shared crafted output that can be delivered in the scene

## ADDED Requirements

### Requirement: Demo showcases differentiated logistics item visuals
The game SHALL render moving logistics items in the static sandbox using their configured visual profiles so different item kinds are readable during normal play.

#### Scenario: Existing items receive immediate color differentiation
- **WHEN** the starter layout is running with the current baseline item set
- **THEN** coal, ore, plates, parts, and ammo payloads each appear with distinct first-pass colors while moving through the logistics network

#### Scenario: Configured billboard or model items appear in the authored layout
- **WHEN** the starter layout includes an item kind configured with a billboard sprite or 3D model transport profile
- **THEN** that moving item appears in the authored logistics line using its configured representation instead of the generic placeholder cube

### Requirement: Demo verification covers richer chain readability
The game SHALL include authored use cases or smoke coverage that verify both the expanded production ladder and the visibility of differentiated moving items in the default sandbox.

#### Scenario: Smoke flow validates expanded crafted output chain
- **WHEN** the static sandbox smoke test runs against the default starter layout
- **THEN** it verifies that at least one multi-stage crafted output depending on more than one resource branch is produced and delivered without manual intervention

#### Scenario: Visual readability checks do not require identical meshes
- **WHEN** smoke or regression verification inspects the authored starter layout after item-visual profiles are enabled
- **THEN** it confirms that moving payloads remain visible and distinguishable by profile configuration without depending on every item using the same geometry
