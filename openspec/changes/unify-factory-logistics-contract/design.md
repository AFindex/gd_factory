## Context

The current factory logistics stack resolves one structure's logistics shape through several partially overlapping systems:

- `FactoryStructureFootprint` mixes occupied geometry, input/output cells, preview sizing, and transfer-cell inference.
- `FactoryStructureFactory.GetFootprint(...)` can override definition-level footprint data, so a structure's declared shape is not the only authority.
- Runtime transfer still passes bare `Vector2I` source and requester cells through `IFactoryItemProvider` and `IFactoryItemReceiver`, forcing receivers to infer whether the value represents an anchor cell, an output cell, an input cell, or an integrated body-side port.
- `FactoryStructurePortResolver` and the send/receive helpers in `SimulationController`, `GridManager`, and `MobileFactorySite` patch around that ambiguity by trying direct occupancy first and then retrying through port-cell resolution.
- `FactoryLogisticsPreview` and `FactoryMapValidationTopologyHelper` each reinterpret structure connectivity separately instead of consuming the same resolved contract as runtime.
- `MobileFactoryBoundaryAttachmentStructure` still recognizes concrete converter classes directly for some heavy handoff paths.

That split has made port and footprint changes high-entropy work: a local change to one cargo structure now ripples into runtime routing, preview hints, validation findings, and boundary handoff logic. This design change focuses on reducing that entropy by centralizing the resolved logistics contract and migrating consumers onto it in a controlled way.

Constraints:

- Keep the existing site, structure, and simulation framework intact; this is not a rewrite of the whole factory runtime.
- Preserve current authored maps and placement semantics during migration.
- Avoid a flag day conversion of every structure type; the first implementation can adapt old structures behind a new contract-resolver layer.
- Maintain current gameplay behavior for belts, mergers, and non-cargo structures except where contract unification requires consistent interpretation.

## Goals / Non-Goals

**Goals:**
- Establish one resolved structure logistics contract authority that answers occupied cells, input cells, output cells, dispatch source cells, preview anchors, and boundary-facing anchors from one place.
- Replace the weakest routing ambiguity, namely bare source-cell semantics, with an explicit handoff description that can express which provider, port, dispatch cell, and receiver contract edge participated in a transfer.
- Make runtime routing, preview markers, focused/headless validation, and mobile-factory boundary handoff consume the same resolved contract semantics instead of maintaining parallel interpreters.
- Provide an incremental migration path so current structure classes can keep their existing APIs while the contract layer becomes the new authority.
- Add regression coverage that detects cross-layer drift for representative multi-cell and cargo-conversion structures.

**Non-Goals:**
- Do not redesign all manufacturing or cargo-form rules.
- Do not split `CargoPacker` and `CargoUnpacker` into entirely separate world-only and interior-only prototype kinds in this change.
- Do not replace every structure-specific override immediately; legacy adaptation is acceptable where it preserves behavior while authority is centralized.
- Do not introduce a second standalone logistics simulation for heavy cargo.

## Decisions

### 1. Introduce a resolved contract layer above raw footprint data

The new authority will be a resolver layer, tentatively centered around types equivalent to:

- `FactoryStructureLogisticsContractDefinition`: declarative inputs describing occupied geometry, logistics ports, dispatch edges, preview anchors, and boundary anchors.
- `ResolvedFactoryStructureLogisticsContract`: resolved per structure instance and facing, with concrete occupied cells, input cells, output cells, dispatch cells, and presentation anchors.
- `FactoryStructureLogisticsContractResolver`: a shared entry point that resolves the effective contract for `kind`, placement context, map recipe, configuration, or a placed `FactoryStructure`.

This lets the project stop treating `FactoryStructureFootprint` as both geometry definition and full logistics meaning. Footprint data can remain as one input to contract resolution, but no longer acts as the final authority for every consumer.

Alternatives considered:

- Continue extending `FactoryStructureFootprint` until every consumer can read more fields from it. Rejected because that keeps geometry, logistics, preview, and combat concerns coupled inside one overburdened type.
- Make runtime, preview, and validation each ask structure subclasses directly. Rejected because authority would remain fragmented and special cases would keep multiplying.

### 2. Add an explicit handoff descriptor and route through it

Transfers will move toward an explicit handoff value, conceptually containing:

- provider structure identity
- receiver structure identity
- targeted contract edge or port
- provider dispatch cell
- receiver acceptance cell
- whether the receiver was reached via occupied-cell ownership or via a contract input edge

The old `Vector2I requesterCell/sourceCell` interface surface will remain temporarily through adapters, but the adapters will be fed by the contract resolver rather than by ad hoc reinterpretation. The goal is to remove "guessing after the fact" from routing logic.

Alternatives considered:

