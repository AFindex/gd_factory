## ADDED Requirements

### Requirement: Manufacturing structures declare cargo-form-aware input and output contracts
The game SHALL let manufacturing and conversion structures declare not only which resources they consume and produce, but also which cargo forms are valid on each input and output contract.

#### Scenario: Recipe-capable machine requires matching cargo forms
- **WHEN** a manufacturing structure receives the right resource identities but one or more items arrive in unsupported cargo forms
- **THEN** the machine does not start or continue the recipe until the required resource and cargo-form contract is satisfied

### Requirement: Manufacturing catalog supports explicit standard-conversion buildings
The game SHALL support conversion-oriented buildings, such as unpacking, packing, or equivalent transfer preparation structures, as first-class authored machines in the manufacturing catalog instead of treating industrial-standard conversion as a hidden transport side effect.

#### Scenario: Unpacking converts world cargo into interior feed
- **WHEN** an unpacking machine receives a supported world cargo form and has a valid downstream path
- **THEN** it consumes the configured world cargo input and emits the configured interior feed form through the normal deterministic logistics layer

#### Scenario: Packing converts interior feed into world delivery cargo
- **WHEN** a packing machine receives a supported interior cargo form and has a valid downstream path
- **THEN** it consumes the configured interior feed input and emits the configured world delivery cargo form through the normal deterministic logistics layer
