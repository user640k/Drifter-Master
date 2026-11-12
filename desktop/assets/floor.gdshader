// The floor: P-tile laid on world coordinates (so floor, slopes and bridge line up), with the hall's wear sheet on top.
shader_type spatial;
uniform sampler2D tile_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D tile_rough : filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D wear : source_color, filter_linear_mipmap, repeat_disable;
uniform sampler2D wear_rough : filter_linear_mipmap, repeat_disable;
uniform vec2 hall_half = vec2(4.5, 2.5);
uniform float tile_size = 1.2192;
void vertex() {
	vec3 wpos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	UV = wpos.xz / tile_size;                              // tiles
	UV2 = (wpos.xz + hall_half) / (2.0 * hall_half);       // the wear sheet over the whole hall
}
void fragment() {
	vec4 w = texture(wear, UV2);
	ALBEDO = mix(texture(tile_albedo, UV).rgb, w.rgb, w.a);
	ROUGHNESS = clamp(texture(tile_rough, UV).r + (texture(wear_rough, UV2).r - 0.5) + w.a * 0.25, 0.08, 1.0);
	SPECULAR = 0.5;
}
