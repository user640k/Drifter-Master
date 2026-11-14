# Drift Master: desktop version (work in progress)

A separate build of Drift Master for desktop, made with Godot 4.6 and C#. The browser version in the repository root is unchanged.

One room, four layouts (CLUB 1, CLUB 2, HILLSIDE and INFIELD), five bodies on one chassis.

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
| Colour (12), rims (12) | P, O | |
| Bonnet (body colour or carbon) | L | |
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
- **Sound** (`scripts/CarAudio.cs`): the web version's sound, carried over as it is. There it is a Web Audio graph; here the same graph is computed sample by sample: a synthesised brushless motor (commutation whine through a resonant peak, gear mesh, drivetrain growl, the ESC's drive tone, boost and turbo rasp), three loops cut from a recording of a real RC drift car (cruise, drift, tyre scrub) kept under it, knocks, an air filter that dulls with distance, an inverse-distance panner, and a compressor. Heard from the driver's position. The loops are `assets/snd/web_*.wav`, taken from the web page.
- **Icon**: the blue sedan above "DriftMaster" in white, no background (`icon.png`, `icon.ico`, built by `tools/make_icon.py` from a render of the game's own car). It is the window and taskbar icon. After a Windows export, `python3 tools/stamp_icon.py "build/win/Drift Master.exe" icon.png` writes it over Godot's icon inside the exe.
- **Splash**: the game starts as a small window with no frame and no background, so the icon floats over the desktop (`splash.png`, also the engine's start-up picture). After about two seconds it fades and the window becomes the game's, framed and 1280 x 720. Where the graphics driver cannot do a see-through window the icon shows on black.
- **Music**: the web version's track, *Off Road* by Aylex (`assets/music/off-road.mp3`), looping on its own bus with the room on it, as if from the hall's speakers. A "now playing" line shows at the bottom left for the first five seconds. On/off and volume are in the menu.
- **Menu** (`scripts/Menu.cs`, Esc): three plain pages. CAR: body, colour, bonnet, rims, shell, windows, neon. SETUP, the 1/10 RWD drift baseline and only that: motor (10.5T or 13.5T sensored), front camber (-6 to -9), rear camber (-1 to -3), front caster (6 to 10), front toe-out and rear toe-in (1 to 3), shock oil (150 to 300 cSt), ride height (6 to 8 mm), gyro gain (40 to 65%), springs (1.1 to 1.3 mm wire), spool or tight gear diff, weight bias (40/60 to 35/65); hard plastic tyres, near-parallel Ackermann, 2S with boost and turbo timing and a 0.06 s servo are fixed. GAME: track, camera, volume, motor sound, music, shadows, resolution, full screen, reset, quit. KEYS: every key and the gamepad. The line along the bottom of the screen lists only the main keys, so it fits any window. Everything is kept between runs. The single keys still work on the track.
- **Camber**: front and rear, -9 to +6 degrees, straight (0) by default. The wheels lean accordingly (negative: tops in). It changes each axle's grip (`CarPhysics.CamberGrip`): about -3 is best (+2%), -9 rides the inner edge (-7%), positive loses grip (+6: -15%). Rear camber is felt most: less rear grip means a slower, tighter slide.
- **Collisions**: the car is two circles, one on each axle, kept inside the lane and the hall. A hard knock bounces it back a little and scrubs speed: a PVC board is springy (30% back, 12% lost), a foam block soaks it up (8% back, 24% lost). The body leans and pitches on its springs up to bump stops (about 4.5 and 3 degrees). Stress runs (ten minutes of random driving per track, body and setup) never put the car more than 0.2 mm through a barrier.
- **Bump map** (`Track.Bump`): each track has its own, made once from its name so a bump is always in the same place. Everywhere: tiles a hair high or low, joint grooves, a slow undulation. Here and there: shallow humps and dips, the lines where sub-floor sheets meet (a small step and ridge right across the hall), a few lifted tile corners. Along the lane: kerbs 1.2 mm proud on the inside of INFIELD's hairpins, and the joints between deck boards on HILLSIDE's raised road. All well under a millimetre except the kerbs. The chassis rides it on its springs; the shell, a light lid on four posts, shivers at about 22 Hz when jolted (about three or four noticeable jolts a lap, a third of a millimetre, drawn three times life size so it can be seen) and its rattle is in the sound. The floor's reflections are bent by the same map (nine times life size).
- **Tracks**: two layouts in the same 9.0 x 5.0 m hall, changed with `F1` (the hall is built again). HILLSIDE: the figure of eight with the climb and the bridge, foam blocks. INFIELD (`tools/make_track.py`, after a photo of a club track): an oval round the outside with an S through three hairpins inside it, 31 m a lap in a 0.9 m lane, low white boards, artificial turf between the lanes and red and white kerbs on the inside of the hairpins (turf and kerbs are painted into that track's floor sheet).
- **Bodies**: sedan, coupe (with a GT wing: a blade on two stays with end plates, built in `CarView`), pickup, tuner (a 1990s two-door coupe from the owner's free-to-use model, emptied into a shell by `../tools/fbx_shells.py`: no engine, interior, badges or plates) and van (the web version's forward-control van; its weathered texture is reduced to lamps and grille over clean paint by `tools/make_van_detail.py`). All `assets/*.glb`, exported through the web version, on the same chassis. The on-board and stick cameras move to each shell's cabin and roof.
- **Paint** (`assets/paint.gdshader`): one template. A solid colour (twelve), the bonnet in it or in carbon weave (a 2x2 twill that shimmers as the eye moves), and the rims in their own colour (twelve: black, white, gunmetal, silver, gold, bronze and six loud ones). The shell is made to look like what RC drift shells are: clear polycarbonate (Lexan) sprayed on the inside, so the outside is the plastic itself, smooth and very glossy with a deep even colour, and the inside is flat dried paint. Tuned side by side against photos of a painted shell and a raw clear one: the plastic mirrors the room (ceiling lamps as long bars, pale walls, dark floor) weakly face-on and strongly at a glancing angle, the reflections are bent a little by the formed sheet's waviness, and the colour under it is shaded darker where it faces down and along the sills.
- **Tyres**: the sidewall lettering reads DRIFTMASTER (`tools/make_stickers.py`), in place of the web model's strip.
- **Training** (`scripts/Judging.cs`, `scripts/Compete.cs`, TRAINING page): solo only. A judged run is one lap from the start line, scored like solo qualifying (tanso): line 40 (clips and zones on a natural line; reaching for a clip halves it), angle 40 (depth 25, consistency 15), style 20 (fluidity 7, commitment 7, initiation 6) and up to 5 bonus for initiating before the first bend's initiation line; speed earns nothing by itself. Deductions for straightening up in a bend and for wall taps; a spin or stop scores zero. The course marks are set out from each track's bends: cones at inner clipping points, yellow dashes for outer zones. **Ghost**: a see-through copy of your own car replays a recording, your fastest lap in free practice (from the line) and your highest-scoring run in a judged run (from the start); a better lap or run replaces it. Recordings are 60 frames a second and kept per track in `user://ghost_*.bin`. (An earlier rival car and tandem battles were taken out.)
- **Parking bay**: K (or TRAINING > PARK THE CAR) takes the car off the track into a marked bay by the driver's stand; R returns it to the start line.
- **The hall and the driver**: 11 x 7 m (`Hall.HalfX`): 11 x 5 m for the layouts plus 2 m of clear floor on the driver's side (`Hall.Back`). The driver stands on a low stand at the back of that floor, in front of the middle of the layout, eyes 1.9 m up (`Main.DriverEye`), and the main camera sees like a person from there: a steady field of view (58 degrees, easing to 34 far off) with the gaze about half a second ahead of the car. The sound is heard from the same place.
- **CLUB** (`tools/make_track.py club`, `assets/club.json`): the main track, straights and arcs along the route drawn on a picture of a printed drift mat: far straight with the line, the big corner at the window end, the near side, a left hairpin, back through the middle, a right hairpin round the roundabout, the long diagonal, a wide left hairpin under the pits and up the right-hand side; 30.2 m a lap, tightest bend 0.67 m. The floor sheet (`tools/make_textures.py`) paints its turf (outside the big corner), kerbs, the two blue sections, the yellow tape (big corner and start arrows) and the pit bays; three display cars stand in the bays. Kerbs are raised 5 mm (`Track.KerbH`, mesh in `scripts/Decor.cs`).
- **CLUB 2** (`tools/make_track.py club2`, `assets/club2.json`): the earlier club track, a smooth curve through chosen points after a photo of a club's room: 37.5 m a lap, three turf islands with blue tips, no yellow.
- **Lights out** (`tools/make_led.py`, `scripts/Decor.cs`, `Hall.Dark`): LIGHTS on the GAME page. The room's lights go off; LED strips are drawn as glowing ribbons on the barriers, under the kerbs and across the line, their light on the floor comes from a baked sheet for each track (`assets/tex/led*.png`, added by the floor shader), the barriers and lower walls get a wash added in the nearest strip's colour, and two lights that travel with the car (ice blue on its left, pink on its right) reach only the cars. HEADLIGHTS on the CAR page (`CarView.SetLamps`): an LED kit on the chassis, and the light drawn as additive halos at the lamps and a streaked pool on the floor, with no real light. The ghost car is made at load and drawn once under the floor so nothing loads when it appears.
- **Tyre arch** (`tools/make_arch.py`, `scripts/Decor.cs`): on CLUB 1, half a tyre across the lane with DRIFT MASTER on its sidewall.
- **Suspension** (`scripts/Suspension.cs`): the fixed shocks and lower arms are cut out of the chassis model when it loads and built again as moving parts, as the web version has them: arms hinged on the chassis that follow their wheels, coil-overs whose shafts slide and springs squeeze, and the antenna on a spring of its own. Drawn in the close views only.
- **Barriers** (`scripts/CarPhysics.cs`, `Track.ContactsFor`): two circles on the axles and a small one in each corner of the shell against the lane's edges and the hall's walls.
- **Draw calls** (`scripts/Merge.cs`): what never moves is merged into one mesh for each material (room, dressing, course marks, transmitter). The television's camera sees layer 1 (the room) and layer 3 (a block for each car) under a light of its own with no shadow; the cars' meshes are on layer 2.
- **The room** (`scripts/Room.cs`, `tools/make_room.py`): the door corner set back from the far right of the hall (a CC0 wood from ambientCG on the door), yellow notices, a fire extinguisher, a television whose screen is the overhead camera's picture (a 384 x 216 view refreshed every third frame), two printed PVC vinyl banners on the far wall (hung by cord, with a normal map of their folds) and a window with its blind down in the left wall. All our own artwork.
- **Trackside dressing** (`scripts/Decor.cs`, `tools/make_flags.py`): a few stacks of old tyres lying flat inside the two tightest bends, the raised kerbs, and fourteen flags round the lap (feather flags and framed banners, red and white, three feathers in blue, yellow and purple), each turned to face the driver.
- **RC radio** (`scripts/Radio.cs`, CONTROLLER page): a real pistol-grip transmitter plugged into the PC drives the car. The game finds the steering and throttle channels by watching which one moves, learns centre and end points, and can reverse either. Wheel and trigger act one to one; pushing the trigger brakes, then reverses.
- **Transmitter on screen** (`scripts/Transmitter.cs`): a pistol-grip radio in the bottom right corner (our own model, built from plain shapes) whose wheel and trigger follow the inputs, from a radio, a gamepad or the keys. Can be switched off.
- **Windscreen** (`assets/glass.gdshader`): a sun strip, a band of darker tint across the top of the windscreen (found in the glass mesh of each body), with the DRIFT MASTER banner on it.
- **Door stickers** (`tools/make_stickers.py`): four separate cut-vinyl stickers with no backing, stacked in a column down the driver's door under the mirror, the way sponsor decals sit on a drift car: DRIFT MASTER, Send it (script), 640kFPV and SEND IT (block capitals). White with a thin black contour on every paint. They are dropped onto the shell's surface point by point, so they follow a curved door. Each sticker is a satin sheet with the relief of its cut edges (a normal map from its outline), so it reads as vinyl lying on the glossy shell. They replace the web version's three framed strips.
- **Shells and handling**: each shell has its own mass and centre of gravity (`CarPhysics.SetBody`): the pickup is the heaviest and tallest, the coupe the lightest and lowest. The effect is real but small, as on a real car where the shell is about a tenth of the weight: in the same drift the pickup rolls about 12% more than the sedan and 20% more than the coupe and turns in a couple of milliseconds later; drift angle and speed hardly change.
- **Car options** (from the web version; kept between runs in `user://settings.cfg`): the shell on or off (the bare chassis with its body posts), neon tubes under the car in six colours with their light on the floor, and tinted or clear windows.
- **Bare room**: only the room and the track. A neon "Drift Master" sign with cable and socket is built (`Hall.BuildNeon`) but switched off for now.
- **No engine splash**: the boot image is switched off in `project.godot` (black screen until the game appears).
- **Car** (`assets/sedan.glb`): the sedan exported from the browser version, parts merged by material.

## Textures

Wall, foam, ceiling and board textures are from ambientCG.com (CC0 1.0): PaintedPlaster017, Sponge001, OfficeCeiling001, Chipboard004. The P-tile is generated.

## Not in yet

The list, in order:
1. Animated suspension on the chassis (shocks, springs, arms moving with the physics).
2. The room, further: stairs, posts, the ceiling.
3. Left-over texture and shell fitting issues.
4. An exit from every layout to a parking area (replaces the K parking bay).
5. Better textures for the walls and the artificial grass.

Later, maybe: damage, the other two bodies, more tracks; from the web car menu: ride height, wheels and tyres, stickers, body mounts.
