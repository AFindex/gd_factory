## 1. Build Save Snapshot Persistence Foundations

- [x] 1.1 Add runtime save-slot paths, metadata/index models, and save snapshot DTOs for world/interior site captures under the existing `factory-runtime` persistence root.
- [x] 1.2 Implement save snapshot serialization, deserialization, and schema validation without reusing the authored `nfmap` source-save path.
- [x] 1.3 Extend item creation/restoration plumbing so loaded inventories and transit items can preserve saved item ids while keeping future ids conflict-free.

## 2. Capture And Restore Runtime Participants

- [x] 2.1 Add capture/restore hooks for `FactoryStructure` and key structure subclasses to persist health, inventories, recipe selection, production progress, and other structure-owned runtime state.
- [x] 2.2 Add transport snapshot support for `FlowTransportStructure` and related attachment transports so in-transit items restore with the correct routing metadata and progress.
- [x] 2.3 Add capture/restore support for `FactoryPlayerController`, active `FactoryEnemyActor` instances, and `FactoryCombatDirector` lane timers/spawn indices.

## 3. Rebuild Sessions Through Shared Load Flows

- [x] 3.1 Implement a load pipeline that validates a save snapshot, rebuilds saved world/interior maps through the existing map loading flow, and matches reconstructed runtime participants by stable semantic keys.
- [x] 3.2 Rehydrate reconstructed structures, player state, enemy state, and simulation counters before resuming topology rebuilds, HUD refreshes, and ticking.
- [x] 3.3 Abort invalid or mismatched loads before mutating the active session so failed save restores do not leave behind a partial world.

## 4. Integrate Save/Load UX And Verification

- [x] 4.1 Add named runtime save/load actions and status messaging to the static factory demo persistence HUD.
- [x] 4.2 Add equivalent runtime save/load flow for the focused mobile-factory demo, including snapshots that cover both world and interior sites.
- [x] 4.3 Verify round-trip behavior for player position/inventory, structure internals, belt transit items, enemy persistence, and corrupted or unsupported save files.
