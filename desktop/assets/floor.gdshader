// The floor: P-tile laid on world coordinates (so floor, slopes and bridge line up), with the hall's wear sheet on top.
shader_type spatial;
uniform sampler2D tile_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D tile_rough : filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D wear : source_color, filter_linear_mipmap, repeat_disable;
uniform sampler2D wear_rough : filter_linear_mipmap, repeat_disable;
uniform vec2 hall_half = vec2(4.5, 2.5);
uniform float tile_size = 1.2192;
varying vec3 wpos;
void vertex() { wpos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz; }
void fragment() {
	vec2 uv = wpos.xz / tile_size, wuv = (wpos.xz + hall_half) / (2.0 * hall_half);
	vec4 w = texture(wear, wuv);
	ALBEDO = mix(texture(tile_albedo, uv).rgb, w.rgb, w.a);
	ROUGHNESS = clamp(texture(tile_rough, uv).r + (texture(wear_rough, wuv).r - 0.5) * 1.0 + w.a * 0.25, 0.08, 1.0);
	SPECULAR = 0.5;
}
