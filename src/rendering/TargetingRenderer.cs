using Godot;
using System;
using System.Diagnostics;
using Array = Godot.Collections.Array;
using Vector3 = System.Numerics.Vector3;

public class TargetingRenderer : MeshInstance
{
    public static TargetingRenderer Instance;

    [Export] private float _lineSegmentSize = 10.0f;
    [Export] private float _lineSpeed = 10.0f;
    
    private Godot.Vector3[] _vertList = new Godot.Vector3[10000];
    private Color[] _colList = new Color[10000];
    private int[] _indexList = new int[20000];

    private float _time;

    public override void _EnterTree()
    {
        base._EnterTree();

        Instance = this;

        CastShadow = ShadowCastingSetting.Off;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        ArrayMesh outMesh = new();
        Mesh = outMesh;

        if (StateMachine_Game.CurrentState != StateMachine_Game.States.TacticalPause)
            return;
        
        int v = 0;
        int i = 0;

        _time += delta * _lineSpeed;
        
        foreach (BoidAllyBase ally in BoidFactory.Instance.AllyBoids)
        {
            if (ally.CurrentTargetType == BoidBase.TargetType.Enemy || ally.CurrentTargetType == BoidBase.TargetType.Position)
            {
                LineBetween(ally.GlobalPosition.ToNumerics().To3D(), ally.TargetPos.ToNumerics().To3D(), Colors.Green, ref v, ref i);
            }
        }
        
        foreach (BoidEnemyBase enemy in BoidFactory.Instance.EnemyBoids)
        {
            if (enemy.CurrentTargetType == BoidBase.TargetType.Enemy || enemy.CurrentTargetType == BoidBase.TargetType.Position)
            {
                LineBetween(enemy.GlobalPosition.ToNumerics().To3D(), enemy.TargetPos.ToNumerics().To3D(), Colors.Red, ref v, ref i);
            }
        }

        if (v == 0)
            return;
        
        Debug.Assert(v < _vertList.Length, "v < _vertList.Length");
        Debug.Assert(v < _colList.Length, "v < _colList.Length");
        Debug.Assert(i < _indexList.Length, "i < _indexList.Length");

        Span<Godot.Vector3> verts = _vertList.AsSpan(0, v);
        Span<Color> colours = _colList.AsSpan(0, v);
        Span<int> indices = _indexList.AsSpan(0, i);
        
        Array arrays = new();
        arrays.Resize((int) ArrayMesh.ArrayType.Max);
        arrays[(int) ArrayMesh.ArrayType.Vertex] = verts.ToArray();
        arrays[(int) ArrayMesh.ArrayType.Color] = colours.ToArray();
        arrays[(int) ArrayMesh.ArrayType.Index] = indices.ToArray();

        outMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
    }

    private void LineBetween(Vector3 from, Vector3 to, Color col, ref int v, ref int i)
    {
        Vector3 start = from;
        Vector3 end = to;
        Vector3 dir = Vector3.Normalize(end - start);
        float t = _time % (_lineSegmentSize * 1.5f);
        float dist = (end - start).Length();

        if (t > _lineSegmentSize * 0.5f)
        {
            Utils.Line(start, start + dir * (t - _lineSegmentSize * 0.5f), col, ref v, ref i, _vertList, _colList, _indexList);
        }
                
        while (t < dist)
        {
            Utils.Line(start + dir * t, start + dir * Mathf.Min(dist - _lineSegmentSize, t + _lineSegmentSize), col, ref v, ref i, _vertList, _colList, _indexList);
            t += _lineSegmentSize * 1.5f;
        }
    }
}
