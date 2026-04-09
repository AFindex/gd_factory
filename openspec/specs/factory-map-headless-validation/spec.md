# factory-map-headless-validation Specification

## Purpose
Provide a headless validation workflow for authored factory-map targets so teams can catch authored content issues before opening interactive demo scenes.

## Requirements
### Requirement: Headless validator can analyze authored map targets
The system SHALL provide a headless validation workflow for authored factory-map targets so developers can validate standalone world maps and mobile-factory map bundles without opening an interactive demo scene.

#### Scenario: Validate all registered map targets
- **WHEN** a developer or automation runs the headless map-validation workflow in project-wide mode
- **THEN** the system validates every registered authored map target and exits with a failing status if any target produces one or more error-level diagnostics

#### Scenario: Validate one selected target
- **WHEN** a developer runs the headless map-validation workflow for one named target
- **THEN** the system limits validation and reporting to that target while preserving the same diagnostic and exit-code behavior

### Requirement: Headless validator reports actionable authored-map diagnostics
The system SHALL emit diagnostics that identify which authored target and which authored map entry caused a failure or warning so broken map content can be fixed without stepping through a scene manually.

#### Scenario: Placement failure identifies the authored entry
- **WHEN** a structure, deposit, or attachment entry fails document or replay validation
- **THEN** the diagnostic includes the validation target, the relevant map path, the authored kind, the authored anchor cell or equivalent location, and the rule-specific reason

#### Scenario: Validation output includes per-target summaries
- **WHEN** the headless validation workflow completes
- **THEN** the output includes a summary for each validated target that reports the number of errors, warnings, and informational findings

### Requirement: Headless validator reports advisory connectivity findings
The system SHALL surface likely logistics, power, and attachment-connectivity issues as advisory findings so map authors can inspect broken or incomplete routes even when the document is still reconstructable.

#### Scenario: Isolated segments are reported as warnings or info
- **WHEN** replayed map content contains isolated logistics paths, unserved machine ports, disconnected power consumers, or similar topology issues that do not already violate hard placement rules
- **THEN** the validator reports those findings with non-error severity and does not treat them as document corruption

#### Scenario: Clean target can report a zero-warning summary
- **WHEN** a validated target has no replay or connectivity findings beyond successful checks
- **THEN** the validator reports a successful summary with zero errors and zero warnings for that target

### Requirement: Mobile-factory map bundles validate with profile-aware rules
The system SHALL validate mobile-factory authored content as a bundle that includes the selected mobile factory profile plus any paired world and interior maps needed to interpret boundary attachments correctly.

#### Scenario: Interior attachment mount mismatch is rejected
- **WHEN** a mobile-factory interior map places a boundary attachment on a cell, facing, or kind combination that the selected mobile factory profile does not allow
- **THEN** the validator reports an error that identifies the incompatible attachment entry and the failed mount rule

#### Scenario: Focused mobile bundle validates world and interior together
- **WHEN** the focused mobile-factory authored world map and interior map are validated through their registered bundle
- **THEN** the validator checks world placement, interior placement, and profile-dependent attachment projection rules as one target and reports the combined findings coherently

### Requirement: Headless connectivity analysis recognizes expanded transport merges
The headless validator SHALL treat belt midspan merges and three-input mergers as valid transport connectivity patterns so authored maps that use the new logistics topology are not misreported as isolated or disconnected.

#### Scenario: Midspan-fed belt is not reported as isolated
- **WHEN** a validated map contains a feeder belt that outputs into the occupied cell of another belt and that target belt continues to a valid downstream receiver
- **THEN** the validator does not report either transport segment as isolated solely because the connection lands on the target belt's occupied cell instead of its legacy input endpoint

#### Scenario: Three-input merger reports all valid upstream neighbors
- **WHEN** a validated map contains a merger with connected rear, left, or right feeders
- **THEN** the validator and focused connectivity diagnostics recognize each connected feeder as a valid upstream transport neighbor for that merger
