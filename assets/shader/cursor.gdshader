shader_type spatial;
render_mode unshaded;

uniform int u_count;

void fragment()
{
	ALBEDO = vec3(1.0);
	ALPHA = 1.0 - step(float(u_count), UV.x * 512.0);
}