shader_type spatial;
render_mode unshaded;

uniform vec3 u_velocity;

void vertex() 
{	
}

void fragment() 
{
	vec2 vel = u_velocity.xz / 100.0;
	vec2 dir = vel * 0.5 + vec2(0.5, 0.5);
	ALBEDO = vec3(clamp(dir.r, 0.0, 1.0), clamp(dir.g, 0.0, 1.0), 0.0);
}