## ADDED Requirements

### Requirement: Factory sites expose independent industrial build catalogs
The system SHALL distinguish at least `World` and `Interior` factory-site kinds, and each site kind SHALL resolve its own build catalog, placement rules, and allowed structure set without requiring a separate logistics simulation implementation.

#### Scenario: World site resolves world-only structures
- **WHEN** the player opens a build flow on a world-grid factory site
- **THEN** the available build catalog includes only structures allowed for the `World` industrial standard and excludes `Interior`-only structures

#### Scenario: Interior site resolves interior-only structures
- **WHEN** the player opens a build flow inside a mobile-factory interior site
- **THEN** the available build catalog includes only structures allowed for the `Interior` industrial standard and excludes `World`-only structures

### Requirement: Site-specific catalogs preserve shared logistics behavior contracts
The system SHALL allow site-specific structures to map onto shared logistics behavior contracts so equivalent routing semantics remain deterministic across `World` and `Interior` sites even when the concrete structure catalogs differ.

#### Scenario: Equivalent routing pieces keep the same transport semantics across site kinds
- **WHEN** a world transport structure and an interior transport structure both declare the same routing behavior contract
- **THEN** they follow the same connection, dispatch, blockage, and forwarding semantics while still remaining distinct catalog entries

### Requirement: Site validation rejects structures outside the active industrial standard
The system SHALL reject placement, blueprint application, and authored map reconstruction when a structure is not valid for the active site kind and rule set.

#### Scenario: Blueprint containing world structures cannot be applied to an interior site
- **WHEN** the player or loader attempts to apply a blueprint entry that references a `World`-only structure onto an `Interior` site
- **THEN** validation reports the entry as incompatible and leaves the interior layout unchanged

#### Scenario: Authored map entry using interior-only structure on world site is rejected
- **WHEN** a world map document or runtime loader encounters an `Interior`-only structure on a `World` site
- **THEN** the loader reports a site-catalog compatibility error instead of silently substituting another structure
