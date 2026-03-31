# mobile-factory-demo Specification

## Purpose
TBD - created by archiving change add-mobile-factory-support. Update Purpose after archive.
## Requirements
### Requirement: Mobile factory concept ships in a dedicated demo scene
The game SHALL provide a dedicated demo scene for the mobile-factory concept without replacing the existing static factory demo scene.

#### Scenario: Existing factory demo remains available
- **WHEN** the project content is inspected after the change is implemented
- **THEN** the original static factory demo scene still exists and remains available as a separate experience

#### Scenario: Mobile demo can be opened independently
- **WHEN** the player or developer opens the mobile-factory demo scene directly
- **THEN** the scene loads the controls, world content, split-view editing UI, and overlays needed to demonstrate mobile-factory deployment behavior without requiring the static demo to be modified first

### Requirement: Mobile demo demonstrates deploy-recall-redeploy gameplay
The game SHALL use the dedicated mobile-factory demo scene to demonstrate the core mobile-factory loop of deployment, world connection, recall, and redeployment.

#### Scenario: Mobile factory drives an external loop after deployment
- **WHEN** the mobile factory is deployed onto a valid footprint in the dedicated mobile demo
- **THEN** its active port connections feed an observable world-side logistics loop

#### Scenario: Redeployment restores the concept loop
- **WHEN** the mobile factory is recalled and redeployed to another valid location in the dedicated mobile demo
- **THEN** the player can restore the mobile-factory concept loop without recreating the factory's internal setup

