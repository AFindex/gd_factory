## 1. Structure Visual Profile Foundation

- [x] 1.1 Add shared structure visual-profile data types and a presentation controller path that `FactoryStructure` can own without changing core simulation behavior.
- [x] 1.2 Add a legacy compatibility adapter so existing structures that only implement `BuildVisuals()` continue to render while the new pipeline is introduced.
- [x] 1.3 Define and wire a runtime structure visual-state snapshot that includes the machine/power/presentation fields needed for state-driven updates.

## 2. Authored Asset And Fallback Integration

- [x] 2.1 Extend the structure visual pipeline to support authored `PackedScene` or model-based presentations alongside procedural code builders.
- [x] 2.2 Implement deterministic fallback resolution from authored asset to procedural builder to generic placeholder when higher-fidelity resources are missing or invalid.
- [x] 2.3 Add any small helper conventions needed for animation-aware structure presentations, such as named animation channels, node-path bindings, or material-anchor lookup.

## 3. Smelter Furnace Migration

- [x] 3.1 Migrate `SmelterStructure` onto the new structure visual-profile pipeline without changing its recipe, power, or logistics behavior.
- [x] 3.2 Rebuild the smelter's code-defined presentation so it reads more clearly as a furnace, including layered body pieces such as a furnace core, door/firebox area, and chimney/exhaust silhouette.
- [x] 3.3 Add furnace-like animated responses for active processing, cooling, and low-power states, using the shared visual-state update path rather than ad hoc per-node logic in the simulation flow.

## 4. Demo Verification

- [x] 4.1 Update any relevant demo registration or build-definition plumbing so structure visual profiles resolve correctly in the factory sandbox.
- [x] 4.2 Add or extend smoke/regression coverage to verify authored-asset fallback, legacy structure compatibility, and smelter hot/cool state transitions without changing deterministic factory outcomes.
- [x] 4.3 Run the project validation path needed for the factory demo and confirm the smelter remains playable, visible, and responsive across powered, active, and idle states.
