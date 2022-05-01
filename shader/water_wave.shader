shader_type canvas_item;

void fragment() 
{
    vec4 sample = texture(TEXTURE, UV);
	return sample.rgb;
}