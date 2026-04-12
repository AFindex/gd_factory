## MODIFIED Requirements

### Requirement: Factory structures expose configurable visual profiles
The game SHALL let each factory structure kind resolve a visual profile that stays separate from simulation logic and that can also respond to authored cargo-size tiers or chamber configurations, so unpackers, packers, and related heavy handoff structures can present different chamber shells, body proportions, and occupied-volume reads for different world bundle classes.

#### Scenario: Conversion chamber resolves a size-tier-aware presentation
- **WHEN** an unpacker or packer is configured to handle a specific world bundle size tier
- **THEN** its structure visual profile resolves the matching chamber-shell proportions and heavy handling presentation instead of reusing a one-size-fits-all chamber silhouette

#### Scenario: Missing authored chamber variant falls back to deterministic procedural geometry
- **WHEN** a conversion chamber requests a higher-fidelity size-tier-specific presentation that is unavailable
- **THEN** the structure still renders through a deterministic procedural heavy-chamber presentation instead of disappearing or reverting to a tiny generic processor

## ADDED Requirements

### Requirement: Conversion chambers show articulated heavy-payload processing states
The game SHALL let unpackers and packers expose runtime visual state that shows a visible heavy payload at staging, in-process, and dispatch positions together with articulated handling motion such as mechanical-arm, clamp, or slide-rail behavior.

#### Scenario: Unpacker shows an inbound heavy payload being worked
- **WHEN** an unpacker is actively processing a world bundle
- **THEN** its presentation shows the heavy bundle at a visible handling anchor and animates the chamber's handling apparatus or processing shell to communicate that unpacking work is happening

#### Scenario: Packer shows a rebuilt outbound heavy payload
- **WHEN** a packer finishes a bundle template and enters dispatch-ready state
- **THEN** its presentation shows a visible outbound heavy bundle at the dispatch side instead of implying that cabin carriers directly became a world-route payload on the cabin belt
