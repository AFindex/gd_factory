## MODIFIED Requirements

### Requirement: Factory item kinds expose configurable transport visual profiles
The game SHALL let each factory item resolve a transport visual profile using both its resource identity and its active cargo form or industrial-standard context, and that profile SHALL be able to declare distinct world and interior carrier silhouettes in addition to readable tint, texture, model, or billboard fallback data.

#### Scenario: Item profile provides a tinted placeholder
- **WHEN** an item kind and cargo form combination only defines a tint-level fallback and no texture, model, or sprite override
- **THEN** moving transport visuals for that specific combination render with a distinct tinted placeholder instead of sharing the same default cube color or carrier silhouette as unrelated world cargo

#### Scenario: Item profile provides authored visual assets
- **WHEN** an item kind and cargo form combination defines a texture, a 3D model, or a billboard sprite in its visual profile
- **THEN** transport rendering uses those configured visual assets for that specific industrial context instead of treating world cargo and cabin cargo as the same payload presentation

## ADDED Requirements

### Requirement: Interior cargo forms use cabin-native carrier families
The game SHALL render interior cargo forms through cabin-native carrier families such as feed cassettes, sealed canisters, maintenance trays, or magazine-like containers instead of reusing world bulk or world packed silhouettes at a smaller scale.

#### Scenario: Interior feed does not reuse world ore or crate silhouettes
- **WHEN** a resource is moving through an interior logistics path in `InteriorFeed` form
- **THEN** its transport presentation resolves to a cabin-native carrier family that reads as installed supply hardware rather than as a scaled-down world ore chunk, pallet, or crate

#### Scenario: Same resource stays recognizable across world and interior carriers
- **WHEN** the same resource identity appears once in a world cargo form and once in an interior cargo form
- **THEN** the two transport visuals still share recognizable color or motif cues for the resource while clearly differing in carrier silhouette and packaging language
