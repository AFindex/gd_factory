## 1. Expand The Static Factory Stress Layout

- [x] 1.1 Refactor `FactoryDemo.CreateStarterLayout()` into named layout helper methods so different topology segments are authored by intent instead of one long coordinate list
- [x] 1.2 Add several new default topology segments to the static demo, covering longer belt corridors, splitter/merger recombine paths, bridge crossings, and loader/unloader relay cases
- [x] 1.3 Rebalance authored producers, sinks, and recovery paths so the startup scene can run for extended observation without immediately hard-jamming every line

## 2. Compact The HUD And Add Profiler Telemetry

- [x] 2.1 Redesign `FactoryHud` into a denser left-edge layout whose primary panel occupies roughly one fifth of the viewport while preserving build selection and preview feedback
- [x] 2.2 Add a dedicated profiler block to the HUD that displays FPS, frame time, and compact runtime metrics relevant to the factory demo
- [x] 2.3 Introduce lightweight runtime sampling in `FactoryDemo` and/or `SimulationController` so the HUD can report stable hotspot-oriented metrics without heavy profiling overhead

## 3. Fix Splitter Partial-Blockage Routing

- [x] 3.1 Update `SplitterStructure` so output choice is resolved against real-time branch availability when an item is ready to leave the splitter
- [x] 3.2 Preserve left/right balancing intent when both outputs are healthy, while falling back to the open branch when only one output can currently receive items
- [x] 3.3 Verify that splitter buffering only occurs when both outputs are blocked, without regressing existing transport flow behavior

## 4. Strengthen Regression Coverage And Documentation

- [x] 4.1 Expand the static demo smoke test to assert that the richer startup layout still delivers throughput after load
- [x] 4.2 Add a regression check or scripted smoke case that reproduces one blocked splitter branch and confirms the free branch continues delivering items
- [x] 4.3 Update demo-facing notes or comments to describe the denser static stress layout and the new HUD profiler readout
