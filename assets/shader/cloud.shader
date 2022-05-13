shader_type spatial;
render_mode unshaded, world_vertex_coords;

uniform sampler3D u_noise;
uniform vec4 u_colour_a : hint_color;
uniform vec4 u_colour_b : hint_color;
uniform float u_offset;
uniform float u_scroll_speed = 0.1;
uniform float u_turbulence = 0.01;
uniform float u_scale = 256.0;
uniform int u_mode = 0;

varying vec3 v_vertPos;

vec3 hash(vec3 p)
{
	p = vec3( dot(p,vec3(127.1,311.7, 74.7)),
			  dot(p,vec3(269.5,183.3,246.1)),
			  dot(p,vec3(113.5,271.9,124.6)));

	return -1.0 + 2.0*fract(sin(p)*43758.5453123);
}

float remap(float x, float a, float b, float c, float d)
{
    return (((x - a) / (b - a)) * (d - c)) + c;
}

vec4 noised( in vec3 x )
{
    // grid
    vec3 i = floor(x);
    vec3 w = fract(x);
    
    // quintic interpolant
    vec3 u = w*w*w*(w*(w*6.0-15.0)+10.0);
    vec3 du = 30.0*w*w*(w*(w-2.0)+1.0);  
    
    // gradients
    vec3 ga = hash( i+vec3(0.0,0.0,0.0) );
    vec3 gb = hash( i+vec3(1.0,0.0,0.0) );
    vec3 gc = hash( i+vec3(0.0,1.0,0.0) );
    vec3 gd = hash( i+vec3(1.0,1.0,0.0) );
    vec3 ge = hash( i+vec3(0.0,0.0,1.0) );
	vec3 gf = hash( i+vec3(1.0,0.0,1.0) );
    vec3 gg = hash( i+vec3(0.0,1.0,1.0) );
    vec3 gh = hash( i+vec3(1.0,1.0,1.0) );
    
    // projections
    float va = dot( ga, w-vec3(0.0,0.0,0.0) );
    float vb = dot( gb, w-vec3(1.0,0.0,0.0) );
    float vc = dot( gc, w-vec3(0.0,1.0,0.0) );
    float vd = dot( gd, w-vec3(1.0,1.0,0.0) );
    float ve = dot( ge, w-vec3(0.0,0.0,1.0) );
    float vf = dot( gf, w-vec3(1.0,0.0,1.0) );
    float vg = dot( gg, w-vec3(0.0,1.0,1.0) );
    float vh = dot( gh, w-vec3(1.0,1.0,1.0) );
	
    // interpolations
    return vec4( va + u.x*(vb-va) + u.y*(vc-va) + u.z*(ve-va) + u.x*u.y*(va-vb-vc+vd) + u.y*u.z*(va-vc-ve+vg) + u.z*u.x*(va-vb-ve+vf) + (-va+vb+vc-vd+ve-vf-vg+vh)*u.x*u.y*u.z,    // value
                 ga + u.x*(gb-ga) + u.y*(gc-ga) + u.z*(ge-ga) + u.x*u.y*(ga-gb-gc+gd) + u.y*u.z*(ga-gc-ge+gg) + u.z*u.x*(ga-gb-ge+gf) + (-ga+gb+gc-gd+ge-gf-gg+gh)*u.x*u.y*u.z +   // derivatives
                 du * (vec3(vb,vc,ve) - va + u.yzx*vec3(va-vb-vc+vd,va-vc-ve+vg,va-vb-ve+vf) + u.zxy*vec3(va-vb-ve+vf,va-vb-vc+vd,va-vc-ve+vg) + u.yzx*u.zxy*(-va+vb+vc-vd+ve-vf-vg+vh) ));
}

// Tileable 3D worley noise
float worleyNoise(vec3 uv, float freq)
{    
    vec3 id = floor(uv);
    vec3 p = fract(uv);
    
    float minDist = 10000.;
    for (float x = -1.; x <= 1.; ++x)
    {
        for(float y = -1.; y <= 1.; ++y)
        {
            for(float z = -1.; z <= 1.; ++z)
            {
                vec3 offset = vec3(x, y, z);
            	vec3 h = hash(mod(id + offset, vec3(freq))) * .5 + .5;
    			h += offset;
            	vec3 d = p - h;
           		minDist = min(minDist, dot(d, d));
            }
        }
    }
    
    // inverted worley noise
    return 1. - minDist;
}

// Fbm for Perlin noise based on iq's blog
float perlinfbm(vec3 p, float freq, int octaves)
{
    float G = exp2(-.85);
    float amp = 1.;
    float noise = 0.;
    for (int i = 0; i < octaves; ++i)
    {
        noise += amp * noised(p * freq).x;
        freq *= 2.;
        amp *= G;
    }
    
    return noise;
}

// Tileable Worley fbm inspired by Andrew Schneider's Real-Time Volumetric Cloudscapes
// chapter in GPU Pro 7.
float worleyFbm(vec3 p, float freq)
{
    return worleyNoise(p*freq, freq) * .625 +
        	 worleyNoise(p*freq*2., freq*2.) * .25 +
        	 worleyNoise(p*freq*4., freq*4.) * .125;
}

vec4 cloud_noise(vec3 pos)
{	
	vec4 col = vec4(0.);
	
	if (u_mode == 1)
	{
		float freq = 4.;
	    float pfbm = mix(1., perlinfbm(pos, 4., 1), .5);
	    pfbm = abs(pfbm * 2. - 1.); // billowy perlin noise
	
		col.g += worleyFbm(pos, freq);
	    col.b += worleyFbm(pos, freq*2.);
	    col.a += worleyFbm(pos, freq*4.);
	    col.r += remap(pfbm, 0., 1., col.g, 1.); // perlin-worley
	}
	else
	{
		ivec3 texSize = textureSize(u_noise, 0);
		vec3 uv;
		uv.x = mod(pos.x, 1.0);
		uv.y = mod(pos.y, 1.0);
		uv.z = mod(pos.z, 1.0);
		
		vec4 sample = texture(u_noise, uv);
		col = sample;
	}    
	return col;
}

float cloud(vec3 pos)
{
	vec4 cloud_noise = cloud_noise(pos);
	
	vec3 worley = cloud_noise.yzw;
    float wfbm = worley.x * .625 +
        		 worley.y * .125 +
        		 worley.z * .25; 

	float perlinWorley = cloud_noise.r;

    // cloud shape modeled after the GPU Pro 7 chapter
    float cloud = remap(perlinWorley, wfbm - 1., 1., 0., 1.);
    cloud = remap(cloud, .9, 1., 0., 1.); // fake cloud coverage
	return cloud;
}

void vertex()
{
	v_vertPos = VERTEX;
}

void fragment()
{
	vec2 scroll = vec2(TIME, TIME) * u_scroll_speed;
	vec3 pos = vec3(v_vertPos.xz + vec2(u_offset) + scroll, TIME * u_turbulence) / u_scale;

	float kernel = 1.0 / u_scale;
	float R = cloud(pos + vec3(1.0, 0., 0.0) * kernel);
	float L = cloud(pos + vec3(-1.0, 0., 0.0) * kernel);
	float T = cloud(pos + vec3(0.0, 1., 0.0) * kernel);
	float B = cloud(pos + vec3(0.0, -1., 0.0) * kernel);
	vec3 normal = normalize(vec3(2.0 * (R-L), 2.0 * -(B-T), 4.0));

	float d = dot(normal, vec3(0.25, 1.0, 0.0));

	ALBEDO = mix(u_colour_a.rgb, u_colour_b.rgb, step(0.025, d));
	ALPHA = step(0.0, (R+L+T+B) / 4.0);

//	ALBEDO = vec3(cloud_noise(pos).r);
//	ALPHA = 1.0;
}