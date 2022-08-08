// NOTE: Shader automatically converted from Godot Engine 3.5.1.rc.mono's SpatialMaterial.

shader_type spatial;
render_mode async_visible,blend_add,depth_draw_opaque,cull_back,specular_schlick_ggx,world_vertex_coords;
uniform vec4 albedo : hint_color;
uniform sampler2D texture_albedo : hint_albedo;
uniform float specular;
uniform float metallic;
uniform float roughness : hint_range(0,1);
uniform float point_size : hint_range(0,128);
uniform sampler2D texture_normal : hint_normal;
uniform float normal_scale : hint_range(-16,16);

uniform float u_scale = 0.002;
uniform float u_dune_scale_y = 25.0;
uniform sampler2D u_height_tex;

void vertex() 
{
	UV = VERTEX.xz * u_scale;
	
	float duneHeight = texture(u_height_tex, UV).r;
	VERTEX.y += duneHeight * u_dune_scale_y;
}

void fragment() 
{
	vec2 base_uv = UV;
	vec4 albedo_tex = texture(texture_albedo,base_uv);
	ALBEDO = albedo.rgb * albedo_tex.rgb;
	METALLIC = metallic;
	ROUGHNESS = roughness;
	SPECULAR = specular;
	NORMALMAP = texture(texture_normal,base_uv).rgb;
	NORMALMAP_DEPTH = normal_scale;
}
