using Godot;
using System;
using System.Diagnostics;

public class PickupMaterial : Spatial
{
    [Export] private NodePath _meshPath;
    [Export] private NodePath _meshOutsidePath;
    [Export] private float _bounceDelta = 5.0f;
    [Export] private float _damping = 0.05f;
    [Export] private float _attractionRadius = 50.0f;
    [Export] private float _collectionRadius = 10.0f;

    public Action<PickupMaterial> OnCollected;
     
    private MeshInstance _mesh;
    private SpatialMaterial _mat;
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

        _mesh = GetNode<MeshInstance>(_meshPath);
        _mat = _mesh.GetSurfaceMaterial(0) as SpatialMaterial;
        Debug.Assert(_mat != null);
        
        _attractionRadius *= _attractionRadius;
        _collectionRadius *= _collectionRadius;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        _time += delta * 2.0f;
        Vector3 pos = GlobalTransform.origin;
        
        float dist = (_target.GlobalTransform.origin - GlobalTransform.origin).LengthSquared();
        if (dist < _attractionRadius)
        {
            if (dist < _collectionRadius)
            {
                OnCollected?.Invoke(this);
                QueueFree();
            }
            pos += (_target.GlobalTransform.origin - GlobalTransform.origin).Normalized() *
                   (1.0f - (dist / _attractionRadius)) * delta * 100.0f;
        }
        
        pos.y = Mathf.Sin(_time) * _bounceDelta;
        pos += _velocity.To3D() * delta;
        
        GlobalTransform = new Transform(GlobalTransform.basis, pos);
        float scale =  (Mathf.Sin(_time) + 1.0f) * 0.25f + 1.0f;
        _mesh.Scale = Vector3.One * scale;
        
        _velocity *= Mathf.Pow(1.0f - Mathf.Clamp(_damping, 0.0f, 1.0f), delta * 60.0f);
    }
}
