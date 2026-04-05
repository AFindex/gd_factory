## ADDED Requirements

### Requirement: Mining input attachments deploy stakes only on supported world cells
The game SHALL deploy mining-input stakes only on projected world cells that both overlap compatible deposits and have available built-stake stock on the owning port.

#### Scenario: Mixed projection creates stakes only on deposit-backed cells
- **WHEN** a mining input attachment projects across a mix of compatible deposit cells and ordinary empty cells
- **THEN** the deployed attachment creates mining stakes only on the compatible deposit cells and leaves the remaining projected cells without stake structures

#### Scenario: Limited stake stock yields partial deployment
- **WHEN** a mining input port has fewer built stakes than the number of compatible deposit cells in its current projection
- **THEN** the attachment deploys only up to its available built-stake stock and reports a partial deployed count instead of creating fake extra stakes

## MODIFIED Requirements

### Requirement: Active boundary attachments show continuous world connectors
The game SHALL render each active boundary attachment with type-appropriate world geometry: standard ports keep their continuous connector from the hull to the world-side interaction cell, while mining input ports render only the deployed mining stakes and any connector geometry anchored to those stakes, without a standalone relay or transfer-station payload model.

#### Scenario: Deployment creates a visible connector from hull to world cell
- **WHEN** a mobile factory deploys with an active non-mining boundary attachment
- **THEN** the world presentation shows a connector or stem extending from the attachment on the factory model to the world-side target cell instead of only showing an isolated ground marker

#### Scenario: Mining input deployment omits standalone relay model
- **WHEN** a mobile factory deploys with a mining input attachment
- **THEN** the world presentation does not create a separate relay / transfer-station payload model and instead derives the mining-side presentation from the deployed mining stakes themselves
