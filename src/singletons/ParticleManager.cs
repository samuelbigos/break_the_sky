using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using Godot.Collections;

public partial class ParticleManager : Node3D
{
    [Export] private int _poolSize = 100;
    [Export] private Array _particlesToPool;

    private static ParticleManager _instance;
    public static ParticleManager Instance => _instance;

    private System.Collections.Generic.Dictionary<PackedScene, Queue<GpuParticles3D>> _pools = new();
    
    private struct OneShotParticles
    {
        public Queue<GpuParticles3D> Queue;
        public GpuParticles3D Particles;
        public double Lifetime;
        public bool TriggerEmit;
        public ParticleProcessMaterial OriginalMaterial;
    }

    private List<OneShotParticles> _oneShotParticles = new(100);

    public override void _EnterTree()
    {
        base._EnterTree();

        _instance = this;
    }

    public override void _Ready()
    {
        base._Ready();

        foreach (PackedScene particleScene in _particlesToPool)
        {
            Queue<GpuParticles3D> list = new(_poolSize);
            for (int i = 0; i < _poolSize; i++)
            {
                GpuParticles3D particles = particleScene.Instantiate() as GpuParticles3D;
                particles.Visible = false;
                AddChild(particles);
                list.Enqueue(particles);
            }
            _pools.Add(particleScene, list);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        for (int i = _oneShotParticles.Count - 1; i >= 0; i--)
        {
            OneShotParticles osp = _oneShotParticles[i];
            osp.Lifetime -= delta;
            if (osp.TriggerEmit)
            {
                // Start emitting the frame after we set position, otherwise the first particles will
                // appear at the previous location.
                osp.Particles.Emitting = true;
                osp.TriggerEmit = false;
            }
            _oneShotParticles[i] = osp;
            
            if (osp.Lifetime < 0.0f)
            {
                _oneShotParticles.RemoveAt(i);
                osp.Particles.ProcessMaterial = osp.OriginalMaterial;
                osp.Queue.Enqueue(osp.Particles);
                osp.Particles.Visible = false;
            }
        }
    }

    public GpuParticles3D AddOneShotParticles(PackedScene particleScene, Vector3 position, out ParticleProcessMaterial mat, bool duplicateMaterial = false)
    {
        mat = null;
#if !FINAL
        if (!_pools.ContainsKey(particleScene))
        {
            Debug.Assert(false, $"ParticleManager does not pool {particleScene}!");
            return null;
        }
#endif
        Queue<GpuParticles3D> list = _pools[particleScene];
        if (list.Count == 0)
        {
            Debug.Assert(false, $"Increase pool size for {particleScene}!");
            return null;
        }
        GpuParticles3D p = list.Dequeue();
        p.GlobalPosition(position);
        p.Visible = true;
        double lifetime = p.Lifetime + (1.0f - p.Explosiveness) * p.Lifetime; // Account for explosiveness != 1.0.
        
        // Duplicate material if required.
        ParticleProcessMaterial originalMat = p.ProcessMaterial as ParticleProcessMaterial;
        if (duplicateMaterial) p.ProcessMaterial = originalMat.Duplicate() as ParticleProcessMaterial;
        mat = p.ProcessMaterial as ParticleProcessMaterial;
        DebugUtils.Assert(mat != null, "Particle doesn't have a ParticleProcessMaterial.");
        
        _oneShotParticles.Add(new OneShotParticles()
        {
            Queue = list, Particles = p, Lifetime = lifetime, TriggerEmit = true, OriginalMaterial = originalMat
        });
        
        return p;
    }
}
