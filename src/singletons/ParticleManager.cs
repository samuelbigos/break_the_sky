using Godot;
using System.Collections.Generic;
using System.Diagnostics;

public class ParticleManager : Spatial
{
    [Export] private int _poolSize = 100;
    [Export] private List<PackedScene> _particlesToPool;

    private static ParticleManager _instance;
    public static ParticleManager Instance => _instance;

    private Dictionary<PackedScene, Queue<Particles>> _pools = new();
    
    private struct OneShotParticles
    {
        public Queue<Particles> Queue;
        public Particles Particles;
        public float Lifetime;
        public bool TriggerEmit;
        public ParticlesMaterial OriginalMaterial;
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
            Queue<Particles> list = new(_poolSize);
            for (int i = 0; i < _poolSize; i++)
            {
                Particles particles = particleScene.Instance() as Particles;
                particles.Visible = false;
                AddChild(particles);
                list.Enqueue(particles);
            }
            _pools.Add(particleScene, list);
        }
    }

    public override void _Process(float delta)
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

    public Particles AddOneShotParticles(PackedScene particleScene, Vector3 position, out ParticlesMaterial mat, bool duplicateMaterial = false)
    {
        mat = null;
#if !FINAL
        if (!_pools.ContainsKey(particleScene))
        {
            Debug.Assert(false, $"ParticleManager does not pool {particleScene}!");
            return null;
        }
#endif
        Queue<Particles> list = _pools[particleScene];
        if (list.Count == 0)
        {
            Debug.Assert(false, $"Increase pool size for {particleScene}!");
            return null;
        }
        Particles p = list.Dequeue();
        p.GlobalPosition(position);
        p.Visible = true;
        float lifetime = p.Lifetime + (1.0f - p.Explosiveness) * p.Lifetime; // Account for explosiveness != 1.0.
        
        // Duplicate material if required.
        ParticlesMaterial originalMat = p.ProcessMaterial as ParticlesMaterial;
        if (duplicateMaterial) p.ProcessMaterial = originalMat.Duplicate() as ParticlesMaterial;
        mat = p.ProcessMaterial as ParticlesMaterial;
        DebugUtils.Assert(!mat.Null(), "Particle doesn't have a ParticlesMaterial.");
        
        _oneShotParticles.Add(new OneShotParticles()
        {
            Queue = list, Particles = p, Lifetime = lifetime, TriggerEmit = true, OriginalMaterial = originalMat
        });
        
        return p;
    }
}
