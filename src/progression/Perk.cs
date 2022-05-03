using Godot;

public class Perk : Node
{
	[Export] public string id = "";
	[Export] public bool enabled = true;
	[Export] public string displayName = "";
	[Export] public string displayDesc = "";
	[Export] public int maximum = 999;
	[Export] public float reloadMod = 1.0f;
	[Export] public int reinforceMod = 0;
	[Export] public float groupingMod = 0.0f;
	[Export] public float damageMod = 0.0f;
	[Export] public float slowmoMod = 0.0f;
	[Export] public float nukeMod = 0.0f;
	[Export] public float boidSpeedMod = 0.0f;
	[Export] public float playerSpeedMod = 0.0f;
	[Export] public float spreadMod = 1.0f;
	[Export] public float bulletSpeedMod = 0.0f;
	[Export] public bool microturrets = false;
}