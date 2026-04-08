## 1. Render Descriptor Foundation

- [ ] 1.1 Extend `FactoryTransportVisualProfile` / `FactoryTransportVisualFactory` to resolve stable transport render descriptors, shared-batch keys, and deterministic near/mid/far fallback chains for every `FactoryItemKind`
- [ ] 1.2 Add reusable shared mesh/material resource helpers for placeholder, textured, billboard, and supported special-case transport visuals so batchable item kinds do not allocate per-item scene resources

## 2. Transport Snapshot And Batching Pipeline

- [ ] 2.1 Refactor `FlowTransportStructure` transport-item visuals so moving items keep minimal simulation/interpolation state and emit transport render snapshots instead of creating one `Node3D` visual per item
- [ ] 2.2 Implement a `FactoryTransportRenderManager` that collects transport snapshots, buckets them by descriptor and render tier, and submits them through shared instance-oriented render containers
- [ ] 2.3 Add camera-driven visible-rect culling with configurable padding and deterministic fallback handling for descriptors that cannot use the preferred batch path

## 3. Demo Integration And Telemetry

- [ ] 3.1 Wire the transport render manager into `FactoryDemo` using the existing visible-world-cell projection path so the optimized renderer receives current camera coverage each frame
- [ ] 3.2 Extend the static sandbox HUD/telemetry to show transport-render metrics such as active moving items, visible moving items, and batching status while the optimized path is active
- [ ] 3.3 Add or tune a high-density logistics observation segment in the default static sandbox so the optimized transport-render path is meaningfully exercised during normal startup play

## 4. Verification And Regression Coverage

- [ ] 4.1 Expand factory smoke coverage to assert that transport-render telemetry is populated and that the optimized transport-render path remains active while logistics items are moving
- [ ] 4.2 Add regression checks that verify culling reduces the currently rendered transport population versus total active moving items and that mixed item profiles still resolve readable deterministic fallbacks
