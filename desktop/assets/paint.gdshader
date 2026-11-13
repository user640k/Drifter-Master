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
uniform float bonnet_w = 0.062;               // its half width: the wings beside it stay in the body colour
varying vec3 mp;
varying vec3 on;
void vertex() { mp = VERTEX; on = NORMAL; }
void fragment() {
	float along = (mp.z - origin.z) / dim.z;         // -1 tail .. +1 nose
	float up = (mp.y - origin.y) / dim.y;            // 0 sill .. 1 roof
	vec3 c = col_a; float rough = 0.07, coat = 1.0, spec = 0.75;
	if (carbon > 0.5) {
		float bonnet = smoothstep(0.5, 0.8, on.y) * smoothstep(bonnet_box.x, bonnet_box.y, along) * (1.0 - smoothstep(bonnet_box.z, bonnet_box.w, up)) * (1.0 - smoothstep(bonnet_w - 0.004, bonnet_w + 0.002, abs(mp.x - origin.x)));
		// 2x2 twill: tows 1.1 mm wide, each passing over two and under two, the pattern stepping one tow per row
		vec2 q = mp.xz / 0.0011; vec2 cell = floor(q), f = fract(q);
		float over = step(2.0, mod(cell.x + cell.y, 4.0));              // 1: the tow running along x is on top here
		float across = mix(f.x, f.y, over);                             // position across the top tow
		float round_ = 1.0 - 0.75 * pow(abs(across * 2.0 - 1.0), 2.5);   // each tow is a rounded ribbon
		// the two directions catch the light differently as the eye moves: that is the shimmer of carbon
		vec3 vdir = normalize((INV_VIEW_MATRIX * vec4(VIEW, 0.0)).xyz); float sheen = mix(abs(vdir.x), abs(vdir.z), over);
		float weave = (0.022 + 0.085 * sheen) * round_;
		float px = max(fwidth(q.x), fwidth(q.y)); weave = mix(weave, 0.045, smoothstep(0.35, 1.2, px));   // far away it is an even dark grey
		c = mix(c, vec3(weave, weave, weave * 1.06), bonnet);
	}
	if (use_detail > 0.5) { vec4 d = texture(detail, UV); c = mix(c, d.rgb, d.a); rough = mix(rough, 0.3, d.a); }
	if (!FRONT_FACING) { c = mix(c, vec3(dot(c, vec3(0.33))), 0.25) * 0.75; rough = 0.85; coat = 0.0; spec = 0.2; }   // the inside: dried paint
	ALBEDO = c; METALLIC = 0.0; ROUGHNESS = rough; SPECULAR = spec; CLEARCOAT = coat; CLEARCOAT_ROUGHNESS = 0.03;
}
