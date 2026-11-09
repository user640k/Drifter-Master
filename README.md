# Carpet Circuit

Browser RC drift game: a 1/10 RWD drift car with a gloss red S14-style shell (deep bumper with a dark mouth, slanted headlights with amber corners, bolt-on overfenders, silver 5-spoke deep-dish wheels), sliding round a foam-barrier track on polished concrete in a 20 ft x 20 ft white room with the curtains drawn, lit by colour-changing LED mood lights. Early-2000s style procedural textures, synth audio (Web Audio), keyboard controls, fixed 1280x720 render.

## Run
Open `index.html` in a browser (it loads Three.js r128 from cdnjs). Or serve the folder:

    python3 -m http.server 8000

Note: `index.html` has no `<html>/<head>/<body>` wrapper because it was authored as a claude.ai Artifact; browsers render it fine as-is.

## Controls
Arrows/WASD drive (hold Down at a stop to reverse), V drift/grip mode, G car setup, T telemetry, L lights (Party, Lights on), C camera, R reset car, Esc/P pause menu.

## Gamepad
Any controller the browser sees with the standard layout (Xbox, PlayStation, most USB pads). Press a button once after plugging in so the browser picks it up.
Left stick steer, RT/R2 throttle, LT/L2 brake (hold at a stop to reverse), A start, Y camera, X reset, View/Share setup (d-pad to change, B to close), LB telemetry, D-pad up lights, D-pad down drift/grip, Menu/Options pause menu. Barrier hits rumble.

## Menu
A single list, driven with arrows / mouse / gamepad: DRIVE, COURSE, MODE (drift / grip), CAMERA, LIGHTS, CAR SETUP, CONTROLS. Esc / P / Menu opens the same list as a pause menu with RESUME, RESTART and MAIN MENU.

## Courses
Drive through the gates in order (the next gate glows on the floor and on the minimap); gate 0 is start/finish. Each cone you hit adds a second to the lap; knocked cones reset after 3 s. Best lap is kept per course and mode.
- **CONES · EASY**: wide three-cone slalom, round the east cone, back along the north side, round the west cone.
- **CONES · HARD**: tight four-cone slalom, one and a half laps round a four-cone box, a narrow gate, a hairpin and a tight gate home.
- **DRIFT TRACK**: enclosed foam-barrier track on the bare floor (1.3 m between barriers), long sweepers round the room and a flowing transition along the top; four checkpoints.
- **RAMP PARK**: low plywood ramps (two kickers, a table-top, a set of rollers) marked out with cones. The car leaves the ground, flies and lands with a thump. Ramp faces are solid: kickers can only be climbed from the slope side, and the side edges block; the table-top and rollers can be driven either way.
- **LIVING ROOM**: furniture course past the sofa, between the pouf and the TV stand, under the coffee table between its legs and past the armchair. The rug grips like carpet.

## Sound
Motor and tire sounds are loops cut from a recording of a real RC drift car on polished concrete (steady driving, hard drifting, and the tire-scrub band), cross-faded and pitch-shifted live from motor speed, throttle and slide. The motor is silent off the throttle.

## Cameras (C)
Chase, DRIVER POV (standing in the corner at 1.65 m eye height with a natural 55 deg view, head following the car, pistol-grip transmitter in hand whose wheel and trigger follow your inputs), bumper, ceiling.

## Driving model
Real 1/10 numbers (SI units), stepped at 720 Hz.
- **Cars**: RWD drift (default: hard plastic tires, ~57 deg steering lock, 44/56 weight split, steering gyro) or 4WD grip touring (rubber tires).
- **Surfaces**: polished concrete (default, the most common RC drift surface), P-tile (polished vinyl tiles used at dedicated drift tracks), carpet (grip racing; drift tires hook up on it).
- **Motor**: DC torque curve against wheel speed, `F = Fstall * (throttle - wheelSpeed / vFree)`; above the throttle's speed the ESC drags the wheels back, so the trigger sets wheel speed. Motors: brushed 540, 13.5T stock, 6.5T mod, 13.5T drift (high gearing).
- **Tires**: driven wheels have their own speed. Combined slip (slip ratio + slip angle) sets the force, which points against the slip vector, so a hard-spinning rear tire gives thrust instead of side grip. Throttle sets the drift angle.
- **Gyro** (with steering mixing): the stick asks for a turn (full lock about a 1.1 m path radius, capped at 3 rad/s) and the gyro steers to hold that yaw rate, damping extra rotation so the car doesn't spin. Gain OFF/40/70/90%.
- **Driving assist** (keyboard): drift car's throttle key becomes a wheelspin governor aiming for a drift angle set by the steering (~35 deg at full lock); grip car gets grip-limited steering, traction control and ABS.
- **Suspension**: roll and pitch spring/damper systems move load between wheels; roll-bar split sets front/rear balance.
- **Setup (G)** and **telemetry (T)**: wheel loads, grip used, slip angles, g-forces, roll/pitch, steering and gyro output, drift angle, wheelspin.
- **Tire marks**: the rear tires draw into a canvas over the floor while sliding.

## Car viewer
`viewer.html` is a rotatable studio view of the car with numbered parts and the reference photo, for reviewing the shell. It is generated from the car code in `index.html`:

    python3 tools/make_viewer.py

## Publishing on itch.io
The game runs as-is in the browser. To make the upload:

    python3 tools/make_itch.py

This writes `dist/carpet-circuit-itch.zip` (a full HTML document plus a bundled copy of three.js r128, MIT licence included), which runs without internet access. On itch.io: create a new project, Kind = HTML, upload the zip, tick "This file will be played in the browser", and set the viewport to 1280 x 760.
