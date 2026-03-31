using Godot;
using System;
using System.Collections.Generic;

public partial class FactoryHud : CanvasLayer
{
    private readonly Dictionary<BuildPrototypeKind, Button> _selectionButtons = new();

    private Control? _panel;
    private Label? _selectedLabel;
    private Label? _hoverLabel;
    private Label? _previewLabel;
    private Label? _rotationLabel;
    private Label? _deliveryLabel;
    private Label? _limitationLabel;

    public event Action<BuildPrototypeKind>? SelectionChanged;

    public override void _Ready()
    {
        Name = "FactoryHud";

        var root = new MarginContainer();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.AddThemeConstantOverride("margin_left", 18);
        root.AddThemeConstantOverride("margin_top", 18);
        root.AddThemeConstantOverride("margin_right", 18);
        root.AddThemeConstantOverride("margin_bottom", 18);
        AddChild(root);

        var panel = new PanelContainer();
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.AddChild(panel);
        _panel = panel;

        var body = new VBoxContainer();
        body.CustomMinimumSize = new Vector2(340.0f, 0.0f);
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.AddThemeConstantOverride("separation", 8);
        panel.AddChild(body);

        var title = new Label();
        title.MouseFilter = Control.MouseFilterEnum.Ignore;
        title.Text = "Net Factory 工厂 Demo";
        title.AddThemeFontSizeOverride("font_size", 22);
        body.AddChild(title);

        var subtitle = new Label();
        subtitle.MouseFilter = Control.MouseFilterEnum.Ignore;
        subtitle.Text = "3D 工厂玩法骨架，包含固定视角镜头、鼠标建造交互，以及一条可运行的生产演示线。";
        subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        subtitle.Modulate = new Color("A8B8C6");
        body.AddChild(subtitle);

        var buttonGrid = new GridContainer();
        buttonGrid.Columns = 4;
        buttonGrid.MouseFilter = Control.MouseFilterEnum.Ignore;
        buttonGrid.AddThemeConstantOverride("h_separation", 8);
        buttonGrid.AddThemeConstantOverride("v_separation", 8);
        body.AddChild(buttonGrid);

        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Producer, "1 生产器");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Belt, "2 传送带");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Sink, "3 回收站");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Splitter, "4 分流器");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Merger, "5 合并器");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Bridge, "6 跨桥");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Loader, "7 装载器");
        CreateSelectionButton(buttonGrid, BuildPrototypeKind.Unloader, "8 卸载器");

        _selectedLabel = CreateInfoLabel(body);
        _hoverLabel = CreateInfoLabel(body);
        _previewLabel = CreateInfoLabel(body);
        _rotationLabel = CreateInfoLabel(body);
        _deliveryLabel = CreateInfoLabel(body);
        _limitationLabel = CreateInfoLabel(body);

        var help = CreateInfoLabel(body);
        help.Text = "移动镜头：WASD/方向键 | 缩放：滚轮 | 旋转建造朝向：Q/E | 建造：左键 | 拆除：右键/Delete";

        SetSelectedKind(BuildPrototypeKind.Producer, "持续向前方投放物品");
        SetHoverCell(Vector2I.Zero, false);
        SetPreviewStatus(false, "把鼠标移到地面网格上选择格子。");
        SetRotation(FacingDirection.East);
        SetSinkStats(0, 0);
        SetLimitations("当前原型限制：所有物流件暂时都是 1x1 简化模型，还没有完整双 lane、机械臂动画和更复杂配方逻辑。");
    }

    public void SetSelectedKind(BuildPrototypeKind kind, string details)
    {
        foreach (var pair in _selectionButtons)
        {
            pair.Value.Modulate = pair.Key == kind ? Colors.White : new Color(0.72f, 0.78f, 0.86f);
        }

        if (_selectedLabel is not null)
        {
            _selectedLabel.Text = $"当前建造：{GetKindLabel(kind)} - {details}";
        }
    }

    public void SetHoverCell(Vector2I cell, bool hasHover)
    {
        if (_hoverLabel is not null)
        {
            _hoverLabel.Text = hasHover ? $"当前格子：({cell.X}, {cell.Y})" : "当前格子：超出可建造区域";
        }
    }

    public void SetPreviewStatus(bool isValid, string text)
    {
        if (_previewLabel is not null)
        {
            _previewLabel.Text = $"预览：{text}";
            _previewLabel.Modulate = isValid ? new Color("A7F3A0") : new Color("FFB4A2");
        }
    }

    public void SetRotation(FacingDirection facing)
    {
        if (_rotationLabel is not null)
        {
            _rotationLabel.Text = $"建造朝向：{FactoryDirection.ToLabel(facing)}";
        }
    }

    public void SetSinkStats(int deliveredTotal, int deliveredRate)
    {
        if (_deliveryLabel is not null)
        {
            _deliveryLabel.Text = $"回收吞吐：累计 {deliveredTotal} 个 | 最近 {deliveredRate}/秒";
        }
    }

    public void SetLimitations(string text)
    {
        if (_limitationLabel is not null)
        {
            _limitationLabel.Text = text;
            _limitationLabel.Modulate = new Color("EED49F");
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

    private void CreateSelectionButton(Container parent, BuildPrototypeKind kind, string text)
    {
        var localKind = kind;
        var button = new Button();
        button.Text = text;
        button.MouseFilter = Control.MouseFilterEnum.Stop;
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.Pressed += () => SelectionChanged?.Invoke(localKind);
        parent.AddChild(button);
        _selectionButtons[kind] = button;
    }

    private static Label CreateInfoLabel(Container parent)
    {
        var label = new Label();
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        parent.AddChild(label);
        return label;
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
