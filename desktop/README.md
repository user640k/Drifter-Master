# Drift Master: desktop version (work in progress)

A separate build of Drift Master for desktop, made with Godot 4.6 and C#. The browser version in the repository root is unchanged.

One hall, two layouts (HILLSIDE and INFIELD), three bodies on one chassis.

## Run

Open the `desktop` folder in Godot 4.6 (.NET edition) and press Play, or export with the presets in `export_presets.cfg`.

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Throttle / brake, reverse | W / S or arrows | right / left trigger |
| Steer | A / D or arrows | left stick |
| Reset to the start | R | Y |
| Camera (pilot's stand, ceiling, on board, roof stick) | C | X |
| Track (HILLSIDE, INFIELD) | F1 | |
| Body (sedan, coupe, pickup) | T | |
| Colour, second colour (12 each) | P, O | |
| Livery (solid, bonnet, fade, flames, camo, streaks) | L | |
| Shell on / off | B | |
| Neon under the car (off, six colours) | G | |
| Windows tinted / clear | V | |
| Mute, motor sound on/off, volume down / up | M, N, - / = | |
| Shadows on/off | F2 | |
| Frame rate and draw calls | F3 | |
| Resolution 100 / 85 / 70 % | F4 | |
| Full screen | F11 | |
| Menu (pauses the car) | Esc | Start |

## What is in it

- **Physics** (`scripts/CarPhysics.cs`, `scripts/Track.cs`): a C# port of the browser version's car model, stepped at 720 Hz. It has no engine types, so it can be tested outside Godot. On the same inputs it follows the browser version to within a few centimetres over 14 seconds of sliding.
- **Hall** (`scripts/Hall.cs`): built in code, 9.0 x 5.0 m, just big enough for the layout. Photo textures on simple shapes, few materials, the OpenGL (Compatibility) renderer, one shadowed light and one reflection probe, so it suits integrated graphics.
- **Floor**: black P-tile (304.8 mm vinyl composition tiles), drawn after a photo of real drift-track tile, under a wear sheet for the whole hall: dust, lanes the cars keep polished, dried spills, chips, cracked corners, old tape and paint, scratches and tyre lines (`tools/make_textures.py`, `assets/floor.gdshader`). The floor is not perfectly flat either: tile steps, joints, undulation and a few humps are felt through the springs (`Track.MicroH`).
- **On-board camera**: inside the cabin on the centre line, close behind the windscreen. Its picture is an analog FPV camera's, after the Tiny Whoop Pinch as set up in the 640KFPV simulator: a 720 x 480 frame shown as 4:3 with black bars at the sides, 75 degrees top to bottom, soft across (600TVL), punchy colour, deep blacks, a mild fisheye (`assets/analog.gdshader`). While on board the window draws only that small frame.
- **Stick camera**: the same analog camera on a short carbon stick fixed to the roof, about 29 cm behind the rear bumper and 6 cm above the roof (19.5 cm off the floor, so it clears the 23 cm bridge), looking forward and a little down over the car. It is rigid to the body, so it leans and turns with the car; the stick shows only in this view.
- **Sound** (`scripts/CarAudio.cs`, `tools/make_sounds.py`): cut from a reference recording of a 1/10 drift car on a P-tile drift track, the floor the game has (`tools/sound_src/reference.wav`, the sound of a video, used with permission for non-commercial use only). Mostly tyres: a broad roar that comes up as they slide or spin, a quieter texture for rolling. The motor's gliding whine is taken out of the recording and put back by the game, pitched by wheel speed: a tone gliding between about 0.9 and 1.6 kHz with a high whistle three times above it, set to the strength measured in a second sample (`N` switches it off). On top: the body shell buzzing on the chassis, tyres clicking over tile joints, a thud and clack into barriers. Everything is placed at the car and always heard from where the pilot stands, whichever camera is showing: the on-board and stick cameras carry no microphone. How it fades with distance is measured from the same video (a fixed camera, the car near and far): in a hall the car fades much less than outdoors. The recording carries its own room, so the game adds only a touch of echo. `M` mutes, `-` and `=` change the volume.
- **Icon**: the blue sedan above "DriftMaster" in white, no background (`icon.png`, `icon.ico`, built by `tools/make_icon.py` from a render of the game's own car). It is the window and taskbar icon. After a Windows export, `python3 tools/stamp_icon.py build/win/DriftMaster.exe icon.png` writes it over Godot's icon inside the exe.
- **Menu** (`scripts/Menu.cs`, Esc): three plain pages. CAR: body, two colours, livery, shell, windows, neon. SETUP, as in the web version: motor, tyres, gyro gain, springs, shock oil, roll bars, Ackermann, caster, diff, ESC timing, driving assist, each with a line on what it does. GAME: track, camera, volume, motor sound, shadows, resolution, full screen, reset, quit. Everything is kept between runs. The single keys still work on the track.
- **Collisions**: the car is two circles, one on each axle, kept inside the lane and the hall. A hard knock bounces it back a little and scrubs speed: a PVC board is springy (30% back, 12% lost), a foam block soaks it up (8% back, 24% lost). The body leans and pitches on its springs up to bump stops (about 4.5 and 3 degrees). Stress runs (ten minutes of random driving per track, body and setup) never put the car more than 0.2 mm through a barrier.
- **Tracks**: two layouts in the same 9.0 x 5.0 m hall, changed with `F1` (the hall is built again). HILLSIDE: the figure of eight with the climb and the bridge, foam blocks. INFIELD (`tools/make_track.py`, after a photo of a club track): an oval round the outside with an S through three hairpins inside it, 31 m a lap in a 0.9 m lane, low white boards, artificial turf between the lanes and red and white kerbs on the inside of the hairpins (turf and kerbs are painted into that track's floor sheet).
- **Bodies**: sedan, coupe and pickup (`assets/*.glb`, exported from the web version), on the same chassis. The on-board and stick cameras move to each shell's cabin and roof.
- **Paint** (`assets/paint.gdshader`): twelve colours and six liveries drawn on the shell in its own space: solid, a second-colour bonnet, a fade from nose to tail, flames along the sides, camo and raked streaks. (A hologram chrome film is in the shader but not offered for now.) Original artwork from noise shapes, no logos.
- **Shells and handling**: each shell has its own mass and centre of gravity (`CarPhysics.SetBody`): the pickup is the heaviest and tallest, the coupe the lightest and lowest. The effect is real but small, as on a real car where the shell is about a tenth of the weight: in the same drift the pickup rolls about 12% more than the sedan and 20% more than the coupe and turns in a couple of milliseconds later; drift angle and speed hardly change.
- **Car options** (from the web version; kept between runs in `user://settings.cfg`): the shell on or off (the bare chassis with its body posts), neon tubes under the car in six colours with their light on the floor, and tinted or clear windows.
- **Bare room**: only the room and the track. A neon "Drift Master" sign with cable and socket is built (`Hall.BuildNeon`) but switched off for now.
- **No engine splash**: the boot image is switched off in `project.godot` (black screen until the game appears).
- **Car** (`assets/sedan.glb`): the sedan exported from the browser version, parts merged by material.

## Textures

Wall, foam, ceiling and board textures are from ambientCG.com (CC0 1.0): PaintedPlaster017, Sponge001, OfficeCeiling001, Chipboard004. The P-tile is generated.

## Not in yet

Rivals, damage, the other three bodies, more tracks; from the web car menu: ride height, camber, wheels and tyres, stickers, body mounts.
