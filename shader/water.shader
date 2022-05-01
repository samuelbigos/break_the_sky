shader_type spatial;

uniform sampler2D u_texture_to_draw : hint_black;

void vertex() 
{
}

void fragment() 
{
    vec4 sample = texture(u_texture_to_draw, UV);
	ALBEDO = sample.rgb;
}