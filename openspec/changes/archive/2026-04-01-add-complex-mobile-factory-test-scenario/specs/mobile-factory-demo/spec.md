## MODIFIED Requirements

### Requirement: Mobile factory concept ships in a dedicated demo scene
The game SHALL provide a focused dedicated mobile-factory demo scene and a separate large-scale mobile-factory test scenario without replacing the existing static factory demo scene.

#### Scenario: Existing factory demo remains available
- **WHEN** the project content is inspected after the change is implemented
- **THEN** the original static factory demo scene still exists and remains available as a separate experience

#### Scenario: Focused mobile demo can be opened independently
- **WHEN** the player or developer opens the focused mobile-factory demo scene directly
- **THEN** the scene loads the controls, world content, split-view editing UI, and overlays needed to demonstrate core mobile-factory deployment behavior without requiring the static demo to be modified first

#### Scenario: Large-scale mobile factory test scenario can be opened independently
- **WHEN** the player or developer opens the large-scale mobile factory test scenario directly
- **THEN** the project loads the larger world map, multi-factory activity, and observation content for regression-style testing without replacing the focused mobile-factory demo
