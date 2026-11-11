# Drift Master

Browser RC drift game. The car is the owner's own model of a 1970s two-door saloon (a BMW 2002 tii, supplied as a USDZ), used as-is at true 1/10 scale on an electric RWD drift chassis fitted to it, with paint schemes, ride height, body mounting and damage, in an indoor RC arena after reference photos (dark worn epoxy deck, turf islands with red and white kerbs, curbed C island, driver's stand). Low-res photo textures in an early-2000s style, baked glow and shadows, sampled motor and tire sound, keyboard and gamepad, fixed 1280x720 render.

## Run
Open `index.html` in a browser (it loads Three.js r128 from cdnjs). Or serve the folder:

    python3 -m http.server 8000

Note: `index.html` has no `<html>/<head>/<body>` wrapper because it was authored as a claude.ai Artifact; browsers render it fine as-is.

## Controls
Arrows/WASD drive (hold Down at a stop to reverse), C camera, G car setup, T telemetry, M mute (volume is in the menu, saved between sessions), R reset car, Esc/P pause menu.

## RC transmitter
A real RC transmitter works as a controller when it shows up on the computer as a USB joystick (many have a USB / simulator port, or use a USB sim dongle). Its wheel and trigger are axes whose order and range differ by make, so calibrate it once: OPTIONS › TRANSMITTER › CALIBRATE, then follow five steps (let go; wheel full left; full right; trigger full throttle; full brake). Each step finishes when you hold the input steady for a moment, so transmitters with no buttons work. The calibration is saved per device; after that the wheel steers and the trigger throttles and brakes proportionally. Use the keyboard for menus (Esc pause, C camera, R reset).

## Gamepad
Any controller the browser sees with the standard layout (Xbox, PlayStation, most USB pads). Press a button once after plugging in so the browser picks it up.
Left stick steer, RT/R2 throttle, LT/L2 brake (hold at a stop to reverse), A start, Y camera, X reset, View/Share setup (d-pad to change, B to close), LB telemetry Menu/Options pause menu. Barrier hits rumble.

