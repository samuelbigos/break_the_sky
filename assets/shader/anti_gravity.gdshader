shader_type spatial;
render_mode unshaded;

uniform vec4 u_colour : source_color;

void fragment()
{
	float dist_from_cente = UV.x + TIME * 1.0;
	float val = sin(dist_from_cente * 10.0);
	ALBEDO = u_colour.rgb * val;
	ALPHA = clamp(val, 0.0, 1.0);
}