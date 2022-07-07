shader_type spatial;
render_mode world_vertex_coords, cull_front;

// implementation of Horizon Zero Dawn cloud rendering
// https://www.youtube.com/watch?v=-d8qT5-1LOI
// https://www.shadertoy.com/view/3sffzj
// http://advances.realtimerendering.com/s2015/The%20Real-time%20Volumetric%20Cloudscapes%20of%20Horizon%20-%20Zero%20Dawn%20-%20ARTR.pdf
// comments referencing page numbers refer to pages in that PDF

// clouds
uniform sampler3D u_noise;
uniform float u_scroll_speed = 1.0;
uniform float u_scale = 256.0;
uniform float u_density = 0.5;

uniform int u_num_cloud_steps = 32;

uniform vec3 u_cloud_box_min = vec3(-250.0, 0.0, -250.0);
uniform vec3 u_cloud_box_max = vec3(250.0, 100.0, 250.0);

// determines how far we penetrate into clouds before stopping the raymarch, manifests
// as more transparent cloud edges
uniform float u_fluffiness = 5.0;
// cloud light absorbtion factor used in beer's law applicationi
uniform float u_absorbtion = 0.025;
// lit and shadowed cloud colour, we mix between these depending on lighting
uniform vec4 u_colour_lit : hint_color = vec4(1.0);
uniform vec4 u_colour_shadow : hint_color = vec4(0.0);

// lighting
uniform int u_num_light_steps = 16;
uniform vec4 u_sun_colour : hint_color = vec4(1.0);
uniform float u_sun_power = 1.0;

uniform sampler2D u_blue_noise;

// height
uniform sampler2D u_cloud_heightmap;

varying vec3 v_vertex;
varying float v_blue_noise;

const float GOLDEN_RATIO = 1.6180339;

const vec3 sigma = vec3(1.0, 1.0, 1.0);

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

float height_gradient(float height)
{
	vec2 uv = vec2(0.5, 0.0);
	uv.y = height;
	return texture(u_cloud_heightmap, uv).r;
}

/* returns the density of cloud at given world position in 0-1 range. */
float cloud(vec3 pos, out float cloud_height, bool sample_detail)
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
	cloud_height = 1.0 - clamp((pos.y - u_cloud_box_min.y) / (u_cloud_box_max.y - u_cloud_box_min.y), 0.0, 1.0);
	float density = height_gradient(cloud_height);
	cloud *= density;
	
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
        luminance += b * phase * exp(-step_size * extinction * sigma * a);
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
	vec2 intersect = intersect_aabb(ro, rd, u_cloud_box_min, u_cloud_box_max);
	
	// sum up the total density of clouds along the path to the light source
	float cloud_height;
	float step_size = intersect.y / float(u_num_light_steps);
	float density = 0.0;
	for (int i = 0; i < u_num_light_steps; i++)
	{
		vec3 p = ro + rd * step_size * float(i);
		density += cloud(p, cloud_height, true) * step_size;
		
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
	float blue_noise = texture(u_blue_noise, fragcoord * 1024.0f).r;
	float dist_to_start = step_size * blue_noise;
	
	ro += rd * dist_to_start;
	
	int steps = 0;
	float dist = 0.0;
	
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
		
//		if (alpha > 1.0)
//			break;
		
		if (transmittance < 0.01)
			break;
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