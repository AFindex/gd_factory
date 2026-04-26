using Godot;
using System.Collections.Generic;

public partial class DemoLauncher : Control
{
    private string? _mapValidationTargetId;
    private FactoryMapValidationMapScope? _mapValidationFocusScope;
    private Vector2I? _mapValidationFocusCell;
    private IReadOnlyList<FactoryMapCatalogEntry> _worldMapEntries = System.Array.Empty<FactoryMapCatalogEntry>();
    private IReadOnlyList<FactoryMapCatalogEntry> _interiorMapEntries = System.Array.Empty<FactoryMapCatalogEntry>();

    public override void _Ready()
    {
        Name = "DemoLauncher";
        MouseFilter = MouseFilterEnum.Stop;

        if (TryGetMapValidationRequest(out _mapValidationTargetId, out _mapValidationFocusScope, out _mapValidationFocusCell))
        {
            CallDeferred(nameof(RunHeadlessMapValidation));
            return;
        }

        SetAnchorsPreset(LayoutPreset.FullRect);
        BuildScene();

        if (HasUserArg("--factory-smoke-test"))
        {
            CallDeferred(nameof(OpenFactorySmokeScene));
        }
    }

    private void BuildScene()
    {
        _worldMapEntries = FactoryMapCatalog.GetWorldMaps();
        _interiorMapEntries = FactoryMapCatalog.GetInteriorMaps();

        var background = new ColorRect();
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        background.Color = FactoryUiTheme.Canvas;
        AddChild(background);

        var backdrop = new Control();
        backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
        backdrop.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(backdrop);

        AddGlow(backdrop, new Vector2(0.14f, 0.18f), new Vector2(320.0f, 320.0f), new Color(1.0f, 1.0f, 1.0f, 0.05f));
        AddGlow(backdrop, new Vector2(0.82f, 0.20f), new Vector2(260.0f, 260.0f), new Color(1.0f, 1.0f, 1.0f, 0.03f));
        AddGlow(backdrop, new Vector2(0.74f, 0.78f), new Vector2(420.0f, 420.0f), new Color(1.0f, 1.0f, 1.0f, 0.02f));

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

        var titlePanel = CreatePanel(FactoryUiTheme.SurfaceRaised, FactoryUiTheme.BorderStrong);
        titlePanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(titlePanel);

        var titleBody = new VBoxContainer();
        titleBody.AddThemeConstantOverride("separation", 8);
        titlePanel.AddChild(WrapInMargin(titleBody, 20));

        titleBody.AddChild(CreateTag("NET FACTORY DEMOS"));
        titleBody.AddChild(CreateLabel("Launcher", 34, FactoryUiTheme.Text));
        titleBody.AddChild(CreateLabel("从这里进入各个演示场景。每个场景都带有返回 Launcher 的可见按钮，方便切换和回归测试。", 15, FactoryUiTheme.TextMuted, true));

        var statsPanel = CreatePanel(FactoryUiTheme.SurfaceBase, FactoryUiTheme.Border);
        statsPanel.CustomMinimumSize = new Vector2(220.0f, 0.0f);
        header.AddChild(statsPanel);

        var statsBody = new VBoxContainer();
        statsBody.AddThemeConstantOverride("separation", 6);
        statsPanel.AddChild(WrapInMargin(statsBody, 18));
        statsBody.AddChild(CreateLabel("Active Scenes", 13, FactoryUiTheme.TextSubtle));
        statsBody.AddChild(CreateLabel($"{DemoCatalog.LauncherEntries.Count:0}", 30, FactoryUiTheme.Text));
        statsBody.AddChild(CreateLabel("Factory, Mobile, Scenario, UI", 13, FactoryUiTheme.TextMuted, true));

        return header;
    }

    private Control BuildHeroCard()
    {
        var panel = CreatePanel(FactoryUiTheme.SurfaceBase, FactoryUiTheme.Border);

        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 10);
        panel.AddChild(WrapInMargin(body, 20));

