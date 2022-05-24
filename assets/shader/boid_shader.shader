shader_type spatial;
render_mode unshaded;

uniform vec4 u_primary_colour : hint_color;
uniform vec4 u_secondary_colour : hint_color;
uniform float threshold_depth = 0.0;
uniform float blend_depth = 0.01;

void fragment()
{	
	ALBEDO = u_primary_colour.rgb;
}