# factory-item-visual-profiles Specification

## Purpose
Define configurable visual profiles for moving factory items so logistics payloads remain readable while preserving deterministic transport behavior.

## Requirements
### Requirement: Factory item kinds expose configurable transport visual profiles
The game SHALL let each factory item kind resolve a transport visual profile that declares at least a readable tint and may additionally declare a texture, a 3D model, or a billboarded 2D sprite fallback for world-space item presentation.

#### Scenario: Item profile provides a tinted placeholder
- **WHEN** an item kind only defines a tint and no texture, model, or sprite override
- **THEN** moving transport visuals for that item render with a distinct tinted placeholder instead of the shared default cube color

#### Scenario: Item profile provides authored visual assets
- **WHEN** an item kind defines a texture, a 3D model, or a billboard sprite in its visual profile
- **THEN** transport rendering uses those configured visual assets for that item kind instead of treating all items as identical payloads

### Requirement: Transport item rendering supports deterministic visual fallback
The game SHALL resolve transport item visuals through a deterministic fallback chain so missing higher-fidelity assets do not block item rendering.

#### Scenario: Missing model falls back to billboard sprite
- **WHEN** an item profile prefers a 3D model but the model is unavailable while a billboard sprite is configured
- **THEN** the moving item renders with the configured billboard sprite and remains visible on belts or other transport paths

#### Scenario: Missing sprite and model falls back to placeholder
- **WHEN** an item profile has no usable model or billboard sprite for a moving item
- **THEN** the game still renders that item using its configured tint and placeholder geometry rather than hiding the payload

### Requirement: Visual presentation does not alter transport simulation
The game SHALL keep visual-profile selection separate from deterministic logistics behavior so rendering mode changes do not affect item movement, ordering, or transfer rules.

#### Scenario: Mixed visual modes preserve the same transport outcome
- **WHEN** a belt lane carries a mix of placeholder, billboarded, and modeled item kinds through the same transport rules
- **THEN** item spacing, movement order, and delivery outcomes remain governed by the existing logistics simulation rather than by the chosen visual representation

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
