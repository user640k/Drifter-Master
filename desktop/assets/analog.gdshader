// The on-board camera's picture, after the Tiny Whoop Pinch analog camera as set up in the 640KFPV simulator:
// a 720 x 480 frame shown as 4:3, 600TVL (soft across), punchy colour, deep blacks, highlights that clip, mild fisheye.
shader_type canvas_item;
uniform float chroma = 1.32;
uniform float black = 0.075;
uniform float gamma = 1.12;
uniform float gain = 1.22;
uniform float soft = 1.6;      // horizontal softness, in frame pixels
uniform float fisheye = 0.09;
void fragment() {
	vec2 p = UV * 2.0 - 1.0;
	float r2 = dot(p * vec2(1.0, 0.75), p * vec2(1.0, 0.75));
	vec2 uv = p * (1.0 + fisheye * r2) / (1.0 + fisheye * 1.5625) * 0.5 + 0.5;   // the corners stay inside the frame
	float dx = soft * TEXTURE_PIXEL_SIZE.x * 0.5;
	vec3 c = texture(TEXTURE, uv).rgb * 0.5 + texture(TEXTURE, uv + vec2(dx, 0.0)).rgb * 0.25 + texture(TEXTURE, uv - vec2(dx, 0.0)).rgb * 0.25;
	c = pow(max(c - black, 0.0) / (1.0 - black), vec3(gamma)) * gain;
	float y = dot(c, vec3(0.299, 0.587, 0.114));
	c = clamp(mix(vec3(y), c, chroma), 0.0, 1.0);
	COLOR = vec4(c, 1.0);
}
