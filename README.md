# Drift Master

Browser RC drift game. The car is the owner's own model of a 1970s two-door saloon (the sedan, supplied as a USDZ), used as-is at true 1/10 scale on an electric RWD drift chassis fitted to it, with paint schemes, ride height, body mounting and damage, in an indoor RC arena after reference photos (dark worn epoxy deck, turf islands with red and white kerbs, curbed C island, driver's stand). Low-res photo textures in an early-2000s style, baked glow and shadows, sampled motor and tire sound, keyboard and gamepad, fixed 1280x720 render.

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
Menu → CAR (or G while driving): one screen, the car on a turntable at the top in the middle and every option below it.
- LOOKS: BODY (sedan / van), SHELL (on / off: off runs the bare chassis on its posts, with no body damage and no headlights), BODY KIT (original / wide body, sedan only), LIVERY (see below), COLOUR, 2ND COLOUR and ACCENT (the last two only with a livery; swatches from the club cars in the owner's photos, or any colour), RIMS (modern, covered, multi-spoke, original), NEON (off or one of five underglow colours), STICKERS (on / off), HEADLIGHTS (on / off), RIDE HEIGHT, CAMBER, MOUNT (posts / magnets) and RESET LOOK.
- SETUP: MOTOR, TIRES (compound), GYRO GAIN, SPRINGS, SHOCK OIL, ROLL BARS, ACKERMANN, CASTER, DIFF, ESC TIMING, DRIVING ASSIST, with a note on what each choice does.
  - SHOCK OIL in real weights: 10 / 15 / 20 / 25 WT (20 WT default). Thinner oil lets the body roll and pitch faster and further; thicker settles it.
  - ACKERMANN: PARALLEL / MILD / FULL. More Ackermann turns the inside front wheel further than the outside one (you can see it on the wheels); it scrubs a little front grip at big lock, so the front slides earlier and smoother.
  - CASTER: 7° / 9° / 12°. More caster gives more front bite and self-centring at big angle, and jacks the chassis as you steer (the body tilts slightly against the turn, about 0.9° at full lock with 12°, visible from FPV).
  - DIFF: SPOOL (default, both rear wheels locked together: the most predictable drift) or GEAR DIFF (the inside wheel can unload and spin, so drive is limited by the loaded wheel: smoother, less snap on throttle, harder to hold big angle).
  - ESC TIMING: BLINKY (no timing, stock-class) / BOOST (+12 % top speed, a little less torque) / BOOST + TURBO (+25 % top speed, less punch low down). You hear it as the whine climbing past where blinky stops.
 Wheels are the standard 1/10 drift size and not adjustable: 1.9 in (52 mm) rims with 64 x 26 mm hard tyres. Changes apply instantly and are saved in the browser; the opponent's car is picked at random each race (see Racing an opponent).

### Paint and liveries
The paint is a satin finish like painted polycarbonate in the club photos: soft, broad highlights and only a faint reflection, not a mirror gloss. LIVERY picks a design; each loads the colours of the photo it is after, which can then be changed:
- SOLID: one colour.
- SPLIT: the front in the main colour and the rear in the second on a raked line, a thin accent line on the split and three pinstripes running back to it (purple and teal with yellow).
- FADE: the main colour fading into the second front to back, with raked brush strokes in the accent (lime into blue with pink).
- FLAMES: flames licking back from the nose along the sides and over the bonnet, with a dark outline (purple with lime).
- CAMO: digital camo in square pixels, three tones and a few accent flecks (black and grey with teal).
- GRAFFITI: big ink strokes, an accent stroke and splatter (white, black, red).
- WAVES: rolling waves up the sides with foam lines and a sun on the doors (white, black, red).
All original artwork, no real sponsors. They are drawn per pixel from each body's own measurements (length, shoulder line, roof, width), so every livery sits the same way on all six bodies, the wide kit's fenders and the mirrors included, and dents and scuffs show through them. The rival gets a random livery and body each race.

## Real-world specification
The sim follows a modern 1/10 RWD competition drift car:
- Chassis: 257 mm wheelbase, 195 mm across the tyres (169 mm between tyre centres), 2WD rear-wheel drive with the motor rear-mid, black composite plastic with red anodised parts.
- Steering: 70° front lock (real drift chassis run 65° to over 85°); front camber adjustable to -12° (default -9°), rear half that.
- Wheels and tyres: 1.9 in (52 mm) wheels with 64 x 26 mm hard plastic drift tyres (fixed: the standard size).
- Motors: 540 sensored brushless, 10.5T or 13.5T drift (default 13.5T drift-geared); 2S shorty LiPo; high-speed servo (0.06 s) with a drift gyro.
- Bodies are fitted as RC bodies are, not scale models: each keeps its shape, stretched or shrunk lengthwise (and in height by the same factor) to the 257 mm wheelbase and widened to 196 mm so the wheels tuck under the arches. Sedan: 426 x 196 x 141 mm; van: 419 x 196 x 199 mm.
- Physics in real units: 1.45 kg, wheelbase and track as above, 32 mm wheel radius; the car's collision footprint is the 196 mm wide body.

## Bodies
Car screen → BODY: **SEDAN**, **VAN**, **MUSCLE**, **UTE**, **COUPE** or **PICKUP**; the sedan also has BODY KIT **ORIGINAL** or **WIDE BODY**. Everything else works the same on all of them: colour, rims, stickers, ride height, camber, posts or magnets, damage, underglow and the setup.
- SEDAN: the owner's two-door saloon model (see The car model below), fitted to the chassis as an RC body.
- VAN: the owner's van model (a .blend), converted by `tools/zuk_from_blend.py` (needs Blender's Python module: `uv venv -p 3.11 bpyenv && uv pip install -p bpyenv/bin/python bpy numpy pillow`). Converted at true 1/10 (270 mm wheelbase), then fitted to the chassis in the game like the sedan (see Real-world specification). Made into an RC drift shell: the model's wheels, floor pan, inner arch liners, axles and exhaust are left out, and it sits on the same drift chassis with the game's wheels. It keeps the model's texture, rust and all (its number plate is replaced by a white 640kFPV plate and the brand sun strip by a DRIFT MASTER one); paint schemes recolour its paint. Lamps glow from the model's own glow map. Stickers: No Fear on the rear-door glass, the three small stickers on the cab door. Long body posts reach the 21 cm roof.
- MUSCLE, UTE, COUPE, PICKUP: four more of the owner's models (FBX files: a 1970s four-door muscle sedan, a 1970s coupe-utility, a 1990s sports coupe in a wide body kit, and a 1960s pickup truck), converted by `tools/fbx_shells.py <key> <model.fbx> [<texture dir>]` with the same Blender module. Each is scaled to true 1/10 by its real wheelbase and emptied into an RC shell: the wheels, floor pan and underbody, interior, maker's badges and plates are left out, the chrome trim becomes satin black (a chrome delete, as on the sedan), and the rest is split into paint, black trim, glass, headlamps, tail lights and indicators, with the mirrors as their own parts (they break off). The converter also measures each body (windscreen, rear window, mirrors, lamps, plate, shoulder line, roof) and the game places everything from those numbers: the dash and a firewall under the windscreen (so the FPV view never looks down into the chassis), the DRIFT MASTER banner, the rear-window logo, the door stickers, the 640kFPV plate, the headlight and the FPV eye. Like the van, each is fitted to the chassis as an RC body: 196 mm wide with its wheelbase brought to 257 mm. The pickup's texture is cleaned up by the converter: rust, grime, dents and the flaked teal/white patina are filled from the nearest clean paint and the paint is laid back flat, so it is one fresh paint like the other bodies (its black and chrome trim stay). The low-poly bodies are subdivided round the wheels, their inner wheel tubs are left out where the tyres sweep, and the arches are opened like the others: no tyre touches any of them at full lock, at either camber extreme, slammed or at full ride height. Taller bodies sway more on the posts (the pickup about as much as the van).
- WIDE BODY kit (sedan only): moulded over-fenders in the Pandem style, after the owner's reference photo. Each is lofted round its wheel: it starts flush on the body panel, eases out (tangent to the panel) to a slim lip just past the tyre and tucks back in underneath; it tapers back into the panel at both ends and stays below the shoulder line, so there is no hard edge. A few small rivets along the lips, slim side skirts, and 10 mm wheel spacers so the wheels fill the arches.
- RIMS: modern, covered, MULTI-SPOKE (black, after the chassis photos) or original.
- HEADLIGHTS (on by default): the lamps glow and a soft, warm spotlight and beam light the floor just ahead of the car, kept dim for now. The opponent, ghost and display cars carry no light.
- The arches are opened just enough round the tyres whenever they reach further into the body than stock (slammed, bigger tyres, the wider track), allowing for camber, the spacers and full steering lock; on the low-poly van the body is first subdivided near the wheels so no big triangle cuts through a tyre. Checked by sampling every body part against every tyre across full lock for both bodies, both kits, the smallest and largest tyres, full slam and full camber both ways.
- The shell sways on its posts: a springy extra lean on top of the body roll, driven by the sideways acceleration, with a little lag and overshoot. About 1.5° per g on the sedan and 8° per g on the tall van, so the van visibly rocks from one drift into the next.

