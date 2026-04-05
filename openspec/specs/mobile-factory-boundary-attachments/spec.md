# mobile-factory-boundary-attachments Specification

## Purpose
TBD - created by archiving change redesign-mobile-factory-world-ports. Update Purpose after archive.
## Requirements
### Requirement: Mobile factories use buildable boundary attachments for world interaction
The game SHALL represent mobile factory world-interaction ports as buildable boundary attachments instead of fixed hardcoded endpoints.

#### Scenario: Player places an output attachment on a valid boundary mount
- **WHEN** the player selects an output attachment in the mobile factory editor, rotates it, and clicks a valid boundary mount
- **THEN** the attachment is placed as part of the factory layout and records its interaction direction, mount position, and deployment-facing geometry

#### Scenario: Invalid boundary attachment placement is rejected
- **WHEN** the player tries to place a boundary attachment whose required interior, boundary, or exterior stencil cells would violate the factory's placement rules
- **THEN** the editor shows the placement as invalid and does not place the attachment

### Requirement: Boundary attachments define cross-boundary grid shapes
Each boundary attachment SHALL define a rotatable shape that includes cells on the factory interior side and cells on the world-facing side of the hull boundary.

#### Scenario: Preview shows inside and outside shape requirements
- **WHEN** the player previews a boundary attachment in the editor or in deployment-related overlays
- **THEN** the game visualizes which cells belong inside the factory, which cells lie on the boundary, and which cells must project outward toward the world

### Requirement: Boundary attachments support directional item exchange
The game SHALL support both outbound and inbound item attachments using the same boundary-attachment system.

#### Scenario: Output attachment sends items to the world when active
- **WHEN** a deployed mobile factory has an active output attachment connected to a valid world-side route
- **THEN** items can leave the factory interior through that attachment and enter the world-side logistics line

#### Scenario: Input attachment receives items from the world when active
- **WHEN** a deployed mobile factory has an active input attachment connected to a valid world-side route carrying items inward
- **THEN** items can enter the factory interior through that attachment and continue into the internal logistics layout

### Requirement: Boundary attachments can be expanded across multiple mounts
The game SHALL allow a mobile factory to install more than one boundary attachment so the factory's external interaction surface can be expanded over time.

#### Scenario: Additional attachment adds another active interaction point
- **WHEN** the player installs an additional valid boundary attachment on a different mount and deploys the factory
- **THEN** the deployed factory activates both attachments and exposes multiple independent world interaction points according to their configured directions

### Requirement: Active boundary attachments show continuous world connectors
The game SHALL render each active boundary attachment as a continuous structure that visibly extends from the attachment body to the world-side interaction cell.

#### Scenario: Deployment creates a visible connector from hull to world cell
- **WHEN** a mobile factory deploys with an active boundary attachment
- **THEN** the world presentation shows a connector or stem extending from the attachment on the factory model to the world-side target cell instead of only showing an isolated ground marker

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
