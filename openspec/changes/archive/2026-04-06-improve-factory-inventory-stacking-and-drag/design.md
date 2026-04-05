## Context

The current inventory implementation is shared across storage, machine inputs/outputs, fuel slots, and turret ammo racks through `FactorySlottedItemInventory`. Right now each occupied slot stores exactly one `FactoryItem`, `Count` effectively means occupied-slot count, and the detail window only exposes binary occupied/empty slot state with drag-to-empty behavior.

This change needs to increase warehouse usability without destabilizing the factory simulation. We need per-item stack-size tuning, stack-aware storage capacity, and detail panels that clearly communicate counts while preventing invalid drag starts from empty slots. The same detail window code is reused by the static factory demo and the mobile interior editor, so the solution should stay shared and deterministic.

## Goals / Non-Goals

**Goals:**
- Introduce a shared stackable inventory model for all slotted factory inventories.
- Make max stack size configurable per `FactoryItemKind` through the existing item catalog.
- Preserve deterministic add, peek, take, and move behavior so logistics and smoke tests remain stable.
- Update detail panel models and drag behavior to show stack counts, merge into compatible stacks, and ignore drag attempts from empty slots.
- Keep existing one-item-at-a-time logistics transfer semantics so conveyors, inserters, and machine loops do not need a throughput redesign.

**Non-Goals:**
- Redesign belt visuals or world transport to move multiple items at once.
- Add stack splitting, modifier-key drag actions, or freeform drag outside the current slot-grid interaction.
- Introduce save-data migration or persistence changes.
- Change recipe balance beyond whatever stack-cap tuning is needed in item metadata.

## Decisions

### 1. Store stack limits in `FactoryItemCatalog`
Each `FactoryItemDefinition` will gain stack metadata, exposed through the catalog so all inventories can ask for the correct max stack size per item kind.

Why:
- Item kind already owns display and behavior metadata, so stack tuning belongs with the rest of the item definition.
- This avoids scattering per-structure special cases across storage, machines, and UI code.

Alternatives considered:
- A single global stack-size constant was rejected because ore, wires, fuel, and ammo should tune differently.
- Per-structure stack rules were rejected because they would make transfers and panel behavior harder to reason about.

### 2. Rework slotted inventories around per-slot stack records, not plain counts
`FactorySlottedItemInventory` will stop storing a single `FactoryItem` per slot and instead store a per-slot stack record containing same-kind items in insertion order.

Why:
- We need to preserve concrete `FactoryItem` instances and IDs for debug labels, deterministic withdrawal, and future traceability.
- A stack record can answer both slot-level questions and total buffered-item questions without changing how structures request one item at a time.

Alternatives considered:
- Storing only `{ itemKind, count }` was rejected because it loses item identity and makes existing labels/tests less informative.
- Keeping the current dictionary shape and layering a separate count map on top was rejected because it duplicates state and increases desync risk.

### 3. Keep logistics APIs single-item and deterministic
`TryPeekFirst`, `TryTakeFirst`, and structure handoff methods will keep returning exactly one `FactoryItem`. The inventory will fulfill those calls by walking slot order and then consuming the oldest item from the first eligible stack.

Why:
- The player asked for better warehouse backpack management, not a throughput rebalance.
- This keeps belts, inserters, and production logic compatible while still benefiting from increased slot density.

Alternatives considered:
- Batch transfers per stack were rejected because they would alter pacing, balancing, and multiple existing specs.

### 4. Make moves stack-aware but bounded
Inventory moves from the detail panel will only start from occupied source slots. Dropping onto an empty slot moves the whole stack; dropping onto a same-kind, non-full stack merges as many items as fit and leaves overflow in the source slot.

Why:
- This satisfies the user's request to prevent dragging from empty slots.
- Merging into partial stacks is the most natural expectation once stacking exists and avoids forcing players to micromanage empty slots first.

Alternatives considered:
- Only allowing moves into empty slots was rejected because it would make stacking cumbersome.
- Full stack splitting UI was rejected as out of scope for this change.

### 5. Separate slot occupancy from total buffered item counts in UI-facing models
Structures and detail models will expose enough information to show both slot layout and stack counts, including current stack size and max stack size for each occupied slot.

Why:
- Existing summaries such as `容量：x/y` become ambiguous once one slot can hold multiple items.
- The detail window signature must include stack counts so panel refreshes happen when only the count changes.

Alternatives considered:
- Reusing the current label-only slot model was rejected because it would hide stack state and cause stale panel updates.

## Risks / Trade-offs

- [Shared inventory refactor touches many structures] -> Mitigate by keeping `FactorySlottedItemInventory` as the single behavior boundary and updating shared tests/smoke checks before tuning structure-specific UI.
- [Capacity semantics may confuse players if slot count and item count diverge] -> Mitigate by showing stack counts in slots and updating summary text to distinguish slots from buffered items where relevant.
- [Partial merges can feel opaque without strong feedback] -> Mitigate by updating the drag hint text and destination highlighting so valid merge targets are obvious.
- [Signature-based detail refresh could miss count-only changes] -> Mitigate by including stack size and stack limit fields in `FactoryStructureDetailModel.BuildSignature()`.

## Migration Plan

This change does not require persisted save migration because the demo currently rebuilds runtime state on launch. Implementation should proceed in this order:

1. Extend item definitions with stack metadata defaults.
2. Refactor `FactorySlottedItemInventory` and related slot snapshot models to understand stacks.
3. Update structures and summaries to use total-item counts and stack-aware add/remove behavior.
4. Update detail window models and drag validation/merge behavior.
5. Refresh smoke coverage for storage, machine inventories, and invalid drag attempts.

## Open Questions

- Exact numeric stack limits for each item kind will be tuned during implementation in the item catalog, but the design assumes every kind has an explicit configurable maximum rather than inheriting an implicit hardcoded default.
