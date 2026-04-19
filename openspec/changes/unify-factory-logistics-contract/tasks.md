## 1. Shared Contract Authority

- [x] 1.1 Add the shared structure logistics contract definition, resolved contract type, and resolver entry points that can answer occupied, input, output, dispatch, preview, and boundary-facing anchors for a structure kind or placed structure.
- [x] 1.2 Refactor `FactoryStructureFactory`, `FactoryTransportTopology`, and related helpers so resolved footprint and logistics-edge data come from the shared contract resolver instead of ad hoc footprint overrides.
- [x] 1.3 Update multi-cell structure helpers so rotated occupied cells, input/output cells, dispatch cells, and preview anchors all derive from the same resolved contract.

## 2. Contract-First Routing

- [x] 2.1 Introduce an explicit handoff descriptor and compatibility adapters that let current provider/receiver interfaces consume contract-aware dispatch and acceptance semantics without a flag day rewrite.
- [x] 2.2 Update `FactoryStructurePortResolver`, `SimulationController`, `GridManager`, and `MobileFactorySite` so send/receive flows resolve provider and receiver contract edges before validating transfers.
- [x] 2.3 Remove or narrow the current occupancy-first fallback logic once contract-aware routing covers the representative multi-cell and cargo-conversion cases.

## 3. Consumer Convergence

- [x] 3.1 Refactor `FactoryLogisticsPreview` to source preview markers, port facings, and cargo special cases from the shared resolved contract instead of local occupied-cell heuristics.
- [x] 3.2 Refactor focused/headless map validation topology helpers so connectivity findings use the same resolved contract and dispatch semantics as runtime routing.
- [x] 3.3 Replace direct converter class checks in mobile-factory boundary attachments with contract-facing heavy handoff anchor resolution.

## 4. Regression Coverage and Cleanup

- [x] 4.1 Add shared contract drift tests that compare runtime, preview, validation, and boundary-facing resolution for representative structures such as cargo packers, cargo unpackers, transfer buffers, and large multi-cell structures.
- [x] 4.2 Expand smoke coverage only where needed to confirm scenario-level behavior still works after the contract migration, especially for packer/unpacker interior handoffs and focused mobile-factory layouts.
- [x] 4.3 Remove superseded duplicate inference paths and document any remaining temporary adapters or follow-up cleanup needed after the shared contract becomes the single authority.
