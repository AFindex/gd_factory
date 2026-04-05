## ADDED Requirements

### Requirement: Mobile factory attachments can deploy owned child structures into the world
The game SHALL allow a deployed mobile-factory attachment to materialize world-side child structures that belong to that attachment and participate in the normal world structure simulation.

#### Scenario: Mining input deployment creates child stakes on eligible cells
- **WHEN** a mining input port is deployed onto a site where one or more projected cells overlap compatible deposits and the port has built stake stock available
- **THEN** the game creates one mining-stake child structure per deployed cell, registers each child in the world structure simulation, and associates each child with the owning mining input port and world cell

#### Scenario: Recall clears world child structures without deleting surviving stock
- **WHEN** the mobile factory recalls from its deployed site or redeploys to another site
- **THEN** the attachment-owned child structures are removed from the world scene and simulation, and the owning mining input port keeps its surviving built-stake stock for later deployments

### Requirement: Child structures keep independent durability and owner feedback
The game SHALL give each mobile-factory child structure its own health and destruction path, and the owning attachment SHALL react when a child structure is lost.

#### Scenario: Child stake can be attacked independently
- **WHEN** enemies attack a deployed mining stake child structure
- **THEN** that child structure tracks its own health, shows normal structure damage feedback, and can be destroyed without destroying the parent mining input port

#### Scenario: Destroyed child stake updates the owning port
- **WHEN** a mining stake child structure is destroyed
- **THEN** the owning mining input port removes that child from its active deployed set, updates its mining deployment counts, and requires a rebuilt stake before a later deployment can refill the lost slot
