## 1. Shared Heavy-Cargo Ownership Contract

- [x] 1.1 Introduce a shared heavy-cargo presentation handle/state model that tracks the logical cargo identity, active visual owner, current host/anchor, and transfer stage for each active heavy handoff.
- [x] 1.2 Refactor heavy input and output handoff runtime so visual ownership transfers only at explicit world-route accept/release and converter accept/release edges.
- [x] 1.3 Remove or gate legacy world-side, bridge-side, and cabin-side payload spawn paths that can still render a full-size cargo body outside the shared ownership contract.

## 2. Boundary Attachment Staged Path and Timing

- [x] 2.1 Build the ordered staged-anchor path for heavy input and output attachments, covering route handoff, outer staging, bridge alignment, bridge crossing, inner staging, and converter handoff/release.
- [x] 2.2 Update the inbound heavy-port sequence so world-route consumption happens at outer-buffer acceptance and the shared cargo presenter moves continuously from the route edge into the unpacker handoff path.
- [x] 2.3 Update the outbound heavy-port sequence so packed cargo leaves the packer only on output-port acceptance and releases to the world route only at the visible release edge.
- [x] 2.4 Refresh heavy-port debug/status text and inspection details so current owner, current stage, and waiting conditions are readable during runtime validation.

## 3. Converter Takeover and Visual Profiles

- [x] 3.1 Update `CargoUnpacker` and `CargoPacker` processing flow so each chamber explicitly takes ownership of the shared heavy-cargo presenter and keeps it visible until unpack-complete or pack-release.
- [x] 3.2 Refactor heavy-port structure visuals so world-side and cabin-side roots provide static geometry, anchors, and state feedback without independently spawning a full-size cargo body.
- [x] 3.3 Update heavy converter/port visual-profile plumbing so cargo-body visibility always follows shared ownership state instead of local buffered-occupancy heuristics.

## 4. Demo and Regression Coverage

- [x] 4.1 Update the focused mobile-factory demo flow/content so players can clearly observe one continuous inbound heavy cargo and one continuous outbound heavy cargo across the full handoff path.
- [x] 4.2 Extend smoke or regression coverage for duplicate-visual prevention, synchronized route accept/release timing, and converter takeover/release ownership edges.
- [x] 4.3 Run the relevant focused-demo and regression checks, then capture any remaining polish-only follow-ups separately from the core ownership fix.
