shader_type canvas_item;

uniform sampler2D u_prev_wave;

void fragment() 
{
	vec2 texSize;
	texSize.x = float(textureSize(TEXTURE, 0).x);
	texSize.y = float(textureSize(TEXTURE, 0).y);
	vec2 texelSize = 1.0 / texSize;
	
	vec2 dirs[] = {
		normalize(vec2(-1.0, -1.0)), 
		normalize(vec2(1.0, -1.0)), 
		normalize(vec2(-1.0, 1.0)), 
		normalize(vec2(1.0, 1.0)),
		vec2(1.0, 0.0),
		vec2(-1.0, 0.0),
		vec2(0.0, 1.0),
		vec2(0.0, -1.0),
		vec2(0.0, 0.0)};
		
	vec4 samples_boid[9] = vec4[9];
	vec4 samples_prev[9] = vec4[9];
	for (int i = 0; i < 9; i++)
	{
		vec2 uv = UV + dirs[i] * texelSize;
		samples_boid[i] = texture(TEXTURE, uv);
		samples_prev[i] = texture(u_prev_wave, uv);
	}

    float diff = abs(samples_prev[0].r - samples_prev[3].r) 
		+ abs(samples_prev[1].r - samples_prev[2].r) 
		+ abs(samples_prev[4].r - samples_prev[5].r) 
		+ abs(samples_prev[6].r - samples_prev[7].r);

	diff = 1.0 - clamp(diff / 4.0, 0.0, 1.0);
	
	float falloff = (0.1 * diff);
	float val = 0.0;
	for (int i = 0; i < 9; i++)
	{
		val = max(val, max(samples_boid[i].a, samples_prev[i].r - falloff));
	}
	
	COLOR = vec4(val, 0.0, 0.0, 1.0);
}