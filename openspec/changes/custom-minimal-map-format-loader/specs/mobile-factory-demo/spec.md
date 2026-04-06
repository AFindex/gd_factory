## ADDED Requirements

### Requirement: Focused mobile demo authored content loads from factory map data
The game SHALL source the focused mobile-factory demo's authored world layout and authored interior layout from the custom factory map format through the shared runtime loader instead of embedding the full authored payload in the demo controller.

#### Scenario: Focused mobile demo reconstructs world and interior maps
- **WHEN** the focused mobile-factory demo initializes
- **THEN** it reconstructs its authored deployment world content and authored interior production layout from factory map files through the shared loader

#### Scenario: Map-driven mobile demo preserves current interaction flows
- **WHEN** the focused mobile-factory demo is running with map-driven authored content
- **THEN** deployment, recall, editor interactions, blueprint flows, player HUD behavior, and authored logistics behavior remain functionally equivalent to the current focused demo
