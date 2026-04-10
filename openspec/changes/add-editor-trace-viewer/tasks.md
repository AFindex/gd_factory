## 1. Restructure The Diagnostics Plugin For Trace Viewing

- [x] 1.1 Rework `addons/dotnet_diagnostics_profiler/plugin.gd` so the existing attach-trace workflow and a new Trace Viewer page/tab coexist in the same editor plugin UI
- [x] 1.2 Add Trace Viewer toolbar controls for refresh-latest, open-file, profile selection, current file path, and visible loading/status text
- [x] 1.3 Reuse the current diagnostics artifact and status-file conventions so the viewer can resolve the newest completed `trace.speedscope.json` without introducing a new output path

## 2. Implement Speedscope Loading And Aggregation

- [x] 2.1 Add a loader/parser module that reads speedscope JSON, validates required top-level structure, and extracts shared frames plus profile metadata
- [x] 2.2 Implement sampled-profile aggregation that maps frame indexes back to names and folds `samples` plus `weights` into a cumulative call tree with parent/child relationships and max-depth statistics
- [x] 2.3 Add explicit warning/error handling for invalid JSON, unsupported profile structures, empty sample sets, and suspiciously small trace results

## 3. Build The Viewer Rendering And Inspection Flow

- [x] 3.1 Implement a self-drawn flamegraph/icicle control with cached hit-testing so large traces render efficiently without one control per sample
- [x] 3.2 Add hover highlight, tooltip-equivalent data, block labels with long-name truncation, and scroll/resize behavior that keeps the graph usable in small and large windows
- [x] 3.3 Add persistent node-details UI plus zoom-in, back, and reset-root interactions so users can inspect a hotspot subtree without losing context

## 4. Integrate Profile Switching And Diagnostics Feedback

- [x] 4.1 Support switching among profiles in a loaded file while showing the active profile name, type, and whether the selected profile is supported in the first version
- [x] 4.2 Surface successful-load statistics including frame count, sample count, profile count, and maximum stack depth for the active trace view
- [x] 4.3 Hook trace completion feedback into the viewer flow so a newly finished diagnostics session can be refreshed into the Trace Viewer with minimal extra navigation

## 5. Verify Viewer Behavior Against Trace Edge Cases

- [x] 5.1 Validate the viewer against a normal repository trace artifact and confirm it renders an in-editor flamegraph with selectable hotspots
- [x] 5.2 Verify multi-profile switching, subtree zoom/back/reset behavior, and persistent details using a trace file that includes more than one profile
- [x] 5.3 Verify invalid JSON, unsupported/empty trace content, and suspiciously small files all produce explicit user-visible errors or warnings instead of silent failure
