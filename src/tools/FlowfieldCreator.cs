using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using GodotOnReady.Attributes;
using ImGuiNET;
using Array = Godot.Collections.Array;
using Color = Godot.Color;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

public partial class FlowFieldCreator : Spatial
{
    [OnReadyGet] private MeshInstance _mesh;
    [OnReadyGet] private Camera _camera;
    [OnReadyGet] private FileDialog _dialogSave;
    [OnReadyGet] private FileDialog _dialogLoad;
    
    private Vector2 _fieldSize = new(64, 64);
    private float _cellSize = 14.0f;
    private float _brushSize = 9.0f;
    
    private Vector2[,] _vectors;

    private bool _wasMousePressed;
    private Vector2 _mousePosLast;
    private Vector2 _mouseMoveVector;
    private bool _saving;
    private int _brushMode;
    
    private Vector3[] _vertList = new Vector3[50000];
    private Color[] _colList = new Color[50000];
    private int[] _indexList = new int[100000];

    [OnReady] private void Ready()
    {
        base._Ready();

        _vectors = new Vector2[(int)_fieldSize.X, (int)_fieldSize.Y];
        
        Vector2 size = GetViewport().Size.ToNumerics();
        _cellSize = size.Y / _fieldSize.Y;
        
        _camera.Size = size.Y;
        _camera.GlobalTransform = new Transform(_camera.GlobalTransform.basis, new Vector3(_fieldSize.X * _cellSize * 0.5f, 10.0f, size.Y * 0.5f).ToGodot());
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        DrawVectors();
    }

    private Vector3 MousePos()
    {
        Vector3 mousePos3D = _camera.ProjectPosition(GetViewport().GetMousePosition(), 0.0f).ToNumerics();
        return new Vector3(mousePos3D.X, 0.0f, mousePos3D.Z);
    }
    
    public override void _Input(InputEvent evt)
    {
        if (ImGuiGD.ProcessInput(evt))
        {
            GetTree().SetInputAsHandled();
            return;
        }
        
        // process vectors
        Vector2 mousePos = MousePos().To2D();
        if (Input.IsActionPressed("flowfield_tool_mouse") && !_saving)
        {
            if (!_wasMousePressed)
            {
                _mousePosLast = mousePos;
                _wasMousePressed = true;
            }
            else
            {
                float smoothing = 0.25f;
                _mouseMoveVector = _mouseMoveVector * (1.0f - smoothing) + Vector2.Normalize(mousePos - _mousePosLast) * smoothing;
                
                // set vector for all cells covered by brush.
                Vector2 mC = mousePos / _cellSize;
                for (int x = (int) (mC.X - _brushSize * 0.5f); x < mC.X + _brushSize * 0.5f; x++)
                {
                    for (int y = (int) (mC.Y - _brushSize * 0.5f); y < mC.Y + _brushSize * 0.5f; y++)
                    {
                        if (x < 0 || x >= _fieldSize.X ||
                            y < 0 || y >= _fieldSize.Y)
                            continue;
                        
                        if ((new Vector2(x, y) - mousePos / _cellSize).Length() > _brushSize * 0.5f)
                            continue;
                        
                        Vector2 vector = Vector2.Zero;
                        Vector2 cellPos = new Vector2(x, y) * _cellSize;
                        switch (_brushMode)
                        {
                            case 0:
                                vector = _mouseMoveVector;
                                break;
                            case 1:
                                vector = - Vector2.Normalize(cellPos - _fieldSize * _cellSize * 0.5f);
                                break;
                            case 2:
                                vector =  Vector2.Normalize(cellPos - _fieldSize * _cellSize * 0.5f);
                                break;
                        }
                        _vectors[x, y] =  Vector2.Normalize(vector);
                    }
                }
                _mousePosLast = mousePos;
            }
        }
        else
        {
            _wasMousePressed = false;
        }
    }

    private void FillFromCentre()
    {
        for (int x = 0; x < _fieldSize.X; x++)
        {
            for (int y = 0; y < _fieldSize.Y; y++)
            {
                Vector2 cellPos = new Vector2(x, y) * _cellSize;
                Vector2 vector = Vector2.Normalize(cellPos - _fieldSize * _cellSize * 0.5f);
                _vectors[x, y] = Vector2.Normalize(vector);
            }
        }
    }

    private void FillToCentre()
    {
        for (int x = 0; x < _fieldSize.X; x++)
        {
            for (int y = 0; y < _fieldSize.Y; y++)
            {
                Vector2 cellPos = new Vector2(x, y) * _cellSize;
                Vector2 vector = -Vector2.Normalize(cellPos - _fieldSize * _cellSize * 0.5f);
                _vectors[x, y] = Vector2.Normalize(vector);
            }
        }
    }

    private void DrawVectors()
    {
        ArrayMesh outMesh = new();
        int v = 0, i = 0;
        
        // grid
        Color gridCol = Color.Color8(150, 150, 150);
        Vector3 p1 = new Vector3(0.0f, 0.0f, 0.0f) + Vector3.UnitY;
        Vector3 p2 = new Vector3(0.0f, 0.0f, _fieldSize.Y * _cellSize) + Vector3.UnitY;
        Vector3 p3 = new Vector3(_fieldSize.X * _cellSize, 0.0f, _fieldSize.Y * _cellSize) + Vector3.UnitY;
        Vector3 p4 = new Vector3(_fieldSize.X * _cellSize, 0.0f, 0.0f) + Vector3.UnitY;
        Utils.Line(p1, p2, gridCol, ref v, ref i, _vertList, _colList, _indexList);
        Utils.Line(p2, p3, gridCol, ref v, ref i, _vertList, _colList, _indexList);
        Utils.Line(p3, p4, gridCol, ref v, ref i, _vertList, _colList, _indexList);
        Utils.Line(p4, p1, gridCol, ref v, ref i, _vertList, _colList, _indexList);
        
        // flow field
        for (int x = 0; x < _fieldSize.X; x++)
        {
            for (int y = 0; y < _fieldSize.Y; y++)
            {
                gridCol = Color.Color8(30, 30, 30);
                p1 = new Vector3(x - 0.5f, 0.0f, y - 0.5f) * _cellSize;
                p2 = new Vector3(x + 0.5f, 0.0f, y - 0.5f) * _cellSize;
                p3 = new Vector3(x - 0.5f, 0.0f, y + 0.5f) * _cellSize;
                Utils.Line(p1, p2, gridCol, ref v, ref i, _vertList, _colList, _indexList);
                Utils.Line(p1, p3, gridCol, ref v, ref i, _vertList, _colList, _indexList);
                
                if (_vectors[x, y] == Vector2.Zero)
                    continue;
                
                Vector2 dir = _vectors[x, y];
                Color col = new(
                    Mathf.Lerp(0.0f, 1.0f, dir.X * 0.5f + 0.5f),
                    Mathf.Lerp(0.0f, 1.0f, dir.Y * 0.5f + 0.5f),
                    Mathf.Lerp(0.0f, 1.0f, 1.0f - (dir.Y * 0.5f + 0.5f)));
                Vector3 pos = new Vector3(x + 0.5f, 0.0f, y + 0.5f) * _cellSize + Vector3.UnitY;
                Vector3 end = pos + dir.To3D() * _cellSize;
                Utils.Line(pos, end, col, ref v, ref i, _vertList, _colList, _indexList);
                Utils.Circle(pos, 3, 1.0f, Color.Color8(225, 225, 225),  ref v, ref i, _vertList, _colList, _indexList);
            }
        }
        
        // brush
        Color brushCol = Color.Color8(200, 200, 200);
        Utils.Circle(MousePos() + Vector3.UnitY, 32, _brushSize * _cellSize * 0.5f, brushCol, ref v, ref i, _vertList, _colList, _indexList);
        
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
        
        if (v != 0)
        {
            outMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
        }

        _mesh.Mesh = outMesh;
    }

    private void SaveFlowField()
    {
        _saving = true;
        _dialogSave.PopupCentered();
        _dialogSave.Connect("file_selected", this, nameof(_on_FileDialogSave_file_selected));
    }
    
    private void LoadFlowField()
    {
        _saving = true;
        _dialogLoad.PopupCentered();
        _dialogLoad.Connect("file_selected", this, nameof(_on_FileDialogLoad_file_selected));
    }

    public void _on_FileDialogSave_file_selected(string path)
    {
        CSharpScript script = ResourceLoader.Load<CSharpScript>("res://src/tools/FlowFieldResource.cs");
        FlowFieldResource res = script.New() as FlowFieldResource;
        res.X = (int) _fieldSize.X;
        res.Y = (int) _fieldSize.Y;
        res.Vectors = new Vector2[res.X * res.Y];
        for (int x = 0; x < res.X; x++)
        {
            for (int y = 0; y < res.Y; y++)
            {
                res.Vectors[y * res.X + x] = _vectors[x,y];
            }
        }
        string ext = path.Contains(".res") ? "" : ".res";
        GD.Print(ResourceSaver.Save($"{path}{ext}", res));
        
        _saving = false;
    }
    
    public void _on_FileDialogLoad_file_selected(string path)
    {
        FlowFieldResource res = ResourceLoader.Load<FlowFieldResource>(path);
        _fieldSize = new Vector2(res.X, res.Y);
        _vectors = new Vector2[res.X, res.Y];
        for (int x = 0; x < res.X; x++)
        {
            for (int y = 0; y < res.Y; y++)
            {
                _vectors[x, y] = res.VectorAt(x, y);
            }
        }
        
        _saving = false;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        DebugImGui.DrawImGui += _OnImGuiLayout;
        DebugImGui.ManualInputHandling = true;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        DebugImGui.DrawImGui -= _OnImGuiLayout;
        DebugImGui.ManualInputHandling = false;
    }

    private void _OnImGuiLayout()
    {
        if (ImGui.BeginTabItem("FlowField"))
        {
            ImGui.Text("Modify");
            ImGui.SliderFloat("Brush size", ref _brushSize, 1.0f, 16.0f);
            ImGui.Text("Brush Mode:");
            ImGui.RadioButton("Stroke", ref _brushMode, 0);
            
            ImGui.RadioButton("To Centre", ref _brushMode, 1); ImGui.SameLine();
            if (ImGui.Button("Fill To Centre"))
            {
                FillToCentre();
            }
            
            ImGui.RadioButton("From Centre", ref _brushMode, 2); ImGui.SameLine();
            if (ImGui.Button("Fill From Centre"))
            {
                FillFromCentre();
            }
            
            if (ImGui.Button("Clear"))
            {
                _vectors = new Vector2[(int) _fieldSize.X, (int) _fieldSize.Y];
            }
            ImGui.Text("File");
            if (ImGui.Button("Open"))
            {
                LoadFlowField();
            }
            if (ImGui.Button("Save As"))
            {
                SaveFlowField();
            }
            ImGui.Spacing();
            if (ImGui.Button("Test"))
            {
                GetTree().ChangeScene("res://scenes/debug/BoidTestbed.tscn");
            }
            ImGui.EndTabItem();
        }
    }
}
