## ADDED Requirements

### Requirement: Mobile factory mining inputs extract only through surviving deployed stakes
The game SHALL allow a mining input port to mine resources only from compatible deposit cells that currently host a deployed, surviving mining stake child structure.

#### Scenario: Deployed mining stake enables extraction
- **WHEN** a mining input port is deployed with at least one surviving mining stake on compatible deposit cells
- **THEN** the port enters an active mining state and can feed the corresponding raw resource into the mobile factory's logistics flow

#### Scenario: Missing or destroyed stake stops that cell from contributing
- **WHEN** a compatible deposit cell in the mining projection has no deployed stake or its stake has been destroyed
- **THEN** that cell does not contribute mining output until the port has a replacement built stake and deploys it again

#### Scenario: Off-deposit projected cells never mine
- **WHEN** a mining input projection includes projected cells that do not overlap a compatible deposit
- **THEN** those cells do not spawn mining stakes and do not produce resources
