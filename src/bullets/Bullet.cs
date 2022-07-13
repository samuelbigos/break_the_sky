using Godot;

public class Bullet : Area
{
	[Export] private float _baseSpeed = 150.0f;
	[Export] private float _range = 2000.0f;

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
	
	public void Init(Vector2 position, Vector2 velocity, BoidBase.BoidAlignment alignment, float damage)
	{
		_damage = damage;
		_velocity = velocity;
		_alignment = alignment;
		GlobalPosition = position;
		_spawnPos = position;

		Rotation = new Vector3(0.0f, -Mathf.Atan2(_velocity.x, -_velocity.y), 0.0f);
		
		Connect("area_entered", this, nameof(_OnAreaEntered));
	}
	
	public override void _Process(float delta)
	{  
		GlobalTranslate(_velocity.To3D() * delta);
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
		}
	}

	public void OnHit()
	{
		QueueFree();
	}
}