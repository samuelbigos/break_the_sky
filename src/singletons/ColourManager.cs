using System.Diagnostics;
using Godot;
using GodotOnReady.Attributes;

public partial class ColourManager : Singleton<ColourManager>
{
	[OnReadyGet] private SpatialMaterial _redMaterial;
	
	[OnReady] private void Ready()
	{
		_redMaterial.AlbedoColor = Red;
	}
	
	[Export] public Color Primary;
	[Export] public Color Secondary;
	[Export] public Color Tertiary;
	[Export] public Color Four;
	[Export] public Color Accent;
	[Export] public Color Red;
	[Export] public Color Ally;
	[Export] public Color White;
}