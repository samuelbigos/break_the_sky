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

vec4 sampleBuffer(sampler2D sampler, vec2 uv)
{
	return texture(sampler, uv);
}

float remap(float x, float a, float b, float c, float d)
{
    return (((x - a) / (b - a)) * (d - c)) + c;
}

void fragment()
{
	vec2 uv = SCREEN_UV;
	vec2 delta = 1.0 / VIEWPORT_SIZE * 1.0;
	
	vec4 pixelBuffer = sampleBuffer(u_outlineBuffer, uv);
	float pixel = pixelBuffer.a;
	vec3 colour;
	
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
				
			vec4 sample = sampleBuffer(u_outlineBuffer, uv + vec2(delta.x * float(x), delta.y * float(y)));
			
			minVal = min(minVal, sample.a);
			maxVal = max(maxVal, sample.a);
			if (sample.a > 0.5) colour = sample.rgb;
			sum += sample.a;
			count += 1.0;
		}
	}
	sum /= count;
	
	sum = remap(sum, 0.1, mix(0.1, 1.0, u_aa), 0.0, 1.0);
	float diff = smoothstep(minVal, maxVal, sum);
	diff = clamp(diff, 0.0, 1.0);
	
	ALBEDO = colour;
	ALPHA = min(diff, 1.0 - pixel);
}