using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;
using Array = Godot.Collections.Array;

public class Map : Spatial
{
    [Export] private NodePath _cameraPath;
    [Export] private List<NodePath> _worldNodePaths;
    [Export] private List<string> _worldNames;
    [Export] private SpatialMaterial _worldMat;
    [Export] private NodePath _cityContainerPath;
    [Export] private PackedScene _cityMarkerScene;
    
    private List<MeshInstance> _worldMeshes = new List<MeshInstance>();
    private Camera _camera;
    private Spatial _cityContainer;
    private List<Area> _cities = new List<Area>();
    private List<Tooltip> _cityTooltips = new List<Tooltip>();
    
    public override void _Ready()
    {
        base._Ready();

        for (int i = 0; i < _worldNodePaths.Count; i++)
        {
            NodePath t = _worldNodePaths[i];
            MeshInstance mesh = GetNode<MeshInstance>(t);
            mesh.SetSurfaceMaterial(0, _worldMat.Duplicate() as SpatialMaterial);
            _worldMeshes.Add(mesh);
            StaticBody staticBody = mesh.GetNode<StaticBody>("StaticBody");
            //staticBody.Connect("mouse_entered", this, nameof(_OnContinentMouseEntered), new Array {i});
            //staticBody.Connect("mouse_exited", this, nameof(_OnContinentMouseExited), new Array {i});
        }

        _cityContainer = GetNode<Spatial>(_cityContainerPath);
        Array array = _cityContainer.GetChildren();
        for (int i = 0; i < array.Count; i++)
        {
            Area city = array[i] as Area;
            Tooltip tooltip = Resources.Instance.Tooltip.Instance<Tooltip>();
            MeshInstance cityMarker = _cityMarkerScene.Instance<MeshInstance>();
            tooltip.WorldPosition = city.GlobalTransform.origin;
            city.AddChild(tooltip);
            city.AddChild(cityMarker);
            _cities.Add(city);
            _cityTooltips.Add(tooltip);
            city.Connect("mouse_entered", this, nameof(_OnCityMouseEntered), new Array {i});
            city.Connect("mouse_exited", this, nameof(_OnCityMouseExited), new Array {i});

            tooltip.Text = Database.Cities.FindEntry(city.Name).FindProperty<string>("DisplayName");
        }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
    }

    public void _OnContinentMouseEntered(int val)
    {
        SpatialMaterial mat = _worldMeshes[val].GetActiveMaterial(0) as SpatialMaterial;
        mat.AlbedoColor = Colors.Red;
    }
    
    public void _OnContinentMouseExited(int val)
    {
        SpatialMaterial mat = _worldMeshes[val].GetActiveMaterial(0) as SpatialMaterial;
        mat.AlbedoColor = Colors.White;
    }

    public void _OnCityMouseEntered(int val)
    {
        _cityTooltips[val].Showing = true;

        PlayerData.Instance.Level += 1;
        SaveManager.Instance.DoSave();
        
        GD.Print(PlayerData.Instance.Level);
    }
    
    public void _OnCityMouseExited(int val)
    {
        _cityTooltips[val].Showing = false;
    }
}