- Preserve the current `Vector2I` contract and only document its meaning better. Rejected because the same value still cannot encode enough information for multi-cell integrated ports and boundary handoffs.
- Jump immediately to replacing every provider/receiver interface signature. Rejected because it creates too much short-term migration churn; adapters are lower-entropy.

### 3. Make routing contract-first, not occupancy-first

Runtime send/receive flows will resolve the receiver contract before deciding how to validate the handoff. Occupied-cell lookup remains useful for structure ownership, but it should no longer outrank contract semantics. The routing order becomes:

1. Resolve provider contract and dispatch edge.
2. Resolve receiver contract edge for the target cell.
3. Build a handoff descriptor from those two resolved contracts.
4. Ask the receiver/provider adapters to validate or execute the handoff.

This change removes the current pattern where direct occupant checks silently take one path while port-cell ownership checks take another.

Alternatives considered:

- Keep direct occupancy first and only normalize source cells more aggressively. Rejected because it preserves the same conceptual split that caused the current drift.

### 4. Preview, validation, and boundary handoff must become consumers, not co-authors

`FactoryLogisticsPreview`, focused/headless validation helpers, and mobile-factory boundary attachments will migrate to consume resolved contracts rather than each inferring ports from their own heuristics.

That means:

- preview arrows derive from contract preview anchors and edge facing, not from occupied-cell heuristics
- validation walks contract input/output edges and dispatch cells, not a hand-maintained neighbor scan that only approximates runtime
- boundary attachments ask a contract-facing interface for valid heavy handoff anchors, instead of probing for specific converter classes

Alternatives considered:

- Leave preview and validation as-is until later, and only unify runtime first. Rejected because the project has already seen that runtime fixes without consumer convergence leave the overall system misleading and unstable.

### 5. Migrate in stages with legacy adapters and drift tests

The implementation will use staged migration:

1. Add the contract types and a resolver that can derive contracts from current footprint and topology data.
2. Add routing adapters that translate resolved contracts into the current provider/receiver calls.
3. Move preview and validation to the new resolver while preserving current behavior.
4. Move boundary handoffs to contract-facing converter negotiation.
5. Retire duplicated logic once contract-driven paths cover the representative structures.

Each stage will be protected by new contract tests that assert the same structure resolves consistent occupied, input, output, preview, validation, and boundary-facing semantics.

Alternatives considered:

- Big-bang rewrite of all logistics consumers. Rejected because the regression surface is too large and would create more instability than entropy reduction.

## Risks / Trade-offs

- [The resolver layer may initially look like "one more abstraction"] -> Mitigation: make it the only place allowed to answer resolved logistics contract questions, and delete duplicate inference once consumers are migrated.
- [Adapters could let old and new semantics diverge silently] -> Mitigation: add contract drift tests that compare runtime, preview, validation, and boundary outputs for representative structures before deleting old code paths.
- [Cross-cutting changes may stall if every consumer must move at once] -> Mitigation: stage migration so routing adapters can coexist briefly while preview and validation are switched over one subsystem at a time.
- [Boundary handoff abstraction may be underspecified for heavy cargo edge cases] -> Mitigation: start with converter-facing contract anchors that cover current packer/unpacker behavior, and defer broader converter family generalization until the contract is stable.
- [Recipe- or configuration-dependent footprints remain half-supported] -> Mitigation: force all such resolution through the same contract resolver, even if the first version still returns the current static shapes for most structures.

## Migration Plan

1. Introduce the new contract definitions, resolved contract object, and resolver entry points without changing current call sites.
2. Update structure factory and topology helpers to source resolved input/output/dispatch data from the resolver.
3. Add explicit handoff descriptors and adapt the current transfer interfaces through compatibility shims.
4. Migrate preview and focused/headless validation to the resolver; remove local heuristics that duplicate contract inference.
5. Migrate mobile-factory boundary attachments to contract-driven converter handoff anchors.
6. Add shared contract regression tests and retain existing smoke scenarios as scenario-level coverage.

Rollback strategy:

- If a consumer migration destabilizes behavior, keep the resolver and tests in place but temporarily route that consumer through an adapter built on the new contract rather than reverting to the older duplicated inference.
- Avoid deleting old code paths until a subsystem's contract-driven path is covered by regression tests.

## Open Questions

- Should the first handoff descriptor type be introduced as a parallel API only, or should select core interfaces be upgraded immediately where the change is low-risk?
- Does the first contract resolver need a distinct declarative type beyond `FactoryStructureFootprint`, or can it begin as a normalized projection over existing definition inputs and then split further once migration is complete?
- Which representative structures should anchor the first drift suite beyond packer/unpacker: transfer buffer, large storage depot, assembler, and boundary attachments are the leading candidates.
