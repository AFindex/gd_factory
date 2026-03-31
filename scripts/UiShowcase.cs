using Godot;
using System;
using System.Collections.Generic;

public partial class UiShowcase : Control
{
    private sealed class Palette
    {
        public Palette(string name, Color background, Color surface, Color panel, Color accent, Color accentSoft, Color text, Color textMuted)
        {
            Name = name;
            Background = background;
            Surface = surface;
            Panel = panel;
            Accent = accent;
            AccentSoft = accentSoft;
            Text = text;
            TextMuted = textMuted;
        }

        public string Name { get; }
        public Color Background { get; }
        public Color Surface { get; }
        public Color Panel { get; }
        public Color Accent { get; }
        public Color AccentSoft { get; }
        public Color Text { get; }
        public Color TextMuted { get; }
    }

    private readonly Palette[] _palettes =
    {
        new("Amber Grid", Hex("07131F"), Hex("102339"), Hex("15314D"), Hex("FF9F43"), Hex("FFD6A0"), Hex("F4F7FB"), Hex("9FB3C8")),
        new("Cyan Flux", Hex("071017"), Hex("0F2532"), Hex("133345"), Hex("32D1FF"), Hex("9BE9FF"), Hex("F2F8FA"), Hex("A7BECC")),
        new("Lime Pulse", Hex("09140D"), Hex("142A19"), Hex("1C3823"), Hex("B7FF35"), Hex("E1FF9C"), Hex("F7FBF5"), Hex("B4C7B6"))
    };

    private readonly List<Control> _introNodes = new();
    private readonly List<Control> _floatingOrbs = new();
    private readonly List<PanelContainer> _surfacePanels = new();
    private readonly List<Button> _accentButtons = new();
    private readonly List<(ProgressBar Bar, Label ValueLabel, string Unit)> _metricWidgets = new();
    private readonly List<Label> _mutedLabels = new();
    private readonly List<string> _logLines = new();
    private readonly string[] _statusWords = { "Nominal", "Stable", "Boosted", "Warming", "Synchronized", "Charging" };

    private ColorRect? _backgroundRect;
    private Label? _paletteNameLabel;
    private Label? _jobsValueLabel;
    private Label? _stabilityValueLabel;
    private Label? _throughputValueLabel;
    private Label? _latencyValueLabel;
    private ProgressBar? _throughputBar;
    private ProgressBar? _latencyBar;
    private ProgressBar? _bufferBar;
    private ProgressBar? _cacheBar;
    private HSlider? _signalSlider;
    private Label? _signalValueLabel;
    private SpinBox? _workerSpinBox;
    private CheckButton? _autoToggle;
    private OptionButton? _paletteOption;
    private RichTextLabel? _logLabel;
    private Tree? _tree;
    private TabContainer? _tabContainer;
    private Label? _footerStatusLabel;
    private Label? _clockLabel;
    private Label? _headlineValueLabel;
    private PanelContainer? _statusBadge;
    private AcceptDialog? _dialog;
    private VBoxContainer? _toastStack;
    private int _tick;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsPreset(LayoutPreset.FullRect);

        BuildScene();
        RefreshBackgroundLayout();
        ApplyPalette(_palettes[0]);
        PopulateTree();
        AppendLog("UI bootstrap finished");
        AppendLog("Waiting for interactive telemetry...");
        StartIntroAnimation();
        StartAmbientAnimation();

