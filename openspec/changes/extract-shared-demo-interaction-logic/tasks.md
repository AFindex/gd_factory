## 1. Shared Interaction Foundation

- [x] 1.1 Extract the overlapping baseline interaction state and transitions from `FactoryDemo` into shared interaction host types for selection, detail publication, placement arming, interaction modes, blueprint state, preview messages, and shared input guards
- [x] 1.2 Add shared adapter or view-model contracts for baseline HUD projection, inventory/detail resolution, and preview/input gating so both demos can consume the same interaction outputs

## 2. Static Demo Migration

- [x] 2.1 Refactor `FactoryDemo` to use the shared interaction foundation for its baseline HUD state, structure selection/detail routing, player placement arming, build-delete-blueprint switching, and overlapping preview/input coordination
- [x] 2.2 Remove or simplify the static demo's controller-owned interaction glue while keeping sandbox-specific placement validation, authored layout logic, testing workspace behavior, and other static-only rules local

## 3. Mobile Demo Adoption

- [x] 3.1 Integrate the shared interaction foundation into the overlapping world-facing branch of `MobileFactoryDemo` so its baseline factory interactions reuse the same selection, HUD projection, placement, blueprint, and input-gating flow as the static demo
- [x] 3.2 Keep deploy anchors, lifecycle control, editor session routing, interior-only preview rules, and other mobile-specific interaction branches in mobile-demo-owned code while deleting redundant baseline interaction code

## 4. Verification

- [x] 4.1 Update or confirm regression coverage for the static demo's baseline interaction loop, including structure detail, player inventory placement, mode switching, blueprint flow, and shared preview behavior
- [x] 4.2 Update or confirm regression coverage for the mobile demo so the world-facing shared interaction loop remains consistent while deploy and interior-editor-specific behavior still works as before
