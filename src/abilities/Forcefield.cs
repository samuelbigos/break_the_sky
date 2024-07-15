using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;

public partial class Forcefield : Area3D
{
    private struct ForceFieldHit
    {
        public Vector3 Pos;
        public double Time;
    }
    
    [Export] private int _maxForcefieldHits = 10;
    [Export] private float _forcefieldHitDuration = 0.5f;

    [Export] private MeshInstance3D _mesh;
    [Export] private Camera3D _testCamera;
    
    private ShaderMaterial _forcefieldMat;
    private List<ForceFieldHit> _forcefieldHits = new();
    private BoidBase.BoidAlignment _alignment;

    private float _maxHealth;
    private float _health;
    private BoidBase _parentBoid;
    private double _activeTimer;
    private double _destroyTimer;
    private double _regenTimer;

    public override void _Ready()
    {
        Connect("area_entered", new Callable(this, nameof(_OnForceFieldAreaEntered)));
        _forcefieldMat = _mesh.MaterialOverride as ShaderMaterial;
        
        Init(BoidBase.BoidAlignment.Ally, null, 10.0f, 10.0f);
    }

    public void Init(BoidBase.BoidAlignment alignment, BoidBase parent, float radius, float health)
    {
        _alignment = alignment;
        Scale = Vector3.One * radius;
        _forcefieldMat.SetShaderParameter($"u_radius", radius);
        _parentBoid = parent;
        _maxHealth = health;
        
        Activate(_maxHealth);
    }

    public void Activate(float health)
    {
        _activeTimer = 0.0f;
        _destroyTimer = 0.0f;
        _health = health;
        
        _forcefieldMat.SetShaderParameter($"u_destroy_timer", _destroyTimer);
    }

    private void Deactivate()
    {
        _regenTimer = 5.0f;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        // keep rotation static
        Vector3 scale = Scale;
        GlobalTransform = new Transform3D(Basis.Identity, GlobalTransform.Origin);
        Scale = scale;

        if (_health <= 0.0f)
        {
            _destroyTimer += delta;
            _forcefieldMat.SetShaderParameter($"u_destroy_timer", _destroyTimer);

            _regenTimer -= delta;
            if (_regenTimer < 0.0f)
            {
                Activate(_maxHealth);
            }
        }

        int numAddedInShader = 0;
        for (int i = _forcefieldHits.Count - 1; i >= 0; i--)
        {
            ForceFieldHit hit = _forcefieldHits[i];
            hit.Time += delta;
            if (hit.Time > _forcefieldHitDuration)
            {
                _forcefieldHits.RemoveAt(i);
                continue;
            }
            _forcefieldHits[i] = hit;

            if (numAddedInShader < _maxForcefieldHits)
            {
                Rect2 hitData = new Rect2(hit.Pos.X, hit.Pos.Y, hit.Pos.Z, (float) hit.Time);
                _forcefieldMat.SetShaderParameter($"u_hit_{numAddedInShader + 1}", hitData);
                numAddedInShader++;
            }
            _forcefieldMat.SetShaderParameter($"u_hits", numAddedInShader);
        }

        _activeTimer += delta;
        
        _forcefieldMat.SetShaderParameter($"u_active_timer", _activeTimer);
        if (_parentBoid != null)
            _forcefieldMat.SetShaderParameter($"u_heading", _parentBoid.Heading.To3D());
        _forcefieldMat.SetShaderParameter("u_centre", GlobalTransform.Origin);
    }

    // public override void _PhysicsProcess(float delta)
    // {
    //     base._PhysicsProcess(delta);
    //
    //     if (Input.IsActionJustPressed("shoot"))
    //     {
    //         PhysicsDirectSpaceState spacestate = GetWorld().DirectSpaceState;
    //         Vector2 mousepos = GetViewport().GetMousePosition();
    //         Vector3 rayorigin = _testCamera.ProjectRayOrigin(mousepos);
    //         Vector3 rayend = rayorigin + _testCamera.ProjectRayNormal(mousepos) * 2000.0f;
    //         Dictionary intersection = spacestate.IntersectRay(rayorigin, rayend, new Godot.Collections.Array(), 2147483647, false, true);
    //         if (intersection.Count > 0)
    //         {
    //             Vector3 pos = (Vector3) intersection["position"];
    //             ForceFieldHit hit = new() {Pos = pos, Time = 0.0f};
    //             _forcefieldHits.Add(hit);
    //         }
    //     }
    // }

    private void _OnForceFieldAreaEntered(Area3D other)
    {
        if (_health <= 0.0f)
            return;
        
        if (other is Bullet bullet && bullet.Alignment != _alignment)
        {
            Vector3 pos = (bullet.GlobalTransform.Origin - GlobalTransform.Origin).Normalized();
            ForceFieldHit hit = new() {Pos = pos, Time = 0.0f};
            _forcefieldHits.Add(hit);
            _health -= bullet.Damage;
            if (_health <= 0.0f)
            {
                Deactivate();
            }
            bullet.QueueFree();
        }
    }
}
