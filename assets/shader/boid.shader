// NOTE: Shader automatically converted from Godot Engine 3.5.1.rc.mono's SpatialMaterial.

shader_type spatial;
render_mode async_visible,blend_mix,depth_draw_opaque,cull_back,diffuse_burley,specular_schlick_ggx,world_vertex_coords;
uniform vec4 albedo : hint_color;
uniform sampler2D texture_albedo : hint_albedo;
uniform float specular;
uniform float metallic;
uniform float roughness : hint_range(0,1);
uniform float point_size : hint_range(0,128);
uniform sampler2D texture_metallic : hint_white;
uniform vec4 metallic_texture_channel;
uniform vec3 uv1_scale;
uniform vec3 uv1_offset;
uniform vec3 uv2_scale;
uniform vec3 uv2_offset;

// hit
uniform int u_hits = 0;
uniform vec3 u_centre;
uniform vec3 u_hit_pos = vec3(0.0, 0.0, 0.0);
uniform float u_hit_time;
uniform float u_hit_radius = 5.0;
uniform float u_hit_duration = 1.0;
uniform float u_flash_duration = 0.033;
uniform vec4 u_hit_col_0 : hint_color;
uniform vec4 u_hit_col_1 : hint_color;
uniform vec4 u_hit_col_2 : hint_color;
uniform vec4 u_hit_col_3 : hint_color;
uniform vec4 u_hit_col_4 : hint_color;

varying float v_hit_time;
varying vec3 v_vert;
varying vec3 v_norm;

float remap(float x, float a, float b, float c, float d)
{
    return (((x - a) / (b - a)) * (d - c)) + c;
}

float easeOut(float x)
{
	return 1.0 - pow(1.0 - x, 4.0);
}

void vertex() 
{
	UV=UV*uv1_scale.xy+uv1_offset.xy;
	
	//if (u_hits > 0)
	{
		//float hit_time = clamp(u_hit_time / u_hit_duration, 0.0, 1.0);
		v_hit_time = mod(TIME, 1.0);
	}
	
	v_vert = VERTEX;
	v_norm = NORMAL;
}

void fragment() 
{
	vec4 albedo_tex = texture(texture_albedo, UV);
	vec3 diffuse = albedo.rgb * albedo_tex.rgb;
	
	// hit
	vec4 hit_col = vec4(1.0);
	hit_col.rgb *= 5.0;
	
	if (u_hits > 0)
	{
		//vec3 hit_norm = normalize(u_hit_pos - u_centre);
		//float hit_dot = dot(hit_norm, v_norm);
		//hit = min(step(dist, u_hit_radius_2), clamp(remap(hit_dot, 1.0, 0.5, 1.0, 0.0), 0.0, 1.0));
		
		float dist = length(v_vert - u_hit_pos);
		
		//float t = clamp(mod(TIME, u_hit_duration) / u_hit_duration, 0.0, 1.0);
		float t = clamp(u_hit_time / u_hit_duration, 0.0, 1.0);
		
		float grad = dist / u_hit_radius;
		grad = clamp(grad, 0.0, 1.0);
		
		float s0 = 0.0;
		float s1 = 0.2;
		float s2 = 0.4;
		float s3 = 0.6;
		float s4 = 0.8;
		float s5 = 1.0;
		
		hit_col.rgb = mix(hit_col.rgb, u_hit_col_0.rgb, smoothstep(s0, s1, grad));
		hit_col.rgb = mix(hit_col.rgb, u_hit_col_1.rgb, smoothstep(s1, s2, grad));
		hit_col.rgb = mix(hit_col.rgb, u_hit_col_2.rgb, smoothstep(s2, s3, grad));
		hit_col.rgb = mix(hit_col.rgb, u_hit_col_3.rgb, smoothstep(s3, s4, grad));
		hit_col = mix(hit_col, u_hit_col_4, smoothstep(s4, s5, grad));
		
		float a = mix(1.0, 0.0, smoothstep(s4, s5, dist / u_hit_radius)) * hit_col.a;
		a *= clamp(easeOut(1.0 - t), 0.0, 1.0);
		
		//hit_col.a = step(dist, u_hit_radius_1);
		//hit_col.a *= v_hit_time;
		
		diffuse = mix(diffuse, hit_col.rgb, a);
		diffuse = mix(diffuse, vec3(1.0), step(u_hit_time, u_flash_duration));
	}
	
	ALBEDO = diffuse;
	float metallic_tex = dot(texture(texture_metallic, UV), metallic_texture_channel);
	METALLIC = metallic_tex * metallic;
	ROUGHNESS = roughness;
	SPECULAR = specular;
}
