shader_type canvas_item;

uniform sampler2D u_prev_wave;

void fragment() 
{
	vec2 texSize;
	texSize.x = float(textureSize(TEXTURE, 0).x);
	texSize.y = float(textureSize(TEXTURE, 0).y);
	vec2 texelSize = 1.0 / texSize;
		
	vec2 uv = UV;
	vec4 samples_boid = texture(TEXTURE, uv);
	vec4 samples_prev = texture(u_prev_wave, uv);
	
	float val = max(samples_boid.a, samples_prev.r);
	
	COLOR = vec4(val, 0.0, 0.0, 1.0);
}