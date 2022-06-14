using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Array = Godot.Collections.Array;

public class TrailRenderer : MeshInstance
{
    public static TrailRenderer Instance;
    
    private List<BoidTrail> _registeredTrails = new();
    
    private Vector3[] _vertList = new Vector3[10000];
    private Color[] _colList = new Color[10000];
    private int[] _indexList = new int[20000];

    public override void _EnterTree()
    {
        base._EnterTree();

        Instance = this;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        ArrayMesh outMesh = new();
        if (_registeredTrails.Count == 0)
        {
            Mesh = outMesh;
            return;
        }
        
        int v = 0;
        int i = 0;
        
        foreach (BoidTrail trail in _registeredTrails)
        {
            int linePoints = trail.LinePoints;
            Vector3[] trailPositions = trail.TrailPositions;
            int lineIdx = trail.TrailIdx;
            float lineWidth = trail.LineWidth;
            Curve lineWidthCurve = trail.LineWidthCurve;
            
            for (int j = 0; j < linePoints; j++)
            {
                int idx = (lineIdx - j + linePoints) % linePoints;
                Vector3 pos = trailPositions[idx];
                Vector3 posLast = trailPositions[(idx - 1 + linePoints) % linePoints];
                Vector3 dir = -(pos - posLast).Normalized();

                float w = lineWidth * lineWidthCurve.Interpolate((float)j / linePoints) * 0.5f;

                Vector3 cross = dir.Cross(Vector3.Up) * w;
                _colList[v] = ColourManager.Instance.White;
                _vertList[v] =  pos - cross;
                _colList[v + 1] = ColourManager.Instance.White;
                _vertList[v + 1] =  pos + cross;

                if (j > 0)
                {
                    _indexList[i++] = v - 2; // t1
                    _indexList[i++] = v;
                    _indexList[i++] = v + 1;
                    _indexList[i++] = v + 1; // t2
                    _indexList[i++] = v - 1;
                    _indexList[i++] = v - 2;
                }

                v += 2;
            }
        }
        
        Debug.Assert(v < _vertList.Length, "v < _vertList.Length");
        Debug.Assert(v < _colList.Length, "v < _colList.Length");
        Debug.Assert(i < _indexList.Length, "i < _indexList.Length");

        Span<Vector3> verts = _vertList.AsSpan(0, v);
        Span<Color> colours = _colList.AsSpan(0, v);
        Span<int> indices = _indexList.AsSpan(0, i);
        
        Array arrays = new();
        arrays.Resize((int) ArrayMesh.ArrayType.Max);
        arrays[(int) ArrayMesh.ArrayType.Vertex] = verts.ToArray();
        arrays[(int) ArrayMesh.ArrayType.Color] = colours.ToArray();
        arrays[(int) ArrayMesh.ArrayType.Index] = indices.ToArray();

        outMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        Mesh = outMesh;
    }

    public void Register(BoidTrail trail)
    {
        _registeredTrails.Add(trail);
    }
    
    public void UnRegister(BoidTrail trail)
    {
        _registeredTrails.Remove(trail);
    }
}
