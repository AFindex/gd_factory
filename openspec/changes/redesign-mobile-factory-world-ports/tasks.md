## 1. Boundary attachment foundations

- [x] 1.1 Introduce boundary attachment definitions and runtime instances for mobile factories, covering shared metadata for input/output interaction kinds, mount positions, facing, and deployment projections.
- [x] 1.2 Migrate the current fixed mobile factory output bridge/profile data into the new boundary attachment model and add a matching input attachment type.
- [x] 1.3 Implement shared geometry helpers for rotated interior, boundary, and exterior attachment cells so editor previews and deployment validation use the same projection data.

## 2. Lifecycle and logistics runtime

- [x] 2.1 Refactor mobile factory deployment and recall flow to activate/deactivate installed boundary attachments, reserve/release their world-side projection cells, and stop relying on a single hardcoded world port root.
- [x] 2.2 Change outbound attachment runtime behavior so inactive output attachments block outgoing items instead of recycling or silently consuming them.
- [x] 2.3 Implement inbound attachment runtime behavior so deployed factories can receive items from world-side routes through active input attachments while inactive inputs remain disconnected.

## 3. Interior editing and deployment presentation

- [x] 3.1 Extend the mobile factory editor to place, rotate, preview, and remove boundary attachments using valid boundary mounts and cross-boundary shape constraints.
- [x] 3.2 Update editor overlays and status labels to show attachment type, inside/outside shape, and current connection state such as connected, disconnected, or blocked.
- [x] 3.3 Generate deployment-time connector visuals from each active attachment so the world model visibly extends from the factory edge to the target world cell.

## 4. Demo content and authored scenarios

- [x] 4.1 Update the focused mobile factory preset/layout so it includes at least one output attachment and one input attachment with meaningful internal routing.
- [x] 4.2 Adjust the focused mobile factory demo world's external belts, sources, sinks, and feedback text so deployment demonstrates both inbound and outbound interaction loops.
- [x] 4.3 Refresh any port-related HUD or status messaging that still describes inactive ports as internal recycling instead of blocked/disconnected boundary attachments.

## 5. Verification

- [x] 5.1 Add regression coverage for inactive output attachments blocking items and inactive input attachments refusing world imports while the factory is not deployed.
- [x] 5.2 Add deployment validation coverage for attachment projection conflicts, including out-of-bounds or occupied exterior cells and successful activation after redeploy.
- [x] 5.3 Run the focused mobile factory demo and confirm that deployed attachments show continuous connector visuals and restore the expected world interaction loops after recall and redeploy.