        GetViewport().SizeChanged += RefreshBackgroundLayout;
    }

    private static Color Hex(string value)
    {
        return Color.FromString(value, Colors.White);
    }

    private void BuildScene()
    {
        _backgroundRect = new ColorRect();
        _backgroundRect.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_backgroundRect);

        var backgroundDecor = new Control();
        backgroundDecor.SetAnchorsPreset(LayoutPreset.FullRect);
        backgroundDecor.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(backgroundDecor);

        CreateOrb(backgroundDecor, new Vector2(0.16f, 0.18f), 240, 0.08f);
        CreateOrb(backgroundDecor, new Vector2(0.81f, 0.16f), 180, -0.06f);
        CreateOrb(backgroundDecor, new Vector2(0.72f, 0.74f), 280, 0.09f);
        CreateOrb(backgroundDecor, new Vector2(0.28f, 0.82f), 140, -0.05f);
        CreateOrb(backgroundDecor, new Vector2(0.52f, 0.50f), 96, 0.04f);

        var chromeLine = new ColorRect();
        chromeLine.AnchorLeft = 0.05f;
        chromeLine.AnchorRight = 0.95f;
        chromeLine.AnchorTop = 0.08f;
        chromeLine.AnchorBottom = 0.08f;
        chromeLine.OffsetTop = 0;
        chromeLine.OffsetBottom = 2;
        backgroundDecor.AddChild(chromeLine);
        _introNodes.Add(chromeLine);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 28);
        margin.AddThemeConstantOverride("margin_top", 24);
        margin.AddThemeConstantOverride("margin_right", 28);
        margin.AddThemeConstantOverride("margin_bottom", 24);
        AddChild(margin);

        var scroll = new ScrollContainer();
        scroll.SetAnchorsPreset(LayoutPreset.FullRect);
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Auto;
        scroll.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
        margin.AddChild(scroll);

        var root = new VBoxContainer();
        root.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        root.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddThemeConstantOverride("separation", 18);
        scroll.AddChild(root);

        root.AddChild(BuildHeader());
        root.AddChild(BuildBody());
        root.AddChild(BuildFooter());

        _dialog = new AcceptDialog();
        _dialog.Title = "Simulation Event";
        _dialog.DialogText = "Queued a synthetic UI validation event for the current palette profile.";
        AddChild(_dialog);

        _toastStack = new VBoxContainer();
        _toastStack.AnchorLeft = 1.0f;
        _toastStack.AnchorRight = 1.0f;
        _toastStack.OffsetLeft = -320;
        _toastStack.OffsetRight = -20;
        _toastStack.OffsetTop = 24;
        _toastStack.OffsetBottom = 220;
        _toastStack.AddThemeConstantOverride("separation", 10);
        _toastStack.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_toastStack);
    }

    private Control BuildHeader()
    {
        var header = new VBoxContainer();
        header.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddThemeConstantOverride("separation", 16);

        var titlePanel = CreatePanel(new Vector2(0, 132), out var titleBody);
        titlePanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        titleBody.AddChild(CreateEyebrow("NET UI VALIDATION LAB"));

        var titleLabel = new Label();
        titleLabel.Text = "Animated UI Showcase";
        titleLabel.AddThemeFontSizeOverride("font_size", 34);
        titleBody.AddChild(titleLabel);

        var subtitle = new Label();
        subtitle.Text = "Godot 4.6.1 .NET scene for control rendering, transitions, live updates and interaction stress testing.";
        subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        subtitle.AddThemeFontSizeOverride("font_size", 15);
        titleBody.AddChild(subtitle);
        _mutedLabels.Add(subtitle);

        var badgeRow = new HBoxContainer();
        badgeRow.AddThemeConstantOverride("separation", 10);
        badgeRow.AddChild(CreateTag("Tween-heavy"));
        badgeRow.AddChild(CreateTag("Control-rich"));
        badgeRow.AddChild(CreateTag("Mono build"));
        titleBody.AddChild(badgeRow);

        header.AddChild(titlePanel);
        _introNodes.Add(titlePanel);

        var statRail = new GridContainer();
        statRail.Columns = 3;
        statRail.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        statRail.AddThemeConstantOverride("separation", 12);

        var paletteCard = CreateStatCard("Palette", "Amber Grid", "Live accent profile");
        _paletteNameLabel = (Label)paletteCard.GetMeta("value_label");
        statRail.AddChild(paletteCard);

        var jobsCard = CreateStatCard("Jobs/sec", "284", "Synthetic throughput");
        _jobsValueLabel = (Label)jobsCard.GetMeta("value_label");
        statRail.AddChild(jobsCard);

        var stabilityCard = CreateStatCard("Stability", "Nominal", "Runtime posture");
        _stabilityValueLabel = (Label)stabilityCard.GetMeta("value_label");
        statRail.AddChild(stabilityCard);

        header.AddChild(statRail);
        _introNodes.Add(statRail);
        return header;
    }

    private Control BuildBody()
    {
        var body = new HFlowContainer();
        body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        body.AddThemeConstantOverride("separation", 18);

        body.AddChild(BuildLeftRail());
        body.AddChild(BuildCenterColumn());
        body.AddChild(BuildRightRail());
        return body;
    }

    private Control BuildLeftRail()
    {
        var left = new VBoxContainer();
        left.CustomMinimumSize = new Vector2(280, 0);
        left.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        left.SizeFlagsVertical = SizeFlags.ExpandFill;
        left.AddThemeConstantOverride("separation", 18);

        var controlPanel = CreatePanel(new Vector2(280, 0), out var controlsBody);
        controlsBody.AddChild(CreatePanelTitle("Interactive Controls", "Use these to drive visible state changes and animation branches."));

        var buttonRow = new HBoxContainer();
        buttonRow.AddThemeConstantOverride("separation", 10);
        buttonRow.AddChild(CreateActionButton("Pulse Batch", () =>
        {
            AnimateBadge();
            AppendLog("Triggered manual batch pulse");
            SpawnToast("Batch pulse dispatched");
        }));
        buttonRow.AddChild(CreateActionButton("Open Dialog", () =>
        {
            _dialog?.PopupCentered(new Vector2I(480, 220));
            AppendLog("Dialog opened from action rail");
        }));
        controlsBody.AddChild(buttonRow);

        var lineEdit = new LineEdit();
        lineEdit.PlaceholderText = "Send a short test note into the event log";
        lineEdit.TextSubmitted += OnNoteSubmitted;
        controlsBody.AddChild(lineEdit);

        var checkBox = new CheckBox();
        checkBox.Text = "Enable telemetry layer";
        checkBox.ButtonPressed = true;
        checkBox.Toggled += pressed =>
        {
            AppendLog(pressed ? "Telemetry layer enabled" : "Telemetry layer disabled");
            SpawnToast(pressed ? "Telemetry layer on" : "Telemetry layer off");
        };
        controlsBody.AddChild(checkBox);

        _autoToggle = new CheckButton();
        _autoToggle.Text = "Auto animate metrics";
        _autoToggle.ButtonPressed = true;
        _autoToggle.Toggled += pressed =>
        {
            AppendLog(pressed ? "Auto animation resumed" : "Auto animation paused");
        };
        controlsBody.AddChild(_autoToggle);

        _paletteOption = new OptionButton();
        _paletteOption.AddItem("Amber Grid");
        _paletteOption.AddItem("Cyan Flux");
        _paletteOption.AddItem("Lime Pulse");
        _paletteOption.ItemSelected += OnPaletteSelected;
        controlsBody.AddChild(CreateLabeledControl("Palette Profile", _paletteOption));

        _signalSlider = new HSlider();
        _signalSlider.MinValue = 20;
        _signalSlider.MaxValue = 100;
        _signalSlider.Step = 1;
        _signalSlider.Value = 68;
        _signalSlider.ValueChanged += OnSignalValueChanged;

        _signalValueLabel = new Label();
        _signalValueLabel.HorizontalAlignment = HorizontalAlignment.Right;
        _signalValueLabel.Text = "68%";
        controlsBody.AddChild(CreateDualRow("Signal Strength", _signalValueLabel, _signalSlider));

        _workerSpinBox = new SpinBox();
        _workerSpinBox.MinValue = 4;
        _workerSpinBox.MaxValue = 32;
        _workerSpinBox.Step = 1;
        _workerSpinBox.Value = 12;
        controlsBody.AddChild(CreateLabeledControl("Concurrent Workers", _workerSpinBox));

        _throughputBar = CreateProgressBar();
        _latencyBar = CreateProgressBar();
        controlsBody.AddChild(CreateDualRow("Throughput", CreateValueLabel(out _throughputValueLabel, "72%"), _throughputBar));
        controlsBody.AddChild(CreateDualRow("Latency Budget", CreateValueLabel(out _latencyValueLabel, "38%"), _latencyBar));

        left.AddChild(controlPanel);
        _introNodes.Add(controlPanel);

        var queuePanel = CreatePanel(new Vector2(280, 0), out var queueBody);
        queueBody.AddChild(CreatePanelTitle("Action Queue", "A compact checklist to verify focus, hover and state transitions."));

        foreach (var item in new[] { "Button hover bounce", "Dialog opening", "Timer-driven progress fill", "Palette recolor propagation", "Tab content swapping" })
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);

            var dot = new PanelContainer();
            dot.CustomMinimumSize = new Vector2(12, 12);
            dot.AddThemeStyleboxOverride("panel", CreateChipStyle(Hex("7AE582")));
            row.AddChild(dot);

            var label = new Label();
            label.Text = item;
            row.AddChild(label);

            queueBody.AddChild(row);
        }

        left.AddChild(queuePanel);
        _introNodes.Add(queuePanel);
        return left;
    }

    private Control BuildCenterColumn()
    {
        var center = new VBoxContainer();
        center.CustomMinimumSize = new Vector2(520, 0);
        center.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        center.SizeFlagsVertical = SizeFlags.ExpandFill;
        center.AddThemeConstantOverride("separation", 18);

        var heroPanel = CreatePanel(new Vector2(0, 172), out var heroBody);
        heroPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        heroBody.AddChild(CreateEyebrow("LIVE SIMULATION"));

        var heroTitle = new Label();
        heroTitle.Text = "Runtime HUD Is Fully Scriptable";
        heroTitle.AddThemeFontSizeOverride("font_size", 28);
        heroBody.AddChild(heroTitle);

        var heroSummary = new Label();
        heroSummary.Text = "Panels, tabs, logs and metrics are all created in C# so this scene is easy to extend as a regression harness for the .NET build.";
        heroSummary.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        heroSummary.AddThemeFontSizeOverride("font_size", 15);
        heroBody.AddChild(heroSummary);
        _mutedLabels.Add(heroSummary);

        _headlineValueLabel = new Label();
        _headlineValueLabel.Text = "Nodes warmed: 128";
        _headlineValueLabel.AddThemeFontSizeOverride("font_size", 22);
        heroBody.AddChild(_headlineValueLabel);

        var heroActions = new HBoxContainer();
        heroActions.AddThemeConstantOverride("separation", 10);
        heroActions.AddChild(CreateTag("Hover Me"));
        heroActions.AddChild(CreateTag("Swap Theme"));
        heroActions.AddChild(CreateTag("Stress Update"));
        heroBody.AddChild(heroActions);

        center.AddChild(heroPanel);
        _introNodes.Add(heroPanel);

        var tabsPanel = CreatePanel(new Vector2(0, 0), out var tabsBody);
        tabsPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        tabsPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        tabsBody.AddChild(CreatePanelTitle("Tabbed Test Surface", "Cycles through content while preserving interactivity and layout behavior."));

        _tabContainer = new TabContainer();
        _tabContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _tabContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
        tabsBody.AddChild(_tabContainer);

        _tabContainer.AddChild(BuildOverviewTab());
        _tabContainer.AddChild(BuildLogTab());
        _tabContainer.AddChild(BuildFormTab());

        center.AddChild(tabsPanel);
        _introNodes.Add(tabsPanel);
        return center;
    }

    private Control BuildRightRail()
    {
        var right = new VBoxContainer();
        right.CustomMinimumSize = new Vector2(300, 0);
        right.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        right.SizeFlagsVertical = SizeFlags.ExpandFill;
        right.AddThemeConstantOverride("separation", 18);

        var metricsPanel = CreatePanel(new Vector2(300, 0), out var metricsBody);
        metricsBody.AddChild(CreatePanelTitle("Animated Metrics", "Live bars refresh on a timer using tweens for smooth visual interpolation."));
        metricsBody.AddChild(CreateMetricRow("GPU Queue", "ms"));
        metricsBody.AddChild(CreateMetricRow("Present Delay", "ms"));
        metricsBody.AddChild(CreateMetricRow("Batch Cache", "%"));
        metricsBody.AddChild(CreateMetricRow("Signal Cleanliness", "%"));
        right.AddChild(metricsPanel);
        _introNodes.Add(metricsPanel);

        var treePanel = CreatePanel(new Vector2(300, 0), out var treeBody);
        treeBody.AddChild(CreatePanelTitle("Scene Tree Probe", "Basic hierarchy sample for row rendering, expansion and icon-less tree behavior."));

        _tree = new Tree();
        _tree.Columns = 1;
        _tree.HideRoot = true;
        _tree.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _tree.SizeFlagsVertical = SizeFlags.ExpandFill;
        _tree.CustomMinimumSize = new Vector2(0, 220);
        treeBody.AddChild(_tree);

        right.AddChild(treePanel);
        _introNodes.Add(treePanel);

        var badgePanel = CreatePanel(new Vector2(300, 0), out var badgeBody);
        badgeBody.AddChild(CreatePanelTitle("State Beacon", "The badge and footer clock keep updating even when the main panels idle."));

        var beaconRow = new HBoxContainer();
        beaconRow.Alignment = BoxContainer.AlignmentMode.Center;
        beaconRow.AddThemeConstantOverride("separation", 12);

        _statusBadge = CreateBadge("SYSTEM READY");
        beaconRow.AddChild(_statusBadge);

        _clockLabel = new Label();
        _clockLabel.Text = DateTime.Now.ToString("HH:mm:ss");
        _clockLabel.AddThemeFontSizeOverride("font_size", 22);
        beaconRow.AddChild(_clockLabel);

        badgeBody.AddChild(beaconRow);
        right.AddChild(badgePanel);
        _introNodes.Add(badgePanel);
        return right;
    }

    private Control BuildFooter()
    {
        var footerPanel = CreatePanel(new Vector2(0, 78), out var footerBody);
        footerPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var footer = new HBoxContainer();
        footer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        footer.Alignment = BoxContainer.AlignmentMode.Center;
        footer.AddThemeConstantOverride("separation", 16);

        _footerStatusLabel = new Label();
        _footerStatusLabel.Text = "Bootstrapped .NET showcase scene";
        _footerStatusLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        footer.AddChild(_footerStatusLabel);

        footer.AddChild(CreateActionButton("Reset Motion", () =>
        {
            _tick = 0;
            UpdateSimulation();
            AppendLog("Motion state reset");
            SpawnToast("Motion reset complete");
        }));

        footer.AddChild(CreateActionButton("Emit Toast", () =>
        {
            SpawnToast("Inline notification test");
            AppendLog("Toast emitted from footer");
        }));

        footerBody.AddChild(footer);
        _introNodes.Add(footerPanel);
        return footerPanel;
    }

    private Control BuildOverviewTab()
    {
        var page = new VBoxContainer();
        page.Name = "Overview";
        page.AddThemeConstantOverride("separation", 12);

        var grid = new GridContainer();
        grid.Columns = 2;
        grid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 12);

        grid.AddChild(CreateMiniCard("Style Overrides", "Panels and buttons recolor with the active palette."));
        grid.AddChild(CreateMiniCard("Signals", "Events funnel into log updates and toast feedback."));
        grid.AddChild(CreateMiniCard("Timers", "Metric bars are tweened by recurring time-based updates."));
        grid.AddChild(CreateMiniCard("Responsive Layout", "Containers keep the scene clean across viewport sizes."));

        page.AddChild(grid);

        _bufferBar = CreateProgressBar();
        _cacheBar = CreateProgressBar();
        page.AddChild(CreateDualRow("Buffer Fill", CreatePassiveValue("64%"), _bufferBar));
        page.AddChild(CreateDualRow("Cache Warmth", CreatePassiveValue("41%"), _cacheBar));
        return page;
    }

    private Control BuildLogTab()
    {
        var page = new VBoxContainer();
        page.Name = "Event Log";

        _logLabel = new RichTextLabel();
        _logLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _logLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
        _logLabel.CustomMinimumSize = new Vector2(0, 280);
        _logLabel.BbcodeEnabled = true;
        _logLabel.ScrollActive = true;
        page.AddChild(_logLabel);
        return page;
    }

    private Control BuildFormTab()
    {
        var page = new VBoxContainer();
        page.Name = "Preview";
        page.AddThemeConstantOverride("separation", 12);

        var itemList = new ItemList();
        itemList.CustomMinimumSize = new Vector2(0, 120);
        itemList.AddItem("Primary button states");
        itemList.AddItem("CheckBox and CheckButton focus chain");
        itemList.AddItem("Slider drag visuals");
        itemList.AddItem("Tab switching transitions");
        itemList.AddItem("Tree row rendering");
        page.AddChild(itemList);

        var preview = new TextEdit();
        preview.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        preview.SizeFlagsVertical = SizeFlags.ExpandFill;
        preview.CustomMinimumSize = new Vector2(0, 160);
        preview.Text = "[ui_showcase]\npalette = live\nanimation = rich\nbuild = godot-net-4.6.1\nnotes = extend this scene with extra controls as needed";
        page.AddChild(preview);
        return page;
    }

    private Control CreateMiniCard(string title, string body)
    {
        var panel = CreatePanel(new Vector2(0, 120), out var cardBody);
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var titleLabel = new Label();
        titleLabel.Text = title;
        titleLabel.AddThemeFontSizeOverride("font_size", 18);
        cardBody.AddChild(titleLabel);

        var bodyLabel = new Label();
        bodyLabel.Text = body;
        bodyLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        bodyLabel.AddThemeFontSizeOverride("font_size", 14);
        cardBody.AddChild(bodyLabel);
        _mutedLabels.Add(bodyLabel);
        return panel;
    }

    private HBoxContainer CreateMetricRow(string title, string unit)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);

        var label = new Label();
        label.Text = title;
        label.CustomMinimumSize = new Vector2(126, 0);
        row.AddChild(label);

        var bar = CreateProgressBar();
        bar.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(bar);

        var valueLabel = new Label();
        valueLabel.Text = "--";
        valueLabel.HorizontalAlignment = HorizontalAlignment.Right;
        valueLabel.CustomMinimumSize = new Vector2(66, 0);
        row.AddChild(valueLabel);

        _metricWidgets.Add((bar, valueLabel, unit));
        return row;
    }

    private VBoxContainer CreatePanelTitle(string title, string subtitle)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 4);

        var titleLabel = new Label();
        titleLabel.Text = title;
        titleLabel.AddThemeFontSizeOverride("font_size", 18);
        box.AddChild(titleLabel);

        var subtitleLabel = new Label();
        subtitleLabel.Text = subtitle;
        subtitleLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        subtitleLabel.AddThemeFontSizeOverride("font_size", 13);
        box.AddChild(subtitleLabel);
        _mutedLabels.Add(subtitleLabel);
        return box;
    }

    private PanelContainer CreatePanel(Vector2 minSize, out VBoxContainer body)
    {
        var panel = new PanelContainer();
        panel.CustomMinimumSize = minSize;
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(_palettes[0].Panel, _palettes[0].Accent, 22));
        _surfacePanels.Add(panel);

        body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 12);
        panel.AddChild(body);
        return panel;
    }

    private PanelContainer CreateStatCard(string title, string value, string caption)
    {
        var panel = CreatePanel(new Vector2(0, 132), out var body);
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        panel.SizeFlagsStretchRatio = 0.8f;

        var titleLabel = new Label();
        titleLabel.Text = title;
        titleLabel.AddThemeFontSizeOverride("font_size", 13);
        body.AddChild(titleLabel);
        _mutedLabels.Add(titleLabel);

        var valueLabel = new Label();
        valueLabel.Text = value;
        valueLabel.AddThemeFontSizeOverride("font_size", 24);
        body.AddChild(valueLabel);

        var captionLabel = new Label();
        captionLabel.Text = caption;
        captionLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        captionLabel.AddThemeFontSizeOverride("font_size", 12);
        body.AddChild(captionLabel);
        _mutedLabels.Add(captionLabel);

        panel.SetMeta("value_label", valueLabel);
        return panel;
    }

    private Label CreateEyebrow(string text)
    {
        var eyebrow = new Label();
        eyebrow.Text = text;
        eyebrow.AddThemeFontSizeOverride("font_size", 12);
        eyebrow.AddThemeColorOverride("font_color", Hex("B8C8D7"));
        return eyebrow;
    }

    private Control CreateTag(string text)
    {
        var chip = CreateBadge(text);
        chip.CustomMinimumSize = new Vector2(0, 32);
        return chip;
    }

    private PanelContainer CreateBadge(string text)
    {
        var badge = new PanelContainer();
        badge.AddThemeStyleboxOverride("panel", CreateChipStyle(_palettes[0].Accent));

        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", Hex("07131F"));
        badge.AddChild(label);
        return badge;
    }

    private Control CreateLabeledControl(string title, Control control)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);

        var label = new Label();
        label.Text = title;
        label.AddThemeFontSizeOverride("font_size", 13);
        box.AddChild(label);
        _mutedLabels.Add(label);

        control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.AddChild(control);
        return box;
    }

    private Control CreateDualRow(string title, Label valueLabel, Control control)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);

        var titleLabel = new Label();
        titleLabel.Text = title;
        titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(titleLabel);
        _mutedLabels.Add(titleLabel);

        row.AddChild(valueLabel);
        box.AddChild(row);

        control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.AddChild(control);
        return box;
    }

    private Label CreatePassiveValue(string text)
    {
        var label = new Label();
        label.Text = text;
        label.HorizontalAlignment = HorizontalAlignment.Right;
        return label;
    }

    private Label CreateValueLabel(out Label target, string text)
    {
        target = new Label();
        target.Text = text;
        target.HorizontalAlignment = HorizontalAlignment.Right;
        return target;
    }

    private Button CreateActionButton(string text, Action callback)
    {
        var button = new Button();
        button.Text = text;
        button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        button.CustomMinimumSize = new Vector2(0, 40);
        button.Pressed += callback;
        button.Resized += () => button.PivotOffset = button.Size * 0.5f;
        button.MouseEntered += () => AnimateButtonScale(button, 1.04f);
        button.MouseExited += () => AnimateButtonScale(button, 1.0f);
        button.ButtonDown += () => AnimateButtonScale(button, 0.98f);
        button.ButtonUp += () => AnimateButtonScale(button, 1.02f);
        _accentButtons.Add(button);
        return button;
    }

    private void AnimateButtonScale(Control control, float targetScale)
    {
        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(control, "scale", Vector2.One * targetScale, 0.14f);
    }

    private ProgressBar CreateProgressBar()
    {
        var bar = new ProgressBar();
        bar.MinValue = 0;
        bar.MaxValue = 100;
        bar.Value = 0;
        bar.ShowPercentage = false;
        bar.CustomMinimumSize = new Vector2(0, 14);
        return bar;
    }

    private void CreateOrb(Control parent, Vector2 anchor, float size, float drift)
    {
        var orb = new Panel();
        orb.MouseFilter = MouseFilterEnum.Ignore;
        orb.Size = Vector2.One * size;
        orb.PivotOffset = Vector2.One * size * 0.5f;
        orb.AddThemeStyleboxOverride("panel", CreateOrbStyle(_palettes[0].Accent, 0.22f));
        orb.SetMeta("anchor", anchor);
        orb.SetMeta("size", size);
        orb.SetMeta("drift", drift);
        parent.AddChild(orb);
        _floatingOrbs.Add(orb);
    }

    private void RefreshBackgroundLayout()
    {
        var viewportSize = GetViewportRect().Size;
        foreach (var orb in _floatingOrbs)
        {
            var anchor = (Vector2)orb.GetMeta("anchor");
            var size = (float)orb.GetMeta("size");
            orb.Size = Vector2.One * size;
            orb.PivotOffset = Vector2.One * size * 0.5f;
            orb.Position = new Vector2(viewportSize.X * anchor.X, viewportSize.Y * anchor.Y) - (Vector2.One * size * 0.5f);
        }
    }

    private void ApplyPalette(Palette palette)
    {
        if (_backgroundRect != null)
        {
            _backgroundRect.Color = palette.Background;
        }

        foreach (var panel in _surfacePanels)
        {
            panel.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(palette.Panel, palette.Accent, 22));
        }

        foreach (var button in _accentButtons)
        {
            button.AddThemeStyleboxOverride("normal", CreateButtonStyle(palette.Accent, palette.AccentSoft, 0.92f));
            button.AddThemeStyleboxOverride("hover", CreateButtonStyle(palette.Accent.Lightened(0.08f), palette.AccentSoft, 1.0f));
            button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(palette.Accent.Darkened(0.15f), palette.AccentSoft, 0.84f));
            button.AddThemeStyleboxOverride("focus", CreateButtonStyle(palette.Accent, palette.AccentSoft, 1.0f));
            button.AddThemeColorOverride("font_color", palette.Background.Darkened(0.2f));
        }

        foreach (var orb in _floatingOrbs)
        {
            orb.AddThemeStyleboxOverride("panel", CreateOrbStyle(palette.Accent, 0.20f));
        }

        foreach (var label in _mutedLabels)
        {
            label.AddThemeColorOverride("font_color", palette.TextMuted);
        }

        foreach (var widget in _metricWidgets)
        {
            StyleProgressBar(widget.Bar, palette.Accent, palette.Surface);
        }

        if (_throughputBar != null) StyleProgressBar(_throughputBar, palette.Accent, palette.Surface);
        if (_latencyBar != null) StyleProgressBar(_latencyBar, palette.AccentSoft, palette.Surface);
        if (_bufferBar != null) StyleProgressBar(_bufferBar, palette.Accent, palette.Surface);
        if (_cacheBar != null) StyleProgressBar(_cacheBar, palette.AccentSoft, palette.Surface);

        if (_statusBadge != null)
        {
            _statusBadge.AddThemeStyleboxOverride("panel", CreateChipStyle(palette.Accent));
        }

        if (_paletteNameLabel != null)
        {
            _paletteNameLabel.Text = palette.Name;
        }
    }

    private static StyleBoxFlat CreateSurfaceStyle(Color fill, Color accent, int cornerRadius)
    {
        var style = new StyleBoxFlat();
        style.BgColor = fill;
        style.CornerRadiusTopLeft = cornerRadius;
        style.CornerRadiusTopRight = cornerRadius;
        style.CornerRadiusBottomRight = cornerRadius;
        style.CornerRadiusBottomLeft = cornerRadius;
        style.BorderColor = accent.Lightened(0.2f);
        style.BorderWidthLeft = 1;
        style.BorderWidthTop = 1;
        style.BorderWidthRight = 1;
        style.BorderWidthBottom = 1;
        style.ShadowColor = new Color(0, 0, 0, 0.22f);
        style.ShadowSize = 16;
        style.ContentMarginLeft = 18;
        style.ContentMarginTop = 18;
        style.ContentMarginRight = 18;
        style.ContentMarginBottom = 18;
        return style;
    }

    private static StyleBoxFlat CreateButtonStyle(Color fill, Color textGlow, float alpha)
    {
        var style = new StyleBoxFlat();
        style.BgColor = new Color(fill, alpha);
        style.CornerRadiusTopLeft = 14;
        style.CornerRadiusTopRight = 14;
        style.CornerRadiusBottomRight = 14;
        style.CornerRadiusBottomLeft = 14;
        style.BorderWidthLeft = 1;
        style.BorderWidthTop = 1;
        style.BorderWidthRight = 1;
        style.BorderWidthBottom = 1;
        style.BorderColor = textGlow;
        style.ShadowColor = new Color(0, 0, 0, 0.18f);
        style.ShadowSize = 10;
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 10;
        style.ContentMarginBottom = 10;
        return style;
    }

    private static StyleBoxFlat CreateChipStyle(Color accent)
    {
        var style = new StyleBoxFlat();
        style.BgColor = accent;
        style.CornerRadiusTopLeft = 999;
        style.CornerRadiusTopRight = 999;
        style.CornerRadiusBottomRight = 999;
        style.CornerRadiusBottomLeft = 999;
        style.ContentMarginLeft = 12;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = 7;
        style.ContentMarginBottom = 7;
        return style;
    }

    private static StyleBoxFlat CreateOrbStyle(Color accent, float alpha)
    {
        var style = new StyleBoxFlat();
        style.BgColor = new Color(accent, alpha);
        style.CornerRadiusTopLeft = 999;
        style.CornerRadiusTopRight = 999;
        style.CornerRadiusBottomRight = 999;
        style.CornerRadiusBottomLeft = 999;
        return style;
    }

    private static void StyleProgressBar(ProgressBar bar, Color fill, Color background)
    {
        var fillStyle = new StyleBoxFlat();
        fillStyle.BgColor = fill;
        fillStyle.CornerRadiusTopLeft = 999;
        fillStyle.CornerRadiusTopRight = 999;
        fillStyle.CornerRadiusBottomRight = 999;
        fillStyle.CornerRadiusBottomLeft = 999;

        var backgroundStyle = new StyleBoxFlat();
        backgroundStyle.BgColor = background.Lightened(0.12f);
        backgroundStyle.CornerRadiusTopLeft = 999;
        backgroundStyle.CornerRadiusTopRight = 999;
        backgroundStyle.CornerRadiusBottomRight = 999;
        backgroundStyle.CornerRadiusBottomLeft = 999;

        bar.AddThemeStyleboxOverride("fill", fillStyle);
        bar.AddThemeStyleboxOverride("background", backgroundStyle);
    }

    private void PopulateTree()
    {
        if (_tree == null)
        {
            return;
        }

        _tree.Clear();
        var root = _tree.CreateItem();
        var uiRoot = _tree.CreateItem(root);
        uiRoot.SetText(0, "UiShowcase");

        var header = _tree.CreateItem(uiRoot);
        header.SetText(0, "Header / Stat Cards");

        var center = _tree.CreateItem(uiRoot);
        center.SetText(0, "Center / Tabs");

        var overview = _tree.CreateItem(center);
        overview.SetText(0, "Overview / Mini Cards");

        var logs = _tree.CreateItem(center);
        logs.SetText(0, "Event Log / RichTextLabel");

        var preview = _tree.CreateItem(center);
        preview.SetText(0, "Preview / ItemList + TextEdit");

        var right = _tree.CreateItem(uiRoot);
        right.SetText(0, "Right Rail / Metrics + Tree + Beacon");
    }

    private void StartIntroAnimation()
    {
        for (var i = 0; i < _introNodes.Count; i++)
        {
            var node = _introNodes[i];
            node.Modulate = new Color(1, 1, 1, 0);
            node.Scale = Vector2.One * 0.96f;

            var tween = CreateTween();
            tween.SetEase(Tween.EaseType.Out);
            tween.SetTrans(Tween.TransitionType.Cubic);
            tween.TweenInterval(i * 0.06f);
            tween.TweenProperty(node, "modulate:a", 1.0f, 0.48f);
            tween.Parallel().TweenProperty(node, "scale", Vector2.One, 0.52f);
        }

        var bootTween = CreateTween();
        bootTween.TweenInterval(0.75f);
        bootTween.TweenCallback(Callable.From(UpdateSimulation));
    }

    private void StartAmbientAnimation()
    {
        for (var i = 0; i < _floatingOrbs.Count; i++)
        {
            var orb = _floatingOrbs[i];
            var drift = (float)orb.GetMeta("drift");
            var origin = orb.Position;
            var tween = CreateTween();
            tween.SetLoops();
            tween.SetEase(Tween.EaseType.InOut);
            tween.SetTrans(Tween.TransitionType.Sine);
            tween.TweenProperty(orb, "position", origin + new Vector2(80 * drift * (i + 1), -50 * drift * (i + 1)), 3.0f + i * 0.35f);
            tween.Parallel().TweenProperty(orb, "scale", Vector2.One * (1.0f + 0.07f * (i % 3)), 2.4f + i * 0.2f);
            tween.TweenProperty(orb, "position", origin, 3.2f + i * 0.25f);
            tween.Parallel().TweenProperty(orb, "scale", Vector2.One, 2.8f + i * 0.2f);
        }

        var simulationTimer = new Timer();
        simulationTimer.WaitTime = 0.9f;
        simulationTimer.Timeout += UpdateSimulation;
        simulationTimer.Autostart = true;
        AddChild(simulationTimer);

        var clockTimer = new Timer();
        clockTimer.WaitTime = 1.0f;
        clockTimer.Timeout += UpdateClock;
        clockTimer.Autostart = true;
        AddChild(clockTimer);
    }

    private void UpdateClock()
    {
        if (_clockLabel != null)
        {
            _clockLabel.Text = DateTime.Now.ToString("HH:mm:ss");
        }
    }

    private void UpdateSimulation()
    {
        _tick++;

        var signal = _signalSlider?.Value ?? 68.0;
        var workers = _workerSpinBox?.Value ?? 12.0;

        if (_autoToggle != null && !_autoToggle.ButtonPressed)
        {
            if (_footerStatusLabel != null)
            {
                _footerStatusLabel.Text = "Auto animation paused, interactive controls still live";
            }
            UpdateClock();
            return;
        }

        var throughput = Clamp01(signal / 100.0 * 0.68 + workers / 32.0 * 0.32 + Math.Sin(_tick * 0.55) * 0.08) * 100.0;
        var latency = Clamp01(0.28 + (100.0 - signal) / 100.0 * 0.4 + Math.Cos(_tick * 0.44) * 0.12) * 100.0;
        var buffer = Clamp01(0.42 + signal / 100.0 * 0.35 + Math.Sin(_tick * 0.25) * 0.09) * 100.0;
        var cache = Clamp01(0.25 + workers / 32.0 * 0.52 + Math.Cos(_tick * 0.38) * 0.08) * 100.0;

        AnimateBar(_throughputBar, throughput);
        AnimateBar(_latencyBar, latency);
        AnimateBar(_bufferBar, buffer);
        AnimateBar(_cacheBar, cache);

        if (_throughputValueLabel != null) _throughputValueLabel.Text = $"{Math.Round(throughput)}%";
        if (_latencyValueLabel != null) _latencyValueLabel.Text = $"{Math.Round(latency)}%";

        for (var i = 0; i < _metricWidgets.Count; i++)
        {
            var wobble = Math.Sin((_tick + i) * (0.38 + i * 0.04));
            var target = i < 2
                ? Clamp01(0.18 + signal / 100.0 * 0.55 + wobble * 0.12) * 100.0
                : Clamp01(0.22 + workers / 32.0 * 0.5 + wobble * 0.1) * 100.0;

            AnimateBar(_metricWidgets[i].Bar, target);
            _metricWidgets[i].ValueLabel.Text = _metricWidgets[i].Unit == "ms"
                ? $"{Math.Round(3 + target * 0.18)} ms"
                : $"{Math.Round(target)}%";
        }

        if (_jobsValueLabel != null)
        {
            _jobsValueLabel.Text = $"{Math.Round(140 + throughput * 2.6)}";
        }

        if (_stabilityValueLabel != null)
        {
            _stabilityValueLabel.Text = _statusWords[_tick % _statusWords.Length];
        }

        if (_headlineValueLabel != null)
        {
            _headlineValueLabel.Text = $"Nodes warmed: {96 + _tick * 4}";
        }

        if (_footerStatusLabel != null)
        {
            _footerStatusLabel.Text = $"Palette {_palettes[_paletteOption?.Selected ?? 0].Name} | signal {Math.Round(signal)}% | workers {workers:0}";
        }

        if (_tick % 2 == 0)
        {
            AppendLog($"Tick {_tick:00}: throughput {Math.Round(throughput)}%, latency {Math.Round(latency)}%");
        }

        if (_tick % 5 == 0 && _tabContainer != null && _tabContainer.GetTabCount() > 0)
        {
            _tabContainer.CurrentTab = (_tabContainer.CurrentTab + 1) % _tabContainer.GetTabCount();
        }

        AnimateBadge();
        UpdateClock();
    }

    private static double Clamp01(double value)
    {
        return Math.Max(0.0, Math.Min(1.0, value));
    }

    private void AnimateBar(ProgressBar? bar, double target)
    {
        if (bar == null)
        {
            return;
        }

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(bar, "value", target, 0.55f);
    }

    private void AnimateBadge()
    {
        if (_statusBadge == null)
        {
            return;
        }

        _statusBadge.PivotOffset = _statusBadge.Size * 0.5f;
        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(_statusBadge, "scale", Vector2.One * 1.06f, 0.18f);
        tween.TweenProperty(_statusBadge, "scale", Vector2.One, 0.22f);
    }

    private void AppendLog(string message)
    {
        var stamp = DateTime.Now.ToString("HH:mm:ss");
        _logLines.Insert(0, $"[color=#9FB3C8]{stamp}[/color]  {message}");

        while (_logLines.Count > 8)
        {
            _logLines.RemoveAt(_logLines.Count - 1);
        }

        if (_logLabel != null)
        {
            _logLabel.Text = string.Join("\n", _logLines);
        }
    }

    private void SpawnToast(string text)
    {
        if (_toastStack == null)
        {
            return;
        }

        var index = _paletteOption?.Selected ?? 0;
        var toast = new PanelContainer();
        toast.AddThemeStyleboxOverride("panel", CreateSurfaceStyle(_palettes[index].Panel, _palettes[index].Accent, 16));

        var label = new Label();
        label.Text = text;
        toast.AddChild(label);
        _toastStack.AddChild(toast);

        toast.Modulate = new Color(1, 1, 1, 0);
        toast.Position += new Vector2(36, 0);

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(toast, "modulate:a", 1.0f, 0.18f);
        tween.Parallel().TweenProperty(toast, "position:x", toast.Position.X - 36, 0.18f);
        tween.TweenInterval(1.8f);
        tween.TweenProperty(toast, "modulate:a", 0.0f, 0.24f);
        tween.Parallel().TweenProperty(toast, "position:x", toast.Position.X + 18, 0.24f);
        tween.TweenCallback(Callable.From(() => toast.QueueFree()));
    }

    private void OnPaletteSelected(long index)
    {
        ApplyPalette(_palettes[index]);
        AppendLog($"Palette switched to {_palettes[index].Name}");
        SpawnToast($"Palette: {_palettes[index].Name}");
    }

    private void OnSignalValueChanged(double value)
    {
        if (_signalValueLabel != null)
        {
            _signalValueLabel.Text = $"{Math.Round(value)}%";
        }
    }

    private void OnNoteSubmitted(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        AppendLog($"Note received: {text.Trim()}");
        SpawnToast("Log note injected");
    }
}
