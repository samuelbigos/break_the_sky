using System.Diagnostics;
using Godot;
using GodotOnReady.Attributes;

public partial class ColourManager : Node
{
	public static ColourManager Instance;

	[OnReadyGet] private SpatialMaterial _redMaterial;
	
	[OnReady] private void Ready()
	{
		Debug.Assert(Instance == null, "Attempting to create multiple ColourManager instances!");
		Instance = this;

		_redMaterial.AlbedoColor = Red;
	}
	
	[Export] public Color Primary;
	[Export] public Color Secondary;
	[Export] public Color Tertiary;
	[Export] public Color Four;
	[Export] public Color Accent;
	[Export] public Color Red;
	[Export] public Color White;
}