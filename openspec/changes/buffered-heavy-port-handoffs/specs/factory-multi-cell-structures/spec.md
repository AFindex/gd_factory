## ADDED Requirements

### Requirement: Multi-cell heavy interfaces define rotated staged handoff edges
The shared multi-cell footprint contract SHALL allow heavy interface and converter structures to resolve staged handoff edges and cache-facing transfer cells consistently with their rotated occupied footprint.

#### Scenario: Rotating a heavy input interface preserves its staged transfer side
- **WHEN** the player previews or places a heavy input interface and changes its facing
- **THEN** the resolved occupied cells, outer/inner handoff direction, and transfer-side cells rotate together from the same footprint definition

#### Scenario: Rotating a converter preserves its intake and release edges
- **WHEN** the player previews or places a heavy unpacker or packer with a rotated facing
- **THEN** the structure keeps a deterministic rotated intake edge and release edge that match the same multi-cell footprint used for occupancy and selection

### Requirement: Heavy multi-cell structures resolve interaction from any occupied cache-side cell
Heavy multi-cell interfaces and converters SHALL resolve inspection, removal, and editor targeting from any occupied cell in their footprint, including non-anchor cells that visually belong to staged buffer or chamber space.

#### Scenario: Removing from a secondary heavy-port cell clears the whole interface
- **WHEN** the player targets a non-anchor occupied cell that belongs to a heavy input or output interface footprint
- **THEN** the editor resolves the owning structure and removes the full interface rather than leaving orphaned staged cells behind

#### Scenario: Inspecting a converter from a chamber-side cell focuses the owner
- **WHEN** the player opens details on a non-anchor occupied cell belonging to a heavy unpacker or packer chamber
- **THEN** the UI resolves the owning converter structure and shows its full staged processing state
