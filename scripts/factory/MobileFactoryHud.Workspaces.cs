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
            ? "切换总览、场景验证、蓝图、诊断、存档和工厂详情工作区。"
            : "切换指挥、内部编辑、验证、蓝图、存档和工厂详情工作区。";
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
        workspaceHost.AddChild(BuildEditorToolWorkspace(GetPrimaryEditorWorkspaceId()));
        if (!UseLargeScenarioWorkspaces)
        {
            workspaceHost.AddChild(BuildTestingWorkspace());
        }
        workspaceHost.AddChild(BuildBlueprintWorkspace());
        workspaceHost.AddChild(BuildSaveWorkspace());

        _worldWorkspaceHintLabel = CreateInfoLabel("当前工作区主要显示在右侧编辑面板。", 11, FactoryUiTheme.TextSubtle);
        _worldWorkspaceHintLabel.Visible = false;
        body.AddChild(_worldWorkspaceHintLabel);
    }

    private void BuildEditorPanel()
    {
        var viewportPanel = new PanelContainer();
        viewportPanel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        viewportPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        viewportPanel.ClipContents = true;
        _editorViewportPanelStyle = CreatePanelStyle(FactoryUiTheme.Border);
        viewportPanel.AddThemeStyleboxOverride("panel", _editorViewportPanelStyle);
        AddChild(viewportPanel);
        _editorViewportPanel = viewportPanel;

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

        AddChild(viewport);

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

        var sidebarScroll = new ScrollContainer();
        sidebarScroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        sidebarScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        sidebarScroll.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
        sidebarScroll.MouseFilter = Control.MouseFilterEnum.Ignore;
        sidebarScroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        sidebarScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        chrome.AddChild(sidebarScroll);

        var sidebar = new VBoxContainer();
        sidebar.MouseFilter = Control.MouseFilterEnum.Ignore;
        sidebar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        sidebar.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        sidebar.AddThemeConstantOverride("separation", 6);
        sidebar.CustomMinimumSize = new Vector2(EditorSidebarWidth - 20.0f, 0.0f);
        sidebarScroll.AddChild(sidebar);

        sidebar.AddChild(CreateEditorLabel("编辑操作面板", 14, FactoryUiTheme.Text));
        sidebar.AddChild(CreateEditorLabel("进入编辑模式后，建造、删除、旋转和蓝图快捷动作集中在这里；主工作区继续承载总览与流程页。", 11, FactoryUiTheme.TextSubtle));

        var sessionRow = new GridContainer();
        sessionRow.Columns = 2;
        sessionRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        sessionRow.AddThemeConstantOverride("h_separation", 6);
        sessionRow.AddThemeConstantOverride("v_separation", 6);
        sidebar.AddChild(sessionRow);

        _editModeButton = CreateEditorActionButton("进入编辑模式 (F)", () => EditModeToggleRequested?.Invoke());
        _editModeButton.ToggleMode = true;
        sessionRow.AddChild(_editModeButton);
        sessionRow.AddChild(CreateEditorActionButton("交互模式", () => EditorInteractionModeRequested?.Invoke()));
        sessionRow.AddChild(CreateEditorActionButton("删除模式", () => EditorDeleteModeRequested?.Invoke()));
        sessionRow.AddChild(CreateEditorActionButton("蓝图工作区", () => SetActiveWorkspace(BlueprintWorkspaceId)));

        var quickRow = new GridContainer();
        quickRow.Columns = 2;
        quickRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        quickRow.AddThemeConstantOverride("h_separation", 6);
        quickRow.AddThemeConstantOverride("v_separation", 6);
        sidebar.AddChild(quickRow);
        quickRow.AddChild(CreateEditorActionButton("存档工作区", () => SetActiveWorkspace(SavesWorkspaceId)));
        quickRow.AddChild(CreateEditorActionButton("工厂详情", () => SetActiveWorkspace(DetailsWorkspaceId)));

        _editorModeLabel = CreateEditorLabel(string.Empty, 12, FactoryUiTheme.TextMuted);
        _selectionLabel = CreateEditorLabel(string.Empty, 12, FactoryUiTheme.Text);
        _selectionTargetLabel = CreateEditorLabel(string.Empty, 12, FactoryUiTheme.TextMuted);
        _portStatusLabel = CreateEditorLabel(string.Empty, 12, FactoryUiTheme.TextSubtle);
        sidebar.AddChild(_editorModeLabel);
        sidebar.AddChild(_selectionLabel);
        sidebar.AddChild(_selectionTargetLabel);
        sidebar.AddChild(_portStatusLabel);

        _inspectionPanel = new PanelContainer { Visible = false };
        sidebar.AddChild(_inspectionPanel);
        var inspectionBody = new VBoxContainer();
        inspectionBody.AddThemeConstantOverride("separation", 4);
        _inspectionPanel.AddChild(inspectionBody);
        _inspectionTitleLabel = CreateEditorLabel(string.Empty, 12, FactoryUiTheme.Text);
        _inspectionBodyLabel = CreateEditorLabel(string.Empty, 11, FactoryUiTheme.TextMuted);
        inspectionBody.AddChild(_inspectionTitleLabel);
        inspectionBody.AddChild(_inspectionBodyLabel);

        sidebar.AddChild(CreateDivider());
        sidebar.AddChild(CreateEditorLabel("建造分类", 12, FactoryUiTheme.TextMuted));
        BuildEditorToolbar(sidebar);
        sidebar.AddChild(CreateDivider());
        _editorPreviewLabel = CreateEditorLabel("内部预览：等待状态更新。", 12, FactoryUiTheme.TextMuted);
        sidebar.AddChild(_editorPreviewLabel);
        sidebar.AddChild(CreateEditorLabel("提示：工作区用于切换总览、蓝图、存档、详情；编辑模式按钮只负责打开或关闭当前舱内编辑会话。", 11, FactoryUiTheme.TextSubtle));

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
                new FactoryWorkspaceDescriptor(BuildTestWorkspaceId, "场景验证"),
                new FactoryWorkspaceDescriptor(BlueprintWorkspaceId, "蓝图"),
                new FactoryWorkspaceDescriptor(DiagnosticsWorkspaceId, "诊断"),
                new FactoryWorkspaceDescriptor(SavesWorkspaceId, "存档"),
                new FactoryWorkspaceDescriptor(DetailsWorkspaceId, "工厂详情")
            }
            : new[]
            {
                new FactoryWorkspaceDescriptor(CommandWorkspaceId, "指挥"),
                new FactoryWorkspaceDescriptor(EditorWorkspaceId, "内部编辑"),
                new FactoryWorkspaceDescriptor(TestingWorkspaceId, "验证"),
                new FactoryWorkspaceDescriptor(BlueprintWorkspaceId, "蓝图"),
                new FactoryWorkspaceDescriptor(SavesWorkspaceId, "存档"),
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

        var actionsGrid = new GridContainer();
        actionsGrid.Columns = 2;
        actionsGrid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        actionsGrid.AddThemeConstantOverride("h_separation", 6);
        actionsGrid.AddThemeConstantOverride("v_separation", 6);
        body.AddChild(actionsGrid);

        _factoryCommandButton = new Button
        {
            Text = "工厂控制 (C)",
            ToggleMode = true,
            MouseFilter = Control.MouseFilterEnum.Stop,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0.0f, 34.0f)
        };
        _factoryCommandButton.AddThemeFontSizeOverride("font_size", 11);
        FactoryUiTheme.ApplyButtonTheme(_factoryCommandButton);
        _factoryCommandButton.Pressed += () => FactoryCommandModeToggleRequested?.Invoke();
        actionsGrid.AddChild(_factoryCommandButton);

        _observerButton = new Button
        {
            Text = "观察模式 (Tab)",
            ToggleMode = true,
            MouseFilter = Control.MouseFilterEnum.Stop,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0.0f, 34.0f)
        };
        _observerButton.AddThemeFontSizeOverride("font_size", 11);
        FactoryUiTheme.ApplyButtonTheme(_observerButton);
        _observerButton.Pressed += () => ObserverModeToggleRequested?.Invoke();
        actionsGrid.AddChild(_observerButton);

        _deployButton = new Button
        {
            Text = "部署预览 (G)",
            ToggleMode = true,
            MouseFilter = Control.MouseFilterEnum.Stop,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0.0f, 34.0f)
        };
        _deployButton.AddThemeFontSizeOverride("font_size", 11);
        FactoryUiTheme.ApplyButtonTheme(_deployButton);
        _deployButton.Pressed += () => DeployModeToggleRequested?.Invoke();
        actionsGrid.AddChild(_deployButton);

        var utilityGrid = new GridContainer();
        utilityGrid.Columns = 2;
        utilityGrid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        utilityGrid.AddThemeConstantOverride("h_separation", 6);
        utilityGrid.AddThemeConstantOverride("v_separation", 6);
        body.AddChild(utilityGrid);
        utilityGrid.AddChild(CreateEditorActionButton("编辑模式 (F)", () => EditModeToggleRequested?.Invoke()));
        utilityGrid.AddChild(CreateEditorActionButton("工厂详情", () => SetActiveWorkspace(DetailsWorkspaceId)));

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
        body.AddChild(CreateEditorLabel(UseLargeScenarioWorkspaces ? "场景验证面板" : "内部编辑工作区", 14, FactoryUiTheme.Text));
        body.AddChild(CreateEditorLabel(
            UseLargeScenarioWorkspaces ? "用这个工作区概览当前场景的编辑入口、验证路径和相关工作区跳转；真正的编辑工具会在开启编辑会话后出现在独立操作面板里。" : "这里负责说明编辑流程与入口；真正的建造、删除、旋转和蓝图快捷操作会在开启编辑模式后进入独立操作面板。",
            11,
            FactoryUiTheme.TextSubtle));

        var sessionGrid = new GridContainer();
        sessionGrid.Columns = 2;
        sessionGrid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        sessionGrid.AddThemeConstantOverride("h_separation", 6);
        sessionGrid.AddThemeConstantOverride("v_separation", 6);
        body.AddChild(sessionGrid);
        sessionGrid.AddChild(CreateEditorActionButton("进入/退出编辑", () => EditModeToggleRequested?.Invoke()));
        sessionGrid.AddChild(CreateEditorActionButton("打开蓝图页", () => SetActiveWorkspace(BlueprintWorkspaceId)));
        sessionGrid.AddChild(CreateEditorActionButton("打开存档页", () => SetActiveWorkspace(SavesWorkspaceId)));
        sessionGrid.AddChild(CreateEditorActionButton("查看工厂详情", () => SetActiveWorkspace(DetailsWorkspaceId)));

        body.AddChild(CreateDivider());
        body.AddChild(CreateEditorLabel("编辑会话说明", 12, FactoryUiTheme.TextMuted));
        body.AddChild(CreateEditorLabel("1. 先在这里或指挥页打开编辑模式。", 11, FactoryUiTheme.TextSubtle));
        body.AddChild(CreateEditorLabel("2. 编辑视口负责放置与选中，右侧独立操作面板负责建造、删除、旋转和蓝图快捷动作。", 11, FactoryUiTheme.TextSubtle));
        body.AddChild(CreateEditorLabel("3. 顶部工作区只切换总览、蓝图、存档、验证、详情，不会自动替你打开或关闭编辑模式。", 11, FactoryUiTheme.TextSubtle));
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
        body.AddChild(CreateEditorLabel(FactoryPersistencePaths.BuildBlueprintPersistenceHint(), 11, FactoryUiTheme.TextSubtle));

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
        _blueprintPanel.CaptureFullRequested += () => BlueprintCaptureFullRequested?.Invoke();
        _blueprintPanel.BlueprintSelected += blueprintId => BlueprintSelected?.Invoke(blueprintId);
        _blueprintPanel.SaveCaptureRuntimeRequested += name => BlueprintRuntimeSaveRequested?.Invoke(name);
        _blueprintPanel.SaveCaptureSourceRequested += name => BlueprintSourceSaveRequested?.Invoke(name);
        _blueprintPanel.ApplyActiveRequested += () => BlueprintApplyRequested?.Invoke();
        _blueprintPanel.ConfirmApplyRequested += () => BlueprintConfirmRequested?.Invoke();
        _blueprintPanel.DeleteSelectedRequested += blueprintId => BlueprintDeleteRequested?.Invoke(blueprintId);
        _blueprintPanel.CancelRequested += () => BlueprintCancelRequested?.Invoke();
        blueprintMargin.AddChild(_blueprintPanel);
        _editorWorkspacePanels[BlueprintWorkspaceId] = workspace;
        return workspace;
    }

    private Control BuildTestingWorkspace()
    {
        var (workspace, body) = CreateWorkspacePanel(_editorWorkspacePanels, TestingWorkspaceId);
        body.AddChild(CreateEditorLabel("验证工作区", 14, FactoryUiTheme.Text));
        body.AddChild(CreateEditorLabel("把 focused mobile demo 的观察、存读档和持久化状态整理成一个独立面板，便于像普通 sandbox 一样做回归验证。", 11, FactoryUiTheme.TextSubtle));

        var jumpGrid = new GridContainer();
        jumpGrid.Columns = 2;
        jumpGrid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        jumpGrid.AddThemeConstantOverride("h_separation", 6);
        jumpGrid.AddThemeConstantOverride("v_separation", 6);
        body.AddChild(jumpGrid);
        jumpGrid.AddChild(CreateEditorActionButton("打开内部编辑", () => SetActiveWorkspace(EditorWorkspaceId)));
        jumpGrid.AddChild(CreateEditorActionButton("打开蓝图页", () => SetActiveWorkspace(BlueprintWorkspaceId)));
        jumpGrid.AddChild(CreateEditorActionButton("打开存档页", () => SetActiveWorkspace(SavesWorkspaceId)));
        jumpGrid.AddChild(CreateEditorActionButton("返回指挥页", () => SetActiveWorkspace(CommandWorkspaceId)));

        body.AddChild(CreateDivider());
        body.AddChild(CreateEditorLabel("地图导出", 12, FactoryUiTheme.TextMuted));
        var exportGrid = new GridContainer();
        exportGrid.Columns = 2;
        exportGrid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        exportGrid.AddThemeConstantOverride("h_separation", 6);
        exportGrid.AddThemeConstantOverride("v_separation", 6);
        exportGrid.AddChild(CreateEditorActionButton("导出世界副本", () => WorldMapSaveRequested?.Invoke()));
        exportGrid.AddChild(CreateEditorActionButton("覆盖世界源", () => WorldMapSourceSaveRequested?.Invoke()));
        exportGrid.AddChild(CreateEditorActionButton("导出内部副本", () => InteriorMapSaveRequested?.Invoke()));
        exportGrid.AddChild(CreateEditorActionButton("覆盖内部源", () => InteriorMapSourceSaveRequested?.Invoke()));
        body.AddChild(exportGrid);

        body.AddChild(CreateDivider());
        _testingEditorStateLabel = CreateEditorLabel("验证状态：等待工厂状态更新。", 11, FactoryUiTheme.TextMuted);
        _testingSelectionTargetLabel = CreateEditorLabel("[TARGET] 验证目标：未选中建筑", 11, FactoryUiTheme.TextMuted);
        _testingPreviewLabel = CreateEditorLabel("[BLOCK] 验证提示：等待状态更新。", 11, FactoryUiTheme.TextMuted);
        _testingPortStatusLabel = CreateEditorLabel("[PORT] 等待端口状态更新。", 11, FactoryUiTheme.TextSubtle);
        _testingHintLabel = CreateEditorLabel("等待操作提示更新。", 11, FactoryUiTheme.TextSubtle);
        _testingPersistenceLabel = CreateEditorLabel(FactoryPersistencePaths.BuildPersistenceSummary(includeInteriorMap: true), 11, FactoryUiTheme.TextSubtle);
        body.AddChild(_testingEditorStateLabel);
        body.AddChild(_testingSelectionTargetLabel);
        body.AddChild(_testingPreviewLabel);
        body.AddChild(_testingPortStatusLabel);
        body.AddChild(_testingHintLabel);
        body.AddChild(_testingPersistenceLabel);

        body.AddChild(CreateDivider());
        body.AddChild(CreateEditorLabel("建议路径：观察部署提示与端口连接状态，打开内部编辑验证放置/拆除，再切到存档页直接保存或读回当前快照。", 11, FactoryUiTheme.TextSubtle));

        return workspace;
    }

    private Control BuildSaveWorkspace()
    {
        var (workspace, body) = CreateWorkspacePanel(_editorWorkspacePanels, SavesWorkspaceId);
        body.AddChild(CreateEditorLabel("存档工作区", 14, FactoryUiTheme.Text));
        body.AddChild(CreateEditorLabel("列出当前运行时存档，并显示每个站点的当前地图路径、工程路径和运行时路径，方便排查快照来源。", 11, FactoryUiTheme.TextSubtle));

        body.AddChild(CreateDivider());
        body.AddChild(CreateEditorLabel("快速存读", 12, FactoryUiTheme.TextMuted));
        var runtimeSlotEdit = new LineEdit
        {
            Text = "progress-1",
            PlaceholderText = "输入存档名",
            MouseFilter = Control.MouseFilterEnum.Stop,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        FactoryUiTheme.ApplyLineEditTheme(runtimeSlotEdit);
        body.AddChild(runtimeSlotEdit);

        var runtimeGrid = new GridContainer();
        runtimeGrid.Columns = 2;
        runtimeGrid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        runtimeGrid.AddThemeConstantOverride("h_separation", 6);
        runtimeGrid.AddThemeConstantOverride("v_separation", 6);
        runtimeGrid.AddChild(CreateEditorActionButton("保存进度", () => RuntimeSaveRequested?.Invoke(runtimeSlotEdit.Text?.Trim() ?? string.Empty)));
        runtimeGrid.AddChild(CreateEditorActionButton("读取进度", () => RuntimeLoadRequested?.Invoke(runtimeSlotEdit.Text?.Trim() ?? string.Empty)));
        runtimeGrid.AddChild(CreateEditorActionButton("刷新列表", () => RuntimeSaveLibraryRefreshRequested?.Invoke()));
        runtimeGrid.AddChild(CreateEditorActionButton("返回建造页", () => SetActiveWorkspace(GetPrimaryEditorWorkspaceId())));
        body.AddChild(runtimeGrid);

        if (UseLargeScenarioWorkspaces)
        {
            body.AddChild(CreateEditorLabel("提示：large scenario 目前只支持浏览存档列表，不支持保存/读取运行时进度。", 11, FactoryUiTheme.StatusWarn));
        }

        body.AddChild(CreateDivider());
        _saveLibraryStatusLabel = CreateEditorLabel(FactoryPersistencePaths.BuildPersistenceSummary(includeInteriorMap: true), 11, FactoryUiTheme.TextSubtle);
        body.AddChild(_saveLibraryStatusLabel);

        body.AddChild(CreateDivider());
        body.AddChild(CreateEditorLabel("存档列表", 12, FactoryUiTheme.TextMuted));
        var list = new VBoxContainer();
        list.MouseFilter = Control.MouseFilterEnum.Ignore;
        list.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        list.AddThemeConstantOverride("separation", 6);
        _saveLibraryList = list;
        body.AddChild(list);
        list.AddChild(CreateEditorLabel("正在读取存档列表...", 11, FactoryUiTheme.TextMuted));

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
        if (_topChromePanel is null || _worldFocusFrame is null || _infoPanel is null || _editorViewportPanel is null || _editorPanel is null || _editorViewport is null || _editorViewportRect is null)
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

        var infoWidth = Mathf.Clamp(viewportSize.X * 0.28f, 300.0f, 400.0f);
        var infoHeight = Mathf.Min(contentHeight, Mathf.Max(220.0f, contentHeight * 0.78f));
        _infoPanel.Position = new Vector2(margin.X, contentTop);
        _infoPanel.Size = new Vector2(infoWidth, infoHeight);

        var editorOperationWidth = Mathf.Clamp(EditorSidebarWidth + 24.0f, 296.0f, 340.0f);
        var editorViewportWidth = Mathf.Max(320.0f, viewportSize.X - infoWidth - editorOperationWidth - margin.X * 3.0f);
        var viewportClosedX = viewportSize.X + 20.0f;
        var viewportOpenX = infoWidth + margin.X * 2.0f;
        var viewportX = Mathf.Lerp(viewportClosedX, viewportOpenX, _editorProgress);
        var operationX = viewportX + editorViewportWidth + 10.0f;

        _editorViewportPanel.Position = new Vector2(viewportX, contentTop);
        _editorViewportPanel.Size = new Vector2(editorViewportWidth, contentHeight);

        _editorPanel.Position = new Vector2(operationX, contentTop);
        _editorPanel.Size = new Vector2(editorOperationWidth, contentHeight);

        var viewportRectSize = _editorViewportRect.Size;
        if (viewportRectSize.X <= 1.0f || viewportRectSize.Y <= 1.0f)
        {
            viewportRectSize = new Vector2(
                Mathf.Max(320.0f, editorViewportWidth - 20.0f),
                Mathf.Max(180.0f, contentHeight - 32.0f));
        }

        var viewportSize2D = new Vector2I(
            Mathf.Max(320, Mathf.RoundToInt(viewportRectSize.X)),
            Mathf.Max(180, Mathf.RoundToInt(viewportRectSize.Y)));
        if (_editorViewport.Size != viewportSize2D)
        {
            _editorViewport.Size = viewportSize2D;
        }

        var dragLeft = Mathf.Min(_editorViewportPanel.Position.X, _editorPanel.Position.X);
        var dragTop = Mathf.Min(_editorViewportPanel.Position.Y, _editorPanel.Position.Y);
        var dragRight = Mathf.Max(_editorViewportPanel.Position.X + _editorViewportPanel.Size.X, _editorPanel.Position.X + _editorPanel.Size.X);
        var dragBottom = Mathf.Max(_editorViewportPanel.Position.Y + _editorViewportPanel.Size.Y, _editorPanel.Position.Y + _editorPanel.Size.Y);
        _detailWindow?.SetDragBounds(new Rect2(new Vector2(dragLeft, dragTop), new Vector2(dragRight - dragLeft, dragBottom - dragTop)));
        RefreshFocusVisuals();
    }

    private void BuildEditorToolbar(Container parent)
    {
        var catalog = FactoryIndustrialStandards.GetBuildCatalog(FactorySiteKind.Interior);
        var tabs = new TabContainer();
        tabs.MouseFilter = Control.MouseFilterEnum.Ignore;
        tabs.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        tabs.AddThemeFontSizeOverride("font_size", CompactTabFontSize);
        tabs.AddThemeConstantOverride("side_margin", 2);
        FactoryUiTheme.ApplyTabContainerTheme(tabs);
        parent.AddChild(tabs);

        for (var categoryIndex = 0; categoryIndex < catalog.Categories.Count; categoryIndex++)
        {
            var category = catalog.Categories[categoryIndex];
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

            for (var kindIndex = 0; kindIndex < category.Kinds.Count; kindIndex++)
            {
                var kind = category.Kinds[kindIndex];
                var button = new Button
                {
                    Text = FactoryIndustrialStandards.GetBuildPaletteLabel(kind, FactorySiteKind.Interior),
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

    private Button CreateEditorActionButton(string text, Action pressed)
    {
        var button = new Button
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Stop,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0.0f, 28.0f)
        };
        button.AddThemeFontSizeOverride("font_size", 11);
        FactoryUiTheme.ApplyButtonTheme(button, compact: true);
        button.Pressed += pressed;
        return button;
    }

    private void RefreshFocusVisuals()
    {
        if (_worldFocusFrame is null || _editorViewportPanel is null || _editorPanel is null)
        {
            return;
        }

        var showFocus = _editorOpen && _editorProgress > 0.01f;
        _worldFocusFrame.Visible = showFocus;
        if (_worldFocusFrameStyle is not null)
        {
            FactoryUiTheme.ConfigureOutlineStyle(_worldFocusFrameStyle, showFocus && !_editorViewportFocused && !_editorOperationFocused ? WorldFocusColor : Colors.Transparent, borderWidth: 3, contentMargin: 12);
        }

        if (_editorViewportPanelStyle is not null)
        {
            FactoryUiTheme.ConfigurePanelStyle(_editorViewportPanelStyle, FactoryUiTheme.SurfaceOverlay, showFocus && _editorViewportFocused ? EditorFocusColor : FactoryUiTheme.Border, borderWidth: 2, cornerRadius: FactoryUiTheme.RadiusNone, contentMargin: 12);
        }

        if (_editorPanelStyle is not null)
        {
            FactoryUiTheme.ConfigurePanelStyle(_editorPanelStyle, FactoryUiTheme.SurfaceOverlay, showFocus && _editorOperationFocused ? EditorFocusColor : Colors.Transparent, borderWidth: 2, cornerRadius: FactoryUiTheme.RadiusNone, contentMargin: 12);
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

}
