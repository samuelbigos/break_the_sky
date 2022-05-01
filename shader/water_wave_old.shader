shader_type canvas_item;

uniform sampler2D u_prev_wave;

void fragment() 
{
    vec4 boid_velocity = texture(TEXTURE, UV) * 2.0 - 1.0;
	vec4 prev_wave = texture(u_prev_wave, UV);
	
	boid_velocity.rgb += prev_wave.rgb;
	
	float outline = 1.0f;
	float texelSize = 1.0 / 1024.0;
	
	vec2 dirs[] = { 
		normalize(vec2(-1.0, -1.0)), 
		normalize(vec2(1.0, -1.0)), 
		normalize(vec2(-1.0, 1.0)), 
		normalize(vec2(1.0, 1.0))};
	
	float diffs[4] = float[4];
	for (int i = 0; i < 4; i++)
	{
		vec2 uv = UV + dirs[i] * texelSize + normalize(boid_velocity.rg) * texelSize * 10.0;
		vec4 sample = texture(TEXTURE, uv);
		diffs[i] = sample.a;
	}

    float diff = abs(diffs[0] - diffs[3]) + abs(diffs[1] - diffs[2]);
    diff = clamp(diff * 0.5, 0.0, 1.0);
	vec3 col = ((boid_velocity.rgb * 0.5 + 0.5) * diff);
   	COLOR = vec4(col.r, col.g, col.b, boid_velocity.a);
}