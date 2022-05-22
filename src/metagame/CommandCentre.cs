using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot.Collections;
using Array = Godot.Collections.Array;

public class CommandCentre : Spatial
{
    [Export] private float _transitionTime;

    [Export] private PackedScene _gameScene;
    [Export] private NodePath _navigationPath;
    [Export] private NodePath _leftButtonPath;
    [Export] private NodePath _rightButtonPath;
    [Export] private NodePath _cameraPath;
    [Export] private NodePath _mapCameraTransformPath;
    [Export] private NodePath _deployCameraTransformPath;

    private Button _leftButton;
    private Button _rightButton;

    private Camera _camera;
    private Spatial _mapCameraTransform;
    private Spatial _deployCameraTransform;

    private bool _transitioning;
    private float _transitionTimer;
    private Spatial _transitionFrom;
    private Spatial _transitionTo;
    private Metagame.GameState _nextGameState;
    private bool _pendingLoadGameScene;
    
    public override void _Ready()
    {
        base._Ready();

        _leftButton = GetNode<Button>(_leftButtonPath);
        _rightButton = GetNode<Button>(_rightButtonPath);

        _leftButton.Connect("pressed", this, nameof(_OnNavigateLeft));
        _rightButton.Connect("pressed", this, nameof(_OnNavigateRight));

        _camera = GetNode<Camera>(_cameraPath);
        _mapCameraTransform = GetNode<Spatial>(_mapCameraTransformPath);
        _deployCameraTransform = GetNode<Spatial>(_deployCameraTransformPath);

        switch (Metagame.Instance.CurrentState)
        {
            case Metagame.GameState.Map:
                _camera.GlobalTransform = _mapCameraTransform.GlobalTransform;
                break;
            case Metagame.GameState.Deploy:
                _camera.GlobalTransform = _deployCameraTransform.GlobalTransform;
                break;
            case Metagame.GameState.Results:
                // we quit the results screen, assume the results have been saved and thus go back to the map screen.
                Metagame.Instance.ChangeState(Metagame.GameState.Map);
                _camera.GlobalTransform = _mapCameraTransform.GlobalTransform;
                break;
            case Metagame.GameState.InGame:
                // we quit the previous game session, so start back in the deploy screen
                Metagame.Instance.ChangeState(Metagame.GameState.Deploy);
                _camera.GlobalTransform = _deployCameraTransform.GlobalTransform;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (_pendingLoadGameScene)
        {
            GetTree().ChangeSceneTo(_gameScene);
            return;
        }

        if (_transitioning)
        {
            _transitionTimer += delta;
            if (_transitionTimer >= _transitionTime)
            {
                _transitioning = false;
                Metagame.Instance.ChangeState(_nextGameState);
                GetNode<Control>(_navigationPath).Visible = true;
            }

            float t = Mathf.Clamp(_transitionTimer / _transitionTime, 0.0f, 1.0f);
            _camera.GlobalTransform = _transitionFrom.GlobalTransform.InterpolateWith(_transitionTo.GlobalTransform, t);
        }
    }

    public void _OnNavigateLeft()
    {
        switch (Metagame.Instance.CurrentState)
        {
            case Metagame.GameState.Map:
                Debug.Assert(false, "Invalid navigation attempt.");
                break;
            case Metagame.GameState.Deploy:
                _transitionFrom = _deployCameraTransform;
                _transitionTo = _mapCameraTransform;
                _nextGameState = Metagame.GameState.Map;
                break;
            case Metagame.GameState.Results:
                break;
            case Metagame.GameState.InGame:
            default:
                throw new ArgumentOutOfRangeException();
        }

        _transitioning = true;
        _transitionTimer = 0.0f;
        GetNode<Control>(_navigationPath).Visible = false;
    }

    public void _OnNavigateRight()
    {
        switch (Metagame.Instance.CurrentState)
        {
            case Metagame.GameState.Map:
                _transitionFrom = _mapCameraTransform;
                _transitionTo = _deployCameraTransform;
                _nextGameState = Metagame.GameState.Deploy;
                break;
            case Metagame.GameState.Deploy:
                Metagame.Instance.ChangeState(Metagame.GameState.InGame);
                _pendingLoadGameScene = true;
                break;
            case Metagame.GameState.Results:
                break;
            case Metagame.GameState.InGame:
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        _transitioning = true;
        _transitionTimer = 0.0f;
        GetNode<Control>(_navigationPath).Visible = false;
    }
}
