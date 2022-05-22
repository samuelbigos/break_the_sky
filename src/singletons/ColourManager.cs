using System.Diagnostics;
using Godot;

public class ColourManager : Node
{
	public static ColourManager Instance;

	public ColourManager()
	{
		Debug.Assert(Instance == null, "Attempting to create multiple ColourManager instances!");
		Instance = this;
	}
	
	[Export] public Color Primary;
	[Export] public Color Secondary;
	[Export] public Color Tertiary;
	[Export] public Color Four;
	[Export] public Color Accent;
	[Export] public Color Red;
	[Export] public Color White;
}