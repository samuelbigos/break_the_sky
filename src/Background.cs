using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class Background : Node2D
{
	[Export] public List<Texture> Clouds = new List<Texture>();
	[Export] public int CloudCount = 200;
	[Export] public float CloudSpeed = 50.0f;
	[Export] public float Radius = 1000.0f;
	[Export] public Vector2 Centre = new Vector2(0.0f, 0.0f);
	[Export] public int HighCloudZ = 2;

	private OpenSimplexNoise _noise = new OpenSimplexNoise();
	private List<Sprite> _farClouds = new List<Sprite>();
	private List<float> _farCloudSpeeds = new List<float>();
	private List<Sprite> _closeClouds = new List<Sprite>();
	private List<float> _closeCloudSpeeds = new List<float>();
	
	public override void _Ready()
	{  
		_noise.Seed = (int) GD.Randi();
		_noise.Octaves = 4;
		_noise.Period = 20.0f;
		_noise.Persistence = 0.8f;
		
		// clouds
		double bounds = Radius + GetViewport().Size.x * 0.5;
		foreach(int i in GD.Range(0, CloudCount))
		{
			Sprite cloud = new Sprite();
			cloud.Texture = Clouds[(int) (GD.Randi() % Clouds.Count)];
			cloud.Position = new Vector2((float) GD.RandRange(-bounds, bounds), (float) GD.RandRange(-bounds, bounds));
			//cloud.rotation = GD.RandRange(0.0, Mathf.Pi * 2.0);
			if(GD.Randi() % 3 == 1)
			{
				cloud.Modulate = ColourManager.Instance.White;
				float s = (float) GD.RandRange(0.5, 0.75);
				cloud.Scale = new Vector2(s, s);
				cloud.ZAsRelative = false;
				cloud.ZIndex = HighCloudZ;
				_closeClouds.Append(cloud);
				_closeCloudSpeeds.Append((float) GD.RandRange(CloudSpeed - CloudSpeed * 0.25f, CloudSpeed + CloudSpeed * 0.25f));
			}
			else
			{
				cloud.Modulate = ColourManager.Instance.Tertiary;
				float s = (float) GD.RandRange(1.0f, 1.5f);
				cloud.Scale = new Vector2(s, s);
				cloud.ZAsRelative = false;
				cloud.ZIndex = -1;
				_farClouds.Append(cloud);
				_farCloudSpeeds.Append((float) GD.RandRange(CloudSpeed - CloudSpeed * 0.25f, CloudSpeed + CloudSpeed * 0.25f) * 0.66f);
				
			}
			AddChild(cloud);
			
		}
	}
	
	public override void _Process(float delta)
	{  
		foreach(int i in GD.Range(0, _farClouds.Count))
		{
			Sprite cloud = _farClouds[i];
			Vector2 cloudPosition = cloud.Position;
			cloudPosition.y += _farCloudSpeeds[i] * 0.5f * delta;
			cloud.Position = cloudPosition;
			_Reposition(cloud);
			
		}
		foreach(int i in GD.Range(0, _closeClouds.Count))
		{
			Sprite cloud = _closeClouds[i];
			Vector2 cloudPosition = cloud.Position;
			cloudPosition.y += _closeCloudSpeeds[i] * delta;
			cloud.Position = cloudPosition;
			_Reposition(cloud);
			
		}
	}
	
	public void _Reposition(Sprite cloud)
	{  
		if(cloud.GlobalPosition.y > Radius + GetViewport().Size.y * 0.5)
		{
			Vector2 pos = cloud.GlobalPosition;
			pos.y = -Radius - GetViewport().Size.y * 0.5f;
			var x = (Radius + GetViewport().Size.x * 0.5);
			pos.x = (float) GD.RandRange(-x, x);
			cloud.GlobalPosition = pos;
		}
	}
	
	public override void _Draw()
	{  
		float bounds = Radius * 2.0f + GetViewport().Size.x * 2.0f;
		DrawArc(Centre, Radius, 0.0f, 360.0f, ColourManager.Instance.Secondary, 3.0f, 128);
	}
	
	public void DrawArc(Vector2 center, float radius, float angleTo, float angleFrom, Color color, float thickness, int segments)
	{  
		var pointNum = segments;
		List<Vector2> points = new List<Vector2>();
		foreach(var i in GD.Range(pointNum + 1))
		{
			var angle = Mathf.Deg2Rad(angleFrom + i * (angleTo - angleFrom) / pointNum - 90);
			points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
		}

		DrawPolyline(points.ToArray(), color, thickness);
	}
}