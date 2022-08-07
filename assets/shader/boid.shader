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
uniform float u_hit_radius_1 = 5.0;
uniform float u_hit_radius_2 = 10.0;
uniform float u_hit_duration = 0.25;

varying float v_hit_flash;

float remap(float x, float a, float b, float c, float d)
{
    return (((x - a) / (b - a)) * (d - c)) + c;
}

void vertex() 
{
	UV=UV*uv1_scale.xy+uv1_offset.xy;
	
	v_hit_flash = 0.0;
	if (u_hits > 0)
	{
		float hit_time = clamp(u_hit_time / u_hit_duration, 0.0, 1.0);
		//float hit_time = 1.0 - clamp(mod(TIME, 1.0) / u_hit_duration, 0.0, 1.0);
		
		vec3 hit_norm = normalize(u_hit_pos - u_centre);
		float hit_dot = dot(hit_norm, NORMAL);
		
		float dist = length(VERTEX - u_hit_pos);
		v_hit_flash = min(step(dist, u_hit_radius_2), clamp(remap(hit_dot, 1.0, 0.5, 1.0, 0.0), 0.0, 1.0));
		v_hit_flash = max(v_hit_flash, step(dist, u_hit_radius_1));
		//v_hit_flash = 1.0;
		v_hit_flash *= hit_time;
	}
}

void fragment() 
{
	vec2 base_uv = UV;
	vec4 albedo_tex = texture(texture_albedo,base_uv);
	ALBEDO = albedo.rgb * albedo_tex.rgb + vec3(v_hit_flash);
	float metallic_tex = dot(texture(texture_metallic,base_uv),metallic_texture_channel);
	METALLIC = metallic_tex * metallic;
	ROUGHNESS = roughness;
	SPECULAR = specular;
}
