using System;
using System.Collections.Generic;
using Godot;

public class BoidPlayer : BoidAllyBase
{
    [Export] private PackedScene _bulletScene;
    [Export] private NodePath _sfxPickupPath;
    [Export] private float _damping = 0.05f;
    [Export] private float _totalSendTime = 1.0f;
    [Export] private Vector2 _sendScaleMinMax = new Vector2(10.0f, 100.0f);

    private AudioStreamPlayer2D _sfxPickup;
    private Vector2 _velocity;
    
    private Vector2 _cachedMousePos;
    private bool _sending;
    private float _sendTime;
    private int _sendCount;
    private int _totalAllies;

    private List<BoidAllyBase> _alliesSent = new();

    public override void _Ready()
    {
        base._Ready();

        _sfxPickup = GetNode<AudioStreamPlayer2D>(_sfxPickupPath);

        _mesh.Visible = true;
    }

    protected override void ProcessAlive(float delta)
    {
        base.ProcessAlive(delta);

        ProcessAllySending(delta);

        _cachedHeading = (_cachedMousePos - GlobalPosition).Normalized();

        if (_acceptInput)
        {
            Vector2 forward = new(0.0f, -1.0f);
            Vector2 left = new(-1.0f, 0.0f);

            Vector2 dir = new(0.0f, 0.0f);
            if (Input.IsActionPressed("w")) dir += forward;
            if (Input.IsActionPressed("s")) dir += -forward;
            if (Input.IsActionPressed("a")) dir += left;
            if (Input.IsActionPressed("d")) dir += -left;
            
            ref SteeringManager.Boid boid = ref SteeringBoid;

            if (dir != new Vector2(0.0f, 0.0f))
            {
                dir = dir.Normalized();
                dir *= MaxVelocity;
                boid.DesiredVelocityOverride = dir.ToNumerics();
                SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Stop, false);
                SetSteeringBehaviourEnabled(SteeringManager.Behaviours.DesiredVelocityOverride, true);
            }
            else
            {
                boid.DesiredVelocityOverride = System.Numerics.Vector2.Zero;
                SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Stop, true);
                SetSteeringBehaviourEnabled(SteeringManager.Behaviours.DesiredVelocityOverride, false);
            }
        }
    }

    private void ProcessAllySending(float delta)
    {
        List<BoidAllyBase> idleAllies = new();
        _totalAllies = 0;
        foreach (BoidAllyBase ally in BoidFactory.Instance.AllyBoids)
        {
            if (ally is BoidPlayer)
                continue;

            _totalAllies++;
            if (_alliesSent.Contains(ally))
                continue;
            
            if (ally.AiState != AIState.Idle)
                continue;
            
            idleAllies.Add(ally);
        }
        
        _cachedMousePos = GameCamera.Instance.MousePosition;

        if (idleAllies.Count > 0)
        {
            float sqrtIdleCount = (float)Math.Sqrt(idleAllies.Count);
            
            // set ally steering parameters
            for (int i = 0; i < BoidFactory.Instance.AllyBoids.Count; i++)
            {
                BoidAllyBase ally = BoidFactory.Instance.AllyBoids[i];
                ref SteeringManager.Boid boid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(ally.SteeringId);

                // set arrive deadzone to some value proportional to the amount of drones we have active
                boid.ArriveDeadzone = _steeringRadius + boid.Radius * Mathf.Max(1.0f, sqrtIdleCount);
                boid.ArriveWeight = 0.5f;
            }
        }
        
        if (_sending)
        {
            _sendTime = Mathf.Clamp(_sendTime + delta, 0.0f, _totalSendTime);
            float t = Mathf.Clamp(_sendTime / _totalSendTime, 0.0f, 1.0f);
            
            // +1 ensures we always send one ally as soon as player clicks
            _sendCount = (int) Mathf.Min(t * Cursor.Instance.TotalPips, _totalAllies - 1) + 1;
            
            Cursor.Instance.PipCount = _sendCount;
            Cursor.Instance.Size = 1.0f + t;
            Cursor.Instance.Activated = true;
            
            idleAllies.Sort(AllySort);

            if (_sendCount > _alliesSent.Count && idleAllies.Count > 0)
            {
                BoidAllyBase allyToSend = idleAllies[0];
                _alliesSent.Add(allyToSend);
                allyToSend.OnBoidDestroyed += OnSentAllyDestroyed;
            }
            
            float sqrtSendCount = (float)Math.Sqrt(_sendCount);
            foreach (BoidAllyBase sentAlly in _alliesSent)
            {
                sentAlly.NavigateTowards(_cachedMousePos);
                ref SteeringManager.Boid boid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(sentAlly.SteeringId);
                boid.ArriveDeadzone = _steeringRadius + boid.Radius * Mathf.Max(1.0f, sqrtSendCount);
            }
        }
    }

    private void OnSentAllyDestroyed(BoidBase ally)
    {
        _alliesSent.Remove(ally as BoidAllyBase);
    }

    private int AllySort(BoidAllyBase a, BoidAllyBase b)
    {
        float aDist = (a.GlobalPosition - _cachedMousePos).LengthSquared();
        float bDist = (b.GlobalPosition - _cachedMousePos).LengthSquared();
        return aDist < bDist ? -1 : 1;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (!_acceptInput)
            return;

        if (@event.IsActionPressed("shoot"))
        {
            _sending = true;
            _sendTime = 0.0f;
            _totalSendTime = Mathf.RangeLerp(_totalAllies, _sendScaleMinMax.x, _sendScaleMinMax.y, 5.0f, 1.0f);
        }

        if (@event.IsActionReleased("shoot"))
        {
            _sending = false;
            Cursor.Instance.Reset();
            foreach (BoidAllyBase ally in _alliesSent)
            {
                ally.OnBoidDestroyed -= OnSentAllyDestroyed;
            }
            _alliesSent.Clear();
        }
    }

    public void RegisterPickup(PickupMaterial pickup)
    {
        pickup.OnCollected += _OnPickupCollected;
    }

    protected override void _OnDestroy(Vector2 hitDir, float hitStrength)
    {
        // TODO: do something when player destroyed.
        SceneTransitionManager.Instance.RequestReloadCurrentScene();
    }

    public override void _OnBoidAreaEntered(Area area)
    {
    }

    private void _OnPickupCollected(PickupMaterial pickup)
    {
        SaveDataPlayer.MaterialCount += 1;
        _sfxPickup.Play();
    }
}