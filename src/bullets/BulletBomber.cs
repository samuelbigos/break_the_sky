public class BulletBomber : Bullet
{
    public BoidBase Target;
    
    public override void _Process(float delta)
    {
        base._Process(delta);

        _velocity = (Target.GlobalPosition - GlobalPosition).Normalized() * _velocity.Length();
    }
}