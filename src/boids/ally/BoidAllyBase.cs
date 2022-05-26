using System;
using Godot;

public class BoidAllyBase : BoidBase
{
    [Export] private float _destroyTime = 3.0f;
    [Export] private float _shootSize = 1.5f;
    [Export] private float _shootTrauma = 0.05f;
    [Export] private float _destroyTrauma = 0.1f;
    [Export] private float _microBulletCd = 1.0f;
    [Export] private float _microBulletRange = 400.0f;
    [Export] private float _microBulletDamageMod = 0.25f;
    
    [Export] public bool BlocksShots = true;
    
    [Export] private NodePath _sfxHitMicroPlayerNode;
    
    [Export] private NodePath _sfxShootPlayerPath;
    private AudioStreamPlayer2D _sfxShootPlayer;

    protected override BoidAlignment Alignment => BoidAlignment.Ally;
    protected override Color BaseColour => ColourManager.Instance.Secondary;

    public override void _Ready()
    {
        base._Ready();
        
        _baseScale = _mesh.Scale;

        _sfxShootPlayer = GetNode<AudioStreamPlayer2D>(_sfxShootPlayerPath);
    }

    protected virtual void _Shoot(Vector2 dir)
    {
        float traumaMod = 1.0f - Mathf.Clamp(_game.NumBoids / 100.0f, 0.0f, 0.5f);
        GameCamera.Instance.AddTrauma(_shootTrauma * traumaMod);
        
        _sfxShootPlayer.Play();
    }

    protected virtual bool _CanShoot(Vector2 dir)
    {
        if (_destroyed || !_acceptInput)
            return false;
        
        // can shoot if there are no other boids in the shoot direction
        bool blocked = false;
        foreach (BoidAllyBase boid in _game.AllyBoids)
        {
            if (boid == this || boid.Destroyed || !boid.BlocksShots)
                continue;

            if ((boid.GlobalPosition - GlobalPosition).Normalized().Dot(dir.Normalized()) < 0.9f)
                continue;
            
            blocked = true;
            break;
        }

        return !blocked;
    }
}