using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Godot;
using GodotOnReady.Attributes;
using Vector3 = Godot.Vector3;

public partial class BoidEnemyShieldbearer : BoidEnemyBase
{
    [Export] private int _maxForcefieldHits = 10;
    [Export] private float _forcefieldHitDuration = 0.5f;

    [OnReadyGet] private MeshInstance _forcefield;
    [OnReadyGet] private Area _forcefieldArea;

    private ShaderMaterial _forcefieldMat;

    struct ForceFieldHit
    {
        public Vector3 Pos;
        public float Time;
    }

    private List<ForceFieldHit> _forcefieldHits = new();

    [OnReady]
    private void Ready()
    {
        _forcefieldArea.Connect("area_entered", this, nameof(_OnForceFieldAreaEntered));
        _forcefieldMat = _forcefield.MaterialOverride as ShaderMaterial;
    }
    
    protected override void ProcessAlive(float delta)
    {
        if (_aiState == AIState.Engaged)
        {
        }

        Vector3 scale = _forcefield.Scale;
        _forcefield.GlobalTransform = new Transform(Basis.Identity, _forcefield.GlobalTransform.origin);
        _forcefield.Scale = scale;

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
                Rect2 hitData = new Rect2(hit.Pos.x, hit.Pos.y, hit.Pos.z, hit.Time);
                _forcefieldMat.SetShaderParam($"u_hit_{numAddedInShader + 1}", hitData);
                numAddedInShader++;
            }
            _forcefieldMat.SetShaderParam($"u_hits", numAddedInShader);
        }

        base.ProcessAlive(delta);
    }

    private void _OnForceFieldAreaEntered(Area other)
    {
        if (other is Bullet bullet && bullet.Alignment != Alignment)
        {
            Vector3 pos = (bullet.GlobalTransform.origin - GlobalTransform.origin).Normalized();
            ForceFieldHit hit = new() {Pos = pos, Time = 0.0f};
            _forcefieldHits.Add(hit);
            bullet.QueueFree();
        }
    }

    protected override void OnEnterAIState_Seeking()
    {
    }

    protected override void OnEnterAIState_Engaged()
    {
    }
}