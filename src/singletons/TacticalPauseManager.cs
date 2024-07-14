using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class TacticalPauseManager : Singleton<TacticalPauseManager>
{
    private Vector2 _dragStart;
    private Vector2 _dragCurrent;
    private bool _dragging;
    private TextureRect _dragTextureRect;
    private List<BoidAllyBase> _selectedBoids = new();

    public override void _Ready()
    {
        base._Ready();

        CanvasLayer canvas = new();
        AddChild(canvas);
        _dragTextureRect = new TextureRect();
        canvas.AddChild(_dragTextureRect);
        _dragTextureRect.Position = Vector2.Zero;
        _dragTextureRect.Size = Vector2.One * 100.0f;
        _dragTextureRect.Texture = ResourceLoader.Load<Texture2D>("res://assets/gui/1px.png");
        _dragTextureRect.ExpandMode = TextureRect.ExpandModeEnum.FitHeight;
        _dragTextureRect.Visible = false;
        _dragTextureRect.Modulate = new Color(0.0f, 0.0f, 0.0f, 0.5f);
        
        StateMachine_Game.OnGameStateChanged += _OnGameStateChanged;
    }

    public override void _Process(double delta)
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
            dragRect.Position = new Vector2(Mathf.Min(_dragCurrent.X, _dragStart.X), Mathf.Min(_dragCurrent.Y, _dragStart.Y));
            dragRect.End = new Vector2(Mathf.Max(_dragCurrent.X, _dragStart.X), Mathf.Max(_dragCurrent.Y, _dragStart.Y));

            _dragTextureRect.Position = dragRect.Position;
            _dragTextureRect.Size = dragRect.End - dragRect.Position;
            
            Rect2 worldRect = new Rect2();
            worldRect.Position = GameCamera.Instance.ProjectToZero(dragRect.Position).To2D();
            worldRect.End = GameCamera.Instance.ProjectToZero(dragRect.End).To2D();
            foreach (BoidAllyBase boid in BoidFactory.Instance.AllyBoids)
            {
                if (boid.GlobalPosition.X > worldRect.Position.X && boid.GlobalPosition.Y > worldRect.Position.Y
                                                                 && boid.GlobalPosition.X < worldRect.End.X &&
                                                                 boid.GlobalPosition.Y < worldRect.End.Y)
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
            foreach (BoidAllyBase boid in _selectedBoids)
            {
                boid.NavigateTowards(GameCamera.Instance.MousePosition);
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
