using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Godot.Collections;

[Tool]
public partial class SkillNode : Button
{
    [Export] public bool IsRoot;
    [Export] public ResourceBoidAlly AllyType;
    [Export] public ResourceSkillNode ResourceSkill;
    [Export] public Array<NodePath> Connections = new();
    [Export] public Vector2 SizeMinor;
    [Export] public Vector2 SizeMajor;

    [Export] private NodePath _activeIndicatorPath;
    [Export] private NodePath _rootPath;
    
    public bool Active => _active;
    
    private SkillNode _root;
    private Control _activeIndicator;
    private bool _active = false;
    
    public override void _Ready()
    {
        base._Ready();
    
        Connect("pressed", new Callable(this, nameof(_OnPressed)));
    
         _activeIndicator = GetNode<Control>(_activeIndicatorPath);
         _root = GetNode<SkillNode>(_rootPath);

         _active |= IsRoot;
         
         Refresh();
    }

    private void Refresh()
    {
        if (!IsRoot)
        {
            Icon = ResourceSkill.Icon;
            CustomMinimumSize = ResourceSkill.Major ? SizeMajor : SizeMinor; 
        }
    }
    
    public override void _Process(double delta)
    {
        base._Process(delta);
    
        if (Engine.IsEditorHint() && ResourceSkill != null)
        {
            Icon = ResourceSkill.Icon;
            Refresh();
        }
    }

    private bool CanBecomeActive()
    {
        if (IsRoot)
            return true;
        
        bool hasActiveConnection = false;
        foreach (NodePath connection in Connections)
        {
            SkillNode skillNode = GetNode<SkillNode>(connection);
            if (skillNode.Active || skillNode.IsRoot)
            {
                hasActiveConnection = true;
                break;
            }
        }
    
        return hasActiveConnection;
    }

    private bool Propagate(SkillNode target, List<SkillNode> visited)
    {
        if (this == target)
            return true;
    
        if (!Active)
            return false;
    
        if (visited.Contains(this))
            return false;
        
        visited.Add(this);
        bool found = false;
        foreach (NodePath path in Connections)
        {
            SkillNode node = GetNode<SkillNode>(path);
            found |= node.Propagate(target, visited);
        }
    
        return found;
    }

    private bool CanRootSeeThisNode()
    {
        if (IsRoot)
            return true;
        
        List<SkillNode> visited = new();
        return _root.Propagate(this, visited);
    }

    private void PropagateDeactivation()
    {
        foreach (NodePath path in Connections)
        {
            SkillNode node = GetNode<SkillNode>(path);
            if (!node.Active)
                continue;
            
            if (node.IsRoot)
                continue;

            bool rootCanSee = node.CanRootSeeThisNode();
            if (!rootCanSee)
            {
                node.TrySetActive(false);
            }
        }
    }

    private void TrySetActive(bool active)
    {
        if (active && CanBecomeActive() && SaveDataPlayer.ConsumeSkillPoint())
        {
            _active = active;
            _root.UpdatePlayerDataFromRoot();
        }
        else if (!active)
        {
            _active = active;
            _root.UpdatePlayerDataFromRoot();
            SaveDataPlayer.SkillPoints++;
            PropagateDeactivation();
        }
        _activeIndicator.Visible = Active;
    }

    private void UpdatePlayerDataFromRoot()
    {
        Debug.Assert(IsRoot, "Only call this from root node.");

        List<ResourceSkillNode> skills = new();
        int children = GetParent().GetChildCount();
        for (int i = 0; i < children; i++)
        {
            Node child = GetParent().GetChild(i);
            if (child is SkillNode skillNode && skillNode.Active)
            {
                skills.Add(skillNode.ResourceSkill);
            }
        }

        if (skills.Count > 0)
        {
            SaveDataPlayer.UpdateActiveSkills(AllyType, skills);
            SaveManager.DoSave();
        }
    }
    
    public void _OnPressed()
    {
        if (IsRoot)
            return;
        
        TrySetActive(!Active);
    }
}
