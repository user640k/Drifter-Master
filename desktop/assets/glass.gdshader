// The shell's windows: the same clear polycarbonate, tinted from inside (or left clear), with a sun strip across the top
// of the windscreen: a band of darker tint, as on a drift car, that the windscreen banner sits on.
shader_type spatial;
render_mode cull_disabled, depth_draw_never;
uniform vec4 tint : source_color = vec4(0.05, 0.065, 0.085, 0.8);
uniform float metal = 0.3;
uniform vec3 strip_c = vec3(0.0, 10.0, 0.0);   // a point on the strip's lower edge
uniform vec3 strip_v = vec3(0.0, 1.0, 0.0);    // up the windscreen
uniform vec3 strip_n = vec3(0.0, 0.0, 1.0);    // out of the windscreen
uniform float strip_on = 0.0;
uniform float strip_show = 1.0;                // (off for the on-board camera, which looks out under it anyway)
uniform vec3 inner_glow : source_color = vec3(0.0);
varying vec3 mp;
varying vec3 on;
void vertex() { mp = VERTEX; on = NORMAL; }
void fragment() {
	float t = dot(mp - strip_c, strip_v);
	float band = smoothstep(-fwidth(t), fwidth(t), t) * step(0.75, abs(dot(normalize(on), strip_n))) * step(abs(dot(mp - strip_c, strip_n)), 0.02) * step(0.0, mp.z - strip_c.z + 0.05) * strip_on * strip_show;
	EMISSION = inner_glow * 0.3 * (1.0 - 0.5 * band);   // the cabin lit from inside by the neon
	ALBEDO = mix(tint.rgb, vec3(0.012, 0.014, 0.018), band); ALPHA = mix(tint.a, mix(tint.a, 1.0, 0.55), band);
	METALLIC = metal; ROUGHNESS = mix(0.08, 0.2, band); SPECULAR = 0.5;
}
