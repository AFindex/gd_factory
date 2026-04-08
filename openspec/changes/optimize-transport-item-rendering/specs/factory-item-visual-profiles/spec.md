## ADDED Requirements

### Requirement: Item visual profiles expose deterministic transport render descriptors and fallbacks
The game SHALL let each factory item kind resolve a transport render descriptor set that identifies its primary world presentation, supported shared-batch representation, and deterministic fallback chain for lighter transport rendering tiers.

#### Scenario: Profile resolves both primary and fallback transport descriptors
- **WHEN** a factory item kind is queried for moving transport presentation
- **THEN** its visual profile resolves a stable primary descriptor plus any supported fallback descriptors needed for batched or degraded transport rendering

#### Scenario: Profile without authored extras still resolves a shared-batch fallback
- **WHEN** an item kind only defines a tint-level placeholder profile without authored texture or model extras
- **THEN** the transport visual system still resolves a valid shared-batch descriptor for that item kind instead of treating it as an unconfigured special case

### Requirement: Visual-profile fallback remains deterministic across render tiers
The game SHALL keep item-profile fallback selection deterministic so the same item kind resolves the same transport-render tier behavior for the same conditions across frames.

#### Scenario: Same item kind does not oscillate between unrelated fallback descriptors
- **WHEN** two moving items of the same kind occupy the same render tier under the same camera conditions
- **THEN** they resolve the same transport-render descriptor chain instead of choosing unrelated fallback visuals frame to frame

#### Scenario: Tier fallback does not alter logistics semantics
- **WHEN** a moving item kind transitions between higher-fidelity and lighter-weight render tiers
- **THEN** the change only affects its transport presentation and does not alter the underlying movement order, spacing, or delivery outcome
