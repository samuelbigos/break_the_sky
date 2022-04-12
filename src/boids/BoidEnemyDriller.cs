using Godot;

public class BoidEnemyDriller : BoidEnemyBase
{
    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(float delta)
    {
        // update trail
        _sprite.Rotation = -Mathf.Atan2(_velocity.x, _velocity.y);
    }

    protected override void Destroy(bool score)
    {
        base.Destroy(score);
        _sfxDestroy.Play();
    }
}