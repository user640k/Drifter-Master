# Drift Master

Browser RC drift game: a 1/10 RWD drift car, the I.A.S RACER (body from a supplied, free-to-use 3D model, simplified to about 72k triangles; each wheel straightened onto its true spin axis so it doesn't wobble), on an indoor RC arena (raised epoxy track, C-shaped turf island, driver's stand). Low-res photo textures in an early-2000s style, baked glow and shadows, sampled motor and tire sound, keyboard and gamepad, fixed 1280x720 render.

## Run
Open `index.html` in a browser (it loads Three.js r128 from cdnjs). Or serve the folder:

    python3 -m http.server 8000

Note: `index.html` has no `<html>/<head>/<body>` wrapper because it was authored as a claude.ai Artifact; browsers render it fine as-is.

## Controls
Arrows/WASD drive (hold Down at a stop to reverse), V drift/grip mode, C camera, G car setup, T telemetry, M mute (volume is in the menu, saved between sessions), R reset car, Esc/P pause menu.

## Gamepad
Any controller the browser sees with the standard layout (Xbox, PlayStation, most USB pads). Press a button once after plugging in so the browser picks it up.
Left stick steer, RT/R2 throttle, LT/L2 brake (hold at a stop to reverse), A start, Y camera, X reset, View/Share setup (d-pad to change, B to close), LB telemetry D-pad down drift/grip, Menu/Options pause menu. Barrier hits rumble.

## Customize
Menu → CUSTOMIZE: paint, accent and wheel colours (swatches or any colour) and underglow colour or off. Colours only for now; the body shape is fixed. Saved in the browser.

## Menu
A single list, driven with arrows / mouse / gamepad: DRIVE, COURSE, MODE (drift / grip), CAMERA, RACE (solo, or against an opponent: easy, medium, hard), CUSTOMIZE, VOLUME, CAR SETUP, CONTROLS. Behind it the arena is shown from the viewpoint of the reference photo. Esc / P / Menu opens the same list as a pause menu with RESUME, RESTART and MAIN MENU.

## Map: RC arena
Modelled on a photo of a real indoor RC drift track, laid out to match it from the photo's viewpoint (front-right corner, over the fence). A room 11.2 x 12.8 m with a 6 m black ceiling: light grey walls to 2.6 m, a white band, then a muted red band and black (bare board-formed concrete on the stand side), conduit runs, steel double doors with an exit sign, a lit doorway and a fire extinguisher. The track is a raised deck of glossy mid-grey speckled epoxy (P-tile grip), about 8 x 10.4 m, with thick black rubber edging and a thin red line along the back corner. In it a wide C-shaped turf island (light olive artificial grass, graded to the photo) with red/white curbs, open towards the back on the fence side; inside the C a pad with a small painted circle and a line out through the opening. A red L-shaped driver's stand wraps the far corner (clear panels, red curtains below, aluminium chairs, stairs down the concrete wall). Along the right: a turf strip, a black chain-link fence, stacks of real-profile tires behind it and two checkered flags; crowd barriers on the left. Six ceiling cans light the track. The island and the track edge are solid.

Vertical LED tubes on the grey walls, each with its power cable looping up to a small controller on the conduit.

## Lighting and textures
Glows (LED tubes) and shadows (under the stand, barriers, tire stacks, along wall bases) are baked: soft textured planes, no extra lights or shadow maps. Photo textures are CC0 (Poly Haven, ambientCG) or public domain, shrunk and posterised for an early-2000s look; see `tools/textures/SOURCES.md`.

The warehouse and living-room maps are removed for now (they are in git history).

## Courses
Drive through the gates in order (the next gate glows on the floor); gate 0 is start/finish. Each cone you hit adds a second to the lap. Best lap is kept per course and mode.
- **MAIN LAYOUT**: full clockwise lap: the long front sweeper, up the fence side, along the back in front of the stand, down the stand side.
- **KEYHOLE**: the main lap with a detour off the fence side into the infield pad, round the painted circle and back out onto the back straight.
- **RAMP LAYOUT**: the main lap with five ramps of different angles: a 6 deg table-top and a 12 deg kicker on the front sweeper, an 18 deg kicker up the fence side, 8 deg rollers along the back and a 24 deg launch down the stand side. Ramp faces are solid: kickers can only be climbed from the slope side.

## Racing an opponent
Menu → RACE: VS EASY / MEDIUM / HARD. A computer car (blue, or red if yours is blue) lines up beside you, a 3-2-1 count starts the race, and the first to finish 3 laps wins; POS shows your place. The opponent follows a racing line for each course, braking and accelerating to a speed set by the corner radius and the difficulty; in drift mode it drifts the corners with countersteer. It is solid: you can bump it, and it loses a little speed when you do.

## Rollover (grip mode)
Rubber touring tires grip hard enough that sliding sideways into a wall, the track edge or the island curb at speed trips the car over its outer wheels. It is a rigid-body roll about the wheel edge (centre of gravity 5 cm up, 10 cm in): a light hit tips it and it falls back on its wheels, a harder one puts it on its side, a big one onto its roof. Left on its side or roof it is put back on its wheels after about a second (and any drift in progress is lost). Drift mode's hard plastic tires just slide, so it does not roll.

## Drift scoring
A drift counts once the car slides past 12 deg at over 0.7 m/s. Points flow in at drift angle x speed and show live at the top of the screen with the multiplier (steps up every 2 s held, up to x5), the time held and the number of links (flicks from one side to the other, +60 each). Straighten up for 0.6 s and the drift banks (DRIFT, GOOD, GREAT, INSANE); a hard hit on a wall, the island or the track edge wipes it. SCORE is the run total; TOP DRIFT is your best single drift on that course and mode, saved in the browser.

## Sound
The motor is a synthesised whine (triangle fundamental, sine harmonics, a quiet gear-mesh tone, a little wobble) that follows motor speed from about 95 Hz at a crawl to roughly 900 Hz flat out, so you hear every bit of extra revs; it is silent off the throttle. Under it, softened (lowpassed) loops cut from a recording of a real RC drift car add tire and drive-train texture, cross-faded by slide. Everything the car makes is placed at the car and heard from the camera: further away it gets quieter, duller and panned to its side (chase cam close, driver's stand and ceiling cam far).

## Camera
Two cameras (C / Y): chase, and DRIVER POV: standing on the driver's stand at about 2.75 m eye height with a natural 55 deg view, head following the car, pistol-grip transmitter in hand whose wheel and trigger follow your inputs. The HUD and menus use Saira Semi Condensed (SIL Open Font License), embedded in the page. The HUD is plain text: lap, time, last, best, gate count, race position, drift score and top drift top left, the live drift counter top centre, speed and drift angle bottom right.

## Driving model
Real 1/10 numbers (SI units), stepped at 720 Hz.
- **Cars**: RWD drift (default: hard plastic tires, ~57 deg steering lock, 44/56 weight split, steering gyro) or 4WD grip touring (rubber tires).
- **Surface**: the arena's epoxy deck grips like P-tile (polished vinyl tiles used at dedicated drift tracks), a little more bite than bare concrete.
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