## Chassis
After the owner's photos of a modern 1/10 RWD drift chassis: black plastic plate, towers and arms with red anodised accents; a twin-rail upper spine on red posts; a long body-mount bar and bumper plate at the front; black shocks with red caps, collars and spring cups and black springs; 540 motor rear-mid with a red spur gear under a black guard; ESC with its cooling fan at the tail; a gyro with a white label on the spine; a shorty LiPo in a black hard case; red T-plug and black and red power wires. The same 257 mm chassis under both bodies.

## The car model
The shell is the owner's model (`BMW_2002ti.usdz`, not in the repo), converted by

    pip install usd-core fast-simplification numpy
    python3 tools/car_from_usdz.py <unzipped>/scene.usdc <textures dir>

The shape is never changed: the model is only scaled uniformly to true 1/10 of the real car (its wheelbase becomes 250 mm; the body is 421 x 176 x 137 mm) and turned into the game's frame. Dense trim parts (window seals and chrome strips made of thousands of tiny pieces) are reduced by quadric simplification and 0.6 mm vertex clustering; the body, glass and wheels keep every triangle (85k triangles in all, about 1.2 MB embedded as BODY_ASSET). Colours come from the model's materials. Everything else fits the shell: the chassis takes its 250 mm wheelbase and 136 mm track and carries the model's own wheels (58 mm tyres, discs and calipers) on its suspension; the physics uses the same wheelbase, track and wheel radius; the collision circles cover its narrower body; the body posts come up through its bonnet and boot lid; decals and damage work on its surface. Breakable: mirrors (each), wipers, front and rear bumpers.

