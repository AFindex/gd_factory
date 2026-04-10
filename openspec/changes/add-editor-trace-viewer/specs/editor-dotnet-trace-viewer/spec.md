## ADDED Requirements

### Requirement: Diagnostics plugin exposes an in-editor trace viewer workspace
The Godot editor SHALL provide a dedicated Trace Viewer page or tab inside the existing diagnostics plugin, separate from the trace-attach controls, so users can inspect saved trace results without leaving the editor.

#### Scenario: Viewer is available without replacing trace attach controls
- **WHEN** the user opens the diagnostics plugin UI in the Godot editor
- **THEN** the plugin shows both the existing trace attach workflow and a distinct Trace Viewer page or tab for browsing saved trace results

#### Scenario: Viewer keeps editor and launched-game trace workflows distinct
- **WHEN** the user interacts with the Trace Viewer after attaching a trace from the diagnostics process list
- **THEN** the viewer presents trace browsing as a separate action from selecting a process to sample, without implying that it is profiling the editor process itself

### Requirement: Viewer loads the latest trace artifact and supports manual file selection
The Trace Viewer SHALL load the latest available `trace.speedscope.json` from the repository's existing diagnostics artifact workflow and SHALL also allow the user to manually open any `trace.speedscope.json` file.

#### Scenario: Refresh latest trace reuses diagnostics artifact conventions
- **WHEN** the user activates the viewer's refresh-latest action
- **THEN** the plugin resolves the newest available diagnostics trace result using the existing `artifacts/dotnet-diagnostics` output directory and/or `plugin-status` records instead of inventing a separate storage location

#### Scenario: Manual open reloads a chosen trace file
- **WHEN** the user selects a specific `trace.speedscope.json` file from disk
- **THEN** the viewer reloads that file and updates the displayed file path and trace contents to match the selected file

#### Scenario: Loaded file path is visible
- **WHEN** a trace file is currently loaded in the viewer
- **THEN** the viewer displays the current file path so the user can confirm which trace result is being inspected

### Requirement: Viewer parses sampled speedscope profiles into an aggregated call tree
The Trace Viewer SHALL parse the standard speedscope JSON fields needed for sampled traces, map frame indexes back to `shared.frames`, and aggregate each sampled profile's `samples` and `weights` into a browsable call tree before rendering.

#### Scenario: Supported sampled profile is aggregated successfully
- **WHEN** the loaded speedscope document contains a valid sampled profile with `shared.frames`, `samples`, and optional `weights`
- **THEN** the viewer builds an aggregated root-to-leaf call tree with correct cumulative weights and child relationships instead of rendering one UI node per raw sample

#### Scenario: Frame names resolve from shared frame indexes
- **WHEN** a sampled profile references frame indexes in its sample stacks
- **THEN** the viewer resolves each frame index back to the corresponding entry in `shared.frames` and uses the resolved frame names in the flamegraph and details panel

#### Scenario: Unsupported or empty sampled content is reported clearly
- **WHEN** the loaded file has no usable sampled profile data, no usable samples, or only unsupported profile structures
- **THEN** the viewer shows an explicit message that the trace content is empty or unsupported instead of silently rendering a blank graph

### Requirement: Viewer supports profile switching and profile metadata display
The Trace Viewer SHALL show the current profile name and type and SHALL allow the user to switch among profiles present in the loaded file.

#### Scenario: Current profile metadata is visible
- **WHEN** a trace file is loaded successfully
- **THEN** the viewer displays the selected profile's name and type alongside the current file information

#### Scenario: Multiple profiles can be switched
- **WHEN** the loaded speedscope file contains more than one profile
- **THEN** the viewer provides a profile-selection control that lets the user switch the active profile and rebuild the rendered view for that profile

### Requirement: Viewer renders an efficient flamegraph or icicle view for aggregated hotspots
The Trace Viewer SHALL render the aggregated sampled call tree as a flamegraph or icicle-style visualization that remains usable for large traces and resizes with the editor window.

#### Scenario: Hotspots are rendered as weighted blocks
- **WHEN** a sampled profile has aggregated call tree data
- **THEN** the viewer draws stack blocks whose width reflects cumulative weight and whose labels show at least the frame name plus a relative share or cumulative weight summary

#### Scenario: Large traces avoid per-sample control explosion
- **WHEN** the loaded sampled profile contains many samples or deep stacks
- **THEN** the viewer uses self-drawn rendering or an equivalently efficient approach instead of creating one Godot UI control per raw sample

#### Scenario: Graph area remains usable after resize
- **WHEN** the viewer window or panel is resized
- **THEN** the flamegraph area, details panel, and surrounding layout remain visible and usable rather than collapsing into a tiny region in the upper-left corner

#### Scenario: Deep stacks remain scrollable
- **WHEN** the aggregated trace contains more visible stack depth than fits in the available viewport
- **THEN** the flamegraph area supports scrolling so the user can inspect deeper stack levels

### Requirement: Viewer supports hover, selection, zoom, and persistent node details
The Trace Viewer SHALL let the user inspect aggregated nodes through hover and selection, show persistent detail for the selected node, and support zooming into a subtree with a way to navigate back out.

#### Scenario: Hover highlights a block and reveals tooltip information
- **WHEN** the pointer hovers a visible flamegraph block
- **THEN** the viewer highlights that block and shows tooltip-equivalent information for the corresponding frame

#### Scenario: Selection keeps full node details visible
- **WHEN** the user clicks a flamegraph block
- **THEN** the details area persistently shows the selected node's full function name, cumulative weight, parent node, and a summary of its direct children

#### Scenario: Long names remain readable through truncation and details
- **WHEN** a frame name is too long to fit inside its visible block
- **THEN** the viewer may truncate the inline label but still exposes the full name through hover and the persistent details area

#### Scenario: User can zoom into a subtree and return
- **WHEN** the user chooses to zoom into a selected node
- **THEN** the viewer updates the graph to treat that node as the current root and also provides a way to go back one level or reset to the original root view

### Requirement: Viewer provides observable loading, validation, and trace statistics
The Trace Viewer SHALL communicate loading progress, parsing/build status, validation failures, suspicious-small-result warnings, and basic trace statistics to the user.

#### Scenario: Load and parse progress are visible
- **WHEN** the viewer is loading or parsing a trace file, especially a larger file
- **THEN** the UI shows explicit status feedback such as loading, parsing, or building the view instead of appearing idle

#### Scenario: Invalid or malformed trace files fail loudly
- **WHEN** the selected file is invalid JSON, structurally incompatible with the supported speedscope sampled format, or cannot be parsed successfully
- **THEN** the viewer displays a clear error message describing the failure rather than silently failing

#### Scenario: Suspiciously small trace result is flagged
- **WHEN** the loaded `trace.speedscope.json` is unusually small or otherwise appears likely to be incomplete
- **THEN** the viewer warns that the trace result may be invalid or suspicious even if the file can still be opened

#### Scenario: Successful load shows summary statistics
- **WHEN** the viewer successfully loads a trace file with usable content
- **THEN** the UI displays at least the frame count, sample count, profile count, and maximum stack depth for the active view
