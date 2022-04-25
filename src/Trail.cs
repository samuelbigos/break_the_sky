using System.Collections.Generic;
using Godot;

public class Trail : Node2D
{
    [Export] public float Width = 2.0f;

    private float _alpha = 1.0f;
    private BoidBase _boid = null;

    public void Init(BoidBase owner)
    {
        _boid = owner;
    }
    
    public override void _Ready()
    {
        ZIndex = -1;
    }

    public override void _Draw()
    {
        List<Vector2> pointArray = new List<Vector2>();
        List<Color> colourArray = new List<Color>();
        int numPoints = _boid._trailPoints.Count;
        if (numPoints < 2)
        {
            return;
        }

        foreach (int i in GD.Range(0, numPoints))
        {
            pointArray.Add(_boid._trailPoints[i] - GlobalPosition);
            var col = ColourManager.Instance.White;
            col.a = 0.25f + ((float) (i) / numPoints) * 0.75f;
            col.a *= _alpha;
            colourArray.Add(col);
        }

        pointArray.Add(new Vector2(0.0f, 0.0f));
        colourArray.Add(ColourManager.Instance.White);
        DrawPolylineColors(pointArray.ToArray(), colourArray.ToArray(), Width);
    }
}