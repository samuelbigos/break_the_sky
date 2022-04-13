using Godot;

public class MenuButton : Button
{
    private bool _hover;
    private float _time;

    public override void _Ready()
    {
        Connect("mouse_entered", this, nameof(OnButtonMouseEntered));
        Connect("mouse_exited", this, nameof(OnButtonMouseExited));
    }

    public override void _Process(float delta)
    {
        _time += delta;
        if (_hover)
        {
            float time = _time / 0.1f;
            float s = 1.0f + Mathf.Cos(time) * 0.05f;
            float r = Mathf.Sin(time) * 0.5f;
            GetNode<TextureRect>("TextureRect").RectPivotOffset = GetNode<TextureRect>("TextureRect").RectSize / 2.0f;
            GetNode<TextureRect>("TextureRect").RectScale = new Vector2(s, s);
            GetNode<TextureRect>("TextureRect").RectRotation = r;
        }
        else
        {
            GetNode<TextureRect>("TextureRect").RectScale = new Vector2(1.0f, 1.0f);
            GetNode<TextureRect>("TextureRect").RectRotation = 0.0f;
        }
    }

    public void OnButtonMouseEntered()
    {
        _hover = true;
        _time = 0.0f;
    }

    public void OnButtonMouseExited()
    {
        _hover = false;
    }
}