---
name: net-factory-godot-helper
description: Terminal-first helper for the net_factory Godot 4.6 C# project. Use when Codex needs to import assets, build C# solutions, open the editor, run scenes, execute scripts, or capture Godot logs for this repository. Prefer this skill whenever the work should be driven through the `godot_mono` CLI from the project root.
---

Use `godot_mono` as the command-line entrypoint for this repository.

## Project Defaults

- Work from `D:\Godot\projs\net-factory`.
- Prefer `godot_mono --path D:\Godot\projs\net-factory ...` so the command always targets the correct project.
- Main scene: `res://scenes/demo_launcher.tscn`.
- C# assembly: `net_factory`.

## Common Commands

- Open the editor:
  `godot_mono --path D:\Godot\projs\net-factory --editor`
- Import resources and quit:
  `godot_mono --path D:\Godot\projs\net-factory --headless --import`
- Build the C# solution:
  `godot_mono --path D:\Godot\projs\net-factory --headless --build-solutions --quit`
- Run the project's default scene:
  `godot_mono --path D:\Godot\projs\net-factory`
- Run a specific scene:
  `godot_mono --path D:\Godot\projs\net-factory --scene res://scenes/demo_launcher.tscn`
- Run a script or syntax check:
  `godot_mono --path D:\Godot\projs\net-factory --headless --script res://path/to/script.gd --check-only`
- Capture a stable log file:
  Add `--log-file artifacts/<name>.log`

## Workflow

1. Choose the smallest `godot_mono` command that can prove the task.
2. Use `--headless` for import, build, and script-driven validation when no UI is needed.
3. Use `--log-file` when the run may be noisy, long, or worth preserving in `artifacts/`.
4. If a command fails, keep the exact `godot_mono` output and fix the project or flags instead of switching to another Godot binary.
5. Prefer `res://` paths for scenes and scripts passed to Godot.

## Guardrails

- Do not replace `godot_mono` with `godot`, a hardcoded `.exe` path, or an editor-only tool unless the user explicitly asks.
- Do not edit `project.godot` directly unless the task truly requires it.
- When C# files change, run the `--build-solutions` command before finishing.