## Damage
Hard hits on walls, the track edge or the opponent (over about 4 km/h into the obstacle) damage the car with the Parts Lab damage model: dents, scuffed paint and torn decals where it hit, mirrors, wing, lip, light pod, scoop, spoiler or mudflaps breaking off and lying on the track, the shell knocked off its magnets or out of its clips, and bent suspension. A bent corner pulls the car to one side. Restart or reset (R) repairs everything.

## Menu
A light, slanted pit-board panel on the left with a red and white kerb along its edge, the title in heavy italic (DRIFT in black, MASTER in red), and the list below it: the selected item is a red slanted chip with its value arrows. DRIVE, TRACK, MODE, OPPONENT and LAPS (when racing), CAR (the car screen) and OPTIONS › (CAMERA, GHOST, VOLUME, CONTROLS, TRANSMITTER). Arrows / mouse / gamepad; Esc / B goes back. Behind it is a still shot of the selected track, taken from high in the venue's photo corner and framed to fit the whole layout in the open part of the screen, with the colour grade lifted so it reads bright and clean. A track card bottom right shows the track, its short description and chips for the mode, laps, opponent and your body. Esc / P / Menu opens the same panel as a pause menu with RESUME, RESTART and MAIN MENU.

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
Five layouts on the warehouse floor (the old curbed C island and infield pad are gone, so the whole floor is open):
- **EMPTY**: the open floor, no layout: cones scattered at random (a new set every time; knock them over) and a few groups of full-size plastic chairs (the car fits under a seat; the legs are solid). Free drive, no laps.
- **FLOW**: a drift loop in the front of the warehouse, 1.2 m wide between low white boards, almost nothing straight; the tyre-stack centrepiece with the tiny whoop flying through it and three display cars.
- **HILLSIDE**: a figure of eight between boards with a 24 cm climb (4-5°) and an overpass; height follows each car's place on the lap, so a car under the bridge stays on the floor.
- **CLUB TRACK**: open dark floor round green turf islands with red/white kerbs (after a photo of a club track); eight checkpoints round the racing line, so every island has to be gone round the right way for a lap to count.
- **JUMP**: a drift loop over the whole floor between boards with a jump on the fence-side run: a 13 cm, 20° kicker launches you over two cars parked across the track onto a landing ramp. Hit the lip at about 3 m/s (11 km/h on the HUD) to clear them; slower and you land on the cars.

On the board tracks the opponent drives a computed racing line: the centreline pulled to the inside of each bend within the lane (relaxed at several scales, a quick minimum-curvature line), kept 16 cm off the boards.

## Modes
Menu → MODE: **PRACTICE** (endless laps, best lap and ghost), **TIME TRIAL** (LAPS laps against the clock from a countdown; cone seconds count; best total kept per track, lap count and car mode) or **RACE** (LAPS laps against the OPPONENT: easy, medium or hard). LAPS: 3, 5, 10 or 20.

## Ghost
Every lap is recorded. Your best lap on each track (per drift / grip mode) is saved in the browser and replayed as a pale see-through car from the moment you cross the start line, so you can race it. Turn it off with GHOST in the menu.

## Idle reset
Once a run is under way, if the car sits still with no input for 15 s the track resets to the start (a RESET IN countdown shows for the last 5 s).

## Racing an opponent
Contact between the cars is a real two-car impact: equal masses, a little bounce (restitution 0.25), and spin from off-centre hits. Ram it from behind and it is shoved forward and turned; T-bone it and it is pushed sideways (about 28 cm at 3 m/s) and yawed; let it hit you and your car is knocked and spun. The opponent then recovers to its racing line. Hard contact damages your car like a wall hit.

Menu → OPPONENT: EASY / MEDIUM / HARD. A computer car lines up beside you - a different one every race: any of the six bodies (the sedan stock or wide), in a random solid colour that is never yours, with its own rims, rim colour and stickers - a 3-2-1 count starts the race, and the first to finish 3 laps wins; POS shows your place. The opponent follows a racing line for each course, braking and accelerating to a speed set by the corner radius and the difficulty; in drift mode it drifts the corners with countersteer. It is solid: you can bump it, and it loses a little speed when you do.

## Rollover (grip mode)
Rubber touring tires grip hard enough that sliding sideways into a wall, the track edge or the island curb at speed trips the car over its outer wheels. It is a rigid-body roll about the wheel edge (centre of gravity 5 cm up, 10 cm in): a light hit tips it and it falls back on its wheels, a harder one puts it on its side, a big one onto its roof. Left on its side or roof it is put back on its wheels after about a second (and any drift in progress is lost). Drift mode's hard plastic tires just slide, so it does not roll.

## Drift scoring
A drift counts once the car slides past 12 deg at over 0.7 m/s. Points flow in at drift angle x speed and show live at the top of the screen with the multiplier (steps up every 2 s held, up to x5), the time held and the number of links (flicks from one side to the other, +60 each). Straighten up for 0.6 s and the drift banks (DRIFT, GOOD, GREAT, INSANE); a hard hit on a wall, the island or the track edge wipes it. SCORE is the run total; TOP DRIFT is your best single drift on that course and mode, saved in the browser.

## Sound
The motor is synthesised after a sensored brushless 1:10 drift car: a buzzy commutation whine (sawtooth plus harmonics, through a resonant band that rises with it) tied to the motor's speed, from about 200 Hz pulling away to roughly 2.4 kHz flat out; the pinion/spur gear mesh as a thinner tone above it with a slow beat; a low drivetrain growl (belt, spool, driveshafts) that comes up under load; and the ESC's thin drive-frequency squeal at low speed on part throttle. It spools: the motor's speed flares up the instant you squeeze (wheelspin) and winds down more slowly than it rises when you lift, so the whine sweeps up and falls away, and it keeps coasting audibly instead of cutting out. With BOOST the whine keeps climbing past where blinky stops, and TURBO adds a hard rasp at the top. Under it, softened loops cut from a recording of a real RC drift car add tyre and drivetrain texture, cross-faded by slide. Everything the car makes is placed at the car and heard from the camera: further away it gets quieter, duller and panned to its side.

Wheel clearance: checked by sampling the body surface against each tyre in the game across full steering lock; with the model's inner wheel-well lining left out where the wheels sweep (RC shells are open underneath), no tyre touches the body at any steering angle, camber or allowed ride height. RIDE HEIGHT goes from -12 mm (slammed) to +10 mm. Below stock the hubs sit higher in the shell, so the arches are opened just enough round the space each tyre sweeps (its camber, the bulge of the sidewalls and, at the front, full steering lock) with 2.5 mm to spare; the rest of the shell is untouched, and nothing on the chassis reaches the body.

## Floor imperfections
Each venue's floor has a gentle height profile at RC scale, felt through the suspension at each wheel (the body heaves, pitches and rolls on its springs and dampers) and so by the FPV camera:
- Arena: cast concrete slabs 1.5 m square, 12 mm joints 1.1 mm deep, each slab up to 0.35 mm proud or low of its neighbour, a soft ±0.8 mm undulation, and nine patched repairs and dips.
Driving across the arena at 2.7 m/s that gives about ±0.6 mm of body heave and up to 0.2° of pitch and roll: felt, not harsh. Faint lines mark the joints. `GROUND` in index.html sets each venue's values.

## Camera
Five cameras (C / Y):
- TRACKSIDE (default): one good vantage point per venue, high in a corner overlooking the whole track (the arena's front-left corner at 3.1 m). It stays put, turns to stay locked on the car, and zooms so the frame is about 3.2 m wide at the car (8-60°).
- FPV: a little camera inside the car, centred in the cabin about 3.5 cm behind the base of the windshield, looking over the bonnet (the body posts and clips are hidden in this view). It rides in the shell itself, so it rolls with the body, sways with the shell on its posts (more in the taller van), tilts with caster jacking when you steer, pitches and slides with the car and follows ramps and landings; it also feels the floor bumps through the suspension, vibrates from the tyres (slightly out of round: once per wheel turn plus the tread) and the motor (a buzz that rises with revs and throttle), and shakes after every knock (landings, wall and car hits, cones). A dark dashboard panel inside the shell hides the chassis, and the tinted windshield is nearly clear from inside.
- DRIVER POV and DRIVER POV 2: two drivers' spots per venue, eyes at standing height with a natural 55° view, head following the car (the transmitter in hand is hidden until a better model is made).
- TOP DOWN: straight above the car about 2.6 m up, the track kept in a fixed orientation (it does not spin with the car), following with a soft lag and leading a little in the direction of travel.
With SHELL OFF the FPV camera sits at the same spot on the bare chassis.

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

