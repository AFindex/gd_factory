## ADDED Requirements

### Requirement: Mining input attachments expose optional mineral coverage
Mining input boundary attachments SHALL evaluate projected world cells individually so deposit coverage is optional for deployment but required for active mining visuals and resource intake.

#### Scenario: Preview shows mining stakes only on deposit-backed cells
- **WHEN** the player previews a mobile factory deployment whose mining-input attachment projects across a mix of deposit-backed cells and ordinary empty cells
- **THEN** the preview shows mining stakes only on the deposit-backed deployable cells and omits stake meshes from the remaining projected cells

#### Scenario: Off-deposit mining deployment stays inactive
- **WHEN** a deployed mobile factory has a mining-input attachment whose projected cells cover no deposits
- **THEN** the factory remains deployed successfully but that attachment does not show active mining stakes or import mined resources until a future deployment overlaps deposits

#### Scenario: Fully connected mining deployment reports the success state
- **WHEN** the player previews or confirms a deployment whose mining-input projection covers at least one valid deposit cell
- **THEN** the mining input attachment is treated as connected, contributes the fully valid deploy state, and renders its active mining presentation on the covered cells
