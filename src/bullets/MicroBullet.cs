
using System;
using Godot;
using Dictionary = Godot.Collections.Dictionary;
using Array = Godot.Collections.Array;


public class MicroBullet : Node2D
{
	public float Damage;
	public Vector2 _velocity;
	public int _alignment;
	public float _playRadius;
	
	private float _length = 5.0f;
	private bool _microbullet = true;
	
	public float GetAlignment()
	{
		return _alignment;

	}
	
	public override void _Ready()
	{  
		
	}
	
	public void Init(Vector2 velocity, int alignment, float playRadius)
	{  
		_velocity = velocity;
		_alignment = alignment;
		_playRadius = playRadius;
		//GetNode("CollisionShape2D").shape.radius =  _damage * 2.0 + 1.0;

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
		DrawLine(new Vector2(0.0f, -_length * 0.5f), new Vector2(0.0f, _length * 0.5f), ColourManager.Instance.Secondary, Damage * 8.0f);
	
	
	}
	
	
	
}