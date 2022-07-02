shader_type spatial;
render_mode world_vertex_coords;

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
	uv.z = mod(pos.z, 1.0);
	
	col = texture(u_noise, uv);
	
	float pfbm = mix(1., col.r, .5);
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
    float cloud = remap(perlinWorley, 0.0, 1.0, worley.x, 1.0);
    cloud = remap(cloud, mix(0.9, 0.95, 1.0 - u_density), 1., 0., 1.); // fake cloud coverage
	
	return cloud;
}

bool ray_hit(vec3 pos, out float dist) 
{
	dist = cloud(pos);
	return dist > 0.0;
}

float volumeCloud(vec3 pos, vec3 view)
{
	vec3 ray_start = pos;
	vec3 ray_dir = normalize(view);
	
	int max_steps = 64;
	vec3 ray = ray_start;
	int steps = 0;
	bool hit = false;
	float density = 0.0;
	for (int i = 0; i < max_steps; i++)
	{
		steps++;
		float dist;
		density += cloud(ray);
		ray += 0.001 * ray_dir;
		if (steps > 16)
		{
			break;
		}
	}
	density /= float(steps);
	return density;
}

void light()
{
	vec3 light = (vec4(LIGHT, 1.0) * INV_CAMERA_MATRIX).rgb;
	vec3 view = (vec4(VIEW, 1.0) * INV_CAMERA_MATRIX).rgb;
	
	vec3 pos = v_vertPos / u_scale;
	
	float c = clamp(volumeCloud(pos, view), 0.0, 1.0);
	DIFFUSE_LIGHT = vec3(c);
	ALPHA = c;
}