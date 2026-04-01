using Godot;
using System;
using System.Collections.Generic;

public partial class FactoryHud : CanvasLayer
{
    private readonly Dictionary<BuildPrototypeKind, Button> _selectionButtons = new();

    private Control? _root;
    private Control? _panel;
    private MarginContainer? _chrome;
    private VBoxContainer? _body;
    private Label? _selectedLabel;
    private Label? _hoverLabel;
    private Label? _previewLabel;
    private Label? _rotationLabel;
    private Label? _deliveryLabel;
    private Label? _noteLabel;
    private Label? _profilerLabel;

    public event Action<BuildPrototypeKind>? SelectionChanged;

    public string ProfilerText => _profilerLabel?.Text ?? string.Empty;

    public override void _Ready()
    {
        Name = "FactoryHud";

        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(root);
        _root = root;

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.AddChild(panel);
        _panel = panel;

        var chrome = new MarginContainer();
        chrome.MouseFilter = Control.MouseFilterEnum.Ignore;
        chrome.AddThemeConstantOverride("margin_left", 12);
        chrome.AddThemeConstantOverride("margin_top", 10);
        chrome.AddThemeConstantOverride("margin_right", 12);
        chrome.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(chrome);
        _chrome = chrome;

        var body = new VBoxContainer();
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.AddThemeConstantOverride("separation", 6);
        chrome.AddChild(body);
        _body = body;

        var title = CreateSectionLabel("Net Factory Stress Demo", 18, Colors.White);
        body.AddChild(title);

        var subtitle = CreateValueLabel("复杂拓扑、吞吐观察与 profiler 回归基线。", new Color("A8B8C6"));
        body.AddChild(subtitle);

        body.AddChild(CreateDivider());
        body.AddChild(CreateSectionLabel("建造面板", 12, new Color("F8FAFC")));

        var buttonGrid = new GridContainer();
        buttonGrid.Columns = 2;
        buttonGrid.MouseFilter = Control.MouseFilterEnum.Ignore;
        buttonGrid.AddThemeConstantOverride("h_separation", 6);
        buttonGrid.AddThemeConstantOverride("v_separation", 6);
        body.AddChild(buttonGrid);

        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Producer, "1 生产器");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Belt, "2 传送带");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Sink, "3 回收站");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Splitter, "4 分流器");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Merger, "5 合并器");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Bridge, "6 跨桥");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Loader, "7 装载器");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Unloader, "8 卸载器");

        body.AddChild(CreateDivider());
        body.AddChild(CreateSectionLabel("状态", 12, new Color("F8FAFC")));
        _selectedLabel = CreateValueLabel(string.Empty);
        _rotationLabel = CreateValueLabel(string.Empty);
        _hoverLabel = CreateValueLabel(string.Empty);
        _previewLabel = CreateValueLabel(string.Empty);
        body.AddChild(_selectedLabel);
        body.AddChild(_rotationLabel);
        body.AddChild(_hoverLabel);
        body.AddChild(_previewLabel);

        body.AddChild(CreateDivider());
        body.AddChild(CreateSectionLabel("吞吐", 12, new Color("F8FAFC")));
        _deliveryLabel = CreateValueLabel(string.Empty);
        _noteLabel = CreateValueLabel(string.Empty, new Color("EED49F"));
        body.AddChild(_deliveryLabel);
        body.AddChild(_noteLabel);

        body.AddChild(CreateDivider());
        body.AddChild(CreateSectionLabel("Profiler", 12, new Color("F8FAFC")));
        _profilerLabel = CreateValueLabel(string.Empty, new Color("CFE7FF"));
        body.AddChild(_profilerLabel);

        body.AddChild(CreateDivider());
        var help = CreateValueLabel("镜头 WASD/方向键 | 缩放 滚轮 | 朝向 Q/E | 放置 左键 | 拆除 右键/Delete", new Color("8EA4B8"));
        body.AddChild(help);

        SetSelectedKind(BuildPrototypeKind.Producer, "持续向前方投放物品");
        SetHoverCell(Vector2I.Zero, false);
        SetPreviewStatus(false, "把鼠标移到地面网格上选择格子。");
        SetRotation(FacingDirection.East);
        SetSinkStats(0, 0, 0);
        SetProfilerStats(0, 0.0, 0, 0, 0.0, 0.0, 0.0);
        SetNote("默认场景已预置多段压力拓扑，右上角保留 smoke 回归探针空区。");
        UpdateLayout();
        GetViewport().SizeChanged += UpdateLayout;
    }

    public override void _ExitTree()
    {
        if (GetViewport() is not null)
        {
            GetViewport().SizeChanged -= UpdateLayout;
        }
    }

    public void SetSelectedKind(BuildPrototypeKind kind, string details)
    {
        foreach (var pair in _selectionButtons)
        {
            pair.Value.Modulate = pair.Key == kind ? Colors.White : new Color(0.72f, 0.78f, 0.86f);
        }

        if (_selectedLabel is not null)
        {
            _selectedLabel.Text = $"当前建造: {GetKindLabel(kind)}\n{details}";
            _selectedLabel.TooltipText = details;
        }
    }

    public void SetHoverCell(Vector2I cell, bool hasHover)
    {
        if (_hoverLabel is not null)
        {
            _hoverLabel.Text = hasHover ? $"格子: ({cell.X}, {cell.Y})" : "格子: 超出可建造区域";
        }
    }

    public void SetPreviewStatus(bool isValid, string text)
    {
        if (_previewLabel is not null)
        {
            _previewLabel.Text = $"预览: {text}";
            _previewLabel.Modulate = isValid ? new Color("A7F3A0") : new Color("FFB4A2");
        }
    }

    public void SetRotation(FacingDirection facing)
    {
        if (_rotationLabel is not null)
        {
            _rotationLabel.Text = $"朝向: {FactoryDirection.ToLabel(facing)}";
        }
    }

    public void SetSinkStats(int deliveredTotal, int deliveredRate, int sinkCount)
    {
        if (_deliveryLabel is not null)
        {
            _deliveryLabel.Text = $"活跃回收端: {sinkCount} | 累计: {deliveredTotal} | 最近: {deliveredRate}/秒";
        }
    }

    public void SetProfilerStats(int fps, double frameMilliseconds, int structureCount, int transitItemCount, double simulationMilliseconds, double visualMilliseconds, double topologyMilliseconds)
    {
        if (_profilerLabel is null)
        {
            return;
        }

        _profilerLabel.Text =
            $"FPS {fps} | 帧 {frameMilliseconds:0.0} ms\n" +
            $"结构 {structureCount} | 在途 {transitItemCount}\n" +
            $"热点 sim {simulationMilliseconds:0.00} ms | visual {visualMilliseconds:0.00} ms\n" +
            $"拓扑重建 {topologyMilliseconds:0.00} ms";
    }

    public void SetNote(string text)
    {
        if (_noteLabel is not null)
        {
            _noteLabel.Text = text;
        }
    }

    public bool BlocksWorldInput(Control? control)
    {
        if (control is null || _panel is null)
        {
            return false;
        }

        var current = control;
        while (current is not null)
        {
            if (current is BaseButton && IsInsidePanel(current))
            {
                return true;
            }

            if (current == _panel)
            {
                return false;
            }

            current = current.GetParent() as Control;
        }

        return false;
    }

    private bool IsInsidePanel(Control control)
    {
        if (_panel is null)
        {
            return false;
        }

        var current = control;
        while (current is not null)
        {
            if (current == _panel)
            {
                return true;
            }

            current = current.GetParent() as Control;
        }

        return false;
    }

    private void UpdateLayout()
    {
        if (_root is null || _panel is null || _chrome is null || _body is null)
        {
            return;
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        var outerMargin = 14.0f;
        var panelWidth = Mathf.Clamp(viewportSize.X * 0.2f, 220.0f, 320.0f);
        var availableHeight = Mathf.Max(220.0f, viewportSize.Y - outerMargin * 2.0f);
        var targetHeight = Mathf.Clamp(viewportSize.Y * 0.48f, 260.0f, availableHeight);
        var innerWidth = Mathf.Max(180.0f, panelWidth - 24.0f);

        _panel.Position = new Vector2(outerMargin, outerMargin);
        _panel.Size = new Vector2(panelWidth, targetHeight);
        _chrome.Size = _panel.Size;
        _body.CustomMinimumSize = new Vector2(innerWidth, 0.0f);
    }

    private void CreateSelectionButton(Container parent, BuildPrototypeKind kind, string text)
    {
        var localKind = kind;
        var button = new Button();
        button.Text = text;
        button.MouseFilter = Control.MouseFilterEnum.Stop;
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.CustomMinimumSize = new Vector2(0.0f, 30.0f);
        button.AddThemeFontSizeOverride("font_size", 12);
        button.Pressed += () => SelectionChanged?.Invoke(localKind);
        parent.AddChild(button);
        _selectionButtons[kind] = button;
    }

    private static Label CreateSectionLabel(string text, int fontSize, Color color)
    {
        var label = new Label();
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.Text = text;
        label.Modulate = color;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private static Label CreateValueLabel(string text, Color? color = null)
    {
        var label = new Label();
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.Text = text;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.Modulate = color ?? new Color("D7E3EE");
        label.AddThemeFontSizeOverride("font_size", 12);
        return label;
    }

    private static ColorRect CreateDivider()
    {
        return new ColorRect
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(0.0f, 1.0f),
            Color = new Color(0.45f, 0.53f, 0.61f, 0.28f)
        };
    }

    private static string GetKindLabel(BuildPrototypeKind kind)
    {
        return kind switch
        {
            BuildPrototypeKind.Producer => "生产器",
            BuildPrototypeKind.Belt => "传送带",
            BuildPrototypeKind.Sink => "回收站",
            BuildPrototypeKind.Splitter => "分流器",
            BuildPrototypeKind.Merger => "合并器",
            BuildPrototypeKind.Bridge => "跨桥",
            BuildPrototypeKind.Loader => "装载器",
            BuildPrototypeKind.Unloader => "卸载器",
            _ => kind.ToString()
        };
    }
}
