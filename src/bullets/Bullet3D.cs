
using System;
using Godot;
using Dictionary = Godot.Collections.Dictionary;
using Array = Godot.Collections.Array;


public class Bullet : Area2D
{
	private Vector2 _velocity;
	private int _alignment;
	private float _playRadius;
	private float _length = 6.0f;
	private float _damage = 1.0f;
	protected bool _microbullet = false;

	public float Alignment => _alignment;
	public Vector2 Velocity => _velocity;
	public bool Microbullet => _microbullet;
	public float Damage => _damage;
	
	public void Init(Vector2 velocity, int alignment, float playRadius, float damage)
	{
		_damage = damage;
		_velocity = velocity;
		_alignment = alignment;
		_playRadius = playRadius;

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

	public void OnHit()
	{
		QueueFree();
	}
	
	public override void _Draw()
	{  
		// outer
		float length = _length + _damage * 2.0f + 4.0f;
		float width = _damage * 2.0f + 4.0f;
		DrawLine(new Vector2(0.0f, -length * 0.5f + 2.0f), new Vector2(0.0f, length * 0.5f), ColourManager.Instance.Secondary, width)	;
		// inner
		length = _length + _damage * 2.0f;
		width = _damage * 2.0f;
		DrawLine(new Vector2(0.0f, -length * 0.5f), new Vector2(0.0f, length * 0.5f), ColourManager.Instance.White, width);
	}
}