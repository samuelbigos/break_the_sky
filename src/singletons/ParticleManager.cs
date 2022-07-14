using Godot;
using System;
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
            _oneShotParticles[i] = osp;
            
            if (osp.Lifetime < 0.0f)
            {
                _oneShotParticles.RemoveAt(i);
                osp.Queue.Enqueue(osp.Particles);
            }
        }
    }

    public void AddOneShotParticles(PackedScene particleScene, Vector3 position)
    {
#if !EXPORT
        if (!_pools.ContainsKey(particleScene))
        {
            Debug.Assert(false, $"ParticleManager does not pool {particleScene}!");
            return;
        }
#endif
        Queue<Particles> list = _pools[particleScene];
#if !EXPORT
        if (list.Count == 0)
        {
            Debug.Assert(false, $"Increase pool size for {particleScene}!");
            return;
        }
#endif
        Particles p = list.Dequeue();
        p.GlobalPosition(position);
        p.Emitting = true;
        _oneShotParticles.Add(new OneShotParticles() { Queue = list, Particles = p, Lifetime = p.Lifetime });
    }
}
