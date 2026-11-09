# Carpet Circuit

Browser RC racer: a 1/10 electric touring car with a GT coupe shell, lapping a foam-barrier carpet track in a 20 ft x 20 ft living room. Early-2000s style procedural textures, synth audio (Web Audio), keyboard controls, fixed 1280x720 render.

## Run
Open `index.html` in a browser (it loads Three.js r128 from cdnjs). Or serve the folder:

    python3 -m http.server 8000

Note: `index.html` has no `<html>/<head>/<body>` wrapper because it was authored as a claude.ai Artifact; browsers render it fine as-is.

## Controls
Arrows/WASD drive, Space handbrake, C camera, R reset car, M music, P pause, Enter start.