## Car screen
Menu → CAR (or G while driving) opens one screen with every car option visible at once; the car stands still in a studio on the right (the main menu behind shows only the venue).
- LOOKS: SCHEME (solid, two-tone, twin stripes, side sweep, fade, contrast roof), COLOURS (palettes) and MAIN / SECOND / ACCENT colours, TYRES (WIDE DRIFT: 30 mm hard drift tyres on 46 mm rims, as on the video's cars; or ORIGINAL), RIMS (MODERN deep-concave twin five-spoke, COVERED solid aero disc, or the model's ORIGINAL, scaled to the tyre), RIM COLOUR (the rim's own FINISH, swatches or any colour; the centre nut keeps its finish), STICKERS (on / off: the tyre lettering on both sidewalls, a DRIFT MASTER banner across the top of the windscreen, the rear-window logo, and RACING / RACE MASTER / 640kFPV vertical stickers on the driver's door below the mirror; tyre lettering and rear logo are the owner's supplied artwork), RIDE HEIGHT, CAMBER, MOUNT (posts and R-clips, or magnets), UNDERGLOW, RESET LOOK.
- SETUP: MOTOR, TIRES (compound), GYRO GAIN, SPRINGS, SHOCK OIL, ROLL BARS, DRIVING ASSIST, with a note on what each choice does.
The car loads all black: satin-black paint, black covered rims, slammed 10 mm, 9° negative camber, pink underglow, set up for smooth, continuous drifting: 13.5T drift motor, hard tyres, 90% gyro, medium springs, oil and roll bars, assist on (picked by measuring slide time, spins and drift share on the drift loops; RESET LOOK goes back to the looks).
CAMBER runs from 12° negative (tops in) on the left through 0 (neutral) in the middle to 12° positive (tops out) on the right at the front; the rear gets half.
Changes apply instantly and are saved in the browser. The opponent gets the same car in contrasting colours. The model has a chrome delete: decorative chrome strips, door handles and the boot-lock trim are removed, and the grille, lamp rings, window frames, mirrors and wipers are satin black. The maker's badges are off and the chrome front bumper is replaced by a simple race bumper in body colour (wrapping to the front of the arches) with a black mesh intake, two side ducts and a 7 mm splitter; white 640kFPV plates front and rear.

## The car model
The shell is the owner's model (`BMW_2002ti.usdz`, not in the repo), converted by

    pip install usd-core fast-simplification numpy
    python3 tools/car_from_usdz.py <unzipped>/scene.usdc <textures dir>

The shape is never changed: the model is only scaled uniformly to true 1/10 of the real car (its wheelbase becomes 250 mm; the body is 421 x 176 x 137 mm) and turned into the game's frame. Dense trim parts (window seals and chrome strips made of thousands of tiny pieces) are reduced by quadric simplification and 0.6 mm vertex clustering; the body, glass and wheels keep every triangle (85k triangles in all, about 1.2 MB embedded as BODY_ASSET). Colours come from the model's materials. Everything else fits the shell: the chassis takes its 250 mm wheelbase and 136 mm track and carries the model's own wheels (58 mm tyres, discs and calipers) on its suspension; the physics uses the same wheelbase, track and wheel radius; the collision circles cover its narrower body; the body posts come up through its bonnet and boot lid; decals and damage work on its surface. Breakable: mirrors (each), wipers, front and rear bumpers.

## Damage
Hard hits on walls, the track edge or the opponent (over about 4 km/h into the obstacle) damage the car with the Parts Lab damage model: dents, scuffed paint and torn decals where it hit, mirrors, wing, lip, light pod, scoop, spoiler or mudflaps breaking off and lying on the track, the shell knocked off its magnets or out of its clips, and bent suspension. A bent corner pulls the car to one side. Restart or reset (R) repairs everything.

## Menu
Minimal: a small spaced title top left, the list bottom left over the venue (no panel), a thin accent line on the selected item, values arrows only on the selected one. DRIVE, TRACK, OPPONENT (none, easy, medium, hard), CAR (the car screen) and OPTIONS › (CAMERA, GHOST, VOLUME, CONTROLS). Arrows / mouse / gamepad; Esc / B goes back. Behind the menu is the selected venue, without the car. Esc / P / Menu opens the same list as a pause menu with RESUME, RESTART and MAIN MENU.

Drift only for now: grip mode is switched off (its code is kept).

## Map: RC arena
Modelled on a photo of a real indoor RC drift track, laid out to match it from the photo's viewpoint (front-right corner, over the fence). A room 11.2 x 12.8 m with a 6 m black ceiling: light grey walls to 2.6 m, a white band, then a muted red band and black (bare board-formed concrete on the stand side), conduit runs, steel double doors with an exit sign, a lit doorway and a fire extinguisher. The track is a raised deck of glossy mid-grey speckled epoxy (P-tile grip), about 8 x 10.4 m, with thick black rubber edging and a thin red line along the back corner. In it a wide C-shaped island outlined by red/white curbs (no grass), open towards the back on the fence side; inside the C a pad with a small painted circle and a line out through the opening. A red L-shaped driver's stand wraps the far corner (clear panels, red curtains below, aluminium chairs, stairs down the concrete wall). Along the right: a black chain-link fence, stacks of real-profile tires behind it and two checkered flags; crowd barriers on the left. Six ceiling cans light the track. The island and the track edge are solid.

The infield pad inside the island has its own surface, smoother and lighter than the track. Most arena textures (track deck, pad, grey wall, concrete, red band, curtains, doors) are sampled from the reference photo of the real track, with its lighting divided out, made seamless and kept small and slightly posterised, in the style of early-2000s add-on scenery.

Vertical LED tubes on the grey walls, each with its power cable looping up to a small controller on the conduit.

After a photo of a club track: the track deck is darker, and a big worn-floor sheet over it adds dusty and darker zones, dust along the edges, oil stains, hairline cracks, chips, tape left from old layouts, faded paint from old lines, fine scratches, and polished lanes with faint tyre lines along every course. Blue carpet round the deck; a DRIFT MASTER club banner, a sunset mural, track rules, a 640kFPV poster, a night-series poster, a lap-times screen and a shelf of kit boxes on the walls; rows of fluorescent strip fixtures under the ceiling. Lighting is cool fluorescent white, and the view is colour graded (a touch more contrast, cool white balance, a soft lens vignette).

## Lighting and textures
Glows (LED tape, wall washes) and shadows (under islands, props, along wall bases) are baked: soft textured planes, no extra lights or shadow maps. Photo textures are CC0 (Poly Haven, ambientCG), public domain (Wikimedia Commons) or sampled from the owner's reference photo, shrunk and posterised for an early-2000s look; see `tools/textures/SOURCES.md`.

The arena is the only map for now: the home track, warehouse, living-room and drift garage maps are removed (they are in git history), and so is the ramp layout (ramp support stays in the code).

## Courses
- **HILLSIDE** (arena): a figure of eight between low white boards with a climb and an overpass. Out of the right-hand loop you pass under the bridge, round the left-hand loop, then climb about 24 cm (4-5°) up its far side, cross over the bridge at the top and drift down the descent into the right-hand loop. Height follows each car's place on the lap, so a car under the bridge stays on the floor and cars only touch when they are at the same level; the raised road has dark side skirts (open where the lower road passes) and piers.
- **CLUB TRACK** (arena): after a photo of a club track: open dark floor round green turf islands. A sweeper round the big kidney island (three display cars parked on it), a hairpin round the tip of the J, a long left-hand loop round the strip island and a flat-out bend along the front. Red and white kerbs wrap the tips and apexes of the tight corners (and a little round island); yellow and white lines are painted on the floor. The islands are solid.
Drive through the gates in order (they are not marked on the floor); gate 0 is start/finish. Each cone you hit adds a second to the lap. Best lap is kept per course and mode.
- **FULL FLOOR** (arena): one layout over the whole floor, marked out by low white boards (after a photo of a club track): a long front straight, up the right side, behind the island, down the left side, then an S and a hairpin through the open front and a sweeping drop back onto the straight.
- **FLOW** (arena): three display cars with different setups (paint, rims, ride height, camber, glow) stand round the tyre-stack centrepiece. A drift loop in the open front of the warehouse, 1.2 m wide between white boards, with almost nothing straight: big round sweepers (about 2 m radius) at both ends, a gently bowed front run, and a shallow curve in the back run for a right-left-right transition. In its infield stands a stack of three full-size tyres with one upright on top, and a tiny-whoop FPV drone (after the owner's own: four black ducts, blue motors, clear 3-blade props, purple canopy with a tilted-up camera, antenna, battery underneath) loops round it at about 2.6 m/s, flying straight through the upright tyre's hole each lap, nose down and banking into its turns. Tuned so a lap is mostly drifting: a simple test driver slides past 20° for 75% of the lap (40-45% on the other tracks).
- **FIGURE 8** (arena): a figure of eight in the open front of the arena round two cones, right-hand loop then left-hand loop: constant transitions from one drift into the other.
- **MAIN LAYOUT** (arena): full clockwise lap: the long front sweeper, up the fence side, along the back in front of the stand, down the stand side.
- **KEYHOLE** (arena): the main lap with a detour off the fence side into the infield pad, round the painted circle and back out onto the back straight.

## Ghost
Every lap is recorded. Your best lap on each track (per drift / grip mode) is saved in the browser and replayed as a pale see-through car from the moment you cross the start line, so you can race it. Turn it off with GHOST in the menu.

## Idle reset
Once a run is under way, if the car sits still with no input for 15 s the track resets to the start (a RESET IN countdown shows for the last 5 s).

## Racing an opponent
Contact between the cars is a real two-car impact: equal masses, a little bounce (restitution 0.25), and spin from off-centre hits. Ram it from behind and it is shoved forward and turned; T-bone it and it is pushed sideways (about 28 cm at 3 m/s) and yawed; let it hit you and your car is knocked and spun. The opponent then recovers to its racing line. Hard contact damages your car like a wall hit.

Menu → OPPONENT: EASY / MEDIUM / HARD. A computer car (blue, or red if yours is blue) lines up beside you, a 3-2-1 count starts the race, and the first to finish 3 laps wins; POS shows your place. The opponent follows a racing line for each course, braking and accelerating to a speed set by the corner radius and the difficulty; in drift mode it drifts the corners with countersteer. It is solid: you can bump it, and it loses a little speed when you do.

## Rollover (grip mode)
Rubber touring tires grip hard enough that sliding sideways into a wall, the track edge or the island curb at speed trips the car over its outer wheels. It is a rigid-body roll about the wheel edge (centre of gravity 5 cm up, 10 cm in): a light hit tips it and it falls back on its wheels, a harder one puts it on its side, a big one onto its roof. Left on its side or roof it is put back on its wheels after about a second (and any drift in progress is lost). Drift mode's hard plastic tires just slide, so it does not roll.

## Drift scoring
A drift counts once the car slides past 12 deg at over 0.7 m/s. Points flow in at drift angle x speed and show live at the top of the screen with the multiplier (steps up every 2 s held, up to x5), the time held and the number of links (flicks from one side to the other, +60 each). Straighten up for 0.6 s and the drift banks (DRIFT, GOOD, GREAT, INSANE); a hard hit on a wall, the island or the track edge wipes it. SCORE is the run total; TOP DRIFT is your best single drift on that course and mode, saved in the browser.

## Sound
The motor is a synthesised whine (triangle fundamental, sine harmonics, a quiet gear-mesh tone, a little wobble) that follows motor speed from about 95 Hz at a crawl to roughly 900 Hz flat out, so you hear every bit of extra revs; it is silent off the throttle. Under it, softened (lowpassed) loops cut from a recording of a real RC drift car add tire and drive-train texture, cross-faded by slide. Everything the car makes is placed at the car and heard from the camera: further away it gets quieter, duller and panned to its side (chase cam close, driver's stand and ceiling cam far).

Wheel clearance: checked by sampling the body surface against each tyre in the game across full steering lock; with the model's inner wheel-well lining left out where the wheels sweep (RC shells are open underneath), no tyre touches the body at any steering angle, camber or allowed ride height. RIDE HEIGHT goes from -12 mm (slammed) to +10 mm. Below stock the hubs sit higher in the shell, so the arches are opened just enough round the space each tyre sweeps (its camber, the bulge of the sidewalls and, at the front, full steering lock) with 2.5 mm to spare; the rest of the shell is untouched, and nothing on the chassis reaches the body.

## Floor imperfections
Each venue's floor has a gentle height profile at RC scale, felt through the suspension at each wheel (the body heaves, pitches and rolls on its springs and dampers) and so by the FPV camera:
- Arena: cast concrete slabs 1.5 m square, 12 mm joints 1.1 mm deep, each slab up to 0.35 mm proud or low of its neighbour, a soft ±0.8 mm undulation, and nine patched repairs and dips.
Driving across the arena at 2.7 m/s that gives about ±0.6 mm of body heave and up to 0.2° of pitch and roll: felt, not harsh. Faint lines mark the joints. `GROUND` in index.html sets each venue's values.

## Camera
Four cameras (C / Y):
- TRACKSIDE (default): one good vantage point per venue, high in a corner overlooking the whole track (the arena's front-left corner at 3.1 m). It stays put, turns to stay locked on the car, and zooms so the frame is about 3.2 m wide at the car (8-60°).
- FPV: a little camera inside the car, centred in the cabin about 3.5 cm behind the base of the windshield, looking over the bonnet (the body posts and clips are hidden in this view). It rides on the sprung body, so it rolls, pitches and slides with the car and follows ramps and landings; it also feels the floor bumps through the suspension, vibrates from the tyres (slightly out of round: once per wheel turn plus the tread) and the motor (a buzz that rises with revs and throttle), and shakes after every knock (landings, wall and car hits, cones). A dark dashboard panel inside the shell hides the chassis, and the tinted windshield is nearly clear from inside.
- DRIVER POV and DRIVER POV 2: two drivers' spots per venue, eyes at standing height with a natural 55° view, head following the car, pistol-grip transmitter in hand whose wheel and trigger follow your inputs.

The title uses Michroma and the HUD and menus Saira Semi Condensed (both SIL Open Font License, embedded in the page; licence in tools/vendor). The HUD is plain text: lap, time, last, best, gate count, race position, drift score and top drift top left, the live drift counter top centre, speed and drift angle bottom right.

## Driving model
Real 1/10 numbers (SI units), stepped at 720 Hz. The car is sized like the 1/10 RWD drift cars in the reference video: about 452 mm long and 203 mm wide over the body, 257 mm wheelbase, 64 mm wheels, 1.45 kg; hard plastic tires on polished tiles or epoxy grip at about 0.5, which is what lets them slide like that.
- **Cars**: RWD drift (default: hard plastic tires, ~57 deg steering lock, 44/56 weight split, steering gyro) or 4WD grip touring (rubber tires).
- **Surface**: the arena's epoxy deck grips like P-tile (polished vinyl tiles used at dedicated drift tracks), a little more bite than bare concrete.
- **Motor**: DC torque curve against wheel speed, `F = Fstall * (throttle - wheelSpeed / vFree)`; above the throttle's speed the ESC drags the wheels back, so the trigger sets wheel speed. Motors: brushed 540, 13.5T stock, 6.5T mod, 13.5T drift (high gearing).
- **Tires**: driven wheels have their own speed. Combined slip (slip ratio + slip angle) sets the force, which points against the slip vector, so a hard-spinning rear tire gives thrust instead of side grip. Throttle sets the drift angle.
- **Drift tuning** (after a video of 1/10 RWD tandem drifting: 45-60° held at 1.5-3 m/s round 1-2 m arcs): 66° steering lock, the gyro takes over 85% of the steering at full gain and asks for a path radius that widens with speed (about 0.7 m slow to 1.5 m at drift speed), and past ~58° of slide it counter-steers harder; with the assist on, the trigger is eased past that angle and the keyboard aims for ~50°. Measured: keyboard 44° at 2.0 m/s, trigger at 55-85% 50° at 1.8-2.1 m/s, no spins; pinning the trigger at full lock can still spin it, as on a real car. The car's 1/10 scale did not need changing: the video's cars are 1/10 drift cars too, and the 2002's 250 mm wheelbase is within 3% of the usual 257 mm.
- **Gyro** (with steering mixing): the stick asks for a turn (full lock about a 1.1 m path radius, capped at 3 rad/s) and the gyro steers to hold that yaw rate, damping extra rotation so the car doesn't spin. Gain OFF/40/70/90%.
- **Driving assist** (keyboard): drift car's throttle key becomes a wheelspin governor aiming for a drift angle set by the steering (~35 deg at full lock); grip car gets grip-limited steering, traction control and ABS.
- **Suspension**: roll and pitch spring/damper systems move load between wheels; roll-bar split sets front/rear balance.
- **Setup (G)** and **telemetry (T)**: wheel loads, grip used, slip angles, g-forces, roll/pitch, steering and gyro output, drift angle, wheelspin.

## Parts Lab (new car, work in progress)
An original 1/10 car is being built part by part in the `PARTS` section of `index.html` (between the `PARTS` markers; the game still drives the current car until it is finished). `parts.html` shows the parts on a turntable:

    python3 tools/make_parts.py

Stage 1, tyres and rims: three tyre types at real 1/10 sizes (DRIFT hard plastic 64 x 26 mm, GRIP rubber with a moulded tread 64 x 26 mm, OFF-ROAD block-knob rubber 70 x 30 mm on a 50 mm rim), each with five rim sets (spoke design + finish; one beadlock with a bolted ring). Rims are modelled with a barrel, rounded lips, a dished wall, a spoke face cut from a disc, hub and 12 mm hex nut. Stage 2, chassis (`parts.html#chassis`), two builds on a 257 mm wheelbase, animated in the lab (wheels roll, steering sweeps, suspension cycles with the shocks compressing):
- DRIFT: electric RWD. Carbon plate and upper deck, 540 motor lengthways in the rear-middle (as on YD-2 / RMX-style chassis) with pinion and 64T spur on a layshaft into the rear gearbox, ESC with fan, 2S shorty LiPo, receiver and antenna, gyro, steering servo. No friction brake: electric RC cars brake through the ESC.
- OFF-ROAD: nitro 4WD. Aluminium plate, .15 engine with finned head, carburettor and glow plug, flywheel and clutch bell, tuned pipe, fuel tank, radio box, 52T spur and a disc brake with caliper on the centre shaft, shafts to front and rear diffs, longer gold shocks.
Both have lower A-arms, uprights, turnbuckle camber links, coil-over shocks on cut-out towers, driveshafts and steering links.

Stage 3, shells (`parts.html#shell`): now the owner's 2002 model (see The car model). The lofted shells built earlier (drift coupé, rally hatch, '80s coupé) and the off-road nitro chassis are in git history.
The lab shows the shell over its chassis (or alone, or see-through) with the suspension still working underneath.

Stage 4, paint (`parts.html#paint`): six schemes worked out per pixel from the shell's own shape (SOLID, TWO-TONE, TWIN STRIPES over the bonnet, roof and boot, SIDE SWEEP with a pinstripe, FADE nose to tail, CONTRAST ROOF), six palettes (SIGNAL, MIDNIGHT, LIME RUSH, SUNSET, ARCTIC, GOLD RUSH) plus free main / second / accent colour pickers, and decals laid onto the body surface: race numbers on the doors and bonnet (any number 0-99), made-up sponsors (GRIPLINE, VOLTCELL, APEX GEARS, NITRO-X, DRIFT MASTER), a windscreen banner, and mud splatter on the rally hatch. All artwork is original.

Stage 5, mounting (`parts.html#mount`): POSTS AND R-CLIPS (four 6 mm nylon posts, fronts on the shock tower and rears on a mount bar behind the gearbox, with height holes every 3 mm, rubber washers on the shell and 1 mm wire R-clips through the hole just above it) or hidden MAGNETS (10 x 3 mm neodymium discs on holders at the same points, steel discs glued inside the shell). LIFT SHELL slides the clips out and lifts the shell off.

Stage 6, damage (`parts.html#damage`): click the shell (or pick a spot) to hit it at 1-12 m/s.
- Dents: the body and its decals push in around the hit, (1 - d/r)^2 falloff, depth and size growing with speed, total push capped at 16 mm.
- Paint scuffs and torn decals around the last eight hits, worked out per pixel.
- Breakable parts (mirrors, wing, front lip, light pod, roof scoop, hatch spoiler, mudflaps) each have a toughness; a hit on or near one beats it and the part flies off and settles on the floor.
- The shell: on magnets it is knocked off above 6 m/s and lands beside the car; on posts a clip pops out above 8.5 m/s (shell sits crooked), and it tears off above 11 m/s.
- Chassis: a hit within 6 cm of a wheel above 4.5 m/s bends that corner, shown as camber and toe on the wheel (to feed into the handling when the new car goes into the game).
REPAIR puts everything back.

The game's car is built from these parts (customize screen); damage is live in the game.

## Publishing on itch.io
The game runs as-is in the browser. To make the upload:

    python3 tools/make_itch.py

This writes `dist/drift-master-itch.zip` (a full HTML document plus a bundled copy of three.js r128, MIT licence included), which runs without internet access. On itch.io: create a new project, Kind = HTML, upload the zip, tick "This file will be played in the browser", and set the viewport to 1280 x 760.

