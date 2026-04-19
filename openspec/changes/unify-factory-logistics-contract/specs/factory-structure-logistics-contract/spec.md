## ADDED Requirements

### Requirement: Structures resolve one authoritative logistics contract
The factory runtime SHALL resolve each buildable structure through one authoritative logistics contract that defines occupied cells, input cells, output cells, dispatch source cells, preview anchors, and boundary-facing anchors for the structure's effective placement context.

#### Scenario: Runtime and preview resolve the same contract for a multi-cell structure
- **WHEN** the game resolves a placed or previewed multi-cell structure for a specific anchor cell, facing, site, and applicable configuration context
- **THEN** the runtime and preview systems receive the same occupied, input, output, dispatch, and anchor data from one resolved contract authority instead of recomputing those values independently

#### Scenario: Configuration-aware resolution still yields one contract authority
- **WHEN** a structure's effective footprint or logistics edges depend on configuration or authored map recipe context
- **THEN** every consumer resolves that structure through the same contract resolver entry point instead of mixing definition-level defaults with consumer-specific overrides

### Requirement: Structure handoffs use explicit contract semantics
The factory logistics system SHALL describe provider-to-receiver handoffs with explicit contract semantics that identify the participating provider, receiver, dispatch source, and acceptance edge instead of relying on a bare grid cell alone.

#### Scenario: Integrated input port keeps its acceptance meaning
- **WHEN** a provider sends an item into a receiver whose valid input cell lies on an occupied body cell or other integrated port location
- **THEN** the resolved handoff preserves which receiver contract edge accepted the item and does not require downstream code to infer that meaning from a raw source cell

#### Scenario: Contract-aware handoff survives non-anchor dispatch
- **WHEN** a multi-cell provider dispatches an item from a transfer edge that is not the provider's anchor cell
- **THEN** the handoff records the provider's effective dispatch source without collapsing that event back to the anchor cell
