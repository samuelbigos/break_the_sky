shader_type spatial;
render_mode unshaded, world_vertex_coords;

// clouds
uniform sampler3D u_noise;
uniform vec4 u_colour_a : hint_color;
uniform vec4 u_colour_b : hint_color;
uniform int u_flip;
uniform float u_scroll_speed = 1.0;
uniform float u_turbulence = 1.0;
uniform float u_scale = 256.0;
uniform float u_density = 0.5;
uniform int u_mode = 0;

// dither
uniform sampler2D u_dither_tex;
uniform int u_bit_depth = 32;
uniform float u_contrast = 0;
uniform float u_offset = 0;
uniform int u_dither_size = 4;

// shadows
uniform vec2 u_plane_size;
uniform bool u_receive_shadow;
uniform vec2 u_shadow_offset;
uniform sampler2D u_boid_vel_tex;

varying vec3 v_vertPos;

void vertex()
{
	v_vertPos = VERTEX;
}

float remap(float x, float a, float b, float c, float d)
{
    return (((x - a) / (b - a)) * (d - c)) + c;
}

vec4 cloud_noise(vec3 pos)
{	
	pos.xyz = pos.xzy;
	vec4 col = vec4(0.);
	
	ivec3 texSize = textureSize(u_noise, 0);
	vec3 uv;
	uv.x = mod(pos.x, 1.0);
	uv.y = mod(pos.y, 1.0);
	uv.z = mod(pos.z + TIME * u_turbulence * 0.01, 1.0);
	
	col = texture(u_noise, uv);
	
	float pfbm= mix(1., col.r, .5);
   	pfbm = abs(pfbm * 2. - 1.); // billowy perlin noise
	
	col.r = remap(col.r, 0., 1., col.g, 1.);
	
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

vec3 scale_pos(vec3 pos)
{
	return vec3(pos.x, 0.0, pos.z) / u_scale;
}

void fragment()
{
	vec3 vertPos = v_vertPos;
	vertPos.x += TIME * u_scroll_speed;
	vertPos.z += TIME * u_scroll_speed;
	
	// flip to hide that we're using the same noise on different layers.
	if (u_flip == 1)
		vertPos.x = -vertPos.x;
		
	vec3 floored_pos = vertPos * float(u_dither_size);
	floored_pos = scale_pos(floor(floored_pos));
	floored_pos /= float(u_dither_size);
	
	vec3 pos = scale_pos(vertPos);

	float kernel = 2.0 / u_scale;
	float R = cloud(floored_pos + vec3(1.0, 0.0, 0.0) * kernel);
	float L = cloud(floored_pos + vec3(-1.0, 0.0, 0.0) * kernel);
	float T = cloud(floored_pos + vec3(0.0, 0.0, 1.0) * kernel);
	float B = cloud(floored_pos + vec3(0.0, 0.0, -1.0) * kernel);
	vec3 normal = normalize(vec3(2.0 * (R-L), 4.0, 2.0 * -(B-T)));

	if (u_flip == 1)
		normal.x = -normal.x;
		
	float d = dot(normal, vec3(1.0, 0.0, 1.0));
	
	d = clamp(d * 10.0, 0.0, 1.0);
	
	// boid shadows
	float shadow;
	if (u_receive_shadow)
	{
		// map boid texture onto clouds
		vec2 texSize = vec2(textureSize(u_boid_vel_tex, 0));
		vec2 uv = UV - 0.5;
		uv.x *= (u_plane_size.x / texSize.x) * 2.0;
		uv.y *= (u_plane_size.y / texSize.y) * 2.0;
		uv.x += normal.x * 0.05;
		uv.y += normal.z * 0.05;
		uv.x += u_shadow_offset.x / u_plane_size.x;
		uv.y += u_shadow_offset.y / u_plane_size.y;
		shadow = texture(u_boid_vel_tex, uv + 0.5).a;
	}
	
	// dither 
	vec3 dithered;
	{
		float lum = min(d, 1.0 - shadow);
		
		ivec2 noise_size = textureSize(u_dither_tex, 0);
		vec2 inv_noise_size = vec2(1.0 / float(noise_size.x), 1.0 / float(noise_size.y));
		vec2 noise_uv = pos.xz * inv_noise_size * u_scale * float(u_dither_size);
		float threshold = texture(u_dither_tex, noise_uv).r;
		
		threshold = threshold * 0.99 + 0.005;
		
		float ramp_val = lum < threshold ? 0.0f : 1.0f;
		dithered = mix(u_colour_b.rgb, u_colour_a.rgb, step(ramp_val, 0.0));
	}
	
	//ALBEDO = mix(u_colour_a.rgb, u_colour_b.rgb, d);
	ALBEDO = dithered;
	ALPHA = step(0.0, cloud(pos));
}

void light()
{ 
	//DIFFUSE_LIGHT = ALBEDO * ATTENUATION;
}