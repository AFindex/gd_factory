## 1. Core Logistics Topology

- [ ] 1.1 Add shared transport-topology helpers for belt midspan inputs and merger inbound faces so runtime, build validation, and diagnostics use the same connection rules.
- [ ] 1.2 Update `BeltStructure` and any required `FlowTransportStructure` collaboration points so belts can accept feeder items into an occupied belt cell and serialize them onto the normal forward output.
- [ ] 1.3 Update `MergerStructure` to accept rear, left, and right inputs while preserving a single forward output and correct transit paths.

## 2. Build and Presentation Updates

- [ ] 2.1 Update world and mobile-interior placement/preview flows so belts can be authored as T-shaped midspan merges without relaxing normal footprint occupancy checks.
- [ ] 2.2 Refresh merger preview/visuals and player-facing descriptions so the building clearly reads as a three-input merger instead of the legacy two-input version.
- [ ] 2.3 Update at least one authored demo or scenario layout to exercise both a belt midspan merge and a three-input merger in normal play.

## 3. Validation and Regression Coverage

- [ ] 3.1 Update headless map validation and focused connectivity diagnostics to recognize belt midspan merges and three-input mergers as valid transport connections.
- [ ] 3.2 Add or extend smoke/regression tests to verify sustained delivery through a belt midspan merge and through a three-input merger.
- [ ] 3.3 Run the relevant smoke and validation workflows, then fix any topology or tooling regressions uncovered by the new logistics rules.
