
using System;
using Godot;
using Dictionary = Godot.Collections.Dictionary;
using Array = Godot.Collections.Array;


public class Bullet : Area2D
{

	public Vector2 _velocity;
	public int Alignment;
	public float _playRadius;
	private float _length = 6.0f;
	public float Damage = 1.0f;
	public bool _microbullet = false;
	
	public float GetAlignment()
	{
		return Alignment;

	}
	
	public override void _Ready()
	{  
		
	}
	
	public void Init(Vector2 velocity, int alignment, float playRadius, float damage)
	{
		Damage = damage;
		_velocity = velocity;
		Alignment = alignment;
		_playRadius = playRadius;
		//GetNode("CollisionShape2D").Radius =  _damage * 2.0f + 1.0f;

		Rotation = -Mathf.Atan2(_velocity.x, _velocity.y);

	}
	
	public override void _Process(float delta)
	{  
		GlobalPosition += _velocity * delta	;
		if(GlobalPosition.Length() + _length > _playRadius)
		{
			QueueFree();
	
		}
	}
	
	public override void _Draw()
	{  
		// outer
		float length = _length + Damage * 2.0f + 4.0f;
		float width = Damage * 2.0f + 4.0f;
		DrawLine(new Vector2(0.0f, -length * 0.5f + 2.0f), new Vector2(0.0f, length * 0.5f), ColourManager.Instance.Secondary, width)	;
		// inner
		length = _length + Damage * 2.0f;
		width = Damage * 2.0f;
		DrawLine(new Vector2(0.0f, -length * 0.5f), new Vector2(0.0f, length * 0.5f), ColourManager.Instance.White, width);
	
	
	}
	
	
	
}