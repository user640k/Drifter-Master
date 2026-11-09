# Carpet Circuit

Browser RC racer: a 1/10 electric touring car with a GT coupe shell, lapping a foam-barrier carpet track in a 20 ft x 20 ft living room. Early-2000s style procedural textures, synth audio (Web Audio), keyboard controls, fixed 1280x720 render.

## Run
Open `index.html` in a browser (it loads Three.js r128 from cdnjs). Or serve the folder:

    python3 -m http.server 8000

Note: `index.html` has no `<html>/<head>/<body>` wrapper because it was authored as a claude.ai Artifact; browsers render it fine as-is.

## Controls
Arrows/WASD drive (hold Down at a stop to reverse), G car setup, T telemetry, C camera, R reset car, M music, P pause, Enter start.

## Driving model
Real 1/10 electric touring car numbers (SI units), stepped at 720 Hz:
- **Motor**: DC torque curve, `F = Fstall * (throttle - wheelSpeed / vFree)`. Brushed 540 RTR ~42 km/h, 13.5T stock ~54 km/h, 6.5T mod ~83 km/h. 0-30 km/h in ~0.85 s (traction-limited).
- **ESC**: brake, then hold at a stop for reverse (capped ~12 km/h), light drag brake off throttle.
- **Tires**: per-axle slip angle into a Pacejka-style curve (peak ~7 deg, ~85% when sliding), combined with drive/brake on a friction circle, load-sensitive grip. 4WD 50/50.
- **Suspension**: roll and pitch spring/damper systems; their deflection moves load between wheels. Roll-bar split decides how much of it lands on the front or rear.
- **Setup (G)**: motor, tire compound, springs, shock oil, roll bars, keyboard steering assist. Saved in the browser.
- **Telemetry (T)**: wheel loads, grip used per axle, slip angles, g-forces, roll/pitch, wheelspin.
