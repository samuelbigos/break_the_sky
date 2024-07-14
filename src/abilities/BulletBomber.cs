using Godot;

public partial class BulletBomber : Bullet
{
    public BoidBase Parent;
    public BoidBase Target;

    private Vector2 _cachedParentPos;

    public override void Init(Vector3 position, BoidBase target, bool leadTarget, float speed, float damage, BoidBase.BoidAlignment alignment)
    {
        base.Init(position, target, leadTarget, speed, damage, alignment);

        Target = target;
    }

    public override void _Process(double delta)
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

    protected override void ProcessOutOfBounds()
    {
        if (IsInstanceValid(Parent))
        {
            _cachedParentPos = Parent.GlobalPosition;
        }
        if((_cachedParentPos - GlobalPosition).LengthSquared() > Mathf.Pow(_range, 2.0f))
        {
            QueueFree();
        }
    }
}