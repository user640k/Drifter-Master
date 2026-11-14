# Drift Master (desktop): changelog and features

## Changelog (this build)

- Drift counter (GAME page, DRIFT COUNTER: ON or OFF): a small read-out in the top right corner, plain milky-white lettering with a soft shadow and nothing round it. Points build while the car is sideways (angle times speed); a change of direction without straightening up is a LINK, shown as a growing chain, and each link raises the multiplier. The run ends a little after the car straightens, and a hard knock loses it. When a drift gets big a call-out appears (NICE, GREAT, INSANE, MONSTER DRIFT) in vibrant pink, a little see-through, for one second, then breaks up into dust. Your best drift on each track and the points you have banked in all are kept between sessions (BEST under the read-out; DRIFT RECORDS on the GAME page). The announcer's line plays at MONSTER DRIFT once a recording is added (assets/snd/monster_drift.wav); there is none yet.
- OPPONENT (TRAINING page, ON or OFF): a pace car on a clean drift line, to follow and learn the track from. It holds the middle of the lane with a drift angle that suits each bend, never touches a barrier and makes no mistakes. It cannot be hit (your car passes through it), and it waits when it gets a few metres ahead. R puts you both back on the line. Free practice only.
- Lights out, on BIG S: the tyre stack is hung with a string of Christmas lights (twenty bulbs, red, green, gold and blue, breathing slowly), and the drone carries a little cool-white LED that pulses.
- The room has a name: CAVE, shown in the corner of the screen with the track's name.
- BIG S, a fifth track in the Cave (42.8 m a lap, the longest): the line drawn over a photo of a club's track, laid out to fit the room. From the line on the right-hand side: up to the far wall and all along it, down the left, back under the far-left island and up in an S onto a second run to the right, down round a hook, up to a hairpin, down the middle of the room, left along the near side, a step up, out to the left, the big left-hander, and the long near straight. It fills the room, so its S-bends are tight (0.45 to 0.5 m).
- On BIG S: a stack of three full-size tyres with a fourth upright on top, and a tiny whoop drone flying a loop through its hole and round the stack (both after the web version).
- The Warehouse map is put away for now (the web version's hall was brought over as a model and worked; `tools/web/export_warehouse.js` makes it again).
- Carbon bonnet, reworked on every body that has one: a panel with a clean, sharp outline (straight across under the windscreen, sides drawing in a little towards the nose, rounded corners), an even fine 2x2 twill in satin black, flat and stiff where the rest of the shell waves, and mirroring the room less than the glossy paint.
- LIGHTS OUT (GAME page, LIGHTS: ON or OUT). The ceiling lights and the window go dark and the track is lit by LED strips: ice blue along the barriers on the left of travel, pink on the right, red under the kerbs, white across the line. Their light on the floor is baked for each track; the barriers and the lower walls take a wash of the nearest strip's colour; the cars are lit as they pass. CLUB 1 has more: amber start arrows, lit pit bays and blue sections, an amber ring on the roundabout, a white strip inside the arch. A green EXIT sign over the door.
- HEADLIGHTS (CAR page): the shell's own moulded headlamps light up white and its tail lamps red, with an LED kit on the chassis behind them (a bar with two LEDs at each end). Each lit lamp has its reflection lying flat on the floor, a streak that starts by the car and runs towards the camera. Nothing glows in the air round the car. No real lights are used, so they cost next to nothing. SEDAN, COUPE and TUNER have them; PICKUP and VAN have no working lamps (the menu says so).
- Changing TRACK or LIGHTS in the menu builds the hall again and comes back to the menu on the same row, in place of dropping you onto the track.
- Lights out costs less: the strips' light on the car comes from two lights that travel with it (ice blue from its left barrier, pink from its right) in place of sixteen along the lane.
- Ghost: the ghost car is made when the track loads and drawn once out of sight, so there is no hitch when it first appears; its file is read and written in one piece.
- A tyre arch on CLUB 1, over the right-hand side of the lap with its face to the driver: DRIFT MASTER round the sidewall, tread on top, a yellow panel underneath.
- Two club tracks. CLUB 1, the main track, is the mat layout below. CLUB 2 is the previous club track (37.5 m, the long S round three turf islands) with no yellow on it and a blue tip on each island. F1 goes CLUB 1, CLUB 2, HILLSIDE, INFIELD.
- Working suspension: at each corner the lower arm follows its wheel up and down and the coil-over shock works with it (the shaft slides, the spring squeezes). The antenna whips with the car. Seen best on board with the shell off, or in the CAR and SETUP previews.
- Barriers: the shell's four corners now meet the boards as well as the two axle circles, so a corner no longer pokes through a board when the car meets it at an angle. Tested by an hour of random driving on each track: no way through a barrier, no car left stuck.
- Performance: about half the draw calls from the driver's stand on CLUB 1 (690 to 337). The wall television no longer draws the whole room, cars and shadows again every third frame (it shows the room and a plain block for each car, lit without shadows), which was a regular hitch. Fixed parts of the room, trackside dressing, course marks and the transmitter are merged; the display cars draw no chassis and cast no shadow; what is under the shell casts no shadow.
- Roof-stick camera: 21.5 cm up, 46 cm back, looking down 10 degrees, and its rod is clamped to the rear bumper, so it crosses the floor behind the car and not the car.
- The club floors are clean: no left-over tape or faded arcs from old layouts, and the painted markings have smooth edges close up.
- CLUB is laid out again, along the route drawn in red on a picture of a printed drift mat: along the far side to the left with the line on it, the big corner all the way round, back along the near side, a left hairpin round the tip of the long island, back to the left through the middle, a right hairpin round the roundabout, the long diagonal sweeping right and down, a wide left hairpin under the pits, up the right-hand side and left onto the far side. 30.2 m a lap, tightest bend 0.67 m. The pits are inside the right-hand end: three marked bays with three display cars in them.
- Kerbs stand up off the floor now (5 mm, ramped on the lane's side) and the car feels them: inside the tight bends (the roundabout among them), and round the outside of the big corner against the turf. Turf only outside the big corner. INFIELD's kerbs are raised too.
- The blue sections, where the mat has them: the long island between the near side and the lane through the middle, running on up the outside of the roundabout hairpin, and a band along the inside of the diagonal.
- Yellow tape: three short lines round the outside of the big corner, and three arrows after the line. Nothing else on the floor is yellow (the judged run's outer-zone dashes are white). A judged run on CLUB has at most six marks.
- Tyres: a few stacks only (three stacks together, lying flat, inside two bends), in place of the rows.
- Three of the feather flags are blue, yellow and purple. DRIFT MASTER on the white wall banner is black.
- Breaker boxes on the left wall at the far end: a grey panel on a plywood board, a small box, a junction box and conduit.

- The room: a corner at the far right where the door is (brown door, yellow notices, bare concrete floor), a fire extinguisher and a wall television beside it, two PVC vinyl banners behind the track (640K in black and white, DRIFT MASTER in colour), and a window with its blind down in the left wall.
- The television plays the overhead camera live; that camera is no longer in the camera cycle (driver, on board, roof stick).
- Trackside flags say 640K, DRIFT MASTER, SEND IT and WINNERS DON'T USE BRAKES.
- MOUNT option on the CAR page: body posts with clips, or hidden magnets (nothing shows through the shell).
- Neon: no more bright streak on the floor from its lights; the windows glow more gently.

- INFIELD is 8 m longer (39 m a lap, was 31 m): its straights run 2 m further, and the hall is 2 m wider for it (11 x 7 m).
- Trackside dressing: old tyres packed on edge at the tips of the infield islands and inside some bends, and DRIFT MASTER flags of two kinds (tall feather flags, small framed banners) in red and white round both layouts.
- Neon reworked: LED strips under the edges of the chassis, a tight pool of light on the floor round the car's outline, lit wheel arches, a glow along the bottom of the body, and the cabin lit from inside.
- The driver's view no longer jolts when the car hits a barrier; only a trace of the knock shows.
- The hall is 2 m deeper on the driver's side (9 x 7 m; the layouts are unchanged), and the driver stands back from the track on a low stand in front of the middle of it, eyes 1.9 m up. The driver's camera sees like a person: a steady natural field of view that no longer zooms to hold the car's size, with the gaze leading the car so the track coming up is in view. The listening position is there too.
- Camber: every degree from -11 to +11 at both ends. Negative: bottoms of the wheels out, tops in. Positive: bottoms in, tops out.
- SETUP rebuilt as the 1/10 RWD drift baseline and nothing else: 10.5T or 13.5T, cambers, caster, front and rear toe (new), shock oil in cSt, ride height (new), gyro 40 to 65%, spring wire, spool or tight diff, weight bias (new). Tyres, Ackermann, ESC timing and servo are fixed to the baseline. Driving assist moved to the CONTROLLER page.
- The car's sound is now the web version's, ported exactly: the synthesised motor, the three recorded loops under it, knocks, distance filter and panning, and the compressor. Everything built for sound on the desktop before this is removed.
- Judged run rescored after the solo qualifying format: line 40, angle 40 (depth and consistency), style 20 (fluidity, commitment, initiation), up to 5 bonus for an early initiation. Speed no longer scores by itself. The card shows the breakdown.
- CAR and SETUP pages: the car is shown still on the right half of the screen while you change things (from lower and further forward on SETUP, so camber and ride height show).
- On-screen transmitter: a soft shadow under it and shading on itself, so it no longer floats.
- Coupe: smaller wing.
- Ceiling view is now an analog camera picture too (4:3, same look as the on-board cameras).
- Rival car and tandem battles removed. Training is solo, against a ghost of your own best: your fastest lap in free practice, your best-scoring run in a judged run (TRAINING page).
- Shell material matched against photos of real polycarbonate shells: it now mirrors the room (ceiling lights, walls, floor), more at glancing angles, with the slight waviness of a formed sheet, and the colour is shaded darker underneath. Hot pink retuned.
- Parking bay on each track (K).
- Driver's stand camera zoomed out and looking ahead of the car.
- Real RC radio support: CONTROLLER page to pick the device, find the steering and throttle channels, calibrate and reverse them.
- A transmitter on screen whose wheel and trigger move with your inputs (radio, gamepad or keys).
- Paint: one livery template. Body colour (12), bonnet in the body colour or in carbon weave, rim colour (12). The six earlier liveries and the second colour are gone.
- Shell material: made to look like a real RC drift shell, clear polycarbonate sprayed on the inside. Glossy outside, flat paint inside. Colours retuned.
- New bodies: TUNER (1990s two-door coupe, no badges) and VAN (lamps and grille over clean paint). Five bodies in all.
- Coupe: GT wing.
- Windscreen: sun strip across the top, with the DRIFT MASTER banner on it.
- Door stickers: a small stack under the driver's mirror, milky white with a thin dark contour, satin with raised edges. DRIFT MASTER, Send it, 640kFPV, SEND IT.
- Tyres: sidewall lettering now reads DRIFTMASTER.
- Rear window: the sticker now reads SEND IT.
- Keys: O now changes the rims, L the bonnet.

## Features

**Driving**
- 1:10 RC drift physics at 720 steps a second: tyre model, gyro, ESC, spool or gear diff, Ackermann, caster, camber, springs, shock oil and roll bars, with bump stops.
- Each body has its own weight and centre of gravity, so taller, heavier shells roll more.
- Bump map on both tracks (tile joints, humps, lifted corners, kerbs); the shell shivers over them.
- Foam barriers and hard boards that bounce differently.
- Keyboard, gamepad or a real RC transmitter over USB. Optional driving assist.
- On-screen transmitter that mirrors your steering and trigger.

**Training**
- Judged run: one lap scored like solo qualifying (line 40, angle 40, style 20, bonus 5), with a score card and breakdown.
- Ghost: a see-through replay of your fastest lap or best judged run, kept per track between sessions.
- Cones mark inner clipping points, yellow dashes mark outer zones.

**Tracks**
- One hall, black P-tile floor with wear, two layouts: HILLSIDE (with a raised section and bridge) and INFIELD (oval with a twisty infield, turf and kerbs).
- Lap timer with last and best lap, speed and drift angle.
- A parking bay beside each layout.

**Car**
- Five bodies on one chassis: SEDAN, COUPE, PICKUP, TUNER, VAN.
- 12 body colours, carbon bonnet option, 12 rim colours.
- Shell on or off, posts or hidden magnets, tinted or clear windows, neon under the car in six colours.
- Setup page, the 1/10 RWD drift baseline: motor, front and rear camber, caster, front and rear toe, shock oil, ride height, gyro gain, springs, differential, weight bias.

**Cameras**
- Driver's view, on-board (analog FPV look, 4:3) and a roof stick camera; the overhead view plays on the room's television.

**Sound**
- The web version's car sound: a synthesised brushless motor over recorded drive, drift and tyre-scrub loops, with knocks, heard from the driver's position.
- Music with its own volume.

**Other**
- Pause menu (CAR, SETUP, GAME, KEYS); settings kept between runs.
- Shadows, frame-rate display, resolution scale and full screen switches.
- Transparent splash screen and its own icon.
