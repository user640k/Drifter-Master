// The shell's paint: one colour or a livery, drawn in the body's own space (so it stays put on the car), under a clear coat.
// Schemes: 0 solid, 1 bonnet in the second colour, 2 fade front to back, 3 flames along the sides, 4 camo,
// 5 streaks, 6 hologram (a chrome film that breaks the light into colours, with line-work in the second colour).
// All original artwork: shapes from noise, no logos.
shader_type spatial;
render_mode cull_disabled;
uniform vec3 col_a : source_color = vec3(0.09, 0.094, 0.106);
uniform vec3 col_b : source_color = vec3(0.9);
uniform vec3 col_c : source_color = vec3(0.05);
uniform int scheme = 0;
uniform vec3 dim = vec3(0.09, 0.13, 0.22);   // half width, height, half length of the painted shell
uniform vec3 origin = vec3(0.0);            // its middle on the floor line
varying vec3 mp;
varying vec3 on;
float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float vnoise(vec2 p) { vec2 i = floor(p), f = fract(p); f = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x), f.y); }
float fbm(vec2 p) { return 0.5 * vnoise(p) + 0.3 * vnoise(p * 2.1 + 7.3) + 0.2 * vnoise(p * 4.3 + 1.9); }
vec3 hue(float h) { return clamp(abs(fract(h + vec3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 1.0, 0.0, 1.0); }
void vertex() { mp = VERTEX; on = NORMAL; }
void fragment() {
	float along = (mp.z - origin.z) / dim.z;         // -1 tail .. +1 nose
	float up = (mp.y - origin.y) / dim.y;            // 0 sill .. 1 roof
	float flank = smoothstep(0.3, 0.65, abs(on.x));  // 1 on the sides of the car, 0 on top
	vec3 c = col_a; float metal = 0.25, rough = 0.42; vec3 glow = vec3(0.0);
	if (scheme == 1) { // the bonnet (and the scuttle) in the second colour, as a carbon or satin bonnet
		float bonnet = smoothstep(0.5, 0.8, on.y) * smoothstep(0.18, 0.24, along) * (1.0 - smoothstep(0.62, 0.7, up));
		c = mix(c, col_b, bonnet); rough = mix(rough, 0.6, bonnet);
	} else if (scheme == 2) { // one colour at the nose running into another at the tail, the join swept back and ragged
		float t = smoothstep(-0.45, 0.5, -along + 0.35 * (up - 0.5) + 0.3 * (fbm(vec2(mp.z * 30.0, mp.y * 40.0)) - 0.5));
		c = mix(col_a, col_b, t);
		float line = smoothstep(0.06, 0.0, abs(t - 0.5) - 0.02); c = mix(c, col_c, line * 0.85 * flank);
	} else if (scheme == 3) { // flames licking back from the front arches along the sides, edged in a third colour
		float u = (0.75 - along) * 1.6;                // 0 at the nose, growing to the tail
		float lick = 0.42 + 0.2 * sin(u * 4.2 + 2.0 * fbm(vec2(u * 2.0, 3.0))) ;
		float f = (1.3 - u * 0.5) - abs(up - lick) * 2.3 + (fbm(vec2(u * 5.0, up * 9.0)) - 0.5) * 1.6;
		float m = smoothstep(0.0, 0.05, f) * step(0.0, u) * flank;
		float edge = smoothstep(0.0, 0.05, f) * (1.0 - smoothstep(0.1, 0.16, f)) * flank;
		c = mix(c, col_b, m); c = mix(c, col_c, edge * 0.9);
	} else if (scheme == 4) { // camouflage: three tones in broken patches
		vec2 q = vec2(mp.z * 22.0 + mp.x * 9.0, mp.y * 26.0 + abs(mp.x) * 14.0);
		float n1 = fbm(q), n2 = fbm(q * 0.8 + 11.0);
		c = mix(col_a, col_b, step(0.5, n1)); c = mix(c, col_c, step(0.56, n2)); rough = 0.55;
	} else if (scheme == 5) { // streaks: slashes of the second and third colour raked back along the sides
		float d = along * 2.6 + up * 2.0 + 0.5 * fbm(vec2(mp.z * 14.0, mp.y * 18.0));
		float band = fract(d * 1.15), ragged = fbm(vec2(mp.z * 60.0, mp.y * 16.0));
		float s1 = smoothstep(0.52, 0.56, band + 0.25 * (ragged - 0.5)) * (1.0 - smoothstep(0.8, 0.84, band));
		float s2 = smoothstep(0.84, 0.86, band) * step(0.35, ragged);
		c = mix(c, col_b, s1 * mix(0.35, 1.0, flank)); c = mix(c, col_c, s2 * flank);
	} else if (scheme == 6) { // hologram: a chrome film that breaks the light into colours. Broad washes of pink, blue, green and
		// gold that slide over the panels as the car turns, fine streaks hanging down the sides like drips, and line-work in
		// the second colour: nested chevrons over the rear quarters and a ring on the bonnet
		float facing = dot(normalize(NORMAL), normalize(VIEW));
		float broad = facing * 0.85 + up * 0.5 + along * 0.55 + 0.4 * fbm(vec2(mp.z * 7.0, mp.y * 9.0 + abs(mp.x) * 5.0));
		float colm = floor(mp.z * 260.0), rnd = hash(vec2(colm, 3.0)), len = 0.25 + 0.75 * hash(vec2(colm, 9.0));
		float drip = smoothstep(len, len - 0.3, 1.0 - up) * rnd * flank;                   // each streak hangs from the top, its own length
		float h = fract(broad * 0.8 + 0.3 * drip + 0.08 * rnd * flank);
		vec3 tint = 0.5 + 0.5 * cos(6.2832 * (h + vec3(0.02, 0.36, 0.64)));          // pink, lilac, blue, aqua, a little green and gold
		vec3 film = mix(vec3(0.9), tint, 0.46 + 0.3 * drip);                         // silver first, colour washed over it; the streaks carry more colour
		// chevrons on the rear quarters
		float q = abs(up - 0.52) * 1.7 + (along + 1.0) * 1.25, band = fract(q * 3.2);
		float zone = (1.0 - smoothstep(-0.2, -0.05, along)) * flank * smoothstep(0.1, 0.2, up);
		float ink = (smoothstep(0.0, 0.04, band) - smoothstep(0.16, 0.2, band)) * zone;
		float fill = step(0.55, fract(q * 1.6)) * zone * 0.35;
		// the ring on the bonnet
		float rr = length(vec2(mp.x / dim.x * 1.15, (along - 0.6) * 2.6)), top = smoothstep(0.5, 0.8, on.y) * step(up, 0.75);
		float ring = (smoothstep(0.30, 0.33, rr) - smoothstep(0.40, 0.43, rr) + smoothstep(0.50, 0.52, rr) - smoothstep(0.56, 0.58, rr)) * top;
		float art = clamp(ink + fill + ring, 0.0, 1.0);
		c = mix(film, mix(col_b, film, 0.2), art); metal = 0.4; rough = mix(0.16, 0.3, art); glow = film * 0.26 * (1.0 - art * 0.8);   // it throws light back, so it is never dark
	}
	ALBEDO = c; METALLIC = metal; ROUGHNESS = rough; CLEARCOAT = 0.8; CLEARCOAT_ROUGHNESS = 0.12; EMISSION = glow;
}
