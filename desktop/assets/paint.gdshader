// The shell's paint: one colour or a livery, drawn in the body's own space (so it stays put on the car), under a clear coat.
// Schemes: 0 solid, 1 bonnet in the second colour, 2 fade front to back, 3 flames along the sides, 4 camo,
// 5 streaks, 6 hologram (chrome wrap that shifts through the rainbow with the angle you see it from).
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
	} else if (scheme == 6) { // hologram: a chrome film whose colour turns with the angle of view, in fine drips down the panels
		float facing = dot(normalize(NORMAL), normalize(VIEW));
		float drip = vnoise(vec2(mp.z * 150.0, 0.0)) * 0.5 + vnoise(vec2(mp.z * 34.0, mp.y * 7.0)) * 0.5;
		float h = fract(facing * 1.15 + up * 0.4 + along * 0.3 + drip * 0.2);
		vec3 film = mix(vec3(0.86), hue(h), 0.46 + 0.2 * drip);   // mostly silver, washed with colour
		c = film; metal = 0.92; rough = 0.13; glow = film * 0.16;
	}
	ALBEDO = c; METALLIC = metal; ROUGHNESS = rough; CLEARCOAT = 0.8; CLEARCOAT_ROUGHNESS = 0.12; EMISSION = glow;
}
