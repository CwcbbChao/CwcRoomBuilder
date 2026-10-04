# CwcRoomBuilder - Grid-Based Procedural Room & Dungeon Builder

[![Unity 2021.3+](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity.com/)
[![License](https://img.shields.io/badge/License-Custom%20(Free%20for%20Games)-blue.svg)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/CwcbbChao/CwcRoomBuilder/pulls)

**English** | [简体中文](README.md)

---

## What Does It Do?

Building indoor rooms and dungeons for Roguelites, RPGs, or adventure games often comes with tedious workflow pain points:
1. **Manual Tile Placement Is Boring**: Level designers have to manually place floor tiles, align walls, rotate corners, and snap ceilings. Resizing a room requires tearing down and rebuilding the whole structure.
2. **Prop Clutter & Intersecting Meshes**: Hand-placing furniture is slow, while random algorithms often leave bookshelves clipped inside walls, tables overlapping each other, or chairs facing walls backward.
3. **Carving Doors Is Frustrating**: Connecting rooms and corridors manually frequently causes misaligned openings or visible wall seams.

`CwcRoomBuilder` is an open-source Unity tool designed specifically to solve these issues. Simply **paint your room layout directly in the Scene view with a brush**, and the tool automatically builds **floors, walls, ceilings, door openings, wall-snapped props, central carpets, and furniture**—all **properly oriented and collision-free**.

---

## Key Problems It Solves

### 1. Paint Rooms Like a Canvas (Interactive Scene Brush)
- Built-in grid canvas directly in the Unity Scene view.
- Supports **brush resizing, box fill, clear, and eraser** modes.
- Drag your mouse across the grid to freely sketch any room layout: L-shaped chambers, winding corridors, grand halls, or interconnected rooms.

### 2. Automated Structural Tile Assembly (Floors, Walls, Ceilings)
- The system automatically analyzes room perimeters:
  - Centers are paved with **floor tiles** and capped with **ceilings**;
  - Edges are lined with **straight walls**;
  - Corners automatically detect and place **inside corner walls** and **outside corner walls** with correct rotations.
- Seamless wall-to-floor alignment eliminates the manual snapping grind.

### 3. Instant Door Carving (DoorPin Workflow)
- Want to connect a room to a hallway? Place a `DoorPin` component at the desired entrance.
- During room generation, `DoorPin` markers are recognized to **automatically remove blocking walls and carve clean doorways without gaps**.

### 4. Smart Prop & Furniture Decoration (Always Oriented, Never Overlapping)
Built-in distance-field algorithms allow props to "know" how far they are from walls and which way to face:
- **Wall Props (Bookshelves, Torches, Paintings)**: Snap flush to walls and **always face inward toward the room**.
- **Open-Space Props (Carpets, Central Statues, Grand Tables)**: Avoid narrow passages and corners, settling in the wide-open center of the room.
- **Corner Props (Potted plants, grandfather clocks, chests)**: Detect and nest snugly into $90^\circ$ corners.
- **3D Collision Protection**: Bounding-box checks ensure tables, chairs, and crates never intersect or overlap.
- **Nested Attachment Slots**: Supports placing candles or cups on tables, or scrolls on bookshelves.

### 5. Instant Art Style Swapping
- Geometry layouts are decoupled from art presets.
- By switching the `RoomPreset` asset, the exact same room footprint can transform from a **dark medieval dungeon** to a **sci-fi starship deck** or a **cozy tavern**.

### 6. One-Click Seed Re-rolling
- Click `Generate New Seed` to instantly randomize prop arrangements, allowing designers to quickly audition room variations.

---

## Installation

### Option A: Via Unity Package Manager (Recommended)
1. In the Unity Editor, open `Window` -> `Package Manager`.
2. Click the `+` button in the top-left corner -> Select **Add package from git URL...**.
3. Enter the repository URL:
   ```text
   https://github.com/CwcbbChao/CwcRoomBuilder.git
   ```
4. Click **Add** to install.

### Option B: Copy Files Directly
Clone or copy the `CwcRoomBuilder` directory directly into your project's `Assets/` or `Packages/` folder.

---

## Quick Start (3 Steps to a Complete Dungeon Room)

### Step 1: Create the Room Builder Node
1. Create an empty GameObject in the scene hierarchy and name it `DungeonRoom`.
2. Add the **`RoomBuilder`** component.
3. Assign an art preset asset (e.g., `Configs/NewRoomPreset.asset`) into the **Preset** slot.
4. Set grid canvas size (e.g., `Canvas Width = 12`, `Canvas Height = 12`, `Tile Size = 3.0` meters).

---

### Step 2: Paint the Room Layout in the Scene View
1. Select the GameObject to view the ground grid in the Scene view.
2. In the Inspector toolbar:
   - Click **Brush Mode: Draw Room**;
   - Click and drag across the grid to paint the room footprint;
   - Use **Box Fill** to quickly fill rectangular rooms.

---

### Step 3: Click Generate!
1. Click the **`Generate Room`** button in the Inspector.
2. Floors, ceilings, walls, and furniture props instantly populate the scene.
3. Click **`Re-Roll Seed`** to generate alternative furniture arrangements.
4. Generated meshes are organized cleanly under the `[Room_DungeonRoom]` child container.

---

## Preset Assets Reference

Configure modular ScriptableObject assets to build your own architectural library:

| Asset Type | Description |
| :--- | :--- |
| **`RoomPreset`** | **Comprehensive Room Style**: Bundles structure groups and decoration groups for a given aesthetic theme. Create via `Create -> Cwcbb -> Room Builder -> Room Preset`. |
| **`StructureGroup`** | **Structural Tile Set**: Configures prefabs for floors, ceilings, straight walls, inside corners, and outside corners with random weighting. |
| **`DecorationGroup`** | **Prop & Furniture Collection**: Configures props (tables, chairs, torches, carpets) with placement rules (Wall / Open / Corner), spacing radii, and spawn chances. |

---

## License & Commercial Use

- Core source code is licensed under the [Cwc Tools Public License (Source-Available)](LICENSE):
  - **Game Projects (End Products)**: **Free to use in personal and commercial games with zero royalties (Royalty-Free)**.
  - **No Tool Resale**: You may not repackage or resell this software as a standalone asset pack, development tool, or plugin on marketplaces.
- See [Third-Party Notices.txt](Third-Party%20Notices.txt) for details.
