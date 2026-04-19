## ADDED Requirements

### Requirement: Routing resolves receiver and provider semantics from the shared contract
The logistics routing system SHALL resolve provider dispatch cells and receiver acceptance cells from the shared structure logistics contract before validating or executing a transfer.

#### Scenario: Contract-first routing accepts an integrated multi-cell receiver
- **WHEN** a source structure targets a cell that belongs to a receiver's resolved contract input edge but does not map cleanly to the receiver's anchor cell
- **THEN** the routing system resolves the receiver through the shared contract and validates the transfer against that contract edge instead of failing over to a separate occupancy-specific interpretation

#### Scenario: Contract-first routing preserves non-anchor dispatch sources
- **WHEN** a multi-cell provider emits an item from a resolved dispatch source cell that differs from its anchor cell
- **THEN** the routing system validates downstream acceptance using that resolved dispatch source from the shared contract
