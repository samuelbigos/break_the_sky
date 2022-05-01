shader_type spatial;
render_mode world_vertex_coords, specular_blinn;

uniform vec4 u_water_col : hint_color;
uniform sampler2D u_wave_texture;
varying vec3 v_normal;

void vertex() 
{
}

void fragment() 
{
	METALLIC = 0.5;
	ROUGHNESS = 0.1;
	
	// Waves
	{
		float kernel_size = 1.0;
		float h_mod = 1.0;
		
		vec2 texSize;
		texSize.x = float(textureSize(u_wave_texture, 0).x);
		texSize.y = float(textureSize(u_wave_texture, 0).y);
		vec2 texelSize = kernel_size / texSize;
		
		// https://stackoverflow.com/questions/49640250/calculate-normals-from-heightmap
		float R = texture(u_wave_texture, UV + vec2(1.0, 0.0) * texelSize).r * h_mod;
		float L = texture(u_wave_texture, UV + vec2(-1.0, 0.0) * texelSize).r * h_mod;
		float T = texture(u_wave_texture, UV + vec2(0.0, 1.0) * texelSize).r * h_mod;
		float B = texture(u_wave_texture, UV + vec2(0.0, -1.0) * texelSize).r * h_mod;
		NORMAL = normalize(vec3(2.0 * (R-L), 2.0 * -(B-T), 4.0));
	}	
	
	ALBEDO = u_water_col.rgb;
}