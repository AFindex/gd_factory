using Godot;
using System;
using System.Collections.Generic;

public partial class MobileFactoryHud
{
    private void BuildTopChrome()
    {
        var topChrome = new PanelContainer();
        topChrome.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        topChrome.MouseFilter = Control.MouseFilterEnum.Stop;
        topChrome.TooltipText = UseLargeScenarioWorkspaces
            ? "切换总览、建造测试、蓝图、诊断和工厂详情工作区。"
            : "切换指挥、内部编辑、蓝图和工厂详情工作区。";
        AddChild(topChrome);
        _topChromePanel = topChrome;

        _workspaceChrome = new FactoryWorkspaceChrome();
        _workspaceChrome.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _workspaceChrome.WorkspaceSelected += workspaceId =>
        {
            ApplyWorkspaceVisibility();
            WorkspaceSelected?.Invoke(workspaceId);
        };
        topChrome.AddChild(_workspaceChrome);
        _workspaceChrome.Configure(
            string.Empty,
            string.Empty,
            BuildWorkspaceDescriptors(),
            GetDefaultWorkspaceId());
    }

    private void BuildInfoPanel()
    {
        var worldFocusFrame = new PanelContainer();
        worldFocusFrame.MouseFilter = Control.MouseFilterEnum.Ignore;
        worldFocusFrame.Visible = false;
        _worldFocusFrameStyle = CreateOutlineOnlyStyle(Colors.Transparent);
        worldFocusFrame.AddThemeStyleboxOverride("panel", _worldFocusFrameStyle);
        AddChild(worldFocusFrame);
        _worldFocusFrame = worldFocusFrame;

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;
        panel.ClipContents = true;
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle(FactoryUiTheme.BorderStrong));
        AddChild(panel);
        _infoPanel = panel;

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        margin.MouseFilter = Control.MouseFilterEnum.Ignore;
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        var bodyScroll = new ScrollContainer();
        bodyScroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        bodyScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        bodyScroll.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
        bodyScroll.MouseFilter = Control.MouseFilterEnum.Ignore;
        bodyScroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        bodyScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        margin.AddChild(bodyScroll);

        var body = new VBoxContainer();
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride("separation", 8);
        body.CustomMinimumSize = new Vector2(0.0f, 0.0f);
        bodyScroll.AddChild(body);

        body.AddChild(CreateInfoLabel("移动工厂总览", 14, FactoryUiTheme.Text));
        _modeLabel = CreateInfoLabel(string.Empty);
        _stateLabel = CreateInfoLabel(string.Empty);
        _hoverLabel = CreateInfoLabel(string.Empty);
        _previewLabel = CreateInfoLabel(string.Empty);
        body.AddChild(_modeLabel);
        body.AddChild(_stateLabel);
        body.AddChild(_hoverLabel);
        body.AddChild(_previewLabel);

        body.AddChild(CreateDivider());
        var workspaceHost = new Control();
        workspaceHost.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        workspaceHost.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        workspaceHost.MouseFilter = Control.MouseFilterEnum.Ignore;
        workspaceHost.ClipContents = true;
        body.AddChild(workspaceHost);

        workspaceHost.AddChild(BuildWorldCommandWorkspace(GetPrimaryWorldWorkspaceId()));
        if (UseLargeScenarioWorkspaces)
        {
            workspaceHost.AddChild(BuildWorldDiagnosticsWorkspace());
        }
        workspaceHost.AddChild(BuildWorldDetailWorkspace());

        _worldWorkspaceHintLabel = CreateInfoLabel("当前工作区主要显示在右侧编辑面板。", 11, FactoryUiTheme.TextSubtle);
        _worldWorkspaceHintLabel.Visible = false;
        body.AddChild(_worldWorkspaceHintLabel);
    }

    private void BuildEditorPanel()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;
        panel.ClipContents = true;
        _editorPanelStyle = CreatePanelStyle(Colors.Transparent);
        panel.AddThemeStyleboxOverride("panel", _editorPanelStyle);
        AddChild(panel);
        _editorPanel = panel;

        var chrome = new MarginContainer();
        chrome.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        chrome.MouseFilter = Control.MouseFilterEnum.Ignore;
        chrome.AddThemeConstantOverride("margin_left", 10);
        chrome.AddThemeConstantOverride("margin_top", 10);
        chrome.AddThemeConstantOverride("margin_right", 10);
        chrome.AddThemeConstantOverride("margin_bottom", 10);
        panel.AddChild(chrome);

        var body = new HBoxContainer();
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride("separation", 10);
        chrome.AddChild(body);

        var viewportPanel = new PanelContainer();
        viewportPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        viewportPanel.ClipContents = true;
        viewportPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        viewportPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        viewportPanel.AddThemeStyleboxOverride("panel", CreatePanelStyle(FactoryUiTheme.Border));
        body.AddChild(viewportPanel);

        var viewportMargin = new MarginContainer();
        viewportMargin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        viewportMargin.MouseFilter = Control.MouseFilterEnum.Ignore;
        viewportMargin.AddThemeConstantOverride("margin_left", 8);
        viewportMargin.AddThemeConstantOverride("margin_top", 8);
        viewportMargin.AddThemeConstantOverride("margin_right", 8);
        viewportMargin.AddThemeConstantOverride("margin_bottom", 8);
        viewportPanel.AddChild(viewportMargin);

        var viewport = new SubViewport();
        viewport.TransparentBg = false;
        viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        _editorViewport = viewport;

        var viewportRect = new TextureRect();
        viewportRect.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        viewportRect.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        viewportRect.StretchMode = TextureRect.StretchModeEnum.Scale;
        viewportRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        viewportRect.MouseFilter = Control.MouseFilterEnum.Ignore;
        viewportRect.CustomMinimumSize = new Vector2(320.0f, 0.0f);
        viewportRect.Texture = viewport.GetTexture();
        viewportMargin.AddChild(viewportRect);
        _editorViewportRect = viewportRect;

        var sidebarPanel = new PanelContainer();
        sidebarPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        sidebarPanel.ClipContents = true;
        sidebarPanel.CustomMinimumSize = new Vector2(EditorSidebarWidth, 0.0f);
        sidebarPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        sidebarPanel.AddThemeStyleboxOverride("panel", CreatePanelStyle(FactoryUiTheme.Border));
        body.AddChild(sidebarPanel);

        var sidebarMargin = new MarginContainer();
        sidebarMargin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        sidebarMargin.MouseFilter = Control.MouseFilterEnum.Ignore;
        sidebarMargin.AddThemeConstantOverride("margin_left", 10);
        sidebarMargin.AddThemeConstantOverride("margin_top", 10);
        sidebarMargin.AddThemeConstantOverride("margin_right", 10);
        sidebarMargin.AddThemeConstantOverride("margin_bottom", 10);
        sidebarPanel.AddChild(sidebarMargin);

        var sidebarScroll = new ScrollContainer();
        sidebarScroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        sidebarScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        sidebarScroll.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
        sidebarScroll.MouseFilter = Control.MouseFilterEnum.Ignore;
        sidebarScroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        sidebarScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        sidebarMargin.AddChild(sidebarScroll);

        var sidebar = new VBoxContainer();
        sidebar.MouseFilter = Control.MouseFilterEnum.Ignore;
        sidebar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        sidebar.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        sidebar.AddThemeConstantOverride("separation", 6);
        sidebar.CustomMinimumSize = new Vector2(EditorSidebarWidth - 20.0f, 0.0f);
        sidebarScroll.AddChild(sidebar);

        sidebar.AddChild(CreateEditorLabel("内部编辑概览", 14, FactoryUiTheme.Text));
        _editorModeLabel = CreateEditorLabel(string.Empty, 12, FactoryUiTheme.TextMuted);
        _selectionLabel = CreateEditorLabel(string.Empty, 12, FactoryUiTheme.Text);
        _selectionTargetLabel = CreateEditorLabel(string.Empty, 12, FactoryUiTheme.TextMuted);
        _portStatusLabel = CreateEditorLabel(string.Empty, 12, FactoryUiTheme.TextSubtle);
        sidebar.AddChild(_editorModeLabel);
        sidebar.AddChild(_selectionLabel);
        sidebar.AddChild(_selectionTargetLabel);
        sidebar.AddChild(_portStatusLabel);

        sidebar.AddChild(CreateDivider());
        var workspaceHost = new Control();
        workspaceHost.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        workspaceHost.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        workspaceHost.MouseFilter = Control.MouseFilterEnum.Ignore;
        workspaceHost.ClipContents = true;
        sidebar.AddChild(workspaceHost);

        workspaceHost.AddChild(BuildEditorToolWorkspace(GetPrimaryEditorWorkspaceId()));
        workspaceHost.AddChild(BuildBlueprintWorkspace());

        _editorWorkspaceHintLabel = CreateEditorLabel("当前工作区主要在左侧世界信息面板中展开；右侧继续保留视口。", 11, FactoryUiTheme.TextSubtle);
        _editorWorkspaceHintLabel.Visible = false;
        sidebar.AddChild(_editorWorkspaceHintLabel);

        AddChild(viewport);

        _detailWindow = new FactoryStructureDetailWindow();
        _detailWindow.InventoryMoveRequested += (inventoryId, fromSlot, toSlot, splitStack) => EditorDetailInventoryMoveRequested?.Invoke(inventoryId, fromSlot, toSlot, splitStack);
        _detailWindow.InventoryTransferRequested += (fromInventoryId, fromSlot, toInventoryId, toSlot, splitStack) => EditorDetailInventoryTransferRequested?.Invoke(fromInventoryId, fromSlot, toInventoryId, toSlot, splitStack);
        _detailWindow.RecipeSelected += recipeId => EditorDetailRecipeSelected?.Invoke(recipeId);
        _detailWindow.DetailActionRequested += actionId => EditorDetailActionRequested?.Invoke(actionId);
        _detailWindow.CloseRequested += () => EditorDetailClosed?.Invoke();
        _overlayRoot?.AddChild(_detailWindow);
    }

    private IReadOnlyList<FactoryWorkspaceDescriptor> BuildWorkspaceDescriptors()
    {
        return UseLargeScenarioWorkspaces
            ? new[]
            {
                new FactoryWorkspaceDescriptor(OverviewWorkspaceId, "总览"),
                new FactoryWorkspaceDescriptor(BuildTestWorkspaceId, "建造测试"),
                new FactoryWorkspaceDescriptor(BlueprintWorkspaceId, "蓝图"),
                new FactoryWorkspaceDescriptor(DiagnosticsWorkspaceId, "诊断"),
                new FactoryWorkspaceDescriptor(DetailsWorkspaceId, "工厂详情")
            }
            : new[]
            {
                new FactoryWorkspaceDescriptor(CommandWorkspaceId, "指挥"),
                new FactoryWorkspaceDescriptor(EditorWorkspaceId, "内部编辑"),
                new FactoryWorkspaceDescriptor(BlueprintWorkspaceId, "蓝图"),
                new FactoryWorkspaceDescriptor(DetailsWorkspaceId, "工厂详情")
            };
    }

    private string GetDefaultWorkspaceId() => UseLargeScenarioWorkspaces ? OverviewWorkspaceId : CommandWorkspaceId;
    private string GetPrimaryWorldWorkspaceId() => UseLargeScenarioWorkspaces ? OverviewWorkspaceId : CommandWorkspaceId;
    private string GetPrimaryEditorWorkspaceId() => UseLargeScenarioWorkspaces ? BuildTestWorkspaceId : EditorWorkspaceId;

    private Control BuildWorldCommandWorkspace(string workspaceId)
    {
        var (workspace, body) = CreateWorkspacePanel(_worldWorkspacePanels, workspaceId);
        body.AddChild(CreateInfoLabel(UseLargeScenarioWorkspaces ? "场景总览" : "指挥工作区", 14, FactoryUiTheme.Text));
        body.AddChild(CreateInfoLabel(
            UseLargeScenarioWorkspaces ? "把移动工厂活动、回收统计和观察辅助集中到一个总览面板中。" : "把观察/部署控制与运行提示集中起来，而不是默认常驻在整块 HUD 上。",
            11,
            FactoryUiTheme.TextSubtle));

        var actionsRow = new HBoxContainer();
        actionsRow.AddThemeConstantOverride("separation", 8);
        body.AddChild(actionsRow);

        _factoryCommandButton = new Button { Text = "进入工厂控制 (C)", ToggleMode = true, CustomMinimumSize = new Vector2(146.0f, 34.0f), MouseFilter = Control.MouseFilterEnum.Stop };
        FactoryUiTheme.ApplyButtonTheme(_factoryCommandButton);
        _factoryCommandButton.Pressed += () => FactoryCommandModeToggleRequested?.Invoke();
        actionsRow.AddChild(_factoryCommandButton);

        _observerButton = new Button { Text = "进入观察模式 (Tab)", ToggleMode = true, CustomMinimumSize = new Vector2(146.0f, 34.0f), MouseFilter = Control.MouseFilterEnum.Stop };
        FactoryUiTheme.ApplyButtonTheme(_observerButton);
        _observerButton.Pressed += () => ObserverModeToggleRequested?.Invoke();
        actionsRow.AddChild(_observerButton);

        _deployButton = new Button { Text = "部署模式 (G)", ToggleMode = true, CustomMinimumSize = new Vector2(126.0f, 34.0f), MouseFilter = Control.MouseFilterEnum.Stop };
        FactoryUiTheme.ApplyButtonTheme(_deployButton);
        _deployButton.Pressed += () => DeployModeToggleRequested?.Invoke();
        actionsRow.AddChild(_deployButton);

        _deliveryLabel = CreateInfoLabel(string.Empty);
        _combatLabel = CreateInfoLabel(string.Empty, 12, FactoryUiTheme.TextSubtle);
        _focusLabel = CreateInfoLabel(string.Empty);
        _hintLabel = CreateInfoLabel(string.Empty, 11, FactoryUiTheme.TextSubtle);
        body.AddChild(_deliveryLabel);
        body.AddChild(_combatLabel);
        body.AddChild(_focusLabel);
        body.AddChild(_hintLabel);
        return workspace;
    }

    private Control BuildWorldDiagnosticsWorkspace()
    {
        var (workspace, body) = CreateWorkspacePanel(_worldWorkspacePanels, DiagnosticsWorkspaceId);
        body.AddChild(CreateInfoLabel("诊断工作区", 14, FactoryUiTheme.Text));
        body.AddChild(CreateInfoLabel("把场景级观测信息单独拆出来，避免和建造测试工具混在一起。", 11, FactoryUiTheme.TextSubtle));
        body.AddChild(CreateInfoLabel("使用总览快速切换部署状态，再在这里观察吞吐、战斗与焦点变化。", 11, FactoryUiTheme.TextSubtle));
        _diagnosticsDeliveryLabel = CreateInfoLabel(string.Empty);
        _diagnosticsCombatLabel = CreateInfoLabel(string.Empty, 12, FactoryUiTheme.TextSubtle);
        _diagnosticsFocusLabel = CreateInfoLabel(string.Empty);
        _diagnosticsHintLabel = CreateInfoLabel(string.Empty, 11, FactoryUiTheme.TextSubtle);
        body.AddChild(_diagnosticsDeliveryLabel);
        body.AddChild(_diagnosticsCombatLabel);
        body.AddChild(_diagnosticsFocusLabel);
        body.AddChild(_diagnosticsHintLabel);
        return workspace;
    }

    private Control BuildWorldDetailWorkspace()
    {
        var (workspace, body) = CreateWorkspacePanel(_worldWorkspacePanels, DetailsWorkspaceId);
        body.AddChild(CreateInfoLabel("移动工厂详情", 14, FactoryUiTheme.Text));
        body.AddChild(CreateInfoLabel("在不打断当前控制模式和分屏编辑的前提下查看生命周期、挂点与内部布局摘要。", 11, FactoryUiTheme.TextSubtle));
        _factoryDetailLabel = CreateInfoLabel("等待工厂状态更新。");
        body.AddChild(_factoryDetailLabel);
        return workspace;
    }

    private Control BuildEditorToolWorkspace(string workspaceId)
    {
        var (workspace, body) = CreateWorkspacePanel(_editorWorkspacePanels, workspaceId);
        body.AddChild(CreateEditorLabel(UseLargeScenarioWorkspaces ? "建造测试面板" : "内部编辑工作区", 14, FactoryUiTheme.Text));
        body.AddChild(CreateEditorLabel(
            UseLargeScenarioWorkspaces ? "把各类建造测试工具拆到独立 panel 中，便于在 large scenario 里做定向验证。" : "内部建造、删除和结构观察集中在这里，蓝图工作流则通过独立菜单切换。",
            11,
            FactoryUiTheme.TextSubtle));

        _inspectionPanel = new PanelContainer { Visible = false };
        body.AddChild(_inspectionPanel);
        var inspectionBody = new VBoxContainer();
        inspectionBody.AddThemeConstantOverride("separation", 4);
        _inspectionPanel.AddChild(inspectionBody);
        _inspectionTitleLabel = CreateEditorLabel(string.Empty, 12, FactoryUiTheme.Text);
        _inspectionBodyLabel = CreateEditorLabel(string.Empty, 11, FactoryUiTheme.TextMuted);
        inspectionBody.AddChild(_inspectionTitleLabel);
        inspectionBody.AddChild(_inspectionBodyLabel);

        body.AddChild(CreateEditorLabel("建造分类", 12, FactoryUiTheme.TextMuted));
        BuildEditorToolbar(body);
        body.AddChild(CreateDivider());
        _editorPreviewLabel = CreateEditorLabel("内部预览：等待状态更新。", 12, FactoryUiTheme.TextMuted);
        body.AddChild(_editorPreviewLabel);
        return workspace;
    }

    private Control BuildBlueprintWorkspace()
    {
        var workspace = new Control();
        workspace.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        workspace.MouseFilter = Control.MouseFilterEnum.Ignore;
        workspace.Visible = false;

        var body = new VBoxContainer();
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride("separation", 8);
        workspace.AddChild(body);

        body.AddChild(CreateEditorLabel("蓝图工作区", 14, FactoryUiTheme.Text));
        body.AddChild(CreateEditorLabel("从顶部菜单进入蓝图面板，保存内部布局或应用共享蓝图。", 11, FactoryUiTheme.TextSubtle));

        var blueprintMargin = new MarginContainer();
        blueprintMargin.MouseFilter = Control.MouseFilterEnum.Ignore;
        blueprintMargin.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        blueprintMargin.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        blueprintMargin.AddThemeConstantOverride("margin_left", 2);
        blueprintMargin.AddThemeConstantOverride("margin_top", 2);
        blueprintMargin.AddThemeConstantOverride("margin_right", 2);
        blueprintMargin.AddThemeConstantOverride("margin_bottom", 2);
        body.AddChild(blueprintMargin);

        _blueprintPanel = new FactoryBlueprintPanel();
        _blueprintPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _blueprintPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _blueprintPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _blueprintPanel.SetDocked(true);
        _blueprintPanel.CaptureSelectionRequested += () => BlueprintCaptureSelectionRequested?.Invoke();
        _blueprintPanel.CaptureFullRequested += () => BlueprintCaptureFullRequested?.Invoke();
        _blueprintPanel.BlueprintSelected += blueprintId => BlueprintSelected?.Invoke(blueprintId);
        _blueprintPanel.SaveCaptureRequested += name => BlueprintSaveRequested?.Invoke(name);
        _blueprintPanel.ApplyActiveRequested += () => BlueprintApplyRequested?.Invoke();
        _blueprintPanel.ConfirmApplyRequested += () => BlueprintConfirmRequested?.Invoke();
        _blueprintPanel.DeleteSelectedRequested += blueprintId => BlueprintDeleteRequested?.Invoke(blueprintId);
        _blueprintPanel.CancelRequested += () => BlueprintCancelRequested?.Invoke();
        blueprintMargin.AddChild(_blueprintPanel);
        _editorWorkspacePanels[BlueprintWorkspaceId] = workspace;
        return workspace;
    }

    private (ScrollContainer workspace, VBoxContainer body) CreateWorkspacePanel(Dictionary<string, Control> registry, string workspaceId)
    {
        var workspace = new ScrollContainer();
        workspace.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        workspace.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        workspace.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
        workspace.MouseFilter = Control.MouseFilterEnum.Ignore;
        workspace.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        workspace.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        workspace.Visible = false;
        var body = new VBoxContainer();
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride("separation", 8);
        body.CustomMinimumSize = new Vector2(220.0f, 0.0f);
        workspace.AddChild(body);
        registry[workspaceId] = workspace;
        return (workspace, body);
    }

    private void ApplyWorkspaceVisibility()
    {
        var activeWorkspaceId = _workspaceChrome?.ActiveWorkspaceId ?? GetDefaultWorkspaceId();
        var hasWorldWorkspace = false;
        foreach (var pair in _worldWorkspacePanels)
        {
            var isVisible = pair.Key == activeWorkspaceId;
            pair.Value.Visible = isVisible;
            hasWorldWorkspace |= isVisible;
        }

        var hasEditorWorkspace = false;
        foreach (var pair in _editorWorkspacePanels)
        {
            var isVisible = pair.Key == activeWorkspaceId;
            pair.Value.Visible = isVisible;
            hasEditorWorkspace |= isVisible;
        }

        if (_worldWorkspaceHintLabel is not null)
        {
            _worldWorkspaceHintLabel.Visible = !hasWorldWorkspace;
        }

        if (_editorWorkspaceHintLabel is not null)
        {
            _editorWorkspaceHintLabel.Visible = !hasEditorWorkspace;
        }
    }

    private void SetActiveWorkspace(string workspaceId, bool emitSignal = true)
    {
        _workspaceChrome?.SetActiveWorkspace(workspaceId, emitSignal);
        ApplyWorkspaceVisibility();
    }

    private void UpdateLayout()
    {
        if (_topChromePanel is null || _worldFocusFrame is null || _infoPanel is null || _editorPanel is null || _editorViewport is null || _editorViewportRect is null)
        {
            return;
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        var margin = new Vector2(18.0f, 18.0f);
        var chromeHeight = 38.0f;
        var contentTop = chromeHeight + 8.0f;
        var contentHeight = Mathf.Max(240.0f, viewportSize.Y - contentTop - margin.Y);
        var worldWidth = viewportSize.X / 6.0f;

        _topChromePanel.Position = Vector2.Zero;
        _topChromePanel.Size = new Vector2(viewportSize.X, chromeHeight);

        _worldFocusFrame.Position = new Vector2(0.0f, contentTop);
        _worldFocusFrame.Size = new Vector2(worldWidth, contentHeight);

        var infoWidth = Mathf.Clamp(worldWidth - margin.X * 1.2f, 250.0f, 340.0f);
        var infoTargetHeight = contentHeight * (_editorOpen ? 0.54f : 0.72f);
        var infoHeight = Mathf.Min(contentHeight, Mathf.Max(180.0f, infoTargetHeight));
        _infoPanel.Position = new Vector2(margin.X, contentTop);
        _infoPanel.Size = new Vector2(infoWidth, infoHeight);

        var editorWidth = viewportSize.X - worldWidth - margin.X;
        var left = Mathf.Lerp(viewportSize.X + 12.0f, worldWidth, _editorProgress);
        _editorPanel.Position = new Vector2(left, contentTop);
        _editorPanel.Size = new Vector2(editorWidth, contentHeight);

        var viewportRectSize = _editorViewportRect.Size;
        if (viewportRectSize.X <= 1.0f || viewportRectSize.Y <= 1.0f)
        {
            viewportRectSize = new Vector2(
                Mathf.Max(320.0f, editorWidth - EditorSidebarWidth - 44.0f),
                Mathf.Max(180.0f, contentHeight - 32.0f));
        }

        var viewportSize2D = new Vector2I(
            Mathf.Max(320, Mathf.RoundToInt(viewportRectSize.X)),
            Mathf.Max(180, Mathf.RoundToInt(viewportRectSize.Y)));
        if (_editorViewport.Size != viewportSize2D)
        {
            _editorViewport.Size = viewportSize2D;
        }

        _detailWindow?.SetDragBounds(new Rect2(_editorPanel.Position, _editorPanel.Size));
        RefreshFocusVisuals();
    }

    private void BuildEditorToolbar(Container parent)
    {
        var tabs = new TabContainer();
        tabs.MouseFilter = Control.MouseFilterEnum.Ignore;
        tabs.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        tabs.AddThemeFontSizeOverride("font_size", CompactTabFontSize);
        tabs.AddThemeConstantOverride("side_margin", 2);
        FactoryUiTheme.ApplyTabContainerTheme(tabs);
        parent.AddChild(tabs);

        for (var categoryIndex = 0; categoryIndex < EditorPaletteCategories.Length; categoryIndex++)
        {
            var category = EditorPaletteCategories[categoryIndex];
            var section = new VBoxContainer();
            section.Name = category.Title;
            section.MouseFilter = Control.MouseFilterEnum.Ignore;
            section.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            section.AddThemeConstantOverride("separation", 4);
            tabs.AddChild(section);

            var paletteGrid = new GridContainer();
            paletteGrid.Columns = 2;
            paletteGrid.MouseFilter = Control.MouseFilterEnum.Ignore;
            paletteGrid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            paletteGrid.AddThemeConstantOverride("h_separation", 6);
            paletteGrid.AddThemeConstantOverride("v_separation", 6);
            section.AddChild(paletteGrid);

            for (var kindIndex = 0; kindIndex < category.Kinds.Length; kindIndex++)
            {
                var kind = category.Kinds[kindIndex];
                var button = new Button
                {
                    Text = GetKindLabel(kind),
                    ToggleMode = true,
                    MouseFilter = Control.MouseFilterEnum.Stop,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    CustomMinimumSize = new Vector2(0.0f, 28.0f)
                };
                button.AddThemeFontSizeOverride("font_size", 11);
                FactoryUiTheme.ApplyButtonTheme(button, compact: true);
                button.Pressed += () => EditorPaletteSelected?.Invoke(kind);
                paletteGrid.AddChild(button);
                _paletteButtons[kind] = button;
            }
        }

        var rotateRow = new HBoxContainer();
        rotateRow.AddThemeConstantOverride("separation", 6);
        parent.AddChild(rotateRow);

        var rotateLeft = new Button { Text = "旋左", MouseFilter = Control.MouseFilterEnum.Stop, CustomMinimumSize = new Vector2(0.0f, 28.0f), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        rotateLeft.AddThemeFontSizeOverride("font_size", 11);
        FactoryUiTheme.ApplyButtonTheme(rotateLeft, compact: true);
        rotateLeft.Pressed += () => EditorRotateRequested?.Invoke(-1);
        rotateRow.AddChild(rotateLeft);

        var rotateRight = new Button { Text = "旋右", MouseFilter = Control.MouseFilterEnum.Stop, CustomMinimumSize = new Vector2(0.0f, 28.0f), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        rotateRight.AddThemeFontSizeOverride("font_size", 11);
        FactoryUiTheme.ApplyButtonTheme(rotateRight, compact: true);
        rotateRight.Pressed += () => EditorRotateRequested?.Invoke(1);
        rotateRow.AddChild(rotateRight);
    }

    private void RefreshFocusVisuals()
    {
        if (_worldFocusFrame is null || _editorPanel is null)
        {
            return;
        }

        var showFocus = _editorOpen && _editorProgress > 0.01f;
        _worldFocusFrame.Visible = showFocus;
        if (_worldFocusFrameStyle is not null)
        {
            FactoryUiTheme.ConfigureOutlineStyle(_worldFocusFrameStyle, showFocus && !_editorFocused ? WorldFocusColor : Colors.Transparent, borderWidth: 3, contentMargin: 12);
        }

        if (_editorPanelStyle is not null)
        {
            FactoryUiTheme.ConfigurePanelStyle(_editorPanelStyle, FactoryUiTheme.SurfaceOverlay, showFocus && _editorFocused ? EditorFocusColor : Colors.Transparent, borderWidth: 2, cornerRadius: FactoryUiTheme.RadiusNone, contentMargin: 12);
        }
    }

    private void RefreshPaletteButtons(BuildPrototypeKind? selectedKind)
    {
        foreach (var pair in _paletteButtons)
        {
            pair.Value.ButtonPressed = selectedKind.HasValue && pair.Key == selectedKind.Value;
        }
    }

    private static bool BlocksInteractiveInput(Control? control, Control? container)
    {
        if (control is null || container is null)
        {
            return false;
        }

        var current = control;
        while (current is not null)
        {
            if (current == container)
            {
                return false;
            }

            if (current is BaseButton or ItemList or LineEdit)
            {
                return IsInside(current, container);
            }

            current = current.GetParent() as Control;
        }

        return false;
    }

    private static bool ContainsScreenPoint(Control? control, Vector2 screenPoint)
    {
        return control is not null
            && control.Visible
            && control.GetGlobalRect().HasPoint(screenPoint);
    }

    private static bool IsInside(Control control, Control container)
    {
        var current = control;
        while (current is not null)
        {
            if (current == container)
            {
                return true;
            }

            current = current.GetParent() as Control;
        }

        return false;
    }

    private static Label CreateInfoLabel(string text, int fontSize = 12, Color? color = null)
    {
        var label = new Label { MouseFilter = Control.MouseFilterEnum.Ignore, Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.Modulate = color ?? FactoryUiTheme.TextMuted;
        return label;
    }

    private static Label CreateEditorLabel(string text, int fontSize = 13, Color? color = null)
    {
        var label = new Label { MouseFilter = Control.MouseFilterEnum.Ignore, Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.Modulate = color ?? FactoryUiTheme.TextMuted;
        return label;
    }

    private static ColorRect CreateDivider()
    {
        return FactoryUiTheme.CreateDivider();
    }

    private static StyleBoxFlat CreateOutlineOnlyStyle(Color borderColor)
    {
        return FactoryUiTheme.CreateOutlineStyle(borderColor, borderWidth: 3, contentMargin: 12);
    }

    private static StyleBoxFlat CreatePanelStyle(Color borderColor)
    {
        return FactoryUiTheme.CreatePanelStyle(FactoryUiTheme.SurfaceOverlay, borderColor, borderWidth: 2, cornerRadius: FactoryUiTheme.RadiusNone, contentMargin: 12);
    }

    private static string GetKindLabel(BuildPrototypeKind kind)
    {
        return kind switch
        {
            BuildPrototypeKind.Producer => "生产器",
            BuildPrototypeKind.Belt => "传送带",
            BuildPrototypeKind.Splitter => "分流器",
            BuildPrototypeKind.Merger => "合并器",
            BuildPrototypeKind.Bridge => "跨桥",
            BuildPrototypeKind.Loader => "装载器",
            BuildPrototypeKind.Unloader => "卸载器",
            BuildPrototypeKind.Storage => "仓储",
            BuildPrototypeKind.LargeStorageDepot => "大型仓储",
            BuildPrototypeKind.Inserter => "机械臂",
            BuildPrototypeKind.Wall => "墙体",
            BuildPrototypeKind.AmmoAssembler => "弹药组装器",
            BuildPrototypeKind.GunTurret => "机枪炮塔",
            BuildPrototypeKind.HeavyGunTurret => "重型炮塔",
            BuildPrototypeKind.OutputPort => "输出端口",
            BuildPrototypeKind.InputPort => "输入端口",
            BuildPrototypeKind.MiningInputPort => "采矿输入端口",
            BuildPrototypeKind.Sink => "回收器",
            BuildPrototypeKind.Generator => "发电机",
            BuildPrototypeKind.PowerPole => "电线杆",
            BuildPrototypeKind.Smelter => "熔炉",
            BuildPrototypeKind.Assembler => "组装机",
            _ => kind.ToString()
        };
    }
}
