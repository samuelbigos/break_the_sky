shader_type spatial;
render_mode world_vertex_coords;

uniform vec4 u_sand_lit : hint_color;
uniform vec4 u_sand_shadow : hint_color;
uniform vec3 u_light_dir = vec3(0.0, 1.0, 0.0);
uniform sampler2D u_grain_texture;
uniform float u_grain_scale = 25.0f;
uniform float u_fresnel_pow = 5.0;
uniform float u_fresnel_strength = 1.0;
uniform float u_grain_strength = 0.25;
uniform float u_oceanspec_pow = 5.0;
uniform float u_oceanspec_strength = 0.5;
uniform sampler2D u_glitter_texture;
uniform float u_glitter_scale = 25.0f;
uniform float u_glitter_threshold = 0.01;

varying vec3 u_vertex;
varying vec3 u_normal;

// https://www.shadertoy.com/view/ltcGDl
vec2 hash( vec2 p )
{
	p = vec2( dot(p,vec2(127.1,311.7)),
			  dot(p,vec2(269.5,183.3)) );

	return -1.0 + 2.0*fract(sin(p)*43758.5453123);
}

float noise( in vec2 p )
{
    vec2 i = floor( p );
    vec2 f = fract( p );
	
	vec2 u = f*f*(3.0-2.0*f);

    return mix( mix( dot( hash( i + vec2(0.0,0.0) ), f - vec2(0.0,0.0) ), 
                     dot( hash( i + vec2(1.0,0.0) ), f - vec2(1.0,0.0) ), u.x),
                mix( dot( hash( i + vec2(0.0,1.0) ), f - vec2(0.0,1.0) ), 
                     dot( hash( i + vec2(1.0,1.0) ), f - vec2(1.0,1.0) ), u.x), u.y);
}

float sandH(vec2 p)
{
	//small
	float valS = noise(p * 0.5) + 0.5;//0~1
    valS = 1.0 - abs(valS - 0.5) * 2.0;
    valS = pow(valS,2.0);

    //middle
    float valM = noise(p * 0.26) + 0.5;//0~1
    valM = 1.0 - abs(valM - 0.5) * 2.0;
    valM = pow(valM,2.0);
    
    //big
    float valB = smoothstep(0.0,1.0,noise(p * 0.2) + 0.5);//0~1

    float val = valS * 0.0 + valM * 0.19 + valB * 0.8;

    return val * 1.3 - 0.3;
}

vec3 calcNormal(vec2 pos)
{
    vec2  eps = vec2( 0.1, 0.0 );
    return normalize( vec3( sandH(pos.xy-eps.xy) - sandH(pos.xy+eps.xy),
                            2.0*eps.x,
                            sandH(pos.xy-eps.yx) - sandH(pos.xy+eps.yx) ) );
}

void vertex()
{
	vec2 uv = vec2(VERTEX.x, VERTEX.z) * 0.05;
	VERTEX.y += sandH(uv) * 10.0;
	NORMAL.xyz = normalize(calcNormal(uv));
	
	// store worldspace vert/normal
	u_vertex = VERTEX;
	u_normal = NORMAL;
}

vec3 rippleNormal(vec3 n)
{
	return n;
}

vec3 sandNormal(vec3 n, vec2 uv)
{
	vec3 grain = texture(u_grain_texture, uv / u_grain_scale).rgb;
	grain = grain * 2.0 - 1.0f;
	return normalize(mix(n, grain, u_grain_strength));
}

vec3 diffuseCol(vec3 n, vec3 l)
{
	float lum;
	n.y *= 0.5;
	lum = max(0, 4.0 * dot(n, l)); // john edwards
	return mix(u_sand_shadow, u_sand_lit, lum).rgb;
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
	return step(rDotV, u_glitter_threshold) * (1.0 - rDotV) * vec3(1.0, 1.0, 1.0);
}

void fragment()
{
}

void light()
{ 
	vec3 normal = u_normal;
	normal = rippleNormal(normal);
	normal = sandNormal(normal, u_vertex.xz);
	
	vec3 light = (vec4(LIGHT, 1.0) * INV_CAMERA_MATRIX).rgb;
	vec3 view = (vec4(VIEW, 1.0) * INV_CAMERA_MATRIX).rgb;
	
	vec3 diffuse = diffuseCol(normal, light);
	vec3 rim = rimCol(normal, view);
	vec3 ocean = oceanSpec(normal, view, light);
	vec3 glitter = glitterSpec(normal, u_vertex.xz, light, view);
	
	vec3 spec = clamp(max(rim, ocean), 0.0, 1.0);
	vec3 col = diffuse + spec + glitter;
	
	DIFFUSE_LIGHT = col;
}