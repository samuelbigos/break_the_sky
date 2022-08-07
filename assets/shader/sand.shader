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
	
//	v_ripple_uv = v_uv / u_ripple_scale;
//	//v_ripple_uv += vec2(TIME * 0.2);
//	vec3 normal = normalize(texture(u_normal_tex, v_uv).rbg * 2.0 - 1.0);
//	normal.y *= 0.25;
//	float windDotRipple = dot(normalize(vec3(u_wind.x, 0.0, u_wind.y)), normalize(normal));
//	v_ripple_strength = max(0.0, pow(0.25 + windDotRipple, 1.0));
	
	// texture
	float duneHeight = texture(u_height_tex, v_uv).r;
	VERTEX.y += duneHeight * u_dune_scale_y;
	//VERTEX.y += texture(u_ripple_height_tex, v_ripple_uv).r * 5.0 * v_ripple_strength * u_ripple_scale;
	
	// store worldspace vert/normal
	v_vertex = VERTEX;
}

vec3 sandNormal(vec2 uv)
{
	vec3 dunes = normalize(texture(u_normal_tex, v_uv).rbg * 2.0 - 1.0);
	vec3 grain = normalize(texture(u_grain_texture, uv / u_grain_scale).rgb * 2.0 - 1.0);
	vec3 ripple = normalize(texture(u_ripple_normal_tex, v_ripple_uv).rbg * 2.0 - 1.0);
	vec3 n = normalize(mix(dunes, ripple, v_ripple_strength));
	return normalize(mix(n, grain, u_grain_strength));
}

vec3 diffuseCol(vec3 n, vec3 l)
{
	float lum;
	n.y *= 0.5;
	lum = max(0.0, 2.0 * dot(n, l)); // john edwards
	vec3 albedoDune = texture(u_albedo_tex, v_uv).rgb;
	vec3 albedoRipple = texture(u_ripple_albedo_tex, v_ripple_uv).rgb;
	vec3 sandLit = mix(albedoDune, albedoRipple, v_ripple_strength);
	vec3 sandShadow = sandLit * u_sand_shadow.rgb;
	
	sandLit = u_sand_lit.rgb * sandLit;
	sandShadow = u_sand_shadow.rgb;
	
	return mix(sandShadow, sandLit, lum).rgb;
}

vec3 rimCol(vec3 n, vec3 v)
{
	float rim = 1.0 - clamp(dot(n, v), 0.0, 1.0);
	rim = clamp(pow(rim, u_fresnel_pow) * u_fresnel_strength, 0.0, 1.0);
	rim = max(rim, 0);
	return rim * vec3(1.0, 1.0, 1.0);
}

vec3 oceanSpec(vec3 n, vec3 v, vec3 l)
{    
    vec3 h = normalize(v + l);
    float ndotH = max(0, dot(n, h));
    float specular = pow(ndotH, u_oceanspec_pow) * u_oceanspec_strength;
    return specular * vec3(1.0, 1.0, 1.0);
}

vec3 glitterSpec(vec3 n, vec2 uv, vec3 l, vec3 v)
{
	vec3 glitter = texture(u_glitter_texture, uv / u_glitter_scale).rgb;
	glitter = normalize(glitter * 2.0 - 1.0);
	vec3 r = reflect(l, glitter);
	float rDotV = abs(dot(r, v));
	return step(rDotV, u_glitter_threshold) * smoothstep(0.0, u_glitter_threshold, rDotV) * vec3(1.0, 1.0, 1.0);
}

vec3 colour(vec3 light, vec3 view)
{
	vec3 normal = sandNormal(v_vertex.xz);
	
	vec3 diffuse = diffuseCol(normal, light);
	vec3 rim = rimCol(normal, view);
	vec3 ocean = oceanSpec(normal, view, light);
	vec3 glitter = glitterSpec(normal, v_vertex.xz, light, view);
	
	vec3 spec = clamp(max(rim, ocean), 0.0, 1.0);
	vec3 col = diffuse + spec + glitter;
	
	return col;
}

// Converts a color from linear light gamma to sRGB gamma
vec4 fromLinear(vec4 linearRGB)
{
    bvec4 cutoff = lessThan(linearRGB, vec4(0.0031308));
    vec4 higher = vec4(1.055)*pow(linearRGB, vec4(1.0/2.4)) - vec4(0.055);
    vec4 lower = linearRGB * vec4(12.92);

    return mix(higher, lower, cutoff);
}

// Converts a color from sRGB gamma to linear light gamma
vec4 toLinear(vec4 sRGB)
{
    bvec4 cutoff = lessThan(sRGB, vec4(0.04045));
    vec4 higher = pow((sRGB + vec4(0.055))/vec4(1.055), vec4(2.4));
    vec4 lower = sRGB/vec4(12.92);

    return mix(higher, lower, cutoff);
}

void fragment()
{
	ALBEDO = vec3(0.0);
}

void light()
{ 
	vec3 light = (vec4(LIGHT, 1.0) * INV_CAMERA_MATRIX).rgb;
	vec3 view = (vec4(VIEW, 1.0) * INV_CAMERA_MATRIX).rgb;

	//DIFFUSE_LIGHT = toLinear(vec4(colour(light, view), 1.0)).rgb;
	DIFFUSE_LIGHT = vec3(colour(light, view)) * ATTENUATION;
}