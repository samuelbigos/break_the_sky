using Godot;
using System;
using System.Collections.Generic;
using GodotOnReady.Attributes;

public partial class LaserVFX : Spatial
{
    [Export] public float ChargeTime = 1.0f;
    [Export] public float FireTime = 1.0f;
    
    private enum State
    {
        Idle,
        Charging,
        Firing
    }
    
    [OnReadyGet] private Particles _centre;
    [OnReadyGet] private Particles _particles;
    [OnReadyGet] private Particles _shoot1;
    [OnReadyGet] private Particles _shoot2;
    [OnReadyGet] private Particles _shoot3;
    [OnReadyGet] private MeshInstance _laserMesh;

    private State _state;
    private float _timer;
    private ParticlesMaterial _centreMat;
    private ParticlesMaterial _particlesMat;
    private List<Particles> _shootParticles = new List<Particles>();

    [OnReady] private void Ready()
    {
        _centreMat = _centre.ProcessMaterial as ParticlesMaterial;
        _particlesMat = _particles.ProcessMaterial as ParticlesMaterial;
        
        _shootParticles.Add(_shoot1);
        _shootParticles.Add(_shoot2);
        _shootParticles.Add(_shoot3);
        
        Reset();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        _timer += delta;
        
        switch (_state)
        {
            case State.Charging:
            {
                float t = Mathf.InverseLerp(0.0f, ChargeTime, _timer);
                _centreMat.Scale = Mathf.Lerp(0.0f, 5.0f, t);
                _centreMat.ScaleRandom = Mathf.Lerp(0.0f, 1.0f, t);
                _particlesMat.EmissionSphereRadius = Mathf.Lerp(1.0f, 5.0f, t);
                _particlesMat.RadialAccel = Mathf.Lerp(-0.0f, -100.0f, t);
                _particlesMat.Scale = Mathf.Lerp(0.05f, 0.3f, t);

                if (_timer > ChargeTime)
                {
                    _centre.Visible = false;
                    _particles.Visible = false;
                    foreach (Particles particle in _shootParticles)
                    {
                        particle.Emitting = true;
                        particle.Visible = true;
                    }
                    _laserMesh.Visible = true;
                    _state = State.Firing;
                    _timer = 0.0f;
                }
                break;
            }
            case State.Firing:
            {
                float t = Mathf.InverseLerp(0.0f, FireTime, _timer);

                float scale = Mathf.Sin(t * Mathf.Pi);
                _laserMesh.Scale = new Vector3(scale, 1.0f, scale);
                
                if (_timer > FireTime)
                {
                    Reset();
                }
                break;
            }
            case State.Idle:
            default:
                break;
        }
    }

    public void Start()
    {
        _state = State.Charging;
        _timer = 0.0f;
        _centre.Visible = true;
        _particles.Visible = true;
    }

    public void Reset()
    {
        _state = State.Idle;
        _timer = 0.0f;
        _centre.Visible = false;
        _particles.Visible = false;
        _laserMesh.Visible = false;
        foreach (Particles particle in _shootParticles)
        {
            particle.Visible = false;
        }
    }
}
