shader_type spatial;
render_mode unshaded, world_vertex_coords, cull_disabled;

// UNIFORMS
uniform vec4 u_colour : source_color;
uniform vec4 u_hit_colour : source_color;
uniform vec3 u_centre;
uniform float u_radius = 1.0;
uniform float u_glow = 5.0;
uniform float u_fill = 0.01;
uniform sampler2D u_texture;

uniform float u_speed = 1.0;
uniform float u_ripple_width = 0.2;

uniform int u_hits = 0;
uniform vec4 u_hit_1 = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 u_hit_2 = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 u_hit_3 = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 u_hit_4 = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 u_hit_5 = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 u_hit_6 = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 u_hit_7 = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 u_hit_8 = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 u_hit_9 = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 u_hit_10 = vec4(0.0, 0.0, 0.0, 0.0);

uniform vec3 u_heading = vec3(0.0, 0.0, 1.0);
uniform float u_active_timer;
uniform float u_activation_speed = 2.0;
uniform float u_destroy_timer;
uniform float u_destroy_speed = 1.0;

// VARYINGS
varying vec3 v_cam;
varying vec3 v_vert_pos;
varying float v_depth;
varying vec3 v_depth_pos;
varying vec3 v_normal;
varying float v_ripple;
varying float v_destroy;

uniform sampler2D DEPTH_TEXTURE : hint_depth_texture, filter_linear_mipmap;

float easeOutElastic(float x, float strength) 
{
	float c4 = (2.0 * PI) / 3.0;

	if(x == 0.0) return 0.0;
	if(x == 1.0) return 1.0;
	return pow(2.0, -10.0 * x * (1.0 / strength)) * sin((x * 10.0 - 0.75) * c4) + 1.0;
}

float easeOutCubic(float x)
{
	return 1.0 - pow(1.0 - x, 3.0);
}

float easeInOutSine(float x)
{
	return -(cos(PI * x) - 1.0) / 2.0;
}

float easeInOutCubic(float x)
{
	return x < 0.5 ? 4.0 * x * x * x : 1.0 - pow(-2.0 * x + 2.0, 3.0) / 2.0;
}

void sum_hit(inout float o_dent, inout float o_ripple, vec4 hit)
{
	float duration = (2.0 + u_ripple_width);
	float time = clamp(hit.w * u_speed, 0.0, duration);
	
	vec3 hit_normal = hit.xyz;
	float hit_dot = dot(hit_normal, v_normal);
	float ripple_time = (1.0 + u_ripple_width) - (time / duration) * duration;
	float ripple = min(step(hit_dot, ripple_time), step(ripple_time - u_ripple_width, hit_dot));
	
	float dent_t = 1.0 - easeOutElastic(clamp(time * 0.5, 0.0, 1.0), 1.0);
	float dent = pow(clamp(hit_dot, 0.0, 1.0), 3.0) * dent_t;
	dent *= u_radius * 0.75;
	
	o_ripple = max(ripple, o_ripple);
	o_dent += dent;
}

void vertex()
{
	v_cam = (INV_VIEW_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
	v_normal = normalize(NORMAL);
	
	// hit effect
	float dent = 0.0;
	float ripple = 0.0;
	
	if (u_hits > 0) sum_hit(dent, ripple, u_hit_1);
	if (u_hits > 1) sum_hit(dent, ripple, u_hit_2);
	if (u_hits > 2) sum_hit(dent, ripple, u_hit_3);
	if (u_hits > 3) sum_hit(dent, ripple, u_hit_4);
	if (u_hits > 4) sum_hit(dent, ripple, u_hit_5);
	if (u_hits > 5) sum_hit(dent, ripple, u_hit_6);
	if (u_hits > 6) sum_hit(dent, ripple, u_hit_7);
	if (u_hits > 7) sum_hit(dent, ripple, u_hit_8);
	if (u_hits > 8) sum_hit(dent, ripple, u_hit_9);
	if (u_hits > 9) sum_hit(dent, ripple, u_hit_10);
	
	v_ripple = ripple;
	vec3 offset_ripple = v_normal * v_ripple * u_radius * 0.1;
	vec3 offset_dent = v_normal * dent;
	
	// generate
	float gen_time = clamp(u_active_timer * u_activation_speed, 0.0, 2.0);
	float gen_dot = dot(-u_heading, v_normal) * 0.5 + 0.5;
	float gen_offset = easeOutElastic(clamp((gen_time - gen_dot), 0.0, 1.0), 0.75);
	v_ripple = max(v_ripple, gen_offset > 1.1 ? 1.0 : 0.0);
	
	// destroy
	float destroy_time = easeOutCubic(clamp(u_destroy_timer * u_destroy_speed, 0.0, 1.0));
	float destroy_offset = destroy_time;
	v_destroy = 1.0 - destroy_time; 
	
	VERTEX += offset_ripple - offset_dent;
	VERTEX = mix(u_centre, VERTEX, gen_offset);
	VERTEX += destroy_offset * v_normal * u_radius;
	v_vert_pos = VERTEX;
}

void fragment()
{
	float depth = texture(DEPTH_TEXTURE, SCREEN_UV).x;
	vec3 ndc = vec3(SCREEN_UV, depth) * 2.0 - 1.0;
	vec4 world = INV_VIEW_MATRIX * INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
  	vec3 world_position = world.xyz / world.w;
	
	// intersect
	float intersectDist = length(v_vert_pos - world_position);
	intersectDist = 1.0 - clamp(intersectDist * 0.1f, 0.0, 1.0);
	intersectDist = pow(intersectDist, 5.0);
	
	// fresnel
	vec3 camDir = normalize(v_cam - v_vert_pos);
	float fDot = abs(dot(v_normal, camDir));
	float fresnel = pow(1.0 - fDot, 2.0);
	
	// intersect + fresnel
	float edge = smoothstep(0.0, 1.0, max(intersectDist, fresnel));
	
	// col
	vec3 base_col = mix(u_colour.rgb, u_hit_colour.rgb, v_ripple + (1.0 - v_destroy));
	vec3 col = mix(base_col, vec3(1.0), edge * 0.25 - 0.075) * u_glow;
	float grid = texture(u_texture, UV).a;
	col *= 1.0 - grid;
	
	ALBEDO = col;
	ALPHA = (edge + u_fill) * v_destroy;
}

void light()
{
	
}