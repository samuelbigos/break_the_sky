using System;
using Godot;

public class BoidPlayer : BoidBase
{
    [Export] private NodePath _sfxPickupPath;

    private AudioStreamPlayer2D _sfxPickup;
    private Vector2 _velocity;

    public override void _Ready()
    {
        base._Ready();

        _sfxPickup = GetNode<AudioStreamPlayer2D>(_sfxPickupPath);

        _mesh.Visible = true;
    }

    protected override void RegisterSteeringBoid(Vector2 velocity)
    {
    }

    public override void _Process(float delta)
    {
        if (!_destroyed && _acceptInput)
        {
            Vector2 mousePos = GameCamera.Instance.MousePosition();
            Vector2 lookAt = mousePos - GlobalPosition;
            Rotation = new Vector3(0.0f, -Mathf.Atan2(lookAt.x, -lookAt.y), 0.0f);

            Vector2 forward = new Vector2(0.0f, -1.0f);
            Vector2 left = new Vector2(-1.0f, 0.0f);

            Vector2 dir = new Vector2(0.0f, 0.0f);
            if (Input.IsActionPressed("w"))
            {
                dir += forward;
            }

            if (Input.IsActionPressed("s"))
            {
                dir += -forward;
            }

            if (Input.IsActionPressed("a"))
            {
                dir += left;
            }

            if (Input.IsActionPressed("d"))
            {
                dir += -left;
            }

            if (dir != new Vector2(0.0f, 0.0f))
            {
                dir = dir.Normalized();
                dir *= MaxVelocity * delta;
                _velocity += dir;
            }
            
            GlobalTranslate(_velocity.To3D() * delta);

            _velocity *= Mathf.Pow(1.0f - Mathf.Clamp(Damping, 0.0f, 1.0f), delta * 60.0f);
        }
    }

    public void RegisterPickup(PickupMaterial pickup)
    {
        pickup.OnCollected += _OnPickupCollected;
    }

    public override void _OnBoidAreaEntered(Area area)
    {
    }

    private void _OnPickupCollected(PickupMaterial pickup)
    {
        SaveDataPlayer.MaterialCount += 1;
        _sfxPickup.Play();
    }
}