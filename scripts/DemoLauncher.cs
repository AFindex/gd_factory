using Godot;

public partial class DemoLauncher : Control
{
    public override void _Ready()
    {
        Name = "DemoLauncher";
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsPreset(LayoutPreset.FullRect);
        BuildScene();
    }

    private void BuildScene()
    {
        var background = new ColorRect();
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        background.Color = new Color("08141C");
        AddChild(background);

        var backdrop = new Control();
        backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
        backdrop.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(backdrop);

        AddGlow(backdrop, new Vector2(0.14f, 0.18f), new Vector2(320.0f, 320.0f), new Color(0.16f, 0.76f, 0.80f, 0.18f));
        AddGlow(backdrop, new Vector2(0.82f, 0.20f), new Vector2(260.0f, 260.0f), new Color(0.96f, 0.66f, 0.24f, 0.16f));
        AddGlow(backdrop, new Vector2(0.74f, 0.78f), new Vector2(420.0f, 420.0f), new Color(0.22f, 0.52f, 0.96f, 0.12f));

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 28);
        margin.AddThemeConstantOverride("margin_top", 22);
        margin.AddThemeConstantOverride("margin_right", 28);
        margin.AddThemeConstantOverride("margin_bottom", 22);
        AddChild(margin);

        var root = new VBoxContainer();
        root.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        root.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddThemeConstantOverride("separation", 18);
        margin.AddChild(root);

        root.AddChild(BuildHeader());

        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        root.AddChild(scroll);

        var list = new VBoxContainer();
        list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        list.AddThemeConstantOverride("separation", 14);
        scroll.AddChild(list);

        var hero = BuildHeroCard();
        list.AddChild(hero);

        for (var i = 0; i < DemoCatalog.LauncherEntries.Count; i++)
        {
            list.AddChild(BuildDemoCard(DemoCatalog.LauncherEntries[i], i));
        }

