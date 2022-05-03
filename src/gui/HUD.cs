using System.Collections.Generic;
using Godot;
using Godot.Collections;

public class HUD : CanvasLayer
{
    [Export] private PackedScene FloatingScoreScene;
    [Export] public float ScoreCountTime = 5.0f;
    [Export] public int StarThreshold1 = 1000;
    [Export] public int StarThreshold2 = 3000;
    [Export] public int StarThreshold3 = 8000;
    [Export] public int StarThreshold4 = 15000;
    [Export] public int StarThreshold5 = 25000;
    [Export] public Array<NodePath> PerkButtonPaths;
    [Export] public NodePath MenuButtonPath;
    
    private Control _perks;
    public Label _perkLabel1;
    public Label _perkLabel2;
    public Label _perkLabel3;
    public Label _perkLabel1Desc;
    public Label _perkLabel2Desc;
    public Label _perkLabel3Desc;

    public Control _perkBG1;
    public Control _perkOutline1;
    public Control _perkBG2;
    public Control _perkOutline2;
    public Control _perkBG3;
    public Control _perkOutline3;

    public Control _score;
    public Control _scoreMulti;
    public Label _wave;
    public Control _subWave;

    public Control _loseScreen;
    public Button _loseButton;
    public Label _loseLabel;
    public Control _loseOutline;
    public Control _loseBG;

    public Control _star1;
    public Control _star2;
    public Control _star3;
    public Control _star4;
    public Control _star5;

    public Label _scoreLabel;
    public Button _menuButton;

    

    private List<Perk> _perkSelections = new List<Perk>();
    private List<Button> _perkButtons = new List<Button>();
    private List<Button> _buttons = new List<Button>();
    private Button _buttonSelected;
    public float _animTime;
    private Game _game = null;
    public float _waveShowTimer;
    public int _resultsScore;
    public float _scoreCountTimer;

    [Signal]
    public delegate void onPerkSelected();

    private enum InputType
    {
        ButtonUp,
        ButtonDown,
        MouseEnter,
        MouseExit
    }

    public override void _Ready()
    {
        _perks = (Control) GetNode("Perks");
        _perkLabel1 = GetNode<Label>("Perks/VBoxContainer/Perk1/Outline/Label");
        _perkLabel2 = GetNode<Label>("Perks/VBoxContainer/Perk2/Outline/Label");
        _perkLabel3 = GetNode<Label>("Perks/VBoxContainer/Perk3/Outline/Label");
        _perkLabel1Desc = GetNode<Label>("Perks/VBoxContainer/Perk1/Outline/Desc");
        _perkLabel2Desc = GetNode<Label>("Perks/VBoxContainer/Perk2/Outline/Desc");
        _perkLabel3Desc = GetNode<Label>("Perks/VBoxContainer/Perk3/Outline/Desc");

        _perkBG1 = (Control) GetNode("Perks/VBoxContainer/Perk1/TextureRect");
        _perkOutline1 = (Control) GetNode("Perks/VBoxContainer/Perk1/Outline");
        _perkBG2 = (Control) GetNode("Perks/VBoxContainer/Perk2/TextureRect");
        _perkOutline2 = (Control) GetNode("Perks/VBoxContainer/Perk2/Outline");
        _perkBG3 = (Control) GetNode("Perks/VBoxContainer/Perk3/TextureRect");
        _perkOutline3 = (Control) GetNode("Perks/VBoxContainer/Perk3/Outline");

        _score = (Control) GetNode("Score");
        _scoreMulti = (Control) GetNode("ScoreMulti");
        _wave = GetNode<Label>("Wave");
        _subWave = (Control) GetNode("Subwave");

        _loseScreen = (Control) GetNode("LoseScreen");
        _loseButton = GetNode<Button>("LoseScreen/VBoxContainer/MenuButton");
        _loseLabel = GetNode<Label>("LoseScreen/VBoxContainer/MenuButton/Outline/Label");
        _loseOutline = (Control) GetNode("LoseScreen/VBoxContainer/MenuButton/Outline");
        _loseBG = (Control) GetNode("LoseScreen/VBoxContainer/MenuButton/TextureRect");

        _star1 = (Control) GetNode("LoseScreen/Stars/Star/StarInner");
        _star2 = (Control) GetNode("LoseScreen/Stars/Star2/StarInner");
        _star3 = (Control) GetNode("LoseScreen/Stars/Star3/StarInner");
        _star4 = (Control) GetNode("LoseScreen/Stars/Star4/StarInner");
        _star5 = (Control) GetNode("LoseScreen/Stars/Star5/StarInner");

        _perkButtons.Add(GetNode<Button>("Perks/VBoxContainer/Perk1"));
        _perkButtons.Add(GetNode<Button>("Perks/VBoxContainer/Perk2"));
        _perkButtons.Add(GetNode<Button>("Perks/VBoxContainer/Perk3"));
        for (int i = 0; i < _perkButtons.Count; i++)
        {
            ConnectButton(_perkButtons[i], i);
        }
        
        _menuButton = GetNode<Button>(MenuButtonPath);
        ConnectButton(_menuButton, 0);

        _scoreLabel = GetNode<Label>("LoseScreen/VBoxContainer/Score");

        Color fontCol = ColourManager.Instance.White;
        _perkLabel1.AddColorOverride("font_color", fontCol);
        _perkLabel2.AddColorOverride("font_color", fontCol);
        _perkLabel3.AddColorOverride("font_color", fontCol);
        _perkLabel1Desc.AddColorOverride("font_color", fontCol);
        _perkLabel2Desc.AddColorOverride("font_color", fontCol);
        _perkLabel3Desc.AddColorOverride("font_color", fontCol);
        _loseLabel.AddColorOverride("font_color", fontCol);

        fontCol = ColourManager.Instance.Secondary;
        _score.AddColorOverride("font_color", fontCol);
        _scoreMulti.AddColorOverride("font_color", fontCol);
        _wave.AddColorOverride("font_color", ColourManager.Instance.White);
        _subWave.AddColorOverride("font_color", fontCol);

        Color bgCol = ColourManager.Instance.Secondary;
        Color outlineCol = ColourManager.Instance.Tertiary;
        _perkBG1.Modulate = bgCol;
        _perkOutline1.Modulate = outlineCol;
        _perkBG2.Modulate = bgCol;
        _perkOutline2.Modulate = outlineCol;
        _perkBG3.Modulate = bgCol;
        _perkOutline3.Modulate = outlineCol;

        _loseBG.Modulate = bgCol;
        _loseOutline.Modulate = outlineCol;

        _buttons.Add(_loseButton);
    }

    private void ConnectButton(Button button, int value)
    {
        button.Connect("button_up", this, nameof(OnButtonInput),
            new Array {InputType.ButtonUp, button, value});
        button.Connect("button_down", this, nameof(OnButtonInput),
            new Array {InputType.ButtonDown, button, value});
        button.Connect("mouse_entered", this, nameof(OnButtonInput),
            new Array {InputType.MouseEnter, button, value});
        button.Connect("mouse_exited", this, nameof(OnButtonInput),
            new Array {InputType.MouseExit, button, value});
    }

    private void OnButtonInput(InputType input, Button button, int value)
    {
        switch (input)
        {
            case InputType.ButtonUp:
            {
                if (button == _menuButton)
                {
                    GetTree().Paused = false;
                    GetTree().ReloadCurrentScene();
                    break;
                }

                _perks.Visible = false;
                EmitSignal("onPerkSelected", _perkSelections[value]);
                GetNode<AudioStreamPlayer>("PerkSelect").Play();
                break;
            }
            case InputType.ButtonDown:
            {
                break;
            }
            case InputType.MouseEnter:
            {
                if (button == _menuButton)
                {
                    _buttonSelected = _loseButton;
                }

                _animTime = 0.0f;
                _buttonSelected = _perkButtons[value];
                break;
            }
            case InputType.MouseExit:
            {
                if (button == _menuButton)
                {
                    _buttonSelected = null;
                }

                _buttonSelected = null;
                break;
            }
            default:
                break;
        }
    }
    
    public override void _Process(float delta)
    {
        _animTime += delta;
        foreach (Button button in _buttons)
        {
            TextureRect bg = button.GetNode<TextureRect>("TextureRect");
            TextureRect outline = button.GetNode<TextureRect>("Outline");
            if (button == _buttonSelected)
            {
                float time = _animTime / 0.1f;
                float s = 1.0f + Mathf.Cos(time) * 0.05f;
                float r = Mathf.Sin(time) * 0.5f;
                bg.RectPivotOffset = bg.RectSize / 2.0f;
                bg.RectScale = new Vector2(s, s);
                bg.RectRotation = r;
                outline.RectPivotOffset = outline.RectSize / 2.0f;
                outline.RectScale = new Vector2(s, s);
                outline.RectRotation = r;
            }
            else
            {
                bg.RectScale = new Vector2(1.0f, 1.0f);
                bg.RectRotation = 0.0f;
                outline.RectScale = new Vector2(1.0f, 1.0f);
                outline.RectRotation = 0.0f;
            }
        }

        if (_loseScreen.Visible)
        {
            float time = _animTime / 0.1f;
            float s = 1.0f + Mathf.Cos(time) * 0.25f;
            float r = Mathf.Sin(time) * 2.0f;
            //_scoreLabel.rect_pivot_offset = _scoreLabel.rect_size / 2.0;
            //_scoreLabel.rect_rotation = r;
            //_scoreLabel.rect_scale = new Vector2(s, s);

            _scoreCountTimer -= delta;
            float score = Mathf.Clamp(Mathf.Lerp(_resultsScore, 0, _scoreCountTimer / ScoreCountTime), 0, _resultsScore);
            _scoreLabel.Text = $"{score}";

            if (!_star1.Visible && score > StarThreshold1)
            {
                GetNode<AudioStreamPlayer>("PerkSelect").Play();
                _star1.Visible = true;
            }

            if (!_star2.Visible && score > StarThreshold2)
            {
                GetNode<AudioStreamPlayer>("PerkSelect").Play();
                _star2.Visible = true;
            }

            if (!_star3.Visible && score > StarThreshold3)
            {
                GetNode<AudioStreamPlayer>("PerkSelect").Play();
                _star3.Visible = true;
            }

            if (!_star4.Visible && score > StarThreshold4)
            {
                GetNode<AudioStreamPlayer>("PerkSelect").Play();
                _star4.Visible = true;
            }

            if (!_star5.Visible && score > StarThreshold5)
            {
                GetNode<AudioStreamPlayer>("PerkSelect").Play();
                _star5.Visible = true;
            }
        }

        _waveShowTimer -= delta;
        if (_waveShowTimer < 0.0)
        {
            _wave.Visible = false;
        }
    }
    
    public void ShowFloatingScore(int score, Vector2 worldPos, Game game)
    {
        FloatingScore floatingScore = FloatingScoreScene.Instance<FloatingScore>();
        floatingScore.SetScore(score);
        floatingScore.worldPos = worldPos;
        game.AddChild(floatingScore);
    }

    public void SetScore(int score, float multi, int threshold, bool isMax)
    {
        if (isMax)
        {
            GetNode<Label>("ScoreMulti").Text = $"MAX x{multi:F1}";
        }
        else
        {
            GetNode<Label>("ScoreMulti").Text = $"x{multi:F1}";
        }

        GetNode<Label>("Score").Text = $"{score:6D}";
        _resultsScore = score;
    }

    public void SetWave(int wave, int subwave)
    {
        _wave.Text = $"Wave {wave + 1})";
        _waveShowTimer = 2.0f;
        _wave.Visible = true;
        //_subWave.text = "SubWave %d" % subwave	;
    }
    
    public void ShowPerks(List<Perk> perks)
    {
        _perks.Visible = true;
        _perkSelections = perks;
        _perkLabel1.Text = _perkSelections[0].displayName;
        _perkLabel2.Text = _perkSelections[1].displayName;
        _perkLabel3.Text = _perkSelections[2].displayName;
        _perkLabel1Desc.Text = _perkSelections[0].displayDesc;
        _perkLabel2Desc.Text = _perkSelections[1].displayDesc;
        _perkLabel3Desc.Text = _perkSelections[2].displayDesc;
    }

    public void ShowLoseScreen()
    {
        _loseScreen.Visible = true;
        _scoreLabel.Text = $"{000000}";
        _scoreCountTimer = ScoreCountTime;
    }
}
