using Godot;

public class ColourManager : Node
{
	public static ColourManager Instance;
	public override void _Ready()
	{
		Instance = this;
	}
	[Export] public Color Primary;
	[Export] public Color Secondary;
	[Export] public Color Tertiary;
	[Export] public Color White;
	[Export] public Color Accent;
}