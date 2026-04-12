## 1. Heavy Handoff State Model

- [x] 1.1 Add explicit heavy-handoff state and slot ownership to mobile-factory input/output attachments for outer buffer, inner buffer, bridge transfer, phase, and phase progress.
- [x] 1.2 Update heavy input attachments so they accept full-size world cargo from world routes into the outer buffer, advance one cargo at a time across the bridge, and keep cabin-side buffering separate from ordinary transit flow.
- [x] 1.3 Update heavy output attachments so they accept completed full-size packed cargo from packers into the inner buffer, bridge one cargo at a time to the outer buffer, and release to world routes only when the world side can receive it.
- [x] 1.4 Ensure heavy handoff ownership prevents duplicate visuals or duplicate logical ownership of the same cargo across buffers, bridge-transfer state, and connected converters.

## 2. Converter Handshake and Processing

- [x] 2.1 Add explicit ready/accept/release handshakes between heavy input attachments and `CargoUnpacker` so inner-buffer cargo waits until the unpacker can take ownership.
- [x] 2.2 Add explicit ready/accept/release handshakes between `CargoPacker` and heavy output attachments so completed packed cargo waits in the outbound chain until the output interface can own it.
- [x] 2.3 Keep unpacker and packer processing logic single-item for full-size world cargo, and move any fade/dissolve completion transition to the converter chamber rather than the boundary interface.
- [x] 2.4 Verify that full-size world cargo never routes onto ordinary cabin-feed belts, splitters, mergers, or interior rail visuals during the heavy handoff chain.

## 3. Continuous Presentation and Editing Feedback

- [x] 3.1 Add staged visual anchors and continuous path animation for outer buffer, bridge transfer, inner buffer, and converter handoff so full-size cargo moves across the hull without snap-spawning.
- [x] 3.2 Update heavy input/output interface visuals and converter visuals to keep full-size world cargo at world scale in every heavy handoff stage and chamber-processing state.
- [x] 3.3 Add status text, detail lines, and editor-facing indicators for staged heavy-handoff states such as waiting for unpacker, bridging inward, waiting for world pickup, and bridging outward.
- [x] 3.4 Update interior editor previews and detail presentation so heavy staging cells and converter chambers read as heavy-cargo space rather than ordinary cabin-feed rails.

## 4. Demo, Content, and Verification

- [x] 4.1 Update the focused mobile-factory demo layout and authored content so the player can observe inbound buffering, unpacker wait states, outbound buffering, and continuous heavy-cargo release back to the world.
- [x] 4.2 Update world miniature or equivalent runtime presentation so heavy-cargo handoff states remain legible without implying that full-size cargo is traveling on ordinary cabin-feed rails.
- [x] 4.3 Extend smoke coverage for heavy input/output buffering, bridge continuity, converter ownership, and duplicate-visual regression cases.
- [ ] 4.4 Run the relevant build and focused regression checks, then mark any follow-up gaps for shell visibility, path tuning, or additional animation polish separately from the core handoff contract.
