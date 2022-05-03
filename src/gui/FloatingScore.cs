using Godot;

public class FloatingScore : Label
{
	public float lifetime = 1.5f;
	public Vector2 worldPos;
	
	public void SetScore(int score)
	{  
		Text = $"{score}";
	}
	
	public override void _Ready()
	{  
		AddColorOverride("font_color", ColourManager.Instance.Secondary);
	}
	
	public override void _Process(float delta)
	{  
		Visible = true;
		RectGlobalPosition = worldPos - new Vector2(0.0f, 20.0f);
		lifetime -= delta;
		if(lifetime < 0.0f)
		{
			QueueFree();
		}
	}
}