using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using GodotOnReady.Attributes;
using Array = Godot.Collections.Array;

[Tool]
public class GeneratorToolbox : Control
{
    public enum NoiseType
    {
        Gradient,
        Cellular,
        CellularFBM,
        None,
    }
    
    [Export] private PackedScene _channelOptionsScene;
    
    [Export] private NodePath _channelTabsPath;
    [Export] private NodePath _addChannelButtonPath;
    [Export] private NodePath _removeChannelButtonPath;
    [Export] private NodePath _browseButtonPath;
    [Export] private NodePath _browseBoxPath;
    [Export] private NodePath _generateButtonPath;
    [Export] private NodePath _sizePath;
    [Export] private NodePath _dim2DPath;
    [Export] private NodePath _dim3DPath;
    [Export] private NodePath _fileDialogPath;
    [Export] private NodePath _viewerPath;

    private TabContainer _channelTabs;
    private Button _addChannelButton;
    private Button _removeChannelButton;
    private Button _browseButton;
    private LineEdit _browseBox;
    private Button _generateButton;
    private LineEdit _size;
    private CheckBox _dim2D;
    private CheckBox _dim3D;
    private FileDialog _fileDialog;
    private NoiseViewer _viewer;

    private List<ChannelOptions> _channelOptions = new List<ChannelOptions>();
    private Node _anl;

    public override void _Ready()
    {
        base._Ready();
        
        _channelTabs = GetNode<TabContainer>(_channelTabsPath);
        _addChannelButton = GetNode<Button>(_addChannelButtonPath);
        _removeChannelButton = GetNode<Button>(_removeChannelButtonPath);
        _browseButton = GetNode<Button>(_browseButtonPath);
        _browseBox = GetNode<LineEdit>(_browseBoxPath);
        _generateButton = GetNode<Button>(_generateButtonPath);
        _size = GetNode<LineEdit>(_sizePath);
        _dim2D = GetNode<CheckBox>(_dim2DPath);
        _dim3D = GetNode<CheckBox>(_dim3DPath);
        _fileDialog = GetNode<FileDialog>(_fileDialogPath);
        _viewer = GetNode<NoiseViewer>(_viewerPath);

        _addChannelButton.Connect("pressed", this, nameof(_OnAddChannel));
        _removeChannelButton.Connect("pressed", this, nameof(_OnRemoveChannel));
        _generateButton.Connect("pressed", this, nameof(_OnGenerate));
        _browseButton.Connect("pressed", this, nameof(_OnBrowse));
        
         _OnAddChannel();
    }

    private void _OnAddChannel()
    {
        int channels = _channelTabs.GetChildCount();
        if (channels >= 4)
            return;
        
        ChannelOptions channelOptions = _channelOptionsScene.Instance<ChannelOptions>();
        _channelOptions.Add(channelOptions);
        _channelTabs.AddChild(channelOptions);
        channelOptions.Name = $"{channels}";
    }
    
    private void _OnRemoveChannel()
    {
        int channels = _channelTabs.GetChildCount();
        if (channels == 0)
            return;
        
        _channelTabs.RemoveChild(_channelTabs.GetChild(channels - 1));
        _channelOptions.RemoveAt(channels - 1);
    }

    private void _OnBrowse()
    {
        _fileDialog.PopupCentered();
        _fileDialog.Connect("file_selected", this, nameof(_on_FileDialogSave_file_selected));
    }
    
    public void _on_FileDialogSave_file_selected(string path)
    {
        _browseBox.Text = path;
        _fileDialog.Disconnect("file_selected", this, nameof(_on_FileDialogSave_file_selected));
    }

    private void _OnGenerate()
    {
        if (!_browseBox.Text.Contains("res://"))
            return;
        
        DoGenerate(_browseBox.Text);
    }
    
    private void DoGenerate(string filepath)
    {
        NativeScript anlScript = GD.Load("res://gdnative/bin/anl.gdns") as NativeScript;
        Debug.Assert(anlScript != null, "anlScript != null");
        
        uint size = (uint)_size.Text.ToInt();
        int dims = _dim2D.Pressed ? 2 : 3;
        int channels = _channelOptions.Count;
    
        Stopwatch stopwatch = new();
        stopwatch.Start();
        float tick = 1.0f / Stopwatch.Frequency;
        
        _anl = anlScript.New() as Node;
        Debug.Assert(_anl != null, "anl != null");
        if (_anl == null)
            return;

        Resource noiseTex = null;
        
        float[] frequencies = new float[channels];
        NoiseType[] types = new NoiseType[channels];
        int[] octaves = new int[channels];
        float[] lacunarities = new float[channels];
        float[] amplitudes = new float[channels];
        int[] anlHandles = new int[channels];
        bool[] inverts = new bool[channels];
        for (int i = 0; i < channels; i++)
        {
            ChannelOptions options = _channelOptions[i];

            options.GetParams(out frequencies[i], out octaves[i], out lacunarities[i], out amplitudes[i], out inverts[i]);
            anlHandles[i] = -1;

            string anlMethod = $"Generate{dims}D";
            switch (options.GetNoiseType())
            {
                case NoiseType.Gradient:
                    anlMethod += "Gradient";
                    anlHandles[i] = Convert.ToInt32(_anl.Call(anlMethod, size, frequencies[i], octaves[i], DateTime.Now.Millisecond));
                    break;
                case NoiseType.Cellular:
                    anlMethod += "Cellular";
                    anlHandles[i] = Convert.ToInt32(_anl.Call(anlMethod, size, frequencies[i], DateTime.Now.Millisecond));
                    break;
                case NoiseType.CellularFBM:
                    anlMethod += "CellularFBM";
                    anlHandles[i] = Convert.ToInt32(_anl.Call(anlMethod, size, frequencies[i], octaves[i], DateTime.Now.Millisecond));
                    break;
                case NoiseType.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        Image.Format format = Image.Format.Rgba8;
        switch (channels)
        {
            case 1: format = Image.Format.R8; break;
            case 2: format = Image.Format.Rg8; break;
            case 3: format = Image.Format.Rgb8; break;
            case 4: format = Image.Format.Rgba8; break;
        }

        switch (dims)
        {
            case 2:
            {
                Image layer = NoiseLayer(size, format, channels, anlHandles, inverts, -1);
                ImageTexture texture = new();
                noiseTex = texture;
                texture.CreateFromImage(layer, (uint) (Texture.FlagsEnum.Repeat | Texture.FlagsEnum.Filter | Texture.FlagsEnum.ConvertToLinear));
                break;
            }
            case 3:
            {
                Texture3D texture3D = new Texture3D();
                noiseTex = texture3D;
                texture3D.Create(size, size, size, format, (uint) (TextureLayered.FlagsEnum.FlagFilter | TextureLayered.FlagsEnum.FlagRepeat));
                for (int z = 0; z < size; z++)
                {
                    Image layer = NoiseLayer(size, format, channels, anlHandles, inverts, z);
                    texture3D.SetLayerData(layer, z);
                    stopwatch.Restart();
                }
                break;
            }
        }
        
        GD.Print($"Generate3DGradientNoiseImage {stopwatch.ElapsedTicks * tick:F2} seconds.");
        stopwatch.Restart();

        Directory dir = new();
        
        switch (dims)
        {
            case 3:
            {
                string ext = ".tex3d";
                if (!filepath.EndsWith(ext))
                {
                    filepath += ext;
                }
                break;
            }
            case 2:
            {
                string ext = ".tex";
                if (!filepath.EndsWith(ext))
                {
                    filepath += ext;
                }

                break;
            }
        }
        dir.Remove(filepath);
        ResourceSaver.Save($"{filepath}", noiseTex);
        
        _anl.QueueFree();
        GC.Collect();
        
        _viewer.SetResource(noiseTex);
    }

    private Image NoiseLayer(uint size, Image.Format format, int channels, int[] anlHandles, bool[] inverts, int z)
    {
        Image layer = new();
        layer.Create((int) size, (int) size, true, format);
        layer.Lock();
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Color col = new();
                for (int c = 0; c < channels; c++)
                {
                    if (z == -1)
                    {
                        col[c] = Noise2D(new Vector2(x, y), anlHandles[c], inverts[c]);
                    }
                    else
                    {
                        col[c] = Noise3D(new Vector3(x, y, z), anlHandles[c], inverts[c]);
                    }
                }
                layer.SetPixel(x, y, col);
            }
        }
        layer.Unlock();
        return layer;
    }
    
    private float Noise2D(Vector2 pos, int anlHandle, bool invert)
    {
        Debug.Assert(anlHandle != -1);
        float noise = (float) Convert.ToDouble(_anl.Call("Sample2D", anlHandle, pos.x, pos.y));
        return invert ? 1.0f - noise : noise;
    }
    
    private float Noise3D(Vector3 pos, int anlHandle, bool invert)
    {
        Debug.Assert(anlHandle != -1);
        float noise = (float)Convert.ToDouble(_anl.Call("Sample3D", anlHandle, pos.x, pos.y, pos.z));
        return invert ? 1.0f - noise : noise;
    }
}
