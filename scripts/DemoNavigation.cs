using Godot;
using System.Collections.Generic;

public static class DemoLaunchOptions
{
    public static string? FactoryWorldMapPath { get; set; }
    public static string? MobileWorldMapPath { get; set; }
    public static string? MobileInteriorMapPath { get; set; }

    public static string ResolveFactoryWorldMapPath()
    {
        return ResolveOrFallback(FactoryWorldMapPath, FactoryMapPaths.StaticSandboxWorld);
    }

    public static string ResolveMobileWorldMapPath()
    {
        return ResolveOrFallback(MobileWorldMapPath, FactoryMapPaths.FocusedMobileWorld);
    }

    public static string ResolveMobileInteriorMapPath()
    {
        return ResolveOrFallback(MobileInteriorMapPath, FactoryMapPaths.FocusedMobileInterior);
    }

    private static string ResolveOrFallback(string? selectedPath, string fallbackPath)
    {
        if (!string.IsNullOrWhiteSpace(selectedPath) && Godot.FileAccess.FileExists(selectedPath))
        {
            return selectedPath!;
        }

        return fallbackPath;
    }
}

public sealed class DemoSceneEntry
{
    public DemoSceneEntry(string id, string title, string description, string scenePath)
    {
        Id = id;
        Title = title;
        Description = description;
        ScenePath = scenePath;
    }

    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public string ScenePath { get; }
}

public static class DemoCatalog
{
    public const string LauncherScenePath = "res://scenes/demo_launcher.tscn";

    private static readonly DemoSceneEntry[] LauncherEntriesInternal =
    {
        new(
            "factory-demo",
            "Factory Demo",
            "统一工厂沙盒：支持静态工厂建造、移动工厂部署与内部编辑、蓝图、战斗、地图加载与运行时存档。",
            "res://scenes/factory_demo.tscn"),
        new(
            "mobile-factory-test-scenario",
            "Mobile Factory Scenario",
            "更大规模的移动工厂测试场景，用于观察多工厂活动与回归表现。",
            "res://scenes/mobile_factory_test_scenario.tscn"),
        new(
            "ui-showcase",
            "UI Showcase",
            "控制 UI 动效、配色、数据刷新与交互控件的独立展示场景。",
            "res://scenes/ui_showcase.tscn")
    };

    public static IReadOnlyList<DemoSceneEntry> LauncherEntries => LauncherEntriesInternal;
}

public partial class LauncherNavigationOverlay : CanvasLayer
{
    [Export]
    public string ButtonText { get; set; } = "返回 Launcher";

    [Export]
    public float BottomMargin { get; set; } = 16.0f;

    [Export]
    public float RightMargin { get; set; } = 16.0f;

    public override void _Ready()
    {
        Name = "LauncherNavigationOverlay";
        Layer = 50;

        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(root);

        var button = new Button();
        button.Text = ButtonText;
        button.TooltipText = "回到 demo 选择主界面。";
        button.MouseFilter = Control.MouseFilterEnum.Stop;
        button.AnchorLeft = 1.0f;
        button.AnchorRight = 1.0f;
        button.AnchorTop = 1.0f;
        button.AnchorBottom = 1.0f;
        button.OffsetLeft = -164.0f - RightMargin;
        button.OffsetRight = -RightMargin;
        button.OffsetTop = -38.0f - BottomMargin;
        button.OffsetBottom = -BottomMargin;
        button.AddThemeFontSizeOverride("font_size", 14);
        FactoryUiTheme.ApplyButtonTheme(button);
        button.Pressed += ReturnToLauncher;
        root.AddChild(button);
    }

    private void ReturnToLauncher()
    {
        if (GetTree().CurrentScene?.SceneFilePath == DemoCatalog.LauncherScenePath)
        {
            return;
        }

        var error = GetTree().ChangeSceneToFile(DemoCatalog.LauncherScenePath);
        if (error != Error.Ok)
        {
            GD.PushError($"Unable to return to launcher scene '{DemoCatalog.LauncherScenePath}': {error}");
        }
    }
}
