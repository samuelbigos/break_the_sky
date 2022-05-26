shader_type spatial;
render_mode unshaded, depth_test_disable;

uniform vec4 edge_color : hint_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float threshold = 0.0;
uniform float blend = 0.01;
uniform bool enable_depth_pass = true;
uniform float threshold_depth = 0.0;
uniform float blend_depth = 0.01;
uniform float distance_fade_length = 50.0;
uniform float distance_fade_blend = 20.0;

void vertex(){
	VERTEX *= 2.0;
	POSITION = vec4(VERTEX, 1.0);
}

float getGrayScale(sampler2D sampler, vec2 coods){
	vec4 color = texture(sampler,coods);
	float gray = (color.r + color.g + color.b)/3.0;
	return gray;
}

float get_linear_depth(sampler2D sampler, vec2 coords, mat4 ipm){
	float depth = texture(sampler, coords).r;
	vec3 ndc = vec3(coords, depth) * 2.0 - 1.0;
	vec4 view = ipm * vec4(ndc, 1.0);
	view.xyz /= view.w;
	float linear_depth = -view.z;
	return linear_depth;
}

void fragment(){
	
	vec2 SCREEN_PIXEL_SIZE = 1.0 / VIEWPORT_SIZE;
	vec2 iResolution = VIEWPORT_SIZE;
	float m = max(iResolution.x,iResolution.y);
	vec2 texCoords = SCREEN_UV;
	vec2 delta = SCREEN_PIXEL_SIZE;
	
	vec3 screen_color = texture(SCREEN_TEXTURE, SCREEN_UV).rgb;
	
	float c1y = getGrayScale(SCREEN_TEXTURE, texCoords.xy-delta/2.0);
	float c2y = getGrayScale(SCREEN_TEXTURE, texCoords.xy+delta/2.0);
	float c1x = getGrayScale(SCREEN_TEXTURE, texCoords.xy-delta.yx/2.0);
	float c2x = getGrayScale(SCREEN_TEXTURE, texCoords.xy+delta.yx/2.0);
	float dcdx = (c2x - c1x)/(delta.x*10.0);
	float dcdy = (c2y - c1y)/(delta.y*10.0);
	
	vec2 dcdi = vec2(dcdx,dcdy);
	float edge = length(dcdi)/10.0;
	edge = 1.0 - edge;
	edge = smoothstep(threshold, threshold + blend, edge);
	float final_edge = edge;
	
	// Depth-Pass
	if (enable_depth_pass){
		c1y = get_linear_depth(DEPTH_TEXTURE, texCoords.xy-delta/2.0, INV_PROJECTION_MATRIX);
		c2y = get_linear_depth(DEPTH_TEXTURE, texCoords.xy+delta/2.0, INV_PROJECTION_MATRIX);
		c1x = get_linear_depth(DEPTH_TEXTURE, texCoords.xy-delta.yx/2.0, INV_PROJECTION_MATRIX);
		c2x = get_linear_depth(DEPTH_TEXTURE, texCoords.xy+delta.yx/2.0, INV_PROJECTION_MATRIX);
		dcdx = (c2x - c1x)/(delta.x*10.0);
		dcdy = (c2y - c1y)/(delta.y*10.0);
		dcdi = vec2(dcdx,dcdy);
		float depth_edge = length(dcdi)/10.0;
		depth_edge = 1.0 - depth_edge;
		depth_edge = smoothstep(threshold_depth, threshold_depth + blend_depth, depth_edge);
		final_edge *= depth_edge;
	}
	
	// Distance Fading
	float linear_depth = get_linear_depth(DEPTH_TEXTURE, SCREEN_UV, INV_PROJECTION_MATRIX);
	float df = 1.0 - smoothstep(distance_fade_length, distance_fade_length + distance_fade_blend, linear_depth);
	final_edge = (1.0 - final_edge) * df;
	final_edge = 1.0 - final_edge;
	
	ALBEDO = mix(edge_color.rgb, screen_color.rgb, final_edge);
	//float d = get_linear_depth(DEPTH_TEXTURE, SCREEN_UV, INV_PROJECTION_MATRIX);
	//ALBEDO = vec3(d);
}