shader_type spatial;
render_mode world_vertex_coords;

// implementation of Horizon Zero Dawn cloud rendering
// http://advances.realtimerendering.com/s2015/The%20Real-time%20Volumetric%20Cloudscapes%20of%20Horizon%20-%20Zero%20Dawn%20-%20ARTR.pdf
// comments referencing page numbers refer to pages in that PDF

// clouds
uniform sampler3D u_noise;
uniform vec4 u_colour_a : hint_color;
uniform vec4 u_colour_b : hint_color;
uniform int u_flip;
uniform float u_scroll_speed = 1.0;
uniform float u_turbulence = 1.0;
uniform float u_scale = 256.0;
uniform float u_pos_y;
uniform float u_density = 0.5;
uniform bool u_transparent;
uniform vec4 u_transparent_col : hint_color;
uniform vec4 u_transparent_tex;

// determines how far we penetrate into clouds before stopping the raymarch, manifests
// as more transparent cloud edges
uniform float u_fluffiness = 10.0;
// cloud light absorbtion factor used in beer's law applicationi
uniform float u_absorbtion = 0.01;
// lit and shadowed cloud colour, we mix between these depending on lighting
uniform vec4 u_colour_lit : hint_color = vec4(1.0);
uniform vec4 u_colour_shadow : hint_color = vec4(0.0);

// height
uniform sampler2D u_cloud_heightmap;
uniform float u_height_world_top = 300.0;
uniform float u_height_world_bot = 100.0;

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

varying vec3 v_vertex;

void vertex()
{
	v_vertex = VERTEX;
}

// https://gist.github.com/DomNomNom/46bb1ce47f68d255fd5d
vec2 intersect_aabb(vec3 ro, vec3 rd, vec3 boxMin, vec3 boxMax) 
{
    vec3 tMin = (boxMin - ro) / rd;
    vec3 tMax = (boxMax - ro) / rd;
    vec3 t1 = min(tMin, tMax);
    vec3 t2 = max(tMin, tMax);
    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);
    return vec2(tNear, tFar);
}

float remap(float x, float a, float b, float c, float d)
{
    return (((x - a) / (b - a)) * (d - c)) + c;
}

vec4 cloud_noise(vec3 pos)
{	
	vec3 uv = pos.xzy / u_scale;
	vec4 col = vec4(0.);
	
	ivec3 texSize = textureSize(u_noise, 0);
	uv.x = mod(uv.x, 1.0);
	uv.y = mod(uv.y, 1.0);
	uv.z = mod(uv.z, 1.0);	
	col = texture(u_noise, uv);
	
	float pfbm = mix(1., col.r, .5);
   	pfbm = abs(pfbm * 2. - 1.); // billowy perlin noise	
	col.r = remap(col.r, 0., 1., col.g, 1.);	
	return col;
}

float height_gradient(vec3 pos)
{
	vec2 uv = vec2(0.5, 0.0);
	uv.y = 1.0 - clamp((pos.y - u_height_world_bot) / (u_height_world_top - u_height_world_bot), 0.0, 1.0);
	return texture(u_cloud_heightmap, uv).r;
}

/* returns the density of cloud at given world position in 0-1 range. */
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
    cloud = remap(cloud, mix(0.9, 0.95, 1.0 - u_density), 1.0, 0.0, 1.0); // fake cloud coverage
	cloud *= height_gradient(pos);
	
	return clamp(cloud, 0.0, 1.0);
}

bool ray_hit(vec3 pos, out float dist) 
{
	dist = cloud(pos);
	return dist > 0.0;
}

void cloud_march(vec3 ro, vec3 rd, vec3 light, float max_dist, out float dist, out float alpha, out float density, out float lighting)
{
	int max_steps = 128;
	float step_size = float(max_dist) / float(max_steps);
	int steps = 0;
	bool hit = false;
	dist = 0.0;
	alpha = 0.0;
	density = 0.0;
	
	// start by assuming all the light from the source reaches the camera
	// we'll then begin to attenuate (reduce) this value as we march through the clouds,
	// with more dense clouds (as determined by our cloud function) causing more attenuation
	lighting = 1.0;
	
	for (int i = 0; i < max_steps; i++)
	{
		steps++;
		dist += step_size;
		vec3 ray = ro + rd * dist;
		density = cloud(ray);
		alpha += density * step_size * (1.0 / u_fluffiness);
		
		if (density > 0.0)
		{		
			vec3 l_ro = ray;
			vec3 l_dir = light;
			int l_steps = 32;
			float l_dist = 50.0;
			float l_step_dist = l_dist / float(l_steps);
			
			// sum up the total density of clouds along the path to the light source
			float l_density = 0.0;
			for (int l = 0; l < 16; l++)
			{
				vec3 l_sample = l_ro + l_dir * l_step_dist * (float(l) + 1.0);
				l_density += cloud(l_sample) * l_step_dist;
			}
			
			// use this density to determine how much light has reached this point using beer's law
			// multiplying by step size here means attenuation is constant with respect to depth
			// regardless of how much we're stepping through the cloud
			float transmittance = exp(-l_density * u_absorbtion * step_size);
			
			// lighting is reduced by the transmittance at this sample point
			lighting *= transmittance;
		}
		
		if (steps >= max_steps || dist >= max_dist)
			break;
			
		// p.81 - once the alpha of the image reaches 1 we don’t need to keep sampling so we stop
		// the march early 
		if (alpha >= 1.0)
			break;
	}
}

void volume_cloud(vec3 cam_pos, vec3 light, out float dist, out float alpha, out float depth, out float lighting)
{
	vec3 dir = normalize(v_vertex - cam_pos);
	vec3 cloud_min = vec3(-250.0, u_height_world_bot, -250.0);
	vec3 cloud_max = vec3(250.0, u_height_world_top, 250.0);
	vec2 intersect = intersect_aabb(cam_pos, dir, cloud_min, cloud_max);
	if (intersect.x < intersect.y) // there is an intersection
	{
		vec3 ro = cam_pos + dir * intersect.x;
		float max_dist = intersect.y - intersect.x;
		cloud_march(ro, dir, light, max_dist, dist, alpha, depth, lighting);
	}
}

void light()
{
	vec3 world_camera = (CAMERA_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
	vec3 light = (vec4(LIGHT, 1.0) * INV_CAMERA_MATRIX).rgb;
	
	light = vec3(0.0, 1.0, 0.0);
	
	float dist = 0.0;
	float alpha = 0.0;
	float depth = 0.0;
	float lighting = 0.0;
	volume_cloud(world_camera, light, dist, alpha, depth, lighting);
	
	// set cloud colour based on the lighting value returned
	vec3 col = mix(u_colour_shadow, u_colour_lit, lighting).rgb;
	
	DIFFUSE_LIGHT = col;
	ALPHA = clamp(alpha, 0.0, 1.0);
	//ALPHA = step(0.001, c);
}