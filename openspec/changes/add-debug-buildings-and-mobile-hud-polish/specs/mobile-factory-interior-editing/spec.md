## ADDED Requirements

### Requirement: Editor operation panel stays focused on construction categories
The game SHALL keep the mobile factory editor operation panel focused on build actions and construction categories, and it SHALL NOT repeat the high-level mobile factory overview information that is already available in the main overview panel.

#### Scenario: Editor panel opens as a build-focused tool drawer
- **WHEN** the player enters a mobile factory interior editing session
- **THEN** the editor operation panel presents build-related controls and construction categories without duplicating the overview's lifecycle, summary, or workspace chrome content at the top of that panel

### Requirement: Interior editing exposes debug source modules and permanent test power
The game SHALL expose interior debug source modules and a permanent test generator from the mobile factory editor's construction categories so the player can validate interior logistics or machine layouts without assembling a full support chain first.

#### Scenario: Interior build categories include debug modules
- **WHEN** the player opens the construction categories inside the mobile factory editor
- **THEN** the category list includes clearly labeled debug source modules and a permanent test generator module that can be selected like other interior build tools
