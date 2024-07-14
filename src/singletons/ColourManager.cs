using System.Diagnostics;
using Godot;

public partial class ColourManager : Singleton<ColourManager>
{
	[Export] public Color AllyOutline;
	[Export] public Color EnemyOutline;
	
	[Export] public Color Primary;
	[Export] public Color Secondary;
	[Export] public Color Tertiary;
	[Export] public Color Four;
	[Export] public Color Accent;
	[Export] public Color Red;
	[Export] public Color Ally;
	[Export] public Color White;
}