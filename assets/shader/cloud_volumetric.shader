shader_type spatial;
render_mode world_vertex_coords, cull_front;

// implementation of Horizon Zero Dawn cloud rendering
// https://www.youtube.com/watch?v=-d8qT5-1LOI
// https://www.shadertoy.com/view/3sffzj
// http://advances.realtimerendering.com/s2015/The%20Real-time%20Volumetric%20Cloudscapes%20of%20Horizon%20-%20Zero%20Dawn%20-%20ARTR.pdf
// comments referencing page numbers refer to pages in that PDF

// coverage
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
uniform sampler3D u_detail_noise;

// clouds
uniform float u_scroll_speed = 1.0;
uniform float u_density = 0.5;

uniform int u_num_cloud_steps = 32;

uniform vec3 u_cloud_box_min = vec3(-250.0, 0.0, -250.0);
uniform vec3 u_cloud_box_max = vec3(250.0, 100.0, 250.0);

// lit and shadowed cloud colour, we mix between these depending on lighting
uniform vec4 u_colour_lit : hint_color = vec4(1.0);
uniform vec4 u_colour_shadow : hint_color = vec4(0.0);

// dither
uniform bool u_do_dither = true;
uniform float u_dither_scale = 1024.0;
uniform sampler2D u_blue_noise;

// lighting
uniform int u_num_light_steps = 16;
uniform float u_light_ray_dist = 50.0;
uniform float u_light_absorbtion = 1.0;
uniform vec4 u_sun_colour : hint_color = vec4(1.0);
uniform float u_sun_power = 1.0;


// verying
varying vec3 v_vertex;
varying float v_blue_noise;

const float GOLDEN_RATIO = 1.6180339;
const vec3 SIGMA = vec3(1.0, 1.0, 1.0);

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

float cloud_detail(vec3 pos)
{	
	vec3 uv = pos.xzy / u_detail_scale;
	vec4 detail = texture(u_detail_noise, uv);

	float d = remap(detail.r, 0.0, 1.0, detail.g * 1.5, 1.0);
	return 1.0 - d;
}

/* returns the density of cloud at given world position in 0-1 range. */
float cloud(vec3 pos, out float cloud_height, bool sample_detail)
{
	float cloud = 0.0;
	
	// start with cloud coverage, this gives the basic 2D coverage of clouds in the sky
	float coverage = cloud_coverage(pos);
	cloud = coverage;
	
	// subtract the shape of the cloud, this is defined by the cloud type (i.e. cumulus) and is a density heightmap
	if (u_subtract_shape)
	{
		cloud_height = 1.0 - clamp((pos.y - u_cloud_box_min.y) / (u_cloud_box_max.y - u_cloud_box_min.y), 0.0, 1.0);
		float shape = cloud_shape(cloud_height);
		cloud = min(cloud, shape); 
	}
	
	// subtract detail from cloud
	if (u_subtract_detail)
	{
		float detail = cloud_detail(pos);
		cloud -= detail * u_detail_strength;
	}
	
	return clamp(cloud, 0.0, 1.0);
}

float henyey_greenstein(float g, float costh)
{
	return (1.0 / (4.0 * 3.1415))  * ((1.0 - g * g) / pow(1.0 + g*g - 2.0*g*costh, 1.5));
}

// https://twitter.com/FewesW/status/1364629939568451587/photo/1
vec3 multiple_octaves(float extinction, float mu, float step_size)
{
    vec3 luminance = vec3(0);
    const float octaves = 4.0;
    
    // Attenuation
    float a = 1.0;
    // Contribution
    float b = 1.0;
    // Phase attenuation
    float c = 1.0;
    
    float phase;
	
    for(float i = 0.0; i < octaves; i++)
	{
        // Two-lobed HG
        phase = mix(henyey_greenstein(-0.1 * c, mu), henyey_greenstein(0.3 * c, mu), 0.7);
        luminance += b * phase * exp(-step_size * extinction * SIGMA * a);
        // Lower is brighter
        a *= 0.2;
        // Higher is brighter
        b *= 0.5;
        c *= 0.5;
    }
    return luminance;
}

float light_march(vec3 ro, vec3 rd)
{
	//vec2 intersect = intersect_aabb(ro, rd, u_cloud_box_min, u_cloud_box_max);
	//float step_size = intersect.y / float(u_num_light_steps);
	
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
	// p.80 - define low and high level-of-detail step size
	// we'll march with large steps through the cloud volume until we hit a cloud, then step back and march
	// forward again with smaller step size and more detailed samples to save GPU time
	float step_size = float(max_dist) / float(u_num_cloud_steps);
	
	// blue noise to reduce banding from low step count
	// https://blog.demofox.org/2020/05/10/ray-marching-fog-with-blue-noise/
	float blue_noise = texture(u_blue_noise, fragcoord * u_dither_scale).r;
	
	int steps = 0;
	float dist = 0.0;
	if (u_do_dither)
	{
		dist += step_size * blue_noise;
	}
	
	// start by assuming all the light from the source reaches the camera
	// we'll then begin to attenuate (reduce) this value as we march through the clouds,
	// with more dense clouds (as determined by our cloud function) causing more attenuation
	vec3 cloud_color = vec3(1.0);
	vec3 sun_light = u_sun_colour.rgb * u_sun_power;
	
	// Variable to track transmittance along view ray. 
    // Assume clear sky and attenuate light when encountering clouds.
	float cloud_height;
	
	float transmittance = 1.0;
	float light_energy = 0.0;
	
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

			light_energy += (l_transmittance * transmittance * density) * step_size;

			transmittance *= beers_law(density * step_size);
			
			alpha += density * step_size;
		}
		
		if (alpha >= 1.0)
			break;
		
		if (transmittance < 0.01)
		{
			alpha = 1.0;
			break;
		}
	}
	
	cloud_color = vec3(light_energy);
	alpha = clamp(alpha, 0.0, 1.0);
	return cloud_color;
	//return vec3(cloud(vec3(ro.x, 0.0, ro.z), cloud_height, true));
}

vec3 volume_cloud(vec3 cam, vec3 pixel, vec3 light, vec2 fragcoord, out float alpha)
{
	bool inside = cam.x > u_cloud_box_min.x && cam.x < u_cloud_box_max.x &&
				  cam.y > u_cloud_box_min.y && cam.y < u_cloud_box_max.y &&
				  cam.z > u_cloud_box_min.z && cam.z < u_cloud_box_max.z;
				
	vec3 ro;
	vec3 rd = normalize(v_vertex - cam);
	float dist;
	if (inside)
	{
		ro = cam;
		dist = length(ro - pixel);
	}
	else
	{
		vec2 intersect = intersect_aabb(cam, rd, u_cloud_box_min, u_cloud_box_max);
		if (intersect.x < intersect.y) // there is an intersection
		{
			ro = cam + rd * intersect.x;
			dist = intersect.y - intersect.x;
		}
	}
	
	return cloud_march(ro, rd, light, dist, fragcoord, alpha);
}

void light()
{
	vec3 cam = (CAMERA_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
	vec3 light = (vec4(LIGHT, 1.0) * INV_CAMERA_MATRIX).rgb;
	
	float alpha;
	vec3 col = volume_cloud(cam, v_vertex, light, UV, alpha);
	
	// set cloud colour based on the lighting value returned
	//vec3 col = mix(u_colour_shadow, u_colour_lit, lighting).rgb;
	
	DIFFUSE_LIGHT = col;
	ALPHA = alpha;
}