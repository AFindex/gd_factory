## ADDED Requirements

### Requirement: Runtime map validation can run without scene reconstruction
The system SHALL expose reusable map-validation entry points that analyze authored map documents through the same core runtime rules used by reconstruction, without requiring a caller to boot a full demo scene first.

#### Scenario: Headless preflight reuses runtime validation rules
- **WHEN** a headless validation workflow asks the runtime layer to analyze an authored world map
- **THEN** the runtime layer validates that map through the same document and placement rules used by shared runtime loading instead of a separate handwritten rule set

#### Scenario: Preflight failure blocks reconstruction
- **WHEN** runtime map analysis reports one or more error-level findings for a requested map load
- **THEN** the shared loader fails before partially reconstructing deposits, structures, or runtime state

### Requirement: Runtime interior validation supports mobile factory profiles
The system SHALL allow shared interior-map validation to run against a selected mobile factory profile so boundary attachments and other mobile-only constraints are checked with the correct factory context before reconstruction proceeds.

#### Scenario: Profile-aware validation rejects incompatible attachments
- **WHEN** a mobile-factory interior map is analyzed against a profile whose attachment mounts do not allow one of the authored attachment entries
- **THEN** validation fails with a diagnostic that identifies the authored attachment and the incompatible profile rule

#### Scenario: Profile-aware validation preserves valid focused interior loading
- **WHEN** the focused mobile-factory interior map is analyzed against its authored mobile factory profile
- **THEN** the shared runtime validation accepts the authored attachment layout and allows the existing reconstruction flow to proceed
