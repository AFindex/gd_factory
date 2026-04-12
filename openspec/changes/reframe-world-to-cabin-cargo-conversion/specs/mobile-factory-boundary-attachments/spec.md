## MODIFIED Requirements

### Requirement: Boundary attachments support directional item exchange
The game SHALL support both outbound and inbound item attachments using the same boundary-attachment system, but those attachments MUST exchange world-standard payloads only with dedicated conversion or staging structures instead of implying that world payloads flow directly onto ordinary cabin rails.

#### Scenario: Input attachment hands world cargo to a conversion entry
- **WHEN** a deployed mobile factory has an active input attachment connected to a valid world-side route carrying cargo inward
- **THEN** the visible and logical handoff enters the interior through a world-payload-capable conversion or staging structure rather than appearing as a large world payload riding directly along a cabin feed rail

#### Scenario: Output attachment exports completed world cargo from a conversion exit
- **WHEN** a deployed mobile factory has an active output attachment connected to a valid world-side route
- **THEN** the visible and logical handoff leaves from a world-payload-capable conversion or staging structure and emits a world-standard payload instead of exposing cabin carriers directly to the world route

### Requirement: Active boundary attachments show continuous world connectors
The game SHALL render each active boundary attachment with type-appropriate world geometry that communicates large-payload transfer across the hull boundary: standard cargo ports keep their connector from the hull to the world-side interaction cell, and that connector MUST read as a world-payload handoff path into a conversion bay rather than as a direct cabin rail continuation.

#### Scenario: Cargo connector reads as a large-payload handoff
- **WHEN** a mobile factory deploys with an active non-mining cargo attachment
- **THEN** the world presentation shows a connector or stem that visually bridges the hull boundary to a world-cargo handoff point and does not imply that the same large payload continues unchanged onto the interior feed rail

#### Scenario: Mining deployment still avoids unrelated relay props
- **WHEN** a mobile factory deploys with a mining input attachment
- **THEN** the world presentation continues to derive the mining-side presentation from the deployed mining stakes themselves while keeping any interior cargo handoff semantics consistent with world-payload staging at the hull boundary

## ADDED Requirements

### Requirement: Boundary previews teach the world-to-cabin cargo conversion boundary
The game SHALL use attachment previews and state labels to teach that boundary attachments are large-payload exchange points between world logistics and cabin conversion structures.

#### Scenario: Preview distinguishes world handoff from cabin rail flow
- **WHEN** the player previews or inspects a cargo boundary attachment in the editor or deployment overlay
- **THEN** the preview indicates the world-facing payload path and the interior conversion/staging entry separately instead of showing one uninterrupted rail carrying the same payload shape through both spaces
