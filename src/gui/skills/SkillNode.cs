using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using GodotOnReady.Attributes;

[Tool]
public class SkillNode : Button
{
    [Export] public bool IsRoot = false;
    [Export] public string AllyType; // TODO: this should reference an ally boid Resource not be a string.
    [Export] public SkillNodeResource Skill;
    [Export] public List<NodePath> Connections = new();
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
    
        Connect("pressed", this, nameof(_OnPressed));
    
         _activeIndicator = GetNode<Control>(_activeIndicatorPath);
         _root = GetNode<SkillNode>(_rootPath);

         _active |= IsRoot;
         
         Refresh();
    }

    private void Refresh()
    {
        Icon = Skill.Icon;
        RectMinSize = Skill.Major ? SizeMajor : SizeMinor;
    }
    
    public override void _Process(float delta)
    {
        base._Process(delta);
    
        if (Engine.EditorHint && Skill != null)
        {
            Icon = Skill.Icon;
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

        List<SkillNodeResource> skills = new();
        int children = GetParent().GetChildCount();
        for (int i = 0; i < children; i++)
        {
            Node child = GetParent().GetChild(i);
            if (child is SkillNode skillNode && skillNode.Active)
            {
                skills.Add(skillNode.Skill);
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
