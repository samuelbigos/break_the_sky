using System;
using Godot;
using GodotOnReady.Attributes;

public partial class Bullet : Area
{
	[Export] private float _baseSpeed = 150.0f;
	[Export] protected float _range = 500.0f;

	[OnReadyGet] private MultiViewportMeshInstance _mesh;
	
	public Action<Bullet> OnBulletDestroyed;

	protected Vector2 _velocity;
	private BoidBase.BoidAlignment _alignment;
	private float _length = 6.0f;
	private float _damage = 1.0f;
	private Vector2 _spawnPos;

	public BoidBase.BoidAlignment Alignment => _alignment;
	public Vector2 Velocity => _velocity;
	public float Damage => _damage;
	
	public Vector2 GlobalPosition
	{
		get { return new Vector2(GlobalTransform.origin.x, GlobalTransform.origin.z); }
		set { GlobalTransform = new Transform(GlobalTransform.basis, value.To3D()); }
	}
	
	public void Init(Vector3 position, Vector2 velocity, BoidBase.BoidAlignment alignment, float damage)
	{
		_damage = damage;
		_velocity = velocity;
		_alignment = alignment;
		GlobalPosition = position.To2D();
		_spawnPos = position.To2D();
		
		_mesh.Transform = new Transform(_mesh.Transform.basis, _mesh.Transform.origin + Vector3.Up * position.y);

		Rotation = new Vector3(0.0f, -Mathf.Atan2(_velocity.x, -_velocity.y), 0.0f);

		if (!IsConnected("area_entered", this, nameof(_OnAreaEntered)))
		{
			Connect("area_entered", this, nameof(_OnAreaEntered));
		}
		
		_mesh.AltShaders[0].SetShaderParam("u_outline_colour", _alignment == BoidBase.BoidAlignment.Ally ? 
			ColourManager.Instance.AllyOutline : ColourManager.Instance.EnemyOutline);
	}
	
	public override void _Process(float delta)
	{  
		GlobalTranslate(_velocity.To3D() * delta);
		ProcessOutOfBounds();
	}

	protected virtual void ProcessOutOfBounds()
	{
		if((_spawnPos - GlobalPosition).LengthSquared() > Mathf.Pow(_range, 2.0f))
		{
			QueueFree();
		}
	}
	
	public virtual void _OnAreaEntered(Area area)
	{
		if (area is BoidBase boid && boid.Alignment != _alignment)
		{
			boid.SendHitMessage(_damage, _velocity, GlobalPosition, _alignment);
			OnHit();
		}
	}

	private void OnHit()
	{
		OnBulletDestroyed?.Invoke(this);
		QueueFree();
	}
}