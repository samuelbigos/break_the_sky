public class BulletBomber : Bullet
{
    public BoidBase Target;
    
    public override void _Process(float delta)
    {
        base._Process(delta);

        if (IsInstanceValid(Target))
        {
            _velocity = (Target.GlobalPosition - GlobalPosition).Normalized() * _velocity.Length();
        }

        if (!IsInstanceValid(Target) || Target.Destroyed)
        {
            Target = null;
        }
    }
}