# Drift Master

Browser RC drift game: a 1/10 RWD drift car, the I.A.S RACER (body from a supplied, free-to-use 3D model, simplified to about 72k triangles; each wheel straightened onto its true spin axis so it doesn't wobble), on two venues: a cozy home track (a wallpapered room with a track board in it) and a club garage after a reference video (black glossy tiles, white board walls, planter islands). Low-res photo textures in an early-2000s style, baked glow and shadows, sampled motor and tire sound, keyboard and gamepad, fixed 1280x720 render.

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
Menu → CAR → CUSTOMIZE: paint, accent and wheel colours (swatches or any colour) and underglow colour or off. Colours only for now; the body shape is fixed. Saved in the browser.

## Menu
Short and driven with arrows / mouse / gamepad: DRIVE, TRACK, OPPONENT (none, easy, medium, hard), CAR › and OPTIONS ›. CAR holds MODE (drift / grip), CUSTOMIZE and CAR SETUP; OPTIONS holds CAMERA, VOLUME and CONTROLS. Esc / B goes back out of a submenu. Under the list one line names the venue and the track. Behind it the selected venue is shown from its photo viewpoint. Esc / P / Menu opens the same list as a pause menu with RESUME, RESTART and MAIN MENU.

## Map: HOME TRACK
A cozy room with a drift track built in it (own layout, after a picture of a home-made track). Photo textures throughout: William Morris *Strawberry Thief* wallpaper (1883, public domain) on all four walls, a herringbone parquet floor, a plaster ceiling, a Persian rug (from the Ardabil carpet), dark grey painted boards for the track, bordered by low white flexible walls with warm fairy lights on little posts all the way round, pink and blue LED strips glowing along two sides, three turf islands with rocks and marker flags, a little white pit building with lit windows behind the back wall, a silver foil sheet over one wall, a panelled door and a floor lamp. Night-time mood: the fairy lights and LEDs do most of the lighting, all baked except a few point lights.

## Map: DRIFT GARAGE
Modelled on a video of a club track in an industrial unit, stripped back for now to the track itself. Black glossy floor tiles with barely visible joints (the track is the floor itself, P-tile grip), the track marked out only by low white flexible boards: a hazard-striped run along the back and thick red/white curbs round the right-hand end. Three planter islands with white borders, soil, moss mounds, rocks and plants: a long one in the middle with a vine climbing a pole, a teardrop one with a wrecked-car diorama, a small round one. Plain walls (black drapes at the back, grey elsewhere), purple and blue uplight washes in the back corners and white LED panels in the ceiling that show up as reflections in the floor. The islands and the boards are solid. Round the outside of the track: white plastic patio tables and monobloc chairs, 14 national flags round the walls (public-domain images from Wikimedia Commons) and strings of colourful party bunting criss-crossing the ceiling.

## Lighting and textures
Glows (fairy lights, LED strips) and shadows (under islands, props, along wall bases) are baked: soft textured planes, no extra lights or shadow maps. Photo textures are CC0 (Poly Haven, ambientCG), public domain (Wikimedia Commons) or sampled from the owner's reference photo, shrunk and posterised for an early-2000s look; see `tools/textures/SOURCES.md`.

The warehouse, living-room and RC arena maps are removed for now (they are in git history).

## Courses
Drive through the gates in order (they are not marked on the floor); gate 0 is start/finish. Each cone you hit adds a second to the lap. Best lap is kept per course and mode.
- **HOME LOOP** (home track): along the front, up the pink-lit side, behind the islands past the pit building, back down the blue side.
- **GARAGE LAYOUT** (garage): the long front straight under the drivers' platform, round the outside of the wreck island along the curbs, the back sweeper past the hazard boards, down the left side.
- **HAIRPIN** (garage): the garage lap with a full loop round the wreck island.

## Racing an opponent
Menu → OPPONENT: EASY / MEDIUM / HARD. A computer car (blue, or red if yours is blue) lines up beside you, a 3-2-1 count starts the race, and the first to finish 3 laps wins; POS shows your place. The opponent follows a racing line for each course, braking and accelerating to a speed set by the corner radius and the difficulty; in drift mode it drifts the corners with countersteer. It is solid: you can bump it, and it loses a little speed when you do.

## Rollover (grip mode)
Rubber touring tires grip hard enough that sliding sideways into a wall, the track edge or the island curb at speed trips the car over its outer wheels. It is a rigid-body roll about the wheel edge (centre of gravity 5 cm up, 10 cm in): a light hit tips it and it falls back on its wheels, a harder one puts it on its side, a big one onto its roof. Left on its side or roof it is put back on its wheels after about a second (and any drift in progress is lost). Drift mode's hard plastic tires just slide, so it does not roll.

## Drift scoring
A drift counts once the car slides past 12 deg at over 0.7 m/s. Points flow in at drift angle x speed and show live at the top of the screen with the multiplier (steps up every 2 s held, up to x5), the time held and the number of links (flicks from one side to the other, +60 each). Straighten up for 0.6 s and the drift banks (DRIFT, GOOD, GREAT, INSANE); a hard hit on a wall, the island or the track edge wipes it. SCORE is the run total; TOP DRIFT is your best single drift on that course and mode, saved in the browser.

## Sound
The motor is a synthesised whine (triangle fundamental, sine harmonics, a quiet gear-mesh tone, a little wobble) that follows motor speed from about 95 Hz at a crawl to roughly 900 Hz flat out, so you hear every bit of extra revs; it is silent off the throttle. Under it, softened (lowpassed) loops cut from a recording of a real RC drift car add tire and drive-train texture, cross-faded by slide. Everything the car makes is placed at the car and heard from the camera: further away it gets quieter, duller and panned to its side (chase cam close, driver's stand and ceiling cam far).

## Camera
Three cameras (C / Y): chase, DRIVER POV and DRIVER POV 2 - two drivers' spots per venue (home: at the front by the rug and at the side by the foil wall; garage: standing at the front edge and at the back wall), eyes at standing height with a natural 55 deg view, head following the car, pistol-grip transmitter in hand whose wheel and trigger follow your inputs. The HUD and menus use Saira Semi Condensed (SIL Open Font License), embedded in the page. The HUD is plain text: lap, time, last, best, gate count, race position, drift score and top drift top left, the live drift counter top centre, speed and drift angle bottom right.

## Driving model
Real 1/10 numbers (SI units), stepped at 720 Hz. The car is sized like the 1/10 RWD drift cars in the reference video: about 452 mm long and 203 mm wide over the body, 257 mm wheelbase, 64 mm wheels, 1.45 kg; hard plastic tires on polished tiles or epoxy grip at about 0.5, which is what lets them slide like that.
- **Cars**: RWD drift (default: hard plastic tires, ~57 deg steering lock, 44/56 weight split, steering gyro) or 4WD grip touring (rubber tires).
- **Surface**: both tracks grip like P-tile (polished vinyl tiles used at dedicated drift tracks), a little more bite than bare concrete.
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
