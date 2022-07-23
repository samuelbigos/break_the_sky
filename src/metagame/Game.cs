using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using GodotOnReady.Attributes;
using ImGuiNET;

public partial class Game : Singleton<Game>
{
    [OnReadyGet] private StateMachine_Game _stateMachine;
    [OnReadyGet] private AISpawningDirector _aiSpawningDirector;
    [OnReadyGet] private HUD _hud;
    [OnReadyGet] private MeshInstance _sand;
    [OnReadyGet] private CloudBox _clouds;

    [Export] private Rect2 _areaRect;
    [Export] private PackedScene _playerScene;

    [Export] private ResourceBoidAlly _playerData;
    
    private bool _initialSpawn;
    private bool _fullScreen = false;
    private BoidPlayer _player;
    
    public static BoidPlayer Player => Instance._player;
    public Rect2 SpawningRect => new(Player.GlobalPosition - _areaRect.Size * 0.5f, _areaRect.Size);

    [OnReady]
    private void Ready()
    {
        base._Ready();

        SteeringManager.EdgeBounds = _areaRect;

        _player = _playerScene.Instance<BoidPlayer>();
        AddChild(_player);
        _player.Init(_playerData, _OnPlayerDestroyed, Vector2.Zero, Vector2.Zero);
        BoidFactory.Instance.AllyBoids.Add(_player);
        BoidFactory.Instance.AllBoids.Add(_player);

        _aiSpawningDirector.Init(this, _player);

        GD.Randomize();

        GameCamera.Instance.Init(_player);
        MusicPlayer.Instance.PlayGame();

        StateMachine_Game.Instance.SendInitialStateChange();

        GameCamera.OnPostCameraTransformed += OnPostCameraTransformed;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        
        GameCamera.OnPostCameraTransformed -= OnPostCameraTransformed;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        SteeringManager.EdgeBounds = SpawningRect;

        if (!_initialSpawn)
        {
            // BoidFactory.Instance.CreateEnemyBoid(Database.EnemyBoid("driller"), new Vector2(100.0f, 0.0f), new Vector2(-50.0f, 0.0f));
            // BoidFactory.Instance.CreateEnemyBoid(Database.EnemyBoid("driller"), new Vector2(-100.0f, 0.0f), new Vector2(50.0f, 0.0f));
            
            _initialSpawn = true;
        }
    }
    
    private void OnPostCameraTransformed()
    {
        // scale and position sand
        {
            Vector3 topLeft = GameCamera.Instance.ProjectToY(new Vector2(0.0f, 0.0f), _sand.GlobalTransform.origin.y);
            Vector3 bottomRight = GameCamera.Instance.ProjectToY(GetViewport().Size, _sand.GlobalTransform.origin.y);
            _sand.Scale = new Vector3(bottomRight.x - topLeft.x,1.0f, bottomRight.z - topLeft.z);
            Vector3 pos = GameCamera.Instance.GlobalTransform.origin;
            pos.y = _sand.GlobalTransform.origin.y;
            _sand.GlobalPosition(pos);  
        }
        
        // position clouds
        Vector3 cloudPos = GameCamera.Instance.GlobalTransform.origin;
        _clouds.GlobalPosition(new Vector3(cloudPos.x, _clouds.GlobalTransform.origin.y, cloudPos.z));
    }

    public void RegisterPickup(PickupMaterial pickup)
    {
        AddChild(pickup);
        Player.RegisterPickup(pickup);
    }

    private void _OnPlayerDestroyed(BoidBase player)
    {
        // TODO: game over
    }
}