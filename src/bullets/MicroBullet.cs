using Godot;


public class MicroBullet : Bullet
{
	private readonly float _length = 5.0f;

	public override void _Ready()
	{
		base._Ready();

		_microbullet = true;
	}

	public override void _Draw()
	{  
		DrawLine(new Vector2(0.0f, -_length * 0.5f), new Vector2(0.0f, _length * 0.5f), ColourManager.Instance.Secondary, Damage * 8.0f);
	}
}