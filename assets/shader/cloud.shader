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
uniform float u_pos_y;
uniform vec2 u_parallax_offset;
uniform float u_density = 0.5;
uniform bool u_transparent;
uniform vec4 u_transparent_col : hint_color;
uniform vec4 u_transparent_tex;

// dither
uniform bool u_do_dither = false;
uniform sampler2D u_dither_tex;
uniform int u_bit_depth = 32;
uniform float u_contrast = 0;
uniform float u_offset = 0;
uniform int u_dither_size = 1;

// shadows
uniform vec2 u_plane_size;
uniform bool u_receive_shadow;
uniform vec2 u_shadow_offset;
uniform sampler2D u_boid_vel_tex;

// cloud deform
uniform int u_num_boids;
uniform float u_displace_radius;
uniform vec3 u_boid_pos_1;
uniform vec3 u_boid_pos_2;
uniform vec3 u_boid_pos_3;
uniform vec3 u_boid_pos_4;
uniform vec3 u_boid_pos_5;

varying vec3 v_vertPos;

void vertex()
{
	v_vertPos = VERTEX;
}

float remap(float x, float a, float b, float c, float d)
{
    return (((x - a) / (b - a)) * (d - c)) + c;
}

vec3 scale_pos(vec3 pos)
{
	return vec3(pos.x + u_parallax_offset.x, pos.y, pos.z + u_parallax_offset.y) / u_scale;
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
	
	pos.y = u_pos_y / u_scale;
	float radius = u_displace_radius / u_scale;
	
	if (u_num_boids > 0)
	{
		float d = 99999.0;
		if (u_num_boids > 0)
			d = min(d, length(pos - scale_pos(u_boid_pos_1)));
		if (u_num_boids > 1)
			d = min(d, length(pos - scale_pos(u_boid_pos_2)));
		if (u_num_boids > 2)
			d = min(d, length(pos - scale_pos(u_boid_pos_3)));
		if (u_num_boids > 3)
			d = min(d, length(pos - scale_pos(u_boid_pos_4)));
		if (u_num_boids > 4)
			d = min(d, length(pos - scale_pos(u_boid_pos_5)));
			
		d = clamp(d, 0.0, radius) / radius;
		
		return cloud - (mix(2.0, 0.0, d));
	}
	else
	{
		return cloud;
	}
	
}

void fragment()
{
	vec3 vertPos = v_vertPos;
//	vertPos.x += TIME * u_scroll_speed;
//	vertPos.z += TIME * u_scroll_speed;
	
	// flip to hide that we're using the same noise on different layers.
	if (u_flip == 1)
		vertPos.x = -vertPos.x;
		
	vec3 pos = scale_pos(vertPos);
	vec3 col;
	
	// clouds
	vec3 normal;
	float clouds;
	float lum;
	{
		float kernel = 2.0 / u_scale;
		float R = cloud(pos + vec3(1.0, 0.0, 0.0) * kernel);
		float L = cloud(pos + vec3(-1.0, 0.0, 0.0) * kernel);
		float T = cloud(pos + vec3(0.0, 0.0, 1.0) * kernel);
		float B = cloud(pos + vec3(0.0, 0.0, -1.0) * kernel);
		normal = normalize(vec3(2.0 * (R-L), 4.0, 2.0 * -(B-T)));

		if (u_flip == 1)
			normal.x = -normal.x;
			
		float d = dot(normal, vec3(1.0, 0.0, 1.0));
		lum = clamp(d * 10.0, 0.0, 1.0);
		clouds = step(0.0, cloud(pos));
	}
	
	// outline
//	float edge;
//	{
//		float kernel = 0.5 / u_scale;
//		float R = cloud(pos + vec3(1.0, 0.0, 0.0) * kernel);
//		float L = cloud(pos + vec3(-1.0, 0.0, 0.0) * kernel);
//		float T = cloud(pos + vec3(0.0, 0.0, 1.0) * kernel);
//		float B = cloud(pos + vec3(0.0, 0.0, -1.0) * kernel);
//		normal = normalize(vec3(2.0 * (R-L), 4.0, 2.0 * -(B-T)));
//
//		float dcdx = step(0.0, R) - step(0.0, L);
//		float dcdy = step(0.0, T) - step(0.0, B);
//		vec2 dcdi = vec2(dcdx,dcdy);
//		edge = length(dcdi);
//		edge = 1.0 - edge;
//		edge = smoothstep(0.0, 0.01, edge);
//	}
	
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
	float dither_threshold;
	{
		lum = min(lum, 1.0 - shadow);
		
		ivec2 noise_size = textureSize(u_dither_tex, 0);
		vec2 inv_noise_size = vec2(1.0 / float(noise_size.x), 1.0 / float(noise_size.y));
		vec2 noise_uv = pos.xz * inv_noise_size * u_scale * float(u_dither_size);
		dither_threshold = texture(u_dither_tex, noise_uv).r;
		
		dither_threshold = dither_threshold * 0.99 + 0.005;		
		float ramp_val = lum < dither_threshold ? 0.0f : 1.0f;
		
		if (u_do_dither)
		{
			col = mix(u_colour_b.rgb, u_colour_a.rgb, step(ramp_val, 0.0));
		}
		else
		{
			col = mix(u_colour_b.rgb, u_colour_a.rgb, lum);
		}
	}

	// transparency
	float transparent;
	vec3 transparent_col;
	if (u_transparent)
	{
		// map boid texture onto clouds
		vec2 texSize = vec2(textureSize(u_boid_vel_tex, 0));
		vec2 uv = UV - 0.5;
		uv.x *= (u_plane_size.x / texSize.x) * 2.0;
		uv.y *= (u_plane_size.y / texSize.y) * 2.0;
		uv.x += normal.x * 0.01;
		uv.y += normal.z * 0.01;
		uv.x += u_shadow_offset.x / u_plane_size.x;
		uv.y += u_shadow_offset.y / u_plane_size.y;
		transparent = texture(u_boid_vel_tex, uv + 0.5).a;
		
		transparent_col = mix(u_transparent_col.rgb, col, step(0.5, dither_threshold));
	}

	//ALBEDO = mix(u_colour_a.rgb, u_colour_b.rgb, d);
	ALBEDO = mix(col, transparent_col, transparent);
	//ALBEDO = mix(vec3(0.0), ALBEDO, edge);
	//ALBEDO = vec3(edge);
	ALPHA = clouds;
}