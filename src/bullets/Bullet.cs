using Godot;


public class Bullet : Area
{
	private Vector2 _velocity;
	private BoidBase.BoidAlignment _alignment;
	private float _playRadius;
	private float _length = 6.0f;
	private float _damage = 1.0f;
	protected bool _microbullet = false;

	public BoidBase.BoidAlignment Alignment => _alignment;
	public Vector2 Velocity => _velocity;
	public bool Microbullet => _microbullet;
	public float Damage => _damage;
	
	public Vector2 GlobalPosition
	{
		get { return new Vector2(GlobalTransform.origin.x, GlobalTransform.origin.z); }
		set { GlobalTransform = new Transform(GlobalTransform.basis, value.To3D()); }
	}
	
	public void Init(Vector2 velocity, BoidBase.BoidAlignment alignment, float playRadius, float damage)
	{
		_damage = damage;
		_velocity = velocity;
		_alignment = alignment;
		_playRadius = playRadius;

		Rotation = new Vector3(0.0f, -Mathf.Atan2(_velocity.x, -_velocity.y), 0.0f);
	}
	
	public override void _Process(float delta)
	{  
		GlobalTranslate(_velocity.To3D() * delta);
		if(GlobalPosition.Length() + _length > _playRadius)
		{
			QueueFree();
		}
	}

	public void OnHit()
	{
		QueueFree();
	}
}