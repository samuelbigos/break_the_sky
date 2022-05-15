shader_type spatial;
render_mode unshaded, world_vertex_coords;

uniform sampler2D u_cloud_tex;

void fragment()
{
	// map texture to world space
	vec4 col = texture(u_cloud_tex, SCREEN_UV);
	ALBEDO = col.rgb;
	ALPHA = col.a;
}