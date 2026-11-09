# Carpet Circuit

Browser RC drift game: a 1/10 RWD drift car with a GT coupe shell, sliding round a foam-barrier track on polished concrete in a 20 ft x 20 ft white room with one window at dusk, lit by colour-changing LED mood lights. Early-2000s style procedural textures, synth audio (Web Audio), keyboard controls, fixed 1280x720 render.

## Run
Open `index.html` in a browser (it loads Three.js r128 from cdnjs). Or serve the folder:

    python3 -m http.server 8000

Note: `index.html` has no `<html>/<head>/<body>` wrapper because it was authored as a claude.ai Artifact; browsers render it fine as-is.

## Controls
Arrows/WASD drive (hold Down at a stop to reverse), G car setup, T telemetry, L mood lights (Dream, Sunset, Ocean, Party, Lights on), C camera, R reset car, M music, P pause, Enter start.

## Gamepad
Any controller the browser sees with the standard layout (Xbox, PlayStation, most USB pads). Press a button once after plugging in so the browser picks it up.
Left stick steer, RT/R2 throttle, LT/L2 brake (hold at a stop to reverse), A start, Y camera, X reset, View/Share setup (d-pad to change, B to close), LB telemetry, RB music, D-pad up mood lights, Menu/Options pause. Barrier hits rumble.

## Driving model
Real 1/10 numbers (SI units), stepped at 720 Hz.
- **Cars**: RWD drift (default: hard plastic tires, ~57 deg steering lock, 44/56 weight split, steering gyro) or 4WD grip touring (rubber tires).
- **Surfaces**: polished concrete (default, the most common RC drift surface), P-tile (polished vinyl tiles used at dedicated drift tracks), carpet (grip racing; drift tires hook up on it).
- **Motor**: DC torque curve against wheel speed, `F = Fstall * (throttle - wheelSpeed / vFree)`; above the throttle's speed the ESC drags the wheels back, so the trigger sets wheel speed. Motors: brushed 540, 13.5T stock, 6.5T mod, 13.5T drift (high gearing).
- **Tires**: driven wheels have their own speed. Combined slip (slip ratio + slip angle) sets the force, which points against the slip vector, so a hard-spinning rear tire gives thrust instead of side grip. Throttle sets the drift angle.
- **Gyro**: adds counter-steer against yaw rate (input minus gain x yaw rate), so the stick effectively asks for a yaw rate. Gain OFF/40/70/90%.
- **Driving assist** (keyboard): drift car's throttle key becomes a wheelspin governor (more slip the harder you steer); grip car gets grip-limited steering, traction control and ABS.
- **Suspension**: roll and pitch spring/damper systems move load between wheels; roll-bar split sets front/rear balance.
- **Setup (G)** and **telemetry (T)**: wheel loads, grip used, slip angles, g-forces, roll/pitch, steering and gyro output, drift angle, wheelspin.
- **Tire marks**: the rear tires draw into a canvas over the floor while sliding.
