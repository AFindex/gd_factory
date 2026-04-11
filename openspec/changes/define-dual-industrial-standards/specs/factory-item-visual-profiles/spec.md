## ADDED Requirements

### Requirement: Transport visuals resolve by industrial standard and cargo form
The game SHALL let a transport item resolve its moving visual profile using both the item's resource identity and its active cargo form or industrial-standard context so world logistics and interior logistics can render distinct payload silhouettes for the same resource.

#### Scenario: World and interior transport resolve different visuals for the same resource
- **WHEN** the same resource identity appears once in a world cargo form and once in an interior cargo form
- **THEN** the transport visual system resolves distinct visual descriptors for the two items while preserving their shared resource identity in gameplay systems

### Requirement: Cargo-form-specific visuals remain deterministic within a render tier
The game SHALL keep cargo-form-aware transport visuals deterministic so all items with the same resource identity and cargo form resolve the same descriptor chain under the same render conditions.

#### Scenario: Same cargo form does not oscillate across unrelated payload silhouettes
- **WHEN** two moving items share both resource identity and cargo form under the same active render tier
- **THEN** they resolve the same primary and fallback visual descriptors instead of alternating between unrelated silhouettes frame to frame
