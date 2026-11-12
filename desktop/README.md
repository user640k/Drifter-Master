# Drift Master: desktop version (work in progress)

A separate build of Drift Master for desktop, made with Godot 4.6 and C#. The browser version in the repository root is unchanged.

First playable: one hall, one layout (HILLSIDE, the figure of eight with the climb and the bridge), one car (the sedan).

## Run

Open the `desktop` folder in Godot 4.6 (.NET edition) and press Play, or export with the presets in `export_presets.cfg`.

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Throttle / brake, reverse | W / S or arrows | right / left trigger |
| Steer | A / D or arrows | left stick |
| Reset to the start | R | Y |
| Camera (chase, driver's stand, ceiling, on board, roof stick) | C | X |
| Paint colour | P | |
| Shadows on/off | F2 | |
| Frame rate and draw calls | F3 | |
| Resolution 100 / 85 / 70 % | F4 | |
| Full screen | F11 | |
| Pause (Q quits) | Esc | Start |

## What is in it

- **Physics** (`scripts/CarPhysics.cs`, `scripts/Track.cs`): a C# port of the browser version's car model, stepped at 720 Hz. It has no engine types, so it can be tested outside Godot. On the same inputs it follows the browser version to within a few centimetres over 14 seconds of sliding.
- **Hall** (`scripts/Hall.cs`): built in code, 9.0 x 5.0 m, just big enough for the layout. Photo textures on simple shapes, few materials, the OpenGL (Compatibility) renderer, one shadowed light and one reflection probe, so it suits integrated graphics.
- **Floor**: black P-tile (304.8 mm vinyl composition tiles), drawn after a photo of real drift-track tile, under a wear sheet for the whole hall: dust, lanes the cars keep polished, dried spills, chips, cracked corners, old tape and paint, scratches and tyre lines (`tools/make_textures.py`, `assets/floor.gdshader`). The floor is not perfectly flat either: tile steps, joints, undulation and a few humps are felt through the springs (`Track.MicroH`).
- **On-board camera**: inside the cabin on the centre line, close behind the windscreen. Its picture is an analog FPV camera's, after the Tiny Whoop Pinch as set up in the 640KFPV simulator: a 720 x 480 frame shown as 4:3 with black bars at the sides, 75 degrees top to bottom, soft across (600TVL), punchy colour, deep blacks, a mild fisheye (`assets/analog.gdshader`). While on board the window draws only that small frame.
- **Stick camera**: the same analog camera on a short carbon stick fixed to the roof, about 25 cm behind the rear bumper and 12 cm above the roof, looking forward and a little down over the car. It is rigid to the body, so it leans and turns with the car; the stick shows only in this view.
- **Bare room**: only the room and the track. A neon "Drift Master" sign with cable and socket is built (`Hall.BuildNeon`) but switched off for now.
- **No engine splash**: the boot image is switched off in `project.godot` (black screen until the game appears).
- **Car** (`assets/sedan.glb`): the sedan exported from the browser version, parts merged by material.

## Textures

Wall, foam, ceiling and board textures are from ambientCG.com (CC0 1.0): PaintedPlaster017, Sponge001, OfficeCeiling001, Chipboard004. The P-tile is generated.

## Not in yet

Sound, menus, rivals, liveries, car setup, damage, other bodies and tracks.
