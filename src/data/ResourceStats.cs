using Godot;
using System;

public class ResourceStats : Resource
{
    [Export] public float AttackDamage = 1.0f;
    [Export] public float AttackCooldown = 1.0f;
    [Export] public float AttackSpread = 1.0f;
    [Export] public float AttackVelocity = 1.0f;
    [Export] public float AttackCharge = 1.0f;
    [Export] public float AttackDuration = 1.0f;
    [Export] public int AttackCount = 1;
    [Export] public float AttackRange = 100.0f;
    [Export] public float MoveSpeed = 1.0f;
    [Export] public float MaxHealth = 1.0f;
    [Export] public float Regeneration = 0.0f;
    [Export] public float CollisionDamage = 1.0f;
    [Export] public bool MicroTurrets = false;
    [Export] public float MicroTurretRange = 100.0f;
    [Export] public float MicroTurretCooldown = 0.5f;
    [Export] public float MicroTurretDamage = 2.0f;
    [Export] public float MicroTurretVelocity = 300.0f;

    public float AttackRangeSq => AttackRange * AttackRange;
}
