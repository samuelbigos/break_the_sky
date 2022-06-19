using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class BoidFactory : Singleton<BoidFactory>
{
    public List<BoidBase> AllBoids => _allBoids;
    public List<BoidBase> DestroyedBoids => _destroyedBoids;
    public List<BoidAllyBase> AllyBoids => _allyBoids;
    public List<BoidEnemyBase> EnemyBoids => _enemyBoids;
    public int NumBoids => _allyBoids.Count;
    
    private List<BoidBase> _allBoids = new();
    private List<BoidBase> _destroyedBoids = new();
    private List<BoidEnemyBase> _enemyBoids = new();
    private List<BoidAllyBase> _allyBoids = new();

    public override void _Ready()
    {
        base._Ready();

        SceneTransitionManager.OnSceneTransitionInitiated += _OnSceneChanged;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        for (int i = _allBoids.Count - 1; i >= 0; i--)
        {
            if (!IsInstanceValid(_allBoids[i]))
            {
                if (_allBoids[i] is BoidAllyBase)
                    _allyBoids.Remove(_allBoids[i] as BoidAllyBase);
                
                if (_allBoids[i] is BoidEnemyBase)
                    _enemyBoids.Remove(_allBoids[i] as BoidEnemyBase);
                
                _allBoids.Remove(_allBoids[i]);
            }
        }
        
        Debug.Assert(_allyBoids.Count + _enemyBoids.Count == _allBoids.Count, "Error in boid references.");
    }

    public BoidEnemyBase CreateEnemyBoid(DataEnemyBoid boid, Vector2 pos, Vector2 vel)
    {
        BoidEnemyBase enemy = boid.Scene.Instance<BoidEnemyBase>();
        Game.Instance.AddChild(enemy);
        enemy.Init(boid.Name, _OnBoidDestroyed, pos, vel);
        
        SaveDataPlayer.SetSeenEnemy(boid.Name);
        _enemyBoids.Add(enemy);
        _allBoids.Add(enemy);
        
        return enemy;
    }

    public BoidAllyBase CreateAllyBoid(DataAllyBoid boid)
    {
        if (_allyBoids.Count >= SaveDataPlayer.MaxAllyCount)
            return null;
            
        BoidAllyBase ally = boid.Scene.Instance<BoidAllyBase>();
        Game.Instance.AddChild(ally);

        Vector2 pos = Game.Player.GlobalPosition + Utils.RandV2() * 1.0f;
        ally.Init(ally.Name, _OnBoidDestroyed, pos, Vector2.Zero);
        
        _allyBoids.Add(ally);
        _allBoids.Add(ally);

        return ally;
    }
    
    public void FreeBoid(BoidBase boid)
    {
        _destroyedBoids.Remove(boid);
        boid.QueueFree();
    }
    
    private void _OnBoidDestroyed(BoidBase boid)
    {
        switch (boid)
        {
            case BoidAllyBase @base:
                _allyBoids.Remove(@base);
                break;
            case BoidEnemyBase @base:
                _enemyBoids.Remove(@base);
                break;
        }
        _allBoids.Remove(boid);
        _destroyedBoids.Add(boid);
    }

    private void _OnSceneChanged()
    {
        _allBoids.Clear();
        _allyBoids.Clear();
        _enemyBoids.Clear();
        _destroyedBoids.Clear();
    }
}
