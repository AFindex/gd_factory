## 1. Shared Validation Foundation

- [ ] 1.1 Define validation target, diagnostic, severity, and summary models for standalone world maps and mobile-factory bundles
- [ ] 1.2 Add a shared catalog for current authored map targets, including the static sandbox world map and the focused mobile world/interior pair with its mobile factory profile
- [ ] 1.3 Refactor existing malformed-document and round-trip checks so they feed the shared validation report instead of living only as isolated boolean smoke helpers

## 2. Replay-Based Map Validation

- [ ] 2.1 Implement temporary world-map replay validation that reuses `GridManager`, `SimulationController`, and shared placement rules to surface actionable placement errors
- [ ] 2.2 Implement temporary mobile-factory interior validation that reuses `MobileFactoryInstance` and profile-aware attachment rules to catch invalid mounts, bounds issues, and other mobile-only cases
- [ ] 2.3 Ensure replay validation reports authored context such as map path, entry kind, anchor cell, and rule-specific failure reasons for every hard error

## 3. Advisory Connectivity Reporting

- [ ] 3.1 Add connectivity analysis for replayed maps that reports likely isolated logistics, power, and attachment/port issues as warning or info diagnostics
- [ ] 3.2 Add per-target validation summaries that clearly separate error, warning, and informational findings

## 4. Headless Workflow And Regression Coverage

- [ ] 4.1 Add a headless command/flag that runs the shared map validator, prints the diagnostics report, and exits non-zero when any target has error-level findings
- [ ] 4.2 Update `FactoryMapSmokeSupport` and current factory/mobile smoke flows to invoke the new shared validator while preserving serializer regression coverage
- [ ] 4.3 Document how to run the headless validator locally and what current map targets it validates
