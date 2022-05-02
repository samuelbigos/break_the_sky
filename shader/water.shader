shader_type spatial;
render_mode unshaded;

uniform vec4 u_water_col : hint_color;
uniform sampler2D u_wave_texture;

void vertex() 
{
}

float sample_water(vec2 uv)
{
	return texture(u_wave_texture, uv).r;
}

void light()
{ 
	DIFFUSE_LIGHT = ALBEDO * ATTENUATION;
}

void fragment() 
{
	METALLIC = 0.5;
	ROUGHNESS = 0.5;
	
	vec4 col = u_water_col;
	
	// waves
	vec3 normal;
	{
		float kernel_size = 1.0;
		
		vec2 texSize;
		texSize.x = float(textureSize(u_wave_texture, 0).x);
		texSize.y = float(textureSize(u_wave_texture, 0).y);
		vec2 texelSize = kernel_size / texSize;
		
		// https://stackoverflow.com/questions/49640250/calculate-normals-from-heightmap
		float R = sample_water(UV + vec2(1.0, 0.0) * texelSize);
		float L = sample_water(UV + vec2(-1.0, 0.0) * texelSize);
		float T = sample_water(UV + vec2(0.0, 1.0) * texelSize);
		float B = sample_water(UV + vec2(0.0, -1.0) * texelSize);
		normal = normalize(vec3(2.0 * (R-L), 2.0 * -(B-T), 4.0));
	}
	
	ALBEDO = col.rgb;
	
	// rim highlight
	if (dot(vec3(0.0, 0.0, 1.0), normal) < 0.98)
	{
		ALBEDO = vec3(1.0, 1.0, 1.0);
	}
}