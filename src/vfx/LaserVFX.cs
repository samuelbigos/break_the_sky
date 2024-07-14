using Godot;
using System;
using System.Collections.Generic;

public partial class LaserVFX : Node3D
{
    [Export] public float ChargeTime = 1.0f;
    [Export] public float FireTime = 1.0f;
    
    private enum State
    {
        Idle,
        Charging,
        Firing
    }
    
    [Export] private GpuParticles3D _centre;
    [Export] private GpuParticles3D _particles;
    [Export] private GpuParticles3D _shoot1;
    [Export] private GpuParticles3D _shoot2;
    [Export] private GpuParticles3D _shoot3;
    [Export] private MeshInstance3D _laserMesh;

    private State _state;
    private double _timer;
    private ParticleProcessMaterial _centreMat;
    private ParticleProcessMaterial _particlesMat;
    private List<GpuParticles3D> _shootParticles = new List<GpuParticles3D>();

    public override void _Ready()
    {
        _centreMat = _centre.ProcessMaterial as ParticleProcessMaterial;
        _particlesMat = _particles.ProcessMaterial as ParticleProcessMaterial;
        
        _shootParticles.Add(_shoot1);
        _shootParticles.Add(_shoot2);
        _shootParticles.Add(_shoot3);
        
        Reset();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        _timer += delta;
        
        switch (_state)
        {
            case State.Charging:
            {
                float t = (float)Mathf.InverseLerp(0.0f, ChargeTime, _timer);
                _centreMat.ScaleMin = Mathf.Lerp(0.0f, 5.0f, t);
                _centreMat.ScaleMax = Mathf.Lerp(0.0f, 6.0f, t);
                _particlesMat.EmissionSphereRadius = Mathf.Lerp(1.0f, 5.0f, t);
                _particlesMat.RadialAccelMin = Mathf.Lerp(-0.0f, -100.0f, t);
                _particlesMat.RadialAccelMax = _particlesMat.RadialAccelMin;
                _particlesMat.ScaleMin = Mathf.Lerp(0.05f, 0.3f, t);
                _particlesMat.ScaleMax = _particlesMat.ScaleMin;

                if (_timer > ChargeTime)
                {
                    _centre.Visible = false;
                    _particles.Visible = false;
                    foreach (GpuParticles3D particle in _shootParticles)
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
                float t = (float)Mathf.InverseLerp(0.0f, FireTime, _timer);

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
        foreach (GpuParticles3D particle in _shootParticles)
        {
            particle.Visible = false;
        }
    }
}
