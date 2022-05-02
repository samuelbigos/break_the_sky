using Godot;


public class MicroBullet : Bullet3D
{
	private readonly float _length = 5.0f;

	public override void _Ready()
	{
		base._Ready();

		_microbullet = true;
	}
}