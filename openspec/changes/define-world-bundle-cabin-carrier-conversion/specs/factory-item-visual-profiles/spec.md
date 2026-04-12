## MODIFIED Requirements

### Requirement: Factory item kinds expose configurable transport visual profiles
The game SHALL let each factory item kind resolve a transport visual profile that remains separate from transport simulation and that also respects cargo-standard presentation rules: world bundles SHALL resolve to a 3D textured box presentation with shadow casting disabled and size-tier-aware proportions, while cabin carriers SHALL continue to resolve to lightweight 2D or billboard-style presentations for cabin-belt readability.

#### Scenario: World bundle resolves to a 3D textured box without shadows
- **WHEN** a world cargo item is resolved as a world bundle for world routes, boundary handoff, or heavy conversion staging
- **THEN** its transport visual profile renders that bundle as a 3D textured box-like payload, sized for its bundle tier, with shadow casting disabled instead of using a billboarded 2D sprite

#### Scenario: Cabin carrier remains a lightweight 2D-style payload
- **WHEN** an interior carrier item is resolved for cabin belts, interior staging, or cabin machine feeds
- **THEN** its transport visual profile keeps the lightweight 2D or billboard-style presentation and does not reuse the world bundle's heavy 3D box presentation

## ADDED Requirements

### Requirement: Visual profiles preserve the same resource identity across world bundles and cabin carriers
The game SHALL let world bundles and cabin carriers for the same underlying resource keep shared identifying colors, labels, or iconography while still using different presentation standards for heavy world cargo versus lightweight cabin logistics.

#### Scenario: Same resource keeps recognizable identity across standards
- **WHEN** a resource appears once as a world bundle and again as an unpacked cabin carrier
- **THEN** the two visuals share recognizable identity cues such as accent color, label family, or template marking while remaining clearly different in form factor and presentation standard