        root.AddChild(BuildFooter());
    }

    private Control BuildHeader()
    {
        var header = new HBoxContainer();
        header.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddThemeConstantOverride("separation", 18);

        var titlePanel = CreatePanel(new Color("102433"), new Color("4FD1C5"));
        titlePanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(titlePanel);

        var titleBody = new VBoxContainer();
        titleBody.AddThemeConstantOverride("separation", 8);
        titlePanel.AddChild(WrapInMargin(titleBody, 20));

        titleBody.AddChild(CreateTag("NET FACTORY DEMOS"));
        titleBody.AddChild(CreateLabel("Launcher", 34, Colors.White));
        titleBody.AddChild(CreateLabel("从这里进入各个演示场景。每个场景都带有返回 Launcher 的可见按钮，方便切换和回归测试。", 15, new Color("B9CBDA"), true));

        var statsPanel = CreatePanel(new Color("132B3B"), new Color("F6AD55"));
        statsPanel.CustomMinimumSize = new Vector2(220.0f, 0.0f);
        header.AddChild(statsPanel);

        var statsBody = new VBoxContainer();
        statsBody.AddThemeConstantOverride("separation", 6);
        statsPanel.AddChild(WrapInMargin(statsBody, 18));
        statsBody.AddChild(CreateLabel("Active Scenes", 13, new Color("F6E05E")));
        statsBody.AddChild(CreateLabel($"{DemoCatalog.LauncherEntries.Count:0}", 30, Colors.White));
        statsBody.AddChild(CreateLabel("Factory, Mobile, Scenario, UI", 13, new Color("B9CBDA"), true));

        return header;
    }

    private Control BuildHeroCard()
    {
        var panel = CreatePanel(new Color("0D1C29"), new Color("63B3ED"));

        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 10);
        panel.AddChild(WrapInMargin(body, 20));

        body.AddChild(CreateLabel("快速入口", 22, Colors.White));
        body.AddChild(CreateLabel("适合在 Godot 编辑器之外浏览各个演示切片，或在验证改动时反复往返多个场景。", 14, new Color("B7CAD8"), true));
        body.AddChild(CreateLabel("建议先进入 Factory Sandbox 观察基准物流，再切到移动工厂和 UI 展示页检查单独功能。", 13, new Color("7DD3FC"), true));

        return panel;
    }

    private Control BuildDemoCard(DemoSceneEntry entry, int index)
    {
        var accent = (index % 4) switch
        {
            0 => new Color("5EEAD4"),
            1 => new Color("F6AD55"),
            2 => new Color("90CDF4"),
            _ => new Color("F9A8D4")
        };

        var panel = CreatePanel(new Color("0F202D"), accent);

        var row = new HBoxContainer();
        row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddThemeConstantOverride("separation", 18);
        panel.AddChild(WrapInMargin(row, 18));

        var copy = new VBoxContainer();
        copy.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        copy.AddThemeConstantOverride("separation", 8);
        row.AddChild(copy);

        copy.AddChild(CreateLabel(entry.Title, 24, Colors.White));
        copy.AddChild(CreateLabel(entry.Description, 14, new Color("C8D7E3"), true));
        copy.AddChild(CreateLabel(entry.ScenePath, 12, accent));

        var actions = new VBoxContainer();
        actions.CustomMinimumSize = new Vector2(180.0f, 0.0f);
        actions.AddThemeConstantOverride("separation", 10);
        row.AddChild(actions);

        var launchButton = new Button();
        launchButton.Text = "进入 Demo";
        launchButton.CustomMinimumSize = new Vector2(0.0f, 42.0f);
        launchButton.AddThemeFontSizeOverride("font_size", 15);
        launchButton.Pressed += () => OpenDemo(entry.ScenePath);
        launchButton.AddThemeStyleboxOverride("normal", CreateButtonStyle(new Color("12344C"), accent));
        launchButton.AddThemeStyleboxOverride("hover", CreateButtonStyle(new Color("1B4965"), Colors.White));
        launchButton.AddThemeStyleboxOverride("pressed", CreateButtonStyle(new Color("0B2233"), accent.Lightened(0.2f)));
        actions.AddChild(launchButton);

        actions.AddChild(CreateLabel("场景支持单独运行，也支持从 Launcher 反复进入。", 12, new Color("9BB3C7"), true));

        return panel;
    }

    private Control BuildFooter()
    {
        var footer = new HBoxContainer();
        footer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        footer.AddThemeConstantOverride("separation", 12);

        var note = CreateLabel("提示：如果你是从编辑器直接打开某个 demo，也仍然可以通过右上角按钮回到 Launcher。", 12, new Color("9FB7C9"), true);
        note.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        footer.AddChild(note);

        var quitButton = new Button();
        quitButton.Text = "退出";
        quitButton.CustomMinimumSize = new Vector2(110.0f, 36.0f);
        quitButton.Pressed += () => GetTree().Quit();
        quitButton.AddThemeStyleboxOverride("normal", CreateButtonStyle(new Color("1F2937"), new Color("64748B")));
        quitButton.AddThemeStyleboxOverride("hover", CreateButtonStyle(new Color("334155"), Colors.White));
        footer.AddChild(quitButton);

        return footer;
    }

    private void OpenDemo(string scenePath)
    {
        var error = GetTree().ChangeSceneToFile(scenePath);
        if (error != Error.Ok)
        {
            GD.PushError($"Unable to open demo scene '{scenePath}': {error}");
        }
    }

    private static void AddGlow(Control parent, Vector2 anchor, Vector2 size, Color color)
    {
        var glow = new ColorRect();
        glow.AnchorLeft = anchor.X;
        glow.AnchorRight = anchor.X;
        glow.AnchorTop = anchor.Y;
        glow.AnchorBottom = anchor.Y;
        glow.OffsetLeft = -size.X * 0.5f;
        glow.OffsetRight = size.X * 0.5f;
        glow.OffsetTop = -size.Y * 0.5f;
        glow.OffsetBottom = size.Y * 0.5f;
        glow.Color = color;
        glow.MouseFilter = MouseFilterEnum.Ignore;
        parent.AddChild(glow);
    }

    private static PanelContainer CreatePanel(Color background, Color accent)
    {
        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var style = new StyleBoxFlat();
        style.BgColor = background;
        style.BorderColor = accent;
        style.BorderWidthLeft = 1;
        style.BorderWidthTop = 1;
        style.BorderWidthRight = 1;
        style.BorderWidthBottom = 1;
        style.CornerRadiusTopLeft = 18;
        style.CornerRadiusTopRight = 18;
        style.CornerRadiusBottomRight = 18;
        style.CornerRadiusBottomLeft = 18;
        style.ShadowColor = new Color(0.0f, 0.0f, 0.0f, 0.22f);
        style.ShadowSize = 8;
        panel.AddThemeStyleboxOverride("panel", style);

        return panel;
    }

    private static MarginContainer WrapInMargin(Control child, int margin)
    {
        var wrapper = new MarginContainer();
        wrapper.AddThemeConstantOverride("margin_left", margin);
        wrapper.AddThemeConstantOverride("margin_top", margin);
        wrapper.AddThemeConstantOverride("margin_right", margin);
        wrapper.AddThemeConstantOverride("margin_bottom", margin);
        wrapper.AddChild(child);
        return wrapper;
    }

    private static Label CreateTag(string text)
    {
        var label = CreateLabel(text, 12, new Color("8DECE1"));
        label.Uppercase = true;
        return label;
    }

    private static Label CreateLabel(string text, int fontSize, Color color, bool wrap = false)
    {
        var label = new Label();
        label.Text = text;
        label.Modulate = color;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off;
        return label;
    }

    private static StyleBoxFlat CreateButtonStyle(Color background, Color border)
    {
        var style = new StyleBoxFlat();
        style.BgColor = background;
        style.BorderColor = border;
        style.BorderWidthLeft = 1;
        style.BorderWidthTop = 1;
        style.BorderWidthRight = 1;
        style.BorderWidthBottom = 1;
        style.CornerRadiusTopLeft = 12;
        style.CornerRadiusTopRight = 12;
        style.CornerRadiusBottomRight = 12;
        style.CornerRadiusBottomLeft = 12;
        style.ContentMarginLeft = 14;
        style.ContentMarginRight = 14;
        style.ContentMarginTop = 9;
        style.ContentMarginBottom = 9;
        return style;
    }
}
