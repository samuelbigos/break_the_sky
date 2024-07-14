using System;
using Godot;

public partial class Bullet : Area3D
{
	[Export] private float _baseSpeed = 150.0f;
	[Export] protected float _range = 500.0f;

	[Export] private MultiViewportMeshInstance _mesh;
	
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
		get { return new Vector2(GlobalTransform.Origin.X, GlobalTransform.Origin.Z); }
		set { GlobalTransform = new Transform3D(GlobalTransform.Basis, value.To3D()); }
	}
	
	public virtual void Init(Vector3 position, BoidBase target, bool leadTarget, float speed, float damage, BoidBase.BoidAlignment alignment)
	{
		Vector2 targetPos = target.GlobalPosition;
		if (leadTarget)
		{
			Vector2 toTarget = targetPos - position.To2D();
			float dist = toTarget.Length();
			float t = dist / speed;
			targetPos += target.Velocity * t;
		}
		_damage = damage;
		_velocity = (targetPos - position.To2D()).Normalized() * speed;
		_alignment = alignment;
		GlobalPosition = position.To2D();
		_spawnPos = position.To2D();
		
		_mesh.Transform = new Transform3D(_mesh.Transform.Basis, _mesh.Transform.Origin + Vector3.Up * position.Y);
		_mesh.SetMeshTransform(_mesh.Transform);
		
		Rotation = new Vector3(0.0f, -Mathf.Atan2(_velocity.X, -_velocity.Y), 0.0f);

		if (!IsConnected("area_entered", new Callable(this, nameof(_OnAreaEntered))))
		{
			Connect("area_entered", new Callable(this, nameof(_OnAreaEntered)));
		}
		
		_mesh.AltShaders[0].SetShaderParameter("u_outline_colour", _alignment == BoidBase.BoidAlignment.Ally ? 
			ColourManager.Instance.AllyOutline : ColourManager.Instance.EnemyOutline);
	}
	
	public override void _Process(double delta)
	{  
		GlobalTranslate(_velocity.To3D() * (float) delta);
		ProcessOutOfBounds();
	}

	protected virtual void ProcessOutOfBounds()
	{
		if((_spawnPos - GlobalPosition).LengthSquared() > Mathf.Pow(_range, 2.0f))
		{
			QueueFree();
		}
	}
	
	public virtual void _OnAreaEntered(Area3D area)
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