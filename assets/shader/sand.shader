shader_type spatial;
render_mode unshaded, world_vertex_coords;

uniform vec4 u_sand_col : hint_color;

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
	//VERTEX.y += sandH(uv) * 10.0;
	NORMAL.xyz = normalize(calcNormal(uv));
	
	// store worldspace vert/normal
	u_vertex = VERTEX;
	u_normal = NORMAL;
}

void fragment()
{
	vec3 lightDir = normalize(vec3(0.0, 0.75, 1.0));
	float lambertian = max(dot(lightDir,u_normal), 0.0);
	
	ALBEDO = u_sand_col.rgb * lambertian;
}