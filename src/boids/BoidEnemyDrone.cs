using Godot;

public class BoidEnemyDrone : BoidEnemyBase
{
    public override void _Ready()
    {
    }

    public override void _Process(float delta)
    {
        // update trail
        _sprite.Rotation = -Mathf.Atan2(_velocity.x, _velocity.y);
    }
}