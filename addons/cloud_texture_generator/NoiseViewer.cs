#if TOOLS
using Godot;
using System;
using Array = Godot.Collections.Array;

[Tool]
public class NoiseViewer : VBoxContainer
{
    [Export] private NodePath _noisePreviewLabelPath;
    [Export] private NodePath _noisePreviewPath;
    [Export] private NodePath _noisePreviewSlicePath;
    [Export] private NodePath _noisePreviewSliceLabelPath;
    [Export] private NodePath _noisePreviewChannelAPath;
    [Export] private NodePath _noisePreviewChannelBPath;
    [Export] private NodePath _noisePreviewChannelCPath;
    [Export] private NodePath _noisePreviewChannelDPath;
    [Export] private NodePath _fileDialogPath;
    
    private Label _noisePreviewLabel;
    private TextureRect _noisePreview;
    private HSlider _noisePreviewSlice;
    private Label _noisePreviewSliceLabel;
    private CheckBox _noisePreviewChannelR;
    private CheckBox _noisePreviewChannelB;
    private CheckBox _noisePreviewChannelG;
    private CheckBox _noisePreviewChannelA;
    private ShaderMaterial _noisePreviewMaterial;
    private FileDialog _fileDialog;
    
    private int _previewSlice;
    private int _previewChannel;
    private EditorResourcePicker _picker;

    public override void _Ready()
    {
        base._Ready();

        _noisePreviewLabel = GetNode<Label>(_noisePreviewLabelPath);
        _noisePreview = GetNode<TextureRect>(_noisePreviewPath);
        _noisePreviewSlice = GetNode<HSlider>(_noisePreviewSlicePath);
        _noisePreviewSliceLabel = GetNode<Label>(_noisePreviewSliceLabelPath);
        _noisePreviewChannelR = GetNode<CheckBox>(_noisePreviewChannelAPath);
        _noisePreviewChannelB = GetNode<CheckBox>(_noisePreviewChannelBPath);
        _noisePreviewChannelG = GetNode<CheckBox>(_noisePreviewChannelCPath);
        _noisePreviewChannelA = GetNode<CheckBox>(_noisePreviewChannelDPath);
        _noisePreviewMaterial = _noisePreview.Material as ShaderMaterial;
        _fileDialog = GetNode<FileDialog>(_fileDialogPath);
        
        _noisePreviewChannelR.Connect("pressed", this, nameof(_OnPreviewChannelPressed), new Array(){0});
        _noisePreviewChannelB.Connect("pressed", this, nameof(_OnPreviewChannelPressed), new Array(){1});
        _noisePreviewChannelG.Connect("pressed", this, nameof(_OnPreviewChannelPressed), new Array(){2});
        _noisePreviewChannelA.Connect("pressed", this, nameof(_OnPreviewChannelPressed), new Array(){3});
        _noisePreviewSlice.Connect("value_changed", this, nameof(_OnPreviewSliceChanged));
        
        _noisePreviewMaterial.SetShaderParam("u_mode", 0);
        
        _picker = new EditorResourcePicker();
        _picker.BaseType = "ImageTexture,Texture3D";
        _picker.Connect("resource_changed", this, nameof(_ResourceChanged));
        AddChildBelowNode(_noisePreviewLabel, _picker);
    }

    public void SetResource(Resource res)
    {
        _ResourceChanged(res);
    }
    
    private void SetTexture(ImageTexture texture)
    {
        _noisePreviewMaterial.SetShaderParam("u_noise2d", texture);
        SetTextureInternal(2, texture.GetFormat(), (int)texture.GetSize().x);
    }

    private void SetTexture(Texture3D texture)
    {
        _noisePreviewMaterial.SetShaderParam("u_noise3d", texture);
        SetTextureInternal(3, texture.GetFormat(), (int)texture.GetWidth());
    }

    private void SetTextureInternal(int dims, Image.Format format, int size)
    {
        int channels = 1;
        switch (format)
        {
            case Image.Format.R8:
                channels = 1;
                break;
            case Image.Format.Rg8:
                channels = 2;
                break;
            case Image.Format.Rgb8:
                channels = 3;
                break;
            case Image.Format.Rgba8:
                channels = 4;
                break;
        }
        
        _noisePreviewMaterial.SetShaderParam("u_mode", dims);
        _noisePreviewChannelR.Visible = true;
        _noisePreviewChannelB.Visible = channels > 1;
        _noisePreviewChannelG.Visible = channels > 2;
        _noisePreviewChannelA.Visible = channels > 3;
        _noisePreviewSliceLabel.Visible = dims == 3;
        _noisePreviewSlice.Visible = dims == 3;
        _noisePreviewSlice.MaxValue = size;
        UpdateView();
    }

    private void UpdateView()
    {
        _noisePreviewMaterial.SetShaderParam("u_channel", _previewChannel);
        _noisePreviewMaterial.SetShaderParam("u_slice", _previewSlice);
        _noisePreviewSliceLabel.Text = $"Z: {_previewSlice}";
    }

    private void _OnPreviewChannelPressed(int channel)
    {
        _previewChannel = channel;
        UpdateView();
    }
    
    private void _OnPreviewSliceChanged(float slice)
    {
        _previewSlice = (int)slice;
        UpdateView();
    }

    private void _ResourceChanged(Resource res)
    {
        switch (res)
        {
            case Texture3D texture3D:
                SetTexture(texture3D);
                break;
            case ImageTexture texture:
                SetTexture(texture);
                break;
        }
    }
}
#endif