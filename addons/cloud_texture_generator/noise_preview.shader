shader_type canvas_item;

uniform int u_mode;
uniform int u_channel;
uniform sampler3D u_noise3d;
uniform sampler2D u_noise2d;
uniform int u_slice;

void fragment()
{
	vec3 col = vec3(0.0);
	if (u_mode == 2)
	{
		ivec2 texSize = textureSize(u_noise2d, 0);
		vec4 noise = texture(u_noise2d, UV);
		if (u_channel == 0)
			col = vec3(noise.r, 0.0, 0.0);
		if (u_channel == 1)
			col = vec3(0.0, noise.g, 0.0);
		if (u_channel == 2)
			col = vec3(0.0, 0.0, noise.b);
		if (u_channel == 3)
			col = vec3(noise.a);
	}
	else if (u_mode == 3)
	{
		ivec3 texSize = textureSize(u_noise3d, 0);
		vec4 noise = texture(u_noise3d, vec3(UV.x, UV.y, float(u_slice) / float(texSize.z)));
		if (u_channel == 0)
			col = vec3(noise.r, 0.0, 0.0);
		if (u_channel == 1)
			col = vec3(0.0, noise.g, 0.0);
		if (u_channel == 2)
			col = vec3(0.0, 0.0, noise.b);
		if (u_channel == 3)
			col = vec3(noise.a);
	}
	
	COLOR = vec4(col, 1.0);
}