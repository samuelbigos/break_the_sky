shader_type canvas_item;

uniform vec4 u_connection1;

void fragment()
{
	vec2 p1 = vec2(0.1, 0.1);
	vec2 p2 = vec2(0.9, 0.9);
	
	vec2 c = normalize(p2 - p1);
	c = vec2(c.y, -c.x);
	float dist = dot(UV - p1, c);
	float n1 = dot(UV - p1, normalize(p2 - p1));
	float n2 = dot(p2 - UV, normalize(p1 - p2));
	
	float a = step(abs(dist), 0.01);
	a *= step(0.0, n1);
	a *= step(0.0, -n2);
	COLOR = vec4(1.0, 1.0, 1.0, a);
}