## 1. Cargo Standard Definitions

- [x] 1.1 Extend item visual-profile and transport descriptor configuration so the same resource can resolve distinct world-payload and cabin-carrier presentations without collapsing to pure scale swaps.
- [x] 1.2 Add the presentation/runtime metadata needed for world cargo versus cabin cargo handling contexts, including deterministic fallback behavior for each standard.
- [x] 1.3 Ensure world-payload presentation remains unscaled even when rendered inside cabin-side handoff, conversion, or staging contexts.

## 2. Conversion Structure Presentation

- [x] 2.1 Rework unpacker and packer structure profiles into one-in/one-out conversion chambers that visibly stage a single world payload during processing.
- [x] 2.2 Redesign the transfer buffer and adjacent cabin module presentations so they read as world-payload-sized staging/support equipment instead of generic small processors.
- [x] 2.3 Update cabin rail and compact logistics presentations so they clearly carry cabin carriers only and do not imply direct world-payload traversal.

## 3. Boundary and Editor Readability

- [x] 3.1 Update mobile-factory boundary attachment presentation and preview state so world-facing connectors read as large-payload handoff points into conversion/staging structures.
- [x] 3.2 Update interior-editor previews, labels, and world miniature rendering to teach the scale break between world payload exchange and cabin-carrier flow.

## 4. Demo and Authored Content

- [x] 4.1 Re-author the focused mobile-factory demo cargo loop so players can observe large world payloads entering through unpacking, compact carriers circulating inside, and repacked world payloads leaving the factory.
- [x] 4.2 Adjust authored cabin layouts and scene content so unpacking, buffering, processing, and packing structures occupy credible world-payload-sized module space.

## 5. Verification

- [x] 5.1 Add or update smoke/validation coverage for item descriptors and structure profiles so world payloads never render as direct cabin-rail cargo.
- [x] 5.2 Add or update demo/editor regression checks that verify boundary previews, world miniature flow, and conversion-chamber visuals preserve the world-to-cabin cargo distinction.
