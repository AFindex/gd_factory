## 1. Mining Stake Child Structure Model

- [x] 1.1 Add a mobile-factory child-structure path for deployed mining stakes, including owner references, world-cell identity, and destruction callbacks.
- [x] 1.2 Extend the mining input port runtime state to persist max stake capacity, built stake stock, and currently active deployed stakes across deploy / recall transitions.

## 2. Deployment And Extraction Logic

- [x] 2.1 Refactor mining-input deployment evaluation to separate projected cells, deposit-eligible cells, and actually deployed stake cells while preserving existing hard deployment blockers and reservations.
- [x] 2.2 Replace the mining-input relay payload model with child-structure spawning/cleanup and keep non-mining attachment payload behavior unchanged.
- [x] 2.3 Route mining activity through surviving deployed stakes so destroyed or missing stakes reduce active mining coverage without breaking the rest of the factory deployment flow.

## 3. Detail UI And Interaction

- [x] 3.1 Extend the structure-detail model/window with reusable action controls that attachment-specific structures can invoke.
- [x] 3.2 Show mining input port stake status in the detail window, including built stock, deployed stake count, and eligible mining-cell count.
- [x] 3.3 Implement mining-stake rebuild actions from the mining input port detail window and refresh the editor HUD/detail state after each action.

## 4. Verification

- [x] 4.1 Add or update demo/smoke coverage for mixed mineral coverage so only deposit-backed cells deploy mining stakes and empty projected cells stay stake-free.
- [x] 4.2 Verify stake combat behavior by covering child-structure damage/destruction, rebuilt stock recovery, and later partial redeployment when stock is short.
- [x] 4.3 Regression-check non-mining boundary attachments and existing mobile-factory deployment commands so the refactor does not change their current business logic.
