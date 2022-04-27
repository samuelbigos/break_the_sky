using System.Collections.Generic;
using Godot;

public class TitleScreen : CanvasLayer
{
    [Export] public PackedScene ControlsScreen;

    public VBoxContainer _vBox;

    private List<Label> _text = new List<Label>();
    public float _time;
    public int _index;
    
    public override void _Ready()
    {
        GetNode<MenuButton>("Button").Connect("pressed", this, nameof(OnButtonPressed));

        _vBox = GetNode<VBoxContainer>("CenterContainer/VBoxContainer");

        Label text;
        for (int i = 0; i < _vBox.GetChildCount(); i++)
        {
            if ((text = _vBox.GetChild<Label>(i)) != null)
            {
                _text.Add(text);
                text.Visible = false;
            }
            
        }

        MusicPlayer.Instance.PlayMenu();
    }

    public override void _Process(float delta)
    {
        _time -= delta;
        if (_index < _text.Count)
        {
            if (_time < 0.0)
            {
                _text[_index].Visible = true;
                _time = 2.0f;
                _index += 1;
            }
        }
        else if (_time < 0.0)
        {
            GetNode<MenuButton>("Button").Visible = true;
        }

        if (_time > 0.0 && GetNode<MenuButton>("Button").Visible == false)
        {
            Color mod = _text[_index - 1].Modulate;
            mod.a = Mathf.Lerp(0.0f, 1.0f, 1.0f - (_time / 2.0f));
            _text[_index - 1].Modulate = mod;
        }

        if (Input.IsActionJustReleased("shoot"))
        {
            foreach (Label label in _text)
            {
                label.Visible = true;
                Color mod = label.Modulate;
                mod.a = 1.0f;
                label.Modulate = mod;
                _index = _text.Count;
                GetNode<MenuButton>("Button").Visible = true;
            }
        }
    }

    public void OnButtonPressed()
    {
        GetTree().ChangeSceneTo(ControlsScreen);
    }
}