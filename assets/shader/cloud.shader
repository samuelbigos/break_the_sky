shader_type spatial;
render_mode unshaded, world_vertex_coords;

uniform sampler3D u_noise;
uniform vec4 u_colour_a : hint_color;
uniform vec4 u_colour_b : hint_color;
uniform int u_flip;
uniform float u_scroll_speed = 1.0;
uniform float u_turbulence = 1.0;
uniform float u_scale = 256.0;
uniform float u_density = 0.5;
uniform int u_mode = 0;

uniform sampler2D u_dither_tex;
uniform int u_bit_depth = 32;
uniform float u_contrast = 0;
uniform float u_offset = 0;
uniform int u_dither_size = 4;

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
	pos.xyz = pos.xzy;
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
		uv.z = mod(pos.z + TIME * u_turbulence * 0.01, 1.0);
		
		col = texture(u_noise, uv);
		
		float pfbm= mix(1., col.r, .5);
    	pfbm = abs(pfbm * 2. - 1.); // billowy perlin noise
		
		col.r = remap(col.r, 0., 1., col.g, 1.);
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
    cloud = remap(cloud, mix(0.9, 0.95, 1.0 - u_density), 1., 0., 1.); // fake cloud coverage
	return cloud;
}

void vertex()
{
	v_vertPos = VERTEX;
}

vec3 scale_pos(vec3 pos)
{
	return vec3(pos.x, 0.0, pos.z) / u_scale;
}

void fragment()
{
	vec3 vertPos = v_vertPos;
	vertPos.x += TIME * u_scroll_speed;
	vertPos.z += TIME * u_scroll_speed;
	
	vec3 floored_pos = vertPos * float(u_dither_size);
	floored_pos = scale_pos(floor(floored_pos));
	floored_pos /= float(u_dither_size);
	
	vec3 pos = scale_pos(vertPos);
	
	// flip to hide that we're using the same noise on different layers.
//	if (u_flip == 1)
//		pos.y = -pos.y;

	float kernel = 2.0 / u_scale;
	float R = cloud(floored_pos + vec3(1.0, 0.0, 0.0) * kernel);
	float L = cloud(floored_pos + vec3(-1.0, 0.0, 0.0) * kernel);
	float T = cloud(floored_pos + vec3(0.0, 0.0, 1.0) * kernel);
	float B = cloud(floored_pos + vec3(0.0, 0.0, -1.0) * kernel);
	vec3 normal = normalize(vec3(2.0 * (R-L), 4.0, 2.0 * -(B-T)));

	float d = dot(normal, vec3(1.0, 0.0, 1.0));
	
	d = clamp(d * 10.0, 0.0, 1.0);
	
	// dither 
	vec3 dithered;
	{
		float lum = d;
		
		ivec2 noise_size = textureSize(u_dither_tex, 0);
		vec2 inv_noise_size = vec2(1.0 / float(noise_size.x), 1.0 / float(noise_size.y));
		vec2 noise_uv = pos.xz * inv_noise_size * u_scale * float(u_dither_size);
		float threshold = texture(u_dither_tex, noise_uv).r;
		
		threshold = threshold * 0.99 + 0.005;
		
		float ramp_val = d < threshold ? 0.0f : 1.0f;
		dithered = mix(u_colour_b.rgb, u_colour_a.rgb, step(ramp_val, 0.0));
	}

	//ALBEDO = mix(u_colour_a.rgb, u_colour_b.rgb, d);
	ALBEDO = dithered;
	ALPHA = step(0.0, cloud(pos));
}