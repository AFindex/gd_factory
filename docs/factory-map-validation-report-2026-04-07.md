# Factory Map Validation Report

Date: 2026-04-07  
Workspace: `D:\Godot\projs\net-factory`

## Scope

Validated the three current authored map files:

- `res://data/factory/maps/static-sandbox-world.nfmap`
- `res://data/factory/maps/mobile-focused-world.nfmap`
- `res://data/factory/maps/mobile-focused-interior.nfmap`

Validation was executed through the shared headless map-validation entry:

```powershell
& 'D:\Godot\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe' --headless --path 'D:\Godot\projs\net-factory' -- --factory-map-validate
```

Note: the validator currently groups these three files into two registered targets:

- `static-sandbox-world`
- `focused-mobile-bundle`

The `focused-mobile-bundle` target validates:

- `mobile-focused-world.nfmap`
- `mobile-focused-interior.nfmap`
- profile-aware mobile deployment probes at the focused anchors

## Overall Result

- Status: PASS
- Blocking errors: `0`
- Advisory warnings: `9`
- Informational findings: `175`

The current authored maps are loadable and pass shared headless validation with no blocking reconstruction failures.

## Target Summary

### `static-sandbox-world`

- Result: PASS WITH WARNINGS
- Errors: `0`
- Warnings: `5`
- Info: `138`

Key warning-level findings:

- `Merger` at `(13, 2)` is currently isolated from both upstream and downstream neighbors.
- `MiningDrill` at `(8, -4)` has no reachable power node in range.
- `Belt` at `(6, -2)` is currently isolated from both upstream and downstream neighbors.
- `Belt` at `(11, -5)` is currently isolated from both upstream and downstream neighbors.
- `Belt` at `(9, 18)` is currently isolated from both upstream and downstream neighbors.

Representative info-level findings:

- Multiple belts, generators, assemblers, storage blocks, splitters, mergers, producers, sinks, and turrets are present as partial or probe layouts with missing upstream or downstream neighbors.
- Round-trip serialization check passed.

Assessment:

- This map has no document or replay errors.
- Current findings are consistent with a large sandbox map that intentionally contains multiple partial test lines and isolated regression fixtures.

### `mobile-focused-world.nfmap`

- Result: PASS WITH WARNINGS
- Blocking errors: `0`
- Warning-level findings observed in bundle validation: `2`

Key warning-level findings:

- `MiningDrill` at `(-8, -2)` has no reachable power node in range.
- `MiningDrill` at `(-2, 4)` has no reachable power node in range.

Representative info-level findings:

- Several short belt-to-sink and depot staging lines have no upstream or downstream partner yet.
- Several storage / depot helper clusters are intentionally incomplete from a logistics-topology perspective.
- Round-trip serialization check passed.

Assessment:

- World-side authored layout is valid and replayable.
- Warnings are advisory topology / power observations, not map-format or placement failures.

### `mobile-focused-interior.nfmap`

- Result: PASS WITH WARNINGS
- Blocking errors: `0`
- Warning-level findings observed in bundle validation: `2`

Key warning-level findings:

- `OutputPort` at `(7, 1)` is isolated from both upstream and downstream neighbors.
- `OutputPort` at `(7, 4)` is isolated from both upstream and downstream neighbors.

Representative info-level findings:

- `Assembler` at `(4, 1)` currently has no connected upstream input source and no connected downstream target.
- Interior `Belt` line from `(1, 3)` to `(3, 3)` is only partially connected.
- `AmmoAssembler` at `(4, 4)`, `LargeStorageDepot` at `(5, 6)`, and `Generator` at `(1, 7)` produce advisory connectivity notes.
- Round-trip serialization check passed.

Assessment:

- Interior map passed profile-aware validation and runtime-state application.
- Boundary attachment placement is compatible with the focused mobile-factory profile.
- The warnings indicate topology incompleteness, not invalid attachment mounts or broken reconstruction.

## Mobile Bundle Probe Result

The focused mobile bundle also validated two profile-aware deployment probes:

- `anchor-a` at `(-6, -3)`: validated successfully
- `anchor-b` at `(2, 3)`: validated successfully

This means the focused mobile map pair not only parses and reconstructs, but also remains deployable at the currently registered focused anchors under the selected mobile-factory profile.

## Conclusion

Current status:

- All three authored map files passed headless validation.
- No schema, placement, overlap, bounds, or attachment-mount errors were found.
- Existing findings are advisory and mostly reflect intentionally partial sandbox logistics or missing power coverage for certain authored drill lines.

Recommended follow-up:

- If you want the report to be quieter, consider introducing severity filtering or a stricter notion of which structure classes should participate in connectivity advisories.
- If you want stronger CI gating later, the current warning set is a good baseline for deciding which advisory classes should eventually be escalated.
