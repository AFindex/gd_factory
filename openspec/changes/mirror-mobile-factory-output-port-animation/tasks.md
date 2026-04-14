## 1. Establish the outbound phase contract

- [x] 1.1 Add explicit output-port phase/anchor helpers in `MobileFactoryOutputPortStructure` so `CargoPacker -> InnerBuffer -> BridgeOut -> OuterBuffer -> WorldRelease` is represented as a readable staged contract instead of relying on the base default host selection.
- [x] 1.2 Tighten output-side visual ownership boundaries between `CargoPacker`, `MobileFactoryOutputPortStructure`, and the world route so a packed bundle has exactly one visible owner at every outbound stage.

## 2. Rebuild the output-port staged presentation

- [x] 2.1 Rebuild the `CargoPacker -> InnerBuffer` handoff so the packed bundle visibly settles into the output port's inner buffer before any bridge-out movement begins.
- [x] 2.2 Rebuild the `InnerBuffer -> BridgeOut -> OuterBuffer` path as one continuous outbound transfer with mirrored anchor semantics and no duplicate bridge/outer-buffer visuals.
- [x] 2.3 Rebuild the `OuterBuffer -> WorldRelease` stage so the bundle waits visibly for world pickup and transfers to the world route only at the visible release edge.

## 3. Update observation and regression coverage

- [x] 3.1 Update focused mobile-demo presentation and inspection text so players can read the staged outbound phases, especially the waiting-for-world-pickup state.
- [x] 3.2 Extend smoke coverage for outbound heavy handoffs to verify the staged mirror animation, the single-visible-owner contract, and correct world-release timing.
