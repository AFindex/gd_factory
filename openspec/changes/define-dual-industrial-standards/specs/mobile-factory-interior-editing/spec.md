## ADDED Requirements

### Requirement: Interior editor exposes the interior industrial catalog instead of the world catalog
The game SHALL scope mobile-factory interior editing to the active interior industrial catalog so the editor offers only interior-standard machines, logistics pieces, and interfaces that are valid for the current interior site.

#### Scenario: Interior palette excludes world-only heavy structures
- **WHEN** the player opens the mobile-factory interior editor
- **THEN** the build palette omits world-only heavy structures and presents only the structures allowed by the active interior catalog

### Requirement: Interior previews preserve maintenance-space readability
The game SHALL keep interior editing previews, overlays, and authored case studies readable as maintenance-space layouts by representing logistics and machines according to the interior industrial standard instead of as resized world-floor equipment.

#### Scenario: Interior preview distinguishes embedded logistics from human route space
- **WHEN** the player previews an interior layout or blueprint inside the editor
- **THEN** the preview keeps cargo-routing pieces visually distinct from the authored maintenance-space traversal layer so the layout reads as an interior industrial bay rather than as a miniature world factory
