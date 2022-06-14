using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class TacticalPauseManager : Singleton<TacticalPauseManager>
{
    private Vector2 _dragStart;
    private Vector2 _dragCurrent;
    private bool _dragging;
    private TextureRect _dragTextureRect;
    private List<BoidBase> _selectedBoids = new();

    public override void _Ready()
    {
        base._Ready();

        CanvasLayer canvas = new();
        AddChild(canvas);
        _dragTextureRect = new TextureRect();
        canvas.AddChild(_dragTextureRect);
        _dragTextureRect.RectPosition = Vector2.Zero;
        _dragTextureRect.RectSize = Vector2.One * 100.0f;
        _dragTextureRect.Texture = ResourceLoader.Load<Texture>("res://assets/gui/1px.png");
        _dragTextureRect.Expand = true;
        _dragTextureRect.Visible = false;
        _dragTextureRect.Modulate = new Color(0.0f, 0.0f, 0.0f, 0.5f);
        
        StateMachine_Game.OnGameStateChanged += _OnGameStateChanged;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (StateMachine_Game.CurrentState != StateMachine_Game.States.TacticalPause)
            return;

        if (Input.IsActionJustPressed("tactical_select"))
        {
            _dragStart = GetViewport().GetMousePosition();
            _dragging = true;
            _dragTextureRect.Visible = true;

            foreach (BoidAllyBase boid in BoidFactory.Instance.AllyBoids)
            {
                boid.Selected = false;
            }
        }

        if (Input.IsActionJustReleased("tactical_select"))
        {
            _dragging = false;
            _dragTextureRect.Visible = false;
        }

        if (_dragging)
        {
            _dragCurrent = GetViewport().GetMousePosition();

            Rect2 dragRect = new Rect2();
            dragRect.Position = new Vector2(Mathf.Min(_dragCurrent.x, _dragStart.x), Mathf.Min(_dragCurrent.y, _dragStart.y));
            dragRect.End = new Vector2(Mathf.Max(_dragCurrent.x, _dragStart.x), Mathf.Max(_dragCurrent.y, _dragStart.y));

            _dragTextureRect.RectPosition = dragRect.Position;
            _dragTextureRect.RectSize = dragRect.End - dragRect.Position;
            
            Rect2 worldRect = new Rect2();
            worldRect.Position = GameCamera.Instance.ProjectToZero(dragRect.Position).To2D();
            worldRect.End = GameCamera.Instance.ProjectToZero(dragRect.End).To2D();
            foreach (BoidAllyBase boid in BoidFactory.Instance.AllyBoids)
            {
                if (boid.GlobalPosition.x > worldRect.Position.x && boid.GlobalPosition.y > worldRect.Position.y
                                                                 && boid.GlobalPosition.x < worldRect.End.x &&
                                                                 boid.GlobalPosition.y < worldRect.End.y)
                {
                    _selectedBoids.Add(boid);
                }
            }

            foreach (BoidBase boid in _selectedBoids)
            {
                boid.Selected = true;
            }
        }

        if (Input.IsActionJustReleased("tactical_command"))
        {
            foreach (BoidBase boid in _selectedBoids)
            {
                boid.SetTarget(BoidBase.TargetType.Position, null, GameCamera.Instance.MousePosition);
            }
        }
    }

    private void _OnGameStateChanged(StateMachine_Game.States state, StateMachine_Game.States prevState)
    {
        _selectedBoids.Clear();
        if (prevState == StateMachine_Game.States.TacticalPause)
        {
            foreach (BoidAllyBase boid in BoidFactory.Instance.AllyBoids)
            {
                boid.Selected = false;
            }
        }
    }
}
