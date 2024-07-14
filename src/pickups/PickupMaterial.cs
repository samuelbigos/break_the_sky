using Godot;
using System;
using System.Diagnostics;

public partial class PickupMaterial : Node3D
{
    [Export] private NodePath _meshPath;
    [Export] private float _bounceDelta = 5.0f;
    [Export] private float _damping = 0.05f;
    [Export] private float _attractionRadius = 50.0f;
    [Export] private float _collectionRadius = 10.0f;

    public Action<PickupMaterial> OnCollected;
     
    private MeshInstance3D _mesh;
    private StandardMaterial3D _mat;
    private float _time;
    private Vector2 _velocity;
    private BoidBase _target;

    public void Init(Vector2 velocity, BoidBase target)
    {
        _velocity = velocity;
        _target = target;
    }

    public override void _Ready()
    {
        base._Ready();

        _mesh = GetNode<MeshInstance3D>(_meshPath);
        _mat = _mesh.GetSurfaceOverrideMaterial(0) as StandardMaterial3D;
        Debug.Assert(_mat != null);
        
        _attractionRadius *= _attractionRadius;
        _collectionRadius *= _collectionRadius;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        _time += (float)delta * 2.0f;
        Vector3 pos = GlobalTransform.Origin;
        
        float dist = (_target.GlobalTransform.Origin - GlobalTransform.Origin).LengthSquared();
        if (dist < _attractionRadius)
        {
            if (dist < _collectionRadius)
            {
                OnCollected?.Invoke(this);
                QueueFree();
            }
            pos += (_target.GlobalTransform.Origin - GlobalTransform.Origin).Normalized() *
                   (1.0f - (dist / _attractionRadius)) * (float)delta * 100.0f;
        }
        
        pos.Y = Mathf.Sin(_time) * _bounceDelta;
        pos += _velocity.To3D() * (float)delta;
        
        GlobalTransform = new Transform3D(GlobalTransform.Basis, pos);
        //float scale =  (Mathf.Sin(_time) + 1.0f) * 0.25f + 1.0f;
        //_mesh.Scale = Vector3.One * scale;
        //RotateY(delta * 2.0f);
        
        _velocity *= Mathf.Pow(1.0f - Mathf.Clamp(_damping, 0.0f, 1.0f), (float)delta * 60.0f);
    }
}
