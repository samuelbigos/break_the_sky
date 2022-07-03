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
uniform float u_density = 0.5;
uniform bool u_transparent;
uniform vec4 u_transparent_col : hint_color;
uniform vec4 u_transparent_tex;

uniform float u_edge_strength = 0.5;

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
	cloud *= height_gradient(pos);
	
	return cloud;
}

bool ray_hit(vec3 pos, out float dist) 
{
	dist = cloud(pos);
	return dist > 0.0;
}

float cloud_march(vec3 ro, vec3 rd, out float dist, out float alpha, out float depth)
{
	int max_steps = 128;
	float step_size = 5.0;
	int steps = 0;
	bool hit = false;
	float density = 0.0;
	dist = 0.0;
	alpha = 0.0;
	depth = 0.0;
	for (int i = 0; i < max_steps; i++)
	{
		steps++;
		dist += step_size;
		vec3 ray = ro + rd * dist;
		depth = cloud(ray);
		alpha += depth * u_edge_strength;
		density += depth;
		
		if (steps > max_steps)
			break;
			
		if (alpha >= 1.0)
			break;
	}
	density /= float(steps);
	return density;
}

//float volume_cloud(vec3 cam_pos)
//{
//	vec3 dir = normalize(v_vertex - cam_pos);
//	vec3 cloud_min = vec3(-250.0, u_height_world_bot, -250.0);
//	vec3 cloud_max = vec3(250.0, u_height_world_top, 250.0);
//	vec2 intersect = intersect_aabb(cam_pos, dir, cloud_min, cloud_max);
//	if (intersect.x < intersect.y) // there is an intersection
//	{
//		vec3 ro = cam_pos + dir * intersect.x;
//		return clamp(cloud_march(ro, dir, intersect.y - intersect.x), 0.0, 1.0);
//	}
//	return 0.0;
//}

float volume_cloud(vec3 cam_pos, out float dist, out float alpha, out float depth)
{
	vec3 dir = normalize(v_vertex - cam_pos);
	vec3 ro = v_vertex;
	return clamp(cloud_march(ro, dir, dist, alpha, depth), 0.0, 1.0);
}

void light()
{
	vec3 world_camera = (CAMERA_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
	vec3 light = (vec4(LIGHT, 1.0) * INV_CAMERA_MATRIX).rgb;
	
	float dist = 0.0;
	float alpha = 0.0;
	float depth = 0.0;
	float c = volume_cloud(world_camera, dist, alpha, depth);
	
	// beer's law
	float absorb = 3.0;
	float energy = exp(-absorb * depth);
	vec3 col = vec3(1.0) * energy;
	
	DIFFUSE_LIGHT = col;
	ALPHA = clamp(alpha, 0.0, 1.0);
	//ALPHA = step(0.001, c);
}