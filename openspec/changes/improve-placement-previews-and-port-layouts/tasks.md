## 1. Multi-Port Machine Contract

- [x] 1.1 Update the shared `3x2` assembler footprint metadata so assembler and ammo assembler use three input cells on one long edge and three output cells on the opposite long edge for every facing
- [x] 1.2 Remove the always-on port marker cubes from `AssemblerStructure` and `AmmoAssemblerStructure` so port visuals become preview-driven instead of permanently embedded in the models
- [x] 1.3 Re-check authored factory demo layouts that depend on assembler or ammo assembler IO placement and adjust seeded belt runs if the new sided contract changes connectivity

## 2. Preview Alignment And Contextual Hints

- [x] 2.1 Make blueprint apply overlays render multi-cell structures with footprint-aware preview centers and preview sizes instead of anchor-sized `1x1` meshes
- [x] 2.2 Keep blueprint preview rotation and validity coloring aligned with the same footprint transform used by final placement and ghost previews
- [x] 2.3 Restrict world-grid port hint overlays to belt placement preview mode only
- [x] 2.4 Apply the same belt-only port hint rule to the mobile-factory interior preview path if it shares the contextual logistics hint behavior

## 3. Continuous Placement Interaction

- [x] 3.1 Keep world build mode armed after a successful placement so the selected build kind remains active for repeated placement
- [x] 3.2 Add primary-button drag placement in `FactoryDemo` that attempts placement on each newly hovered valid cell while the button remains held
- [x] 3.3 Prevent duplicate placement attempts and duplicate inventory consumption within a single drag stroke by tracking cells already handled during that press

## 4. Verification

- [x] 4.1 Verify multi-cell blueprint previews for rotated large structures line up with their final committed positions
- [x] 4.2 Verify assembler and ammo assembler IO cells now occupy opposite long edges and only show contextual hints during belt preview
- [x] 4.3 Verify repeated click placement and left-drag placement both keep build mode active, skip invalid cells cleanly, and preserve inventory counts on failed placements
