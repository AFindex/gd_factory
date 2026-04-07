## ADDED Requirements

### Requirement: Headless connectivity analysis recognizes expanded transport merges
The headless validator SHALL treat belt midspan merges and three-input mergers as valid transport connectivity patterns so authored maps that use the new logistics topology are not misreported as isolated or disconnected.

#### Scenario: Midspan-fed belt is not reported as isolated
- **WHEN** a validated map contains a feeder belt that outputs into the occupied cell of another belt and that target belt continues to a valid downstream receiver
- **THEN** the validator does not report either transport segment as isolated solely because the connection lands on the target belt's occupied cell instead of its legacy input endpoint

#### Scenario: Three-input merger reports all valid upstream neighbors
- **WHEN** a validated map contains a merger with connected rear, left, or right feeders
- **THEN** the validator and focused connectivity diagnostics recognize each connected feeder as a valid upstream transport neighbor for that merger
