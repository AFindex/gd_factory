## ADDED Requirements

### Requirement: Mobile factory interiors can be saved as blueprints
The game SHALL let the player capture the current mobile factory interior layout through the editor and save it as a reusable blueprint.

#### Scenario: Saving an interior layout keeps the editor workflow active
- **WHEN** the player saves the current mobile factory interior layout as a blueprint from the split-view editor
- **THEN** the blueprint is added to the shared library while the mobile factory editor remains open and usable

### Requirement: Compatible blueprints can be previewed and applied inside the mobile editor
The game SHALL let the player select a compatible blueprint from the library, preview it inside the mobile interior editor, and apply it when the translated layout satisfies interior bounds and attachment constraints.

#### Scenario: Compatible blueprint applies to the current interior
- **WHEN** the player selects a compatible mobile-interior blueprint and confirms a valid apply preview inside the editor
- **THEN** the mobile factory interior recreates the blueprint layout without closing the split-view editing workspace

#### Scenario: Incompatible attachment requirements are rejected
- **WHEN** the player attempts to apply a mobile-interior blueprint whose required boundary attachments or bounds do not fit the current mobile factory interior
- **THEN** the editor reports the compatibility failure in the blueprint workflow UI and leaves the current interior layout unchanged
