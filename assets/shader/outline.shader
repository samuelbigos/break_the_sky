shader_type spatial;
render_mode unshaded, depth_test_disable;

uniform sampler2D u_outlineBuffer;
uniform vec4 u_edgeColor : hint_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform int u_width;
uniform float u_aa;

void vertex()
{
	VERTEX *= 2.0;
	POSITION = vec4(VERTEX, 1.0);
}

float sampleBuffer(sampler2D sampler, vec2 uv)
{
	return texture(sampler, uv).r;
}

float remap(float x, float a, float b, float c, float d)
{
    return (((x - a) / (b - a)) * (d - c)) + c;
}

void fragment()
{
	vec2 uv = SCREEN_UV;
	vec2 delta = 1.0 / VIEWPORT_SIZE * 1.0;
	
	float pixel = sampleBuffer(u_outlineBuffer, uv);
	
	float minVal = pixel;
	float maxVal = pixel;
	int size = u_width;
	float sum = 0.0;
	float count = 0.0;
	for (int x = -size; x <= size; x++)
	{
		for (int y = -size; y <= size; y++)
		{
			if (x == 0 && y == 0)
				continue;
				
			float sample = sampleBuffer(u_outlineBuffer, uv + vec2(delta.x * float(x), delta.y * float(y)));
			minVal = min(minVal, sample);
			maxVal = max(maxVal, sample);
			sum += sample;
			count += 1.0;
		}
	}
	sum /= count;
	
	sum = remap(sum, 0.1, mix(0.1, 1.0, u_aa), 0.0, 1.0);
	float diff = smoothstep(minVal, maxVal, sum);
	diff = clamp(diff, 0.0, 1.0);
	
	ALBEDO = u_edgeColor.rgb;
	ALPHA = min(diff, 1.0 - pixel);
}