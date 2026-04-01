## 1. Lifecycle And Movement State

- [x] 1.1 Extend the mobile-factory runtime data model to represent in-transit movement, auto-deploy targets, deployment facing, and any new lifecycle states needed for deploy/recall transitions
- [x] 1.2 Update `MobileFactoryInstance` to support world-side movement and turning while in transit using deterministic transform updates instead of instant anchor swapping
- [x] 1.3 Refactor footprint, port-cell, and bridge-facing calculations so deployment validity and visuals respect the selected facing
- [x] 1.4 Enforce that deployed factories reject direct move/turn/redeploy commands until recall completes

## 2. Command Modes And Input Routing

- [x] 2.1 Add a control-mode layer to `MobileFactoryDemo` for factory command mode, deploy preview mode, and observer mode
- [x] 2.2 Rebind the mobile-factory demo so WASD controls the factory in command mode and only controls camera panning while observer mode is active
- [x] 2.3 Add deploy-preview input handling that supports entering deployment mode, rotating the preview facing, confirming a valid target, and canceling back to command mode
- [x] 2.4 Preserve the existing editor-pane hover behavior while making sure editor input, HUD input, and world input do not conflict with the new control modes

## 3. Auto-Deploy Flow And HUD Feedback

- [x] 3.1 Implement the auto-deploy sequence so a confirmed valid target makes the factory move to the anchor, align to the chosen facing, revalidate the target, and then finalize deployment
- [x] 3.2 Update the mobile-factory world preview and hull/port visuals to show facing-aware previews and visible deploy-state transitions
- [x] 3.3 Expand `MobileFactoryHud` with a mode-toggle button, deploy button, clearer mode/state labels, and feedback for auto-deploy progress or failure
- [x] 3.4 Keep recall, redeploy, and interior-editing flows coherent with the new HUD/state messaging

## 4. Verification And Documentation

- [x] 4.1 Extend mobile-factory smoke coverage to verify default command mode, observer-mode camera control, successful auto-deploy, deployed-state movement rejection, recall, and redeploy
- [x] 4.2 Add focused validation for facing-aware footprint/port calculations and deployment failure recovery
- [x] 4.3 Update `docs/factory-demo-notes.md` to document the new command/observer controls, deploy flow, and expected mobile-factory behavior in each mode
