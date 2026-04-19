## ADDED Requirements

### Requirement: Connectivity findings use the shared structure logistics contract
The headless validator SHALL derive logistics connectivity findings from the same resolved structure logistics contract used by runtime routing so authored maps are judged against one connectivity model.

#### Scenario: Validation recognizes a contract-defined integrated input edge
- **WHEN** a validated structure exposes an input edge that is represented on an occupied or integrated contract cell rather than a legacy external port cell
- **THEN** the validator recognizes valid upstream providers according to the shared resolved contract and does not report the structure as disconnected solely because the edge is integrated into the footprint

#### Scenario: Validation follows the same dispatch source as runtime
- **WHEN** a validated source structure resolves a non-anchor dispatch source for one of its output edges
- **THEN** the validator uses that same dispatch source when checking whether a downstream receiver is reachable
