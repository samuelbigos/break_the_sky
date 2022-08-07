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

uniform float u_ground_y;
uniform float u_sand_blend = 0.66;
uniform vec4 u_ground_col_light : hint_color;
uniform vec4 u_ground_col_dark : hint_color;

varying vec3 v_vert;

void vertex() 
{
	UV=UV*uv1_scale.xy+uv1_offset.xy;
	
	v_vert = VERTEX;
}

void fragment() 
{
	vec4 albedo_tex = texture(texture_albedo, UV);
	vec3 diffuse = albedo.rgb * albedo_tex.rgb;
	
	float grad = clamp(smoothstep(u_ground_y, u_ground_y + 5.0, v_vert.y), 0.0, 1.0);
	diffuse = mix(diffuse, u_ground_col_light.rgb, u_sand_blend); // dull ship colours towards ground colour.
	diffuse = mix(u_ground_col_dark.rgb, diffuse, grad); // shade as ship enters the ground.
	
	ALBEDO = diffuse;
	float metallic_tex = dot(texture(texture_metallic, UV), metallic_texture_channel);
	METALLIC = metallic_tex * metallic;
	ROUGHNESS = roughness;
	SPECULAR = specular;
}
