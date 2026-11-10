# Drift Master

Browser RC drift game: a 1/10 RWD drift car, the I.A.S RACER (body from a supplied, free-to-use 3D model, simplified to about 72k triangles; each wheel straightened onto its true spin axis so it doesn't wobble), on two maps: an indoor RC arena (raised epoxy track, C-shaped turf island, driver's stand) and a blacklight glow track (neon rails, acrylic bridge, ramps, twinkling starfield). Low-res photo textures in an early-2000s style, baked glow and shadows, sampled motor and tire sound, keyboard and gamepad, fixed 1280x720 render.

## Run
Open `index.html` in a browser (it loads Three.js r128 from cdnjs). Or serve the folder:

    python3 -m http.server 8000

Note: `index.html` has no `<html>/<head>/<body>` wrapper because it was authored as a claude.ai Artifact; browsers render it fine as-is.

## Controls
Arrows/WASD drive (hold Down at a stop to reverse), V drift/grip mode, G car setup, T telemetry, L lights (Party, Lights on), M mute (volume is in the menu, saved between sessions), C camera, R reset car, Esc/P pause menu.

## Gamepad
Any controller the browser sees with the standard layout (Xbox, PlayStation, most USB pads). Press a button once after plugging in so the browser picks it up.
Left stick steer, RT/R2 throttle, LT/L2 brake (hold at a stop to reverse), A start, Y camera, X reset, View/Share setup (d-pad to change, B to close), LB telemetry, D-pad up lights, D-pad down drift/grip, Menu/Options pause menu. Barrier hits rumble.

## Customize
Menu → CUSTOMIZE: paint, accent and wheel colours (swatches or any colour), underglow colour or off, and sliders for wing height (0 = no wing), body width, ride height, camber and window tint. Looks only; handling is unchanged. Saved in the browser.

## Menu
A single list, driven with arrows / mouse / gamepad: DRIVE, COURSE, MODE (drift / grip), CUSTOMIZE, CAMERA, LIGHTS, CAR SETUP, CONTROLS. Esc / P / Menu opens the same list as a pause menu with RESUME, RESTART and MAIN MENU.

## Map: RC arena
Modelled on a real indoor RC drift track. A room 13.2 x 18 m with a 6.5 m black ceiling: grey walls to 2.6 m, a white band, then red and black (bare board-formed concrete on one side), conduit runs, steel double doors with an exit sign, a lit doorway and a fire extinguisher. The track is a raised deck of dark speckled epoxy (P-tile grip) with black rubber edging, about 9 x 12 m. In it, a C-shaped artificial-turf island with red/white curbs; inside the C a practice pad with a painted circle and a line out through the opening at the back. A red driver's stand in the back corner (clear panels, curtains below, aluminium chairs, stairs) is where DRIVER POV stands. Along one side a turf strip, a chain-link fence, tire stacks and a checkered flag; crowd barriers along the other. Six ceiling cans light the track. The turf island and the track edge are solid.

Vertical LED tubes on the grey walls, each with its power cable looping up to a small controller on the conduit; they glow white with LIGHTS ON and cycle colours for PARTY.

## Map: glow track
Modelled on a blacklight RC track. Black carpet lanes boxed in by fluorescent rails glowing violet: an outer box round a central island with a chicane on one side. An acrylic arched bridge with red LED tape, a black kicker and a black table-top, a raised back border washed cyan from underneath with cross bracing, a dark starfield on the walls and ceiling whose brighter stars twinkle, a yellow loop and a quarter pipe as set dressing, diamond-plate floor outside. There are no lights in this map at all: rails and LEDs are self-lit and every glow and shadow is baked onto flat planes, so it is the cheapest map to render.

## Lighting and textures
Glows (LED tubes, rails, signs) and shadows (under the stand, barriers, tire stacks, ramps, along wall bases) are baked: soft textured planes, no extra lights or shadow maps. Photo textures are CC0 (Poly Haven, ambientCG) or public domain, shrunk and posterised for an early-2000s look; see `tools/textures/SOURCES.md`.

The warehouse and living-room maps are removed for now (they are in git history).

## Courses
Drive through the gates in order (the next gate glows on the floor); gate 0 is start/finish. Each cone you hit adds a second to the lap; knocked cones reset after 3 s. Best lap is kept per course and mode.
- **MAIN LAYOUT**: full clockwise lap of the arena: the long front sweeper, up the fence side, along the back past the stand, down the other side.
- **KEYHOLE**: the main lap with a detour off the back straight into the infield pad, round the painted circle and back out.
- **GLOW CIRCUIT** (glow track): over the acrylic bridge, up the east lane through the chicane, over the table-top along the back border, down the west lane over the kicker.

## Sound
Motor and tire sounds are loops cut from a recording of a real RC drift car on polished concrete (steady driving, hard drifting, and the tire-scrub band), cross-faded and pitch-shifted live from motor speed, throttle and slide. The motor is silent off the throttle.

## Cameras (C)
The HUD and menus use Saira Semi Condensed (SIL Open Font License), embedded in the page. The HUD is plain text: lap, time, last, best and gate count top left, speed and drift angle bottom right; no minimap.

Chase, DRIVER POV (standing on the driver's stand at about 2.75 m eye height with a natural 55 deg view, head following the car, pistol-grip transmitter in hand whose wheel and trigger follow your inputs), ceiling.

## Driving model
Real 1/10 numbers (SI units), stepped at 720 Hz.
- **Cars**: RWD drift (default: hard plastic tires, ~57 deg steering lock, 44/56 weight split, steering gyro) or 4WD grip touring (rubber tires).
- **Surface**: the arena's epoxy deck and the glow track's carpet both grip like P-tile (polished vinyl tiles used at dedicated drift tracks), a little more bite than bare concrete.
- **Motor**: DC torque curve against wheel speed, `F = Fstall * (throttle - wheelSpeed / vFree)`; above the throttle's speed the ESC drags the wheels back, so the trigger sets wheel speed. Motors: brushed 540, 13.5T stock, 6.5T mod, 13.5T drift (high gearing).
- **Tires**: driven wheels have their own speed. Combined slip (slip ratio + slip angle) sets the force, which points against the slip vector, so a hard-spinning rear tire gives thrust instead of side grip. Throttle sets the drift angle.
- **Gyro** (with steering mixing): the stick asks for a turn (full lock about a 1.1 m path radius, capped at 3 rad/s) and the gyro steers to hold that yaw rate, damping extra rotation so the car doesn't spin. Gain OFF/40/70/90%.
- **Driving assist** (keyboard): drift car's throttle key becomes a wheelspin governor aiming for a drift angle set by the steering (~35 deg at full lock); grip car gets grip-limited steering, traction control and ABS.
- **Suspension**: roll and pitch spring/damper systems move load between wheels; roll-bar split sets front/rear balance.
- **Setup (G)** and **telemetry (T)**: wheel loads, grip used, slip angles, g-forces, roll/pitch, steering and gyro output, drift angle, wheelspin.
- **Tire marks**: the rear tires draw into a canvas over the floor while sliding.

## Car viewer
`viewer.html` is a rotatable studio view of the car (default look) with numbered parts, for reviewing the shell. It is generated from the car code in `index.html`:

    python3 tools/make_viewer.py

## Publishing on itch.io
The game runs as-is in the browser. To make the upload:

    python3 tools/make_itch.py

This writes `dist/drift-master-itch.zip` (a full HTML document plus a bundled copy of three.js r128, MIT licence included), which runs without internet access. On itch.io: create a new project, Kind = HTML, upload the zip, tick "This file will be played in the browser", and set the viewport to 1280 x 760.

## Car model
The body comes from a 3D car model supplied with the project as free to use (OBJ + textures, not in the repo because of size). `tools/car_parse_obj.py` and `tools/car_build_asset.py` group its 130 parts by role (paint, glass, trim, lights, four wheel assemblies split into spinning parts and calipers), simplify it from about 280k to 72k triangles, scale it to 1/10.2 (257 mm wheelbase) and pack it as quantised base64 arrays embedded in `index.html`. The tail-light texture is used; the number-plate texture (brand name) and the tire-sidewall texture (tire brand logo) are left out.
