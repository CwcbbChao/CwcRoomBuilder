# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-10-04

### Added
- **Interactive Scene Brush Painting**: Intuitive grid canvas in the Scene view with brush size controls, draw/erase/door modes, box fill, and clear utilities.
- **Automated Structure Tile Assembly**: Automatic detection and instantiation of floors, ceilings, straight walls, inside corners, and outside corners based on adjacent grid occupancy.
- **DoorPin Opening Workflow**: Dedicated `DoorPin` component matching room generator IDs to automatically carve clean door openings without gaps.
- **2D Normalized Signed Distance Field (SDF)**: Built-in 2D distance field with gradient vector fields ($\nabla D$) for precise wall-distance checking and $90^\circ$ discrete inward room alignment.
- **3D OBB Collision-Free Prop Decoration**: Advanced oriented bounding box (OBB) spatial checks preventing furniture and props from overlapping or intersecting walls.
- **Nested Decoration Slots (`RoomSlot`)**: Multi-tier recursive prop attachment (e.g., placing candles on tables, scrolls on bookshelves) with depth limits.
- **ScriptableObject Preset Architecture**: Clean separation between modular art configs (`RoomPreset`, `StructureGroup`, `DecorationGroup`) and runtime generators.
- **Instant Seed Re-roll & Regeneration**: One-click regeneration with random seeds or specific seed reproduction for rapid level design prototyping.
