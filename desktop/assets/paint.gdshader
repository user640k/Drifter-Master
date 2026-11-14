// The shell's paint. An RC drift shell is clear polycarbonate (Lexan), under a millimetre thick, sprayed on the INSIDE:
// what is seen from outside is the colour through the clear plastic, so the surface is the plastic itself: smooth, very
// glossy, no orange peel and no flake, the colour deep and even. Inside, the dried paint is flat and dull.
// One template: a solid colour, with the bonnet either in the body colour or in carbon weave.
shader_type spatial;
render_mode cull_disabled;
uniform vec3 col_a : source_color = vec3(0.09, 0.094, 0.106);
uniform float carbon = 0.0;                  // 1: carbon bonnet
uniform vec3 dim = vec3(0.09, 0.13, 0.22);   // half width, height, half length of the painted shell
uniform vec3 origin = vec3(0.0);            // its middle on the floor line
uniform vec4 bonnet_box = vec4(0.18, 0.24, 0.62, 0.7); // the bonnet: from where along the car (two edges of a soft step), and below what height
uniform sampler2D detail : source_color, filter_linear_mipmap_anisotropic;   // a shell with printed details (lamps, grille): their colour, alpha where they cover the paint
uniform float use_detail = 0.0;
uniform float bonnet_end = 0.9;                // where the bonnet stops, along the car (1: the tip of the nose)
uniform float bonnet_w = 0.062;               // its half width: the wings beside it stay in the body colour
uniform float reflect_k = 1.0;                // how much of the room the plastic mirrors
uniform vec3 neon : source_color = vec3(0.0);   // the colour of the LEDs under the car (black: none)
varying vec3 mp;
varying vec3 on;
void vertex() { mp = VERTEX; on = NORMAL; }
void fragment() {
	float along = (mp.z - origin.z) / dim.z;         // -1 tail .. +1 nose
	float up = (mp.y - origin.y) / dim.y;            // 0 sill .. 1 roof
	vec3 c = col_a; float rough = 0.12, coat = 1.0, spec = 0.4;
	float bon = 0.0;   // how much this point is the carbon bonnet
	if (carbon > 0.5) {
		// the bonnet is a panel with a clean outline: straight across at the base of the windscreen, sides that draw in a
		// little towards the nose, rounded corners, and it ends where the top of the wing turns down into the nose. Its edge
		// is one pixel wide at any distance.
		float z0 = bonnet_box.y * dim.z, z1 = bonnet_end * dim.z, zc = (z0 + z1) * 0.5, zh = (z1 - z0) * 0.5, zz = mp.z - origin.z;
		float hw = bonnet_w * mix(1.0, 0.88, clamp((zz - z0) / (z1 - z0), 0.0, 1.0)), rad = 0.007;
		vec2 dq = abs(vec2(mp.x - origin.x, zz - zc)) - vec2(hw, zh) + rad; float sdf = length(max(dq, 0.0)) + min(max(dq.x, dq.y), 0.0) - rad;
		float aa = max(fwidth(sdf), 0.00015);
		bon = (1.0 - smoothstep(-aa, aa, sdf)) * smoothstep(-0.25, -0.15, on.y) * (1.0 - smoothstep(bonnet_box.w - 0.01, bonnet_box.w + 0.01, up));
		// 2x2 twill, fine and even: tows 0.9 mm wide, each over two and under two, the pattern stepping one tow per row.
		// The two directions catch the light differently as the eye moves; from further off it is one even satin black.
		vec2 q = mp.xz / 0.0009; vec2 cell = floor(q), f = fract(q);
		float over = step(2.0, mod(cell.x + cell.y, 4.0));
		float across = mix(f.x, f.y, over), round_ = 1.0 - 0.45 * pow(abs(across * 2.0 - 1.0), 3.0);
		vec3 vdir = normalize((INV_VIEW_MATRIX * vec4(VIEW, 0.0)).xyz); float sheen = mix(abs(vdir.x), abs(vdir.z), over);
		float weave = (0.014 + 0.036 * sheen) * round_;
		float px = max(fwidth(q.x), fwidth(q.y)); weave = mix(weave, 0.024, smoothstep(0.3, 1.0, px));
		c = mix(c, vec3(weave, weave, weave * 1.04), bon); rough = mix(rough, 0.24, bon); spec = mix(spec, 0.5, bon);
	}
	if (use_detail > 0.5) { vec4 d = texture(detail, UV); c = mix(c, d.rgb, d.a); rough = mix(rough, 0.3, d.a); }
	if (!FRONT_FACING) { c = mix(c, vec3(dot(c, vec3(0.33))), 0.25) * 0.75; rough = 0.85; coat = 0.0; spec = 0.2; }   // the inside: dried paint
	// The clear plastic over the paint. Matched to photos of real shells (a painted one and a raw, clear one):
	//   - the sheet is vacuum-formed, so its surface waves a little: reflections are broad and slightly bent, not mirror sharp
	//   - it mirrors the room: the ceiling lights as long bright bars on the bonnet, roof and shoulders, pale walls above
	//     the horizon, the dark floor below it; weak face-on (about 5%), strong at a glancing angle (Fresnel, n = 1.58)
	//   - the colour under it is shaded by the room like any paint: darker underneath, in the sills and where it faces down
	vec3 nw = normalize((INV_VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz), vw = normalize((INV_VIEW_MATRIX * vec4(VIEW, 0.0)).xyz);
	float shade = mix(0.42, 1.0, smoothstep(-0.7, 0.55, nw.y)) * mix(0.62, 1.0, smoothstep(0.0, 0.45, up)) * 0.9;
	vec3 glow = vec3(0.0);
	if (FRONT_FACING) {
		vec3 wob = vec3(sin(mp.z * 95.0 + mp.y * 40.0), 0.0, sin(mp.x * 110.0 + mp.y * 55.0 + 1.7)) * 0.035 * (1.0 - bon);   // the sheet's waviness, a few centimetres long (the carbon panel is stiff and flat)
		vec3 r = reflect(-vw, normalize(nw + wob)); float f = 0.07 + 0.93 * pow(1.0 - clamp(dot(nw, vw), 0.0, 1.0), 4.0);
		float room = mix(0.02, 0.75, smoothstep(-0.1, 0.18, r.y)) * mix(1.0, 0.6, smoothstep(0.5, 0.9, r.y));             // floor, walls, the dimmer ceiling between the lamps
		float ax = r.x / max(r.y, 0.05), az = r.z / max(r.y, 0.05);
		float bars = smoothstep(0.55, 0.75, r.y) * (smoothstep(0.2, 0.08, abs(abs(ax) - 0.42)) + 0.6 * smoothstep(0.16, 0.06, abs(az))) ;   // the ceiling lamps: two long bars and one across
		glow = vec3(1.0, 0.98, 0.94) * (f * room * reflect_k + bars * (0.1 + 0.9 * f) * 1.6 * reflect_k) * mix(1.0, 0.4, bon);   // (carbon under a satin lacquer mirrors the room less than the glossy shell)
	}
	// neon under the car catches the bottom of the body: strongest at the lower edge, on surfaces that face down or sideways
	glow += neon * (1.0 - smoothstep(0.0, 0.2, up)) * (0.25 + 0.75 * smoothstep(0.3, -0.6, nw.y)) * 0.55 * (FRONT_FACING ? 1.0 : 0.6);
	ALBEDO = c * shade; METALLIC = 0.0; ROUGHNESS = rough; SPECULAR = spec; CLEARCOAT = coat; CLEARCOAT_ROUGHNESS = 0.03; EMISSION = glow;
}
