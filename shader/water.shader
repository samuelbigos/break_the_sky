shader_type spatial;

uniform vec4 some_color : hint_color;

void vertex() 
{
}

void fragment() 
{
    ALBEDO = some_color.rgb;
}