using System.Collections.Generic;
using Godot;
using GodotOnReady.Attributes;

[Tool]
public class SkillNode : Button
{
    [Export] public bool IsRoot = false;
    [Export] public SkillNodeResource Skill;
    [Export] public List<NodePath> Connections = new();
    
    [Export] private NodePath _activeIndicatorPath;
    [Export] private NodePath _rootPath;
    
    public bool Active => _active;
    
    private SkillNode _root;
    private Control _activeIndicator;
    private bool _active = false;
    
    public override void _Ready()
    {
        base._Ready();
    
        Connect("pressed", this, nameof(_OnPressed));
    
         _activeIndicator = GetNode<Control>(_activeIndicatorPath);
         _root = GetNode<SkillNode>(_rootPath);

         _active |= IsRoot;
    }
    
    public override void _Process(float delta)
    {
        base._Process(delta);
    
        if (Engine.EditorHint && Skill != null)
        {
            Icon = Skill.Icon;
        }
    }

    // private bool CanBeActive()
    // {
    //     if (IsRoot)
    //         return true;
    //     
    //     bool hasActiveConnection = false;
    //     foreach (NodePath connection in Connections)
    //     {
    //         SkillNode skillNode = GetNode<SkillNode>(connection);
    //         if (skillNode.Active || skillNode.IsRoot)
    //         {
    //             hasActiveConnection = true;
    //             break;
    //         }
    //     }
    //
    //     return hasActiveConnection;
    // }
    //
    // private bool CanRootSeeThisNode()
    // {
    //     List<SkillNode> visited = new();
    //     return _root.Propagate(this, visited);
    // }
    //
    // private bool Propagate(SkillNode target, List<SkillNode> visited)
    // {
    //     if (this == target)
    //         return true;
    //
    //     if (!Active)
    //         return false;
    //
    //     if (visited.Contains(this))
    //         return false;
    //     
    //     visited.Add(this);
    //     bool found = false;
    //     foreach (NodePath path in _root.Connections)
    //     {
    //         SkillNode node = GetNode<SkillNode>(path);
    //         found |= node.Propagate(target, visited);
    //     }
    //
    //     return found;
    // }
    //
    private void TrySetActive(bool active)
    {
        _active = active;
        _activeIndicator.Visible = Active;
    }
    
    public void _OnPressed()
    {
        if (IsRoot)
            return;
        
        TrySetActive(!Active);
    }
}
