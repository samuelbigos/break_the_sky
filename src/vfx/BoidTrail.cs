using Godot;
using System;
using System.Numerics;
using Vector3 = Godot.Vector3;

public class BoidTrail : MeshInstance
{
    [Export] private int _linePoints = 100;
    [Export] private float _lineInterval = 1.0f / 60.0f;
    [Export] private float _lineWidth = 10.0f;
    [Export] private Curve _lineWidthCurve;

    private SurfaceTool _st = new SurfaceTool();
    private int _trailIdx;
    private Vector3[] _trailPositions;
    private Spatial _parent;
    private float _updateTimer;

    public override void _Ready()
    {
        base._Ready();
        
        _trailPositions = new Vector3[_linePoints];
        _parent = GetParent<Spatial>();
        for (int i = 0; i < _linePoints; i++)
        {
            _trailPositions[i] = _parent.GlobalTransform.origin;
        }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        _updateTimer -= delta;
        if (_updateTimer < 0.0f)
        {
            _trailIdx = (_trailIdx + 1) % _linePoints;
            _trailPositions[_trailIdx] = _parent.GlobalTransform.origin;
            _updateTimer = _lineInterval;
        }

        _st.Begin(Mesh.PrimitiveType.Triangles);
        
        for (int i = 0; i < _linePoints; i++)
        {
            int idx = (_trailIdx - i + _linePoints) % _linePoints;
            Vector3 pos = _trailPositions[idx];
            Vector3 posLast = _trailPositions[(idx - 1 + _linePoints) % _linePoints];
            Vector3 dir = -(pos - posLast).Normalized();

            float w = _lineWidth * _lineWidthCurve.Interpolate((float)i / _linePoints) * 0.5f;

            Vector3 cross = dir.Cross(Vector3.Up) * w;
            _st.AddColor(ColourManager.Instance.White);
            _st.AddVertex(pos - cross);
            _st.AddColor(ColourManager.Instance.White);
            _st.AddVertex(pos + cross);

            if (i > 0)
            {
                int v = i * 2;
                _st.AddIndex(v - 2); // t1
                _st.AddIndex(v);
                _st.AddIndex(v + 1);
                _st.AddIndex(v + 1); // t2
                _st.AddIndex(v - 1);
                _st.AddIndex(v - 2);
            }
        }
        
        Mesh = _st.Commit();

        GlobalTransform = new Transform(Basis.Identity, Vector3.Zero);
    }
}
