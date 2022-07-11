shader_type spatial;
//render_mode unshaded;

uniform vec4 u_primary_colour : hint_color;
uniform vec4 u_secondary_colour : hint_color;

void fragment()
{
	ALBEDO = u_primary_colour.rgb;
	//ALBEDO = mix(u_primary_colour.rgb, u_secondary_colour.rgb, step(0.5, UV.x));
}