## MODIFIED Requirements

### Requirement: Active boundary attachments show continuous world connectors
The game SHALL render each active boundary attachment with type-appropriate world geometry and a cabin-side hull-interface presentation: standard ports keep their continuous connector from the hull to the world-side interaction cell, while mining input ports render only the deployed mining stakes and any connector geometry anchored to those stakes, without a standalone relay or transfer-station payload model.

#### Scenario: Deployment creates a visible connector from hull to world cell
- **WHEN** a mobile factory deploys with an active non-mining boundary attachment
- **THEN** the world presentation shows a connector or stem extending from the attachment on the factory model to the world-side target cell and the cabin-side presentation reads as a hull adapter, transfer collar, or interface well instead of a copied world port model

#### Scenario: Mining input deployment omits standalone relay model
- **WHEN** a mobile factory deploys with a mining input attachment
- **THEN** the world presentation does not create a separate relay / transfer-station payload model and instead derives the mining-side presentation from the deployed mining stakes themselves while the interior side still reads as a dedicated mining intake interface

## ADDED Requirements

### Requirement: Boundary attachments present as hull-standard cabin interfaces
The game SHALL present boundary attachments inside the mobile factory as hull-standard interfaces with cabin-specific adapter geometry, service hatches, and embedded routing transitions rather than as ordinary internal belts or world facilities placed on the edge.

#### Scenario: Interior-side attachment reads as a hull interface
- **WHEN** the player previews or inspects an input, output, or mining boundary attachment from inside the mobile factory
- **THEN** the attachment presents as a hull-mounted interface with visible transition geometry between the hull boundary and the cabin logistics layer instead of as a generic mini conveyor or outdoor loading structure
