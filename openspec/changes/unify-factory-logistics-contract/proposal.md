## Why

The factory logistics stack currently lets runtime routing, structure footprints, preview hints, map validation, and mobile-factory boundary handoffs interpret the same building contract through separate rules. That split authority has made simple port-shape changes cascade across multiple subsystems, increasing entropy, hiding the real source of truth, and making multi-cell cargo structures expensive to change safely.

## What Changes

- Introduce a unified resolved structure logistics contract that describes occupied cells, input cells, output cells, dispatch source cells, preview-facing anchors, and boundary-facing anchors from one authority.
- Refactor runtime routing to resolve send/receive handoffs through the unified contract instead of mixing direct occupancy lookup with port-cell fallbacks.
- Align preview markers and focused map-topology validation with the same resolved contract so visual hints and diagnostics stop maintaining parallel interpretations of multi-cell logistics structures.
- Generalize mobile-factory boundary attachment handoff rules so attachments consume converter-facing contract data instead of hard-coding specific converter classes.
- Add shared contract-focused regression coverage for representative multi-cell and cargo-conversion structures so future footprint or port changes fail fast when any layer drifts.

## Capabilities

### New Capabilities
- `factory-structure-logistics-contract`: Defines the resolved contract authority shared by routing, preview, validation, and boundary handoff consumers.

### Modified Capabilities
- `factory-logistics-routing`: Routing requirements will change to require contract-first provider and receiver resolution with explicit dispatch semantics for multi-cell structures.
- `factory-map-headless-validation`: Validation requirements will change so topology findings are derived from the same structure logistics contract used by runtime routing.
- `factory-multi-cell-structures`: Multi-cell structures will change to require shared occupied, input, output, and preview semantics from one resolved contract authority.
- `mobile-factory-boundary-attachments`: Boundary attachments will change to resolve converter handoffs through shared contract data rather than structure-type-specific logic.

## Impact

- Affected code includes `scripts/factory/FactoryFootprints.cs`, `scripts/factory/FactoryStructureFactory.cs`, `scripts/factory/FactoryTransportTopology.cs`, `scripts/factory/FactoryStructurePortResolver.cs`, `scripts/factory/SimulationController.cs`, `scripts/factory/GridManager.cs`, `scripts/factory/MobileFactorySite.cs`, `scripts/factory/FactoryLogisticsPreview.cs`, `scripts/factory/maps/FactoryMapValidationFocus.cs`, `scripts/factory/maps/FactoryMapValidation.cs`, and `scripts/factory/structures/MobileFactoryBoundaryAttachmentStructure.cs`.
- The change will introduce a new shared contract/resolution layer and may require adapter shims so existing structure interfaces can migrate incrementally.
- Smoke and contract tests for cargo packers, unpackers, transfer buffers, and representative multi-cell structures will need expansion to guard against cross-layer drift.
