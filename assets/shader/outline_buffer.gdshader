shader_type spatial;
render_mode unshaded;

uniform vec4 u_outline_colour : source_color;

void vertex() 
{
}

void fragment() 
{
	ALBEDO = u_outline_colour.rgb;
}