shader_type spatial;
render_mode unshaded;

uniform vec4 u_primary_colour : hint_color;
uniform vec4 u_secondary_colour : hint_color;

void fragment()
{	
	ALBEDO = u_primary_colour.rgb;
	
	// rim highlight
//	if (dot(vec3(0.0, 1.0, 0.0), NORMAL) > 0.5)
//	{
//		ALBEDO = vec3(1.0, 1.0, 1.0);
//	}
}