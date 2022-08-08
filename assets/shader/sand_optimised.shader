shader_type spatial;
render_mode world_vertex_coords;

uniform float u_scale = 0.001;
uniform float u_dune_scale_y = 10.0;
uniform sampler2D u_albedo_tex;
uniform sampler2D u_height_tex;
uniform sampler2D u_normal_tex;

uniform float u_ripple_scale = 0.25;
uniform sampler2D u_ripple_albedo_tex;
uniform sampler2D u_ripple_height_tex;
uniform sampler2D u_ripple_normal_tex;

uniform vec4 u_sand_lit : hint_color;
uniform vec4 u_sand_shadow : hint_color;
uniform sampler2D u_grain_texture;
uniform float u_grain_scale = 25.0f;
uniform float u_fresnel_pow = 25.0;
uniform float u_fresnel_strength = 0.5;
uniform float u_grain_strength = 0.25;
uniform float u_oceanspec_pow = 5.0;
uniform float u_oceanspec_strength = 0.5;
uniform sampler2D u_glitter_texture;
uniform float u_glitter_scale = 25.0f;
uniform float u_glitter_threshold = 0.01;
uniform vec2 u_wind = vec2(0.0, 1.0);

varying vec2 v_uv;
varying vec3 v_vertex;
varying vec2 v_ripple_uv;
varying float v_ripple_strength;

void vertex()
{
	v_uv = VERTEX.xz * u_scale;
	
	float duneHeight = texture(u_height_tex, v_uv).r;
	VERTEX.y += duneHeight * u_dune_scale_y;
	
	v_vertex = VERTEX;
}

vec3 sandNormal(vec2 uv)
{
	vec3 dunes = normalize(texture(u_normal_tex, v_uv).rbg * 2.0 - 1.0);
	return normalize(dunes);
}

vec3 diffuseCol(vec3 n, vec3 l)
{
	float lum;
	n.y *= 0.5;
	lum = max(0.0, 2.0 * dot(n, l)); // john edwards

	vec3 sandLit = u_sand_lit.rgb * texture(u_albedo_tex, v_uv).rgb;
	vec3 sandShadow = u_sand_shadow.rgb;
	
	return mix(sandShadow, sandLit, lum).rgb;
}

vec3 colour(vec3 light)
{
	vec3 normal = sandNormal(v_vertex.xz);	
	vec3 diffuse = diffuseCol(normal, light);
	return diffuse;
}

void fragment()
{
	ALBEDO = vec3(0.0);
}

void light()
{ 
	vec3 light = (vec4(LIGHT, 1.0) * INV_CAMERA_MATRIX).rgb;
	float shadow = 1.0 - pow(1.0 - ATTENUATION.r, 2.0);
	DIFFUSE_LIGHT = vec3(colour(light)) * shadow;
}