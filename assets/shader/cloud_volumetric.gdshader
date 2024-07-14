shader_type spatial;
render_mode world_vertex_coords;//, cull_front;

// mashed together from various sources:
// * Geurilla talk - https://www.youtube.com/watch?v=-d8qT5-1LOI
// * Geurilla paper - http://advances.realtimerendering.com/s2015/The%20Real-time%20Volumetric%20Cloudscapes%20of%20Horizon%20-%20Zero%20Dawn%20-%20ARTR.pdf
// * Shadertoy by alro - https://www.shadertoy.com/view/3sffzj
// * Clouds by Sebastian Lague - https://www.youtube.com/watch?v=4QOcCGI6xOU

uniform vec3 u_cloud_box_min = vec3(-250.0, 0.0, -250.0);
uniform vec3 u_cloud_box_max = vec3(250.0, 100.0, 250.0);

// coverage
uniform bool u_do_coverage = true;
uniform float u_coverage_scale = 256.0;
uniform float u_coverage_density = 1.0;
uniform sampler2D u_coverage_tex;

// shape
uniform bool u_subtract_shape = true;
uniform sampler2D u_shape_tex;

// detail
uniform bool u_subtract_detail = true;
uniform float u_detail_scale = 256.0;
uniform float u_detail_strength = 1.0;
uniform vec3 u_detail_weights = vec3(1.0, 0.5, 0.5);
uniform sampler3D u_detail_noise;

// clouds
uniform int u_num_cloud_steps = 32;
uniform float u_alpha_exponent = 1.0;

// dither
uniform bool u_do_dither = true;
uniform float u_dither_scale = 1024.0;
uniform sampler2D u_blue_noise;

// lighting
uniform vec4 u_colour_lit : source_color = vec4(1.0);
uniform vec4 u_colour_shadow : source_color = vec4(0.0);
uniform int u_num_light_steps = 16;
uniform float u_light_ray_dist = 50.0;
uniform float u_light_absorbtion = 1.0;
uniform float u_sun_power = 1.0;
uniform bool u_do_scattering = true;

// varying
varying vec3 v_vertex;
varying vec3 v_cam;
varying float v_blue_noise;
varying float v_depth;
varying vec3 v_depth_pos;

const float GOLDEN_RATIO = 1.6180339;
const vec3 SIGMA = vec3(1.0, 1.0, 1.0);

uniform sampler2D DEPTH_TEXTURE : hint_depth_texture, filter_linear_mipmap;

void vertex()
{
	v_vertex = VERTEX;
	v_cam = (INV_VIEW_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
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

float cloud_shape(float height)
{
	vec2 uv = vec2(0.5, 0.0);
	uv.y = height;
	return texture(u_shape_tex, uv).r;
}

float cloud_coverage(vec3 pos)
{
	vec4 coverage = texture(u_coverage_tex, pos.xz / u_coverage_scale);
	return clamp(remap(coverage.r, 0.0, 1.0, -(1.0 / u_coverage_density), 1.0), 0.0, 1.0);
}

float cloud_detail(vec3 pos, float cloud_density)
{	
	vec3 uv = pos.xzy / u_detail_scale;
	vec3 detail = texture(u_detail_noise, uv).rgb;

	// sebastian lague

	// horizon
	//float d = remap(detail.r, 0.0, 1.0, detail.g * 1.5, 1.0);
	float d = remap(detail.r * u_detail_weights.r, 0.0, 1.0, 0.0, 1.0);
	d += (detail.g * 2.0 - 1.0) * u_detail_weights.g;
	d += (detail.b * 2.0 - 1.0) * u_detail_weights.b;
	return d * u_detail_strength;
}

/* returns the density of cloud at given world position in 0-1 range. */
float cloud(vec3 pos, out float cloud_height, bool sample_detail)
{
	float cloud = 1.0;
	
	// start with cloud coverage, this gives the basic 2D coverage of clouds in the sky
	if (u_do_coverage)
	{
		float coverage = cloud_coverage(pos);
		cloud = coverage;
	}
	
	// subtract the shape of the cloud, this is defined by the cloud type (i.e. cumulus) and is a density heightmap
	if (u_subtract_shape)
	{
		cloud_height = 1.0 - clamp((pos.y - u_cloud_box_min.y) / (u_cloud_box_max.y - u_cloud_box_min.y), 0.0, 1.0);
		float shape = cloud_shape(cloud_height);
		cloud = min(cloud, shape); 
	}
	
	// subtract detail from cloud
	if (u_subtract_detail && sample_detail)
	{
		float detail = cloud_detail(pos, cloud);
		cloud -= detail;
	}
	
	return clamp(cloud, 0.0, 1.0);
}

float henyey_greenstein(float g, float costh)
{
	return (1.0 / (4.0 * 3.1415))  * ((1.0 - g * g) / pow(1.0 + g*g - 2.0*g*costh, 1.5));
}

float light_march(vec3 ro, vec3 rd)
{
	// sum up the total density of clouds along the path to the light source
	float step_size = u_light_ray_dist / float(u_num_light_steps);
	float density = 0.0;
	float cloud_height;
	for (int i = 0; i < u_num_light_steps; i++)
	{
		vec3 p = ro + rd * step_size * float(i);
		density += cloud(p, cloud_height, true) * step_size * u_light_absorbtion;
	}
	return density;
}

float beers_law(float density)
{
	return exp(-density);
}

vec3 cloud_march(vec3 ro, vec3 rd, vec3 light, float max_dist, vec2 fragcoord, out float alpha)
{	
	int steps = 0;
	float dist = 0.0;
	float cloud_height;
	float transmittance = 1.0;
	float light_energy = 0.0;
	
	// p.80 - define low and high level-of-detail step size
	// we'll march with large steps through the cloud volume until we hit a cloud, then step back and march
	// forward again with smaller step size and more detailed samples to save GPU time
	float step_size = float(max_dist) / float(u_num_cloud_steps);
	
	// blue noise to reduce banding from low step count
	// https://blog.demofox.org/2020/05/10/ray-marching-fog-with-blue-noise/
	if (u_do_dither)
	{
		float blue_noise = texture(u_blue_noise, fragcoord * u_dither_scale).r;
		dist += step_size * blue_noise;
	}
	
	// light scattering - from https://www.shadertoy.com/view/3sffzj
	float phase_function = 1.0;
	if (u_do_scattering)
	{
		float mu = dot(rd, light);
		phase_function = mix(henyey_greenstein(-0.3, mu), henyey_greenstein(0.3, mu), 0.7);
	}
	
	for (int i = 0; i < u_num_cloud_steps; i++)
	{
		vec3 p = ro + rd * dist;
		float density = cloud(p, cloud_height, true);
		
		steps++;
		dist += step_size;
		
		// calculate lighting for this sample if we're in the clouds
		if (density > 0.0)
		{
			float l_density = light_march(p, light);
			float l_transmittance = beers_law(l_density);
			
			float attenuation_component = (l_transmittance * transmittance * density);
			float phase_component = phase_function;
			float in_scattering_component = 1.0 - beers_law(density);
			
			light_energy += attenuation_component * phase_component * in_scattering_component * step_size * u_sun_power;
			transmittance *= beers_law(density * step_size);
			alpha += density * step_size;
		}
		
		if (alpha >= 1.0 || transmittance < 0.01)
			break;
	}
	
	vec3 cloud_color = mix(u_colour_shadow, u_colour_lit, light_energy).rgb;
	alpha = clamp(alpha, 0.0, 1.0);
	return cloud_color;
}

vec3 volume_cloud(vec3 cam, vec3 pixel, vec3 light, vec2 fragcoord, out float alpha, float depth)
{
	vec3 ro;
	vec3 rd = normalize(v_vertex - cam);
	float dist;
	float cam_to_ro;
	
	vec2 intersect = intersect_aabb(cam, rd, u_cloud_box_min, u_cloud_box_max);
	if (intersect.x < intersect.y) // there is an intersection
	{
		ro = cam + rd * intersect.x;
		cam_to_ro = intersect.x;
		dist = intersect.y - intersect.x;
	}
	
	dist = min(dist, depth - cam_to_ro);
	return cloud_march(ro, rd, light, dist, fragcoord, alpha);
}

void fragment()
{
	float depth = texture(DEPTH_TEXTURE, SCREEN_UV).x;
	vec3 ndc = vec3(SCREEN_UV, depth) * 2.0 - 1.0;
	vec4 world = INV_VIEW_MATRIX * INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
  	vec3 world_position = world.xyz / world.w;
	v_depth = length(world_position - v_cam);
	
	ALBEDO = vec3(0.0);
}

void light()
{
	vec3 light = (vec4(LIGHT, 1.0) * VIEW_MATRIX).rgb;

	float alpha;
	vec3 col = volume_cloud(v_cam, v_vertex, light, UV, alpha, v_depth);
	
	// add a shadow effect on the cloud so ships below it are visible
	float depth_pixel_below_cloud_dist = v_depth - length(v_vertex - v_cam);
	float depth_peek = clamp(remap(depth_pixel_below_cloud_dist, 10.0, 100.0, 0.0, 1.0), 0.0, 1.0);
	vec3 ship_shadow_col = mix(u_colour_lit.rgb, u_colour_shadow.rgb, 0.5);
	col = mix(ship_shadow_col, col, depth_peek);
	
	DIFFUSE_LIGHT = col;
	ALPHA = pow(alpha, u_alpha_exponent);
}