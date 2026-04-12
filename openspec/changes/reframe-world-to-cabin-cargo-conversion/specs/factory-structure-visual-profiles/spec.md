## MODIFIED Requirements

### Requirement: Factory structures expose configurable visual profiles
The game SHALL let each factory structure kind resolve a visual profile that is separate from its simulation logic and that can describe a procedural code-built presentation, an authored 3D scene/model presentation, or a scene presentation that includes animation-capable nodes. Those profiles MUST also be able to express which industrial standard the structure belongs to, including world-scale transfer structures, cabin-scale rail structures, and conversion chambers that handle world payloads inside the cabin envelope.

#### Scenario: Cabin structures resolve a cabin-specific presentation family
- **WHEN** a structure kind is instantiated inside a mobile-factory interior using a cabin-standard role
- **THEN** its visual profile resolves to a cabin-specific presentation family such as embedded rail, service module, staging rack, or hull interface instead of reusing the world-structure silhouette at a smaller scale

#### Scenario: Conversion chamber resolves a world-payload-capable presentation
- **WHEN** an unpacker, packer, or related conversion structure is instantiated
- **THEN** its visual profile resolves a presentation that visibly accommodates a world-scale payload within the structure rather than reading as a generic small logistics node

#### Scenario: Authored or procedural overrides remain valid
- **WHEN** a structure kind defines a loadable authored scene or a procedural visual builder for its assigned industrial standard
- **THEN** the structure instantiates that presentation through the same visual-profile pipeline without requiring a separate simulation class

### Requirement: Structure visual updates consume runtime presentation state without altering simulation
The game SHALL drive structure presentation from a dedicated runtime visual-state update path so chamber occupancy, staged payload visibility, and working-state animation do not alter recipe progression, transfer rules, power resolution, or placement behavior.

#### Scenario: Visible chamber load does not change conversion timing
- **WHEN** a conversion structure switches from idle to actively handling a world payload and updates its visible chamber contents
- **THEN** the structure's transfer cadence, recipe timing, and input/output ownership remain governed by the deterministic simulation rules

#### Scenario: Staging visuals do not bypass machine gating
- **WHEN** a packer, unpacker, or staging buffer is underpowered, blocked, or waiting on routing while still showing some payload state
- **THEN** the visible payload presentation does not grant extra throughput or bypass the normal gating behavior

## ADDED Requirements

### Requirement: Cabin conversion and staging structures visibly communicate large-payload handling
The game SHALL make unpackers, packers, buffers, and adjacent cabin modules communicate that world payloads are large enough to require dedicated handling volume instead of fitting onto ordinary cabin rails.

#### Scenario: Buffer reads as a staging bay instead of a generic processor
- **WHEN** a transfer buffer or related staging structure is shown in a cabin layout
- **THEN** its presentation reads as a cradle, rack, dock, or waiting bay for large-payload conversion pacing rather than as a small all-purpose machine

#### Scenario: Cabin module scale remains credible next to world payloads
- **WHEN** a world payload is shown entering, leaving, or being processed by a cabin-side conversion or production module
- **THEN** the surrounding module presentation reads as approximately world-payload-sized rather than as a tiny factory prop next to an oversized cargo model

#### Scenario: Structure presentation accommodates world payloads without shrinking them
- **WHEN** a structure presentation shows a world payload inside an interior-side conversion or staging structure
- **THEN** the structure uses chamber depth, open handling volume, or staging geometry to accommodate that payload instead of shrinking the payload to cabin scale