        body.AddChild(CreateLabel("快速入口", 22, FactoryUiTheme.Text));
        body.AddChild(CreateLabel("适合在 Godot 编辑器之外浏览各个演示切片，或在验证改动时反复往返多个场景。", 14, FactoryUiTheme.TextMuted, true));
        body.AddChild(CreateLabel("建议先进入 Factory Sandbox 观察基准物流，再切到移动工厂和 UI 展示页检查单独功能。现在也可以在启动页直接挑选工程内或运行时保存的地图。", 13, FactoryUiTheme.TextSubtle, true));

        return panel;
    }

    private Control BuildDemoCard(DemoSceneEntry entry, int index)
    {
        var accent = (index % 4) switch
        {
            0 => FactoryUiTheme.BorderStrong,
            1 => FactoryUiTheme.Border,
            2 => FactoryUiTheme.TextMuted,
            _ => FactoryUiTheme.BorderMuted
        };

        var panel = CreatePanel(FactoryUiTheme.SurfaceBase, accent);

        var row = new HBoxContainer();
        row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddThemeConstantOverride("separation", 18);
        panel.AddChild(WrapInMargin(row, 18));

        var copy = new VBoxContainer();
        copy.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        copy.AddThemeConstantOverride("separation", 8);
        row.AddChild(copy);

        copy.AddChild(CreateLabel(entry.Title, 24, FactoryUiTheme.Text));
        copy.AddChild(CreateLabel(entry.Description, 14, FactoryUiTheme.TextMuted, true));
        copy.AddChild(CreateLabel(entry.ScenePath, 12, FactoryUiTheme.TextSubtle));
        AddMapSelectors(entry, copy);

        var actions = new VBoxContainer();
        actions.CustomMinimumSize = new Vector2(180.0f, 0.0f);
        actions.AddThemeConstantOverride("separation", 10);
        row.AddChild(actions);

        var launchButton = new Button();
        launchButton.Text = "进入 Demo";
        launchButton.CustomMinimumSize = new Vector2(0.0f, 42.0f);
        launchButton.AddThemeFontSizeOverride("font_size", 15);
        launchButton.Pressed += () => OpenDemo(entry);
        FactoryUiTheme.ApplyButtonTheme(launchButton);
        actions.AddChild(launchButton);

        actions.AddChild(CreateLabel("场景支持单独运行，也支持从 Launcher 反复进入。", 12, FactoryUiTheme.TextSubtle, true));

        return panel;
    }

    private Control BuildFooter()
    {
        var footer = new HBoxContainer();
        footer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        footer.AddThemeConstantOverride("separation", 12);

        var note = CreateLabel("提示：如果你是从编辑器直接打开某个 demo，也仍然可以通过右上角按钮回到 Launcher。", 12, FactoryUiTheme.TextSubtle, true);
        note.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        footer.AddChild(note);

        var quitButton = new Button();
        quitButton.Text = "退出";
        quitButton.CustomMinimumSize = new Vector2(110.0f, 36.0f);
        quitButton.Pressed += () => GetTree().Quit();
        FactoryUiTheme.ApplyButtonTheme(quitButton);
        footer.AddChild(quitButton);

        return footer;
    }

    private void OpenDemo(DemoSceneEntry entry)
    {
        ApplyLaunchSelection(entry);

        var error = GetTree().ChangeSceneToFile(entry.ScenePath);
        if (error != Error.Ok)
        {
            GD.PushError($"Unable to open demo scene '{entry.ScenePath}': {error}");
        }
    }

    private void OpenFactorySmokeScene()
    {
        OpenDemo(DemoCatalog.LauncherEntries[0]);
    }

    private void RunHeadlessMapValidation()
    {
        try
        {
            if (_mapValidationFocusCell.HasValue)
            {
                if (string.IsNullOrWhiteSpace(_mapValidationTargetId) || !_mapValidationFocusScope.HasValue)
                {
                    throw new System.InvalidOperationException("Focused map validation requires both a target id and a scope.");
                }

                var focusResult = FactoryMapValidationService.ValidateFocus(
                    _mapValidationTargetId!,
                    _mapValidationFocusScope.Value,
                    _mapValidationFocusCell.Value);
                FactoryMapValidationService.PrintFocusReport(focusResult);
                GetTree().Quit(focusResult.HasErrors ? 1 : 0);
                return;
            }

            var report = string.IsNullOrWhiteSpace(_mapValidationTargetId)
                ? FactoryMapValidationService.ValidateAllTargets()
                : FactoryMapValidationService.ValidateTarget(_mapValidationTargetId!);
            FactoryMapValidationService.PrintReport(report);
            GetTree().Quit(report.HasErrors ? 1 : 0);
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"FACTORY_MAP_VALIDATION_FAILED {ex.Message}");
            GetTree().Quit(1);
        }
    }

    private static bool HasUserArg(string argText)
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (string.Equals(arg, argText, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetMapValidationRequest(
        out string? targetId,
        out FactoryMapValidationMapScope? focusScope,
        out Vector2I? focusCell)
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (string.Equals(arg, "--factory-map-validate", System.StringComparison.Ordinal))
            {
                targetId = null;
                focusScope = null;
                focusCell = null;
                return true;
            }

            if (arg.StartsWith("--factory-map-validate=", System.StringComparison.Ordinal))
            {
                var value = arg.Substring("--factory-map-validate=".Length).Trim();
                targetId = string.IsNullOrWhiteSpace(value) ? null : value;
                focusScope = null;
                focusCell = null;
                return true;
            }

            if (arg.StartsWith("--factory-map-validate-cell=", System.StringComparison.Ordinal))
            {
                ParseFocusedValidationRequest(
                    arg.Substring("--factory-map-validate-cell=".Length).Trim(),
                    out targetId,
                    out focusScope,
                    out focusCell);
                return true;
            }
        }

        targetId = null;
        focusScope = null;
        focusCell = null;
        return false;
    }

    private static void ParseFocusedValidationRequest(
        string value,
        out string? targetId,
        out FactoryMapValidationMapScope? focusScope,
        out Vector2I? focusCell)
    {
        var parts = value.Split(':', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
        if (parts.Length < 2 || parts.Length > 3)
        {
            throw new System.InvalidOperationException(
                "Focused map validation expects '<target-id>:<x>,<y>' or '<target-id>:world:<x>,<y>' / '<target-id>:interior:<x>,<y>'.");
        }

        targetId = parts[0];
        if (parts.Length == 2)
        {
            focusScope = FactoryMapValidationMapScope.World;
            focusCell = ParseVector2I(parts[1]);
            return;
        }

        focusScope = parts[1].ToLowerInvariant() switch
        {
            "world" => FactoryMapValidationMapScope.World,
            "interior" => FactoryMapValidationMapScope.Interior,
            _ => throw new System.InvalidOperationException(
                $"Unknown focused validation scope '{parts[1]}'. Use 'world' or 'interior'.")
        };
        focusCell = ParseVector2I(parts[2]);
    }

    private static Vector2I ParseVector2I(string text)
    {
        var parts = text.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], out var x)
            || !int.TryParse(parts[1], out var y))
        {
            throw new System.InvalidOperationException($"Invalid cell '{text}'. Expected '<x>,<y>'.");
        }

        return new Vector2I(x, y);
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
        panel.AddThemeStyleboxOverride("panel", FactoryUiTheme.CreatePanelStyle(background, accent, borderWidth: 1, cornerRadius: FactoryUiTheme.RadiusNone));

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
        var label = CreateLabel(text, 12, FactoryUiTheme.TextSubtle);
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

    private void AddMapSelectors(DemoSceneEntry entry, VBoxContainer parent)
    {
        if (entry.Id == "factory-demo")
        {
            Label? worldSummaryLabel = null;
            var worldSelector = CreateMapSelector("世界地图", _worldMapEntries, DemoLaunchOptions.ResolveMobileWorldMapPath(), selected =>
            {
                DemoLaunchOptions.MobileWorldMapPath = selected.Path;
                if (worldSummaryLabel is not null)
                {
                    worldSummaryLabel.Text = selected.BuildSummaryText();
                }
            });
            worldSummaryLabel = worldSelector.SummaryLabel;
            parent.AddChild(worldSelector.Container);

            Label? interiorSummaryLabel = null;
            var interiorSelector = CreateMapSelector("内部地图", _interiorMapEntries, DemoLaunchOptions.ResolveMobileInteriorMapPath(), selected =>
            {
                DemoLaunchOptions.MobileInteriorMapPath = selected.Path;
                if (interiorSummaryLabel is not null)
                {
                    interiorSummaryLabel.Text = selected.BuildSummaryText();
                }
            });
            interiorSummaryLabel = interiorSelector.SummaryLabel;
            parent.AddChild(interiorSelector.Container);
            return;
        }

        if (entry.Id == "mobile-factory-test-scenario")
        {
            Label? worldSummaryLabel = null;
            var worldSelector = CreateMapSelector("世界地图", _worldMapEntries, DemoLaunchOptions.ResolveMobileWorldMapPath(), selected =>
            {
                DemoLaunchOptions.MobileWorldMapPath = selected.Path;
                if (worldSummaryLabel is not null)
                {
                    worldSummaryLabel.Text = selected.BuildSummaryText();
                }
            });
            worldSummaryLabel = worldSelector.SummaryLabel;
            parent.AddChild(worldSelector.Container);

            Label? interiorSummaryLabel = null;
            var interiorSelector = CreateMapSelector("内部地图", _interiorMapEntries, DemoLaunchOptions.ResolveMobileInteriorMapPath(), selected =>
            {
                DemoLaunchOptions.MobileInteriorMapPath = selected.Path;
                if (interiorSummaryLabel is not null)
                {
                    interiorSummaryLabel.Text = selected.BuildSummaryText();
                }
            });
            interiorSummaryLabel = interiorSelector.SummaryLabel;
            parent.AddChild(interiorSelector.Container);
        }
    }

    private (Control Container, Label SummaryLabel) CreateMapSelector(
        string title,
        IReadOnlyList<FactoryMapCatalogEntry> entries,
        string selectedPath,
        System.Action<FactoryMapCatalogEntry> onSelected)
    {
        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", 6);

        container.AddChild(CreateLabel(title, 12, FactoryUiTheme.TextSubtle));

        var optionButton = new OptionButton();
        optionButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        optionButton.FitToLongestItem = false;
        optionButton.AddThemeFontSizeOverride("font_size", 13);
        container.AddChild(optionButton);

        var summaryLabel = CreateLabel("暂无地图。", 12, FactoryUiTheme.TextSubtle, true);
        container.AddChild(summaryLabel);

        var selectedIndex = 0;
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            optionButton.AddItem(entry.BuildOptionText());
            if (string.Equals(entry.Path, selectedPath, System.StringComparison.OrdinalIgnoreCase))
            {
                selectedIndex = index;
            }
        }

        if (entries.Count > 0)
        {
            optionButton.Select(selectedIndex);
            summaryLabel.Text = entries[selectedIndex].BuildSummaryText();
            onSelected(entries[selectedIndex]);
            optionButton.ItemSelected += selected =>
            {
                var entry = entries[(int)selected];
                summaryLabel.Text = entry.BuildSummaryText();
                onSelected(entry);
            };
        }
        else
        {
            optionButton.Disabled = true;
            summaryLabel.Text = "当前没有可用地图。";
        }

        return (container, summaryLabel);
    }

    private void ApplyLaunchSelection(DemoSceneEntry entry)
    {
        if (entry.Id == "factory-demo")
        {
            if (string.IsNullOrWhiteSpace(DemoLaunchOptions.MobileWorldMapPath))
            {
                DemoLaunchOptions.MobileWorldMapPath = FactoryMapPaths.FocusedMobileWorld;
            }

            if (string.IsNullOrWhiteSpace(DemoLaunchOptions.MobileInteriorMapPath))
            {
                DemoLaunchOptions.MobileInteriorMapPath = FactoryMapPaths.FocusedMobileInterior;
            }
        }

        if (entry.Id == "mobile-factory-test-scenario")
        {
            if (string.IsNullOrWhiteSpace(DemoLaunchOptions.MobileWorldMapPath))
            {
                DemoLaunchOptions.MobileWorldMapPath = FactoryMapPaths.FocusedMobileWorld;
            }

            if (string.IsNullOrWhiteSpace(DemoLaunchOptions.MobileInteriorMapPath))
            {
                DemoLaunchOptions.MobileInteriorMapPath = FactoryMapPaths.FocusedMobileInterior;
            }
        }
    }

}
