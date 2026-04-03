using Godot;
using System.Collections.Generic;

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
            "Factory Sandbox",
            "静态工厂 sandbox，现已包含矿点、发电、真实制造链，以及保留的仓储/蓝图/战斗回归线。",
            "res://scenes/factory_demo.tscn"),
        new(
            "mobile-factory-demo",
            "Mobile Factory Demo",
            "聚焦移动工厂的部署、回收、世界交互与内部编辑分屏。",
            "res://scenes/mobile_factory_demo.tscn"),
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
    public float TopMargin { get; set; } = 16.0f;

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
        button.OffsetLeft = -164.0f - RightMargin;
        button.OffsetRight = -RightMargin;
        button.OffsetTop = TopMargin;
        button.OffsetBottom = TopMargin + 38.0f;
        button.AddThemeFontSizeOverride("font_size", 14);
        button.AddThemeColorOverride("font_color", Colors.White);
        button.Pressed += ReturnToLauncher;
        root.AddChild(button);

        var style = new StyleBoxFlat();
        style.BgColor = new Color("14324A");
        style.BorderColor = new Color("5EEAD4");
        style.BorderWidthLeft = 1;
        style.BorderWidthTop = 1;
        style.BorderWidthRight = 1;
        style.BorderWidthBottom = 1;
        style.CornerRadiusTopLeft = 10;
        style.CornerRadiusTopRight = 10;
        style.CornerRadiusBottomRight = 10;
        style.CornerRadiusBottomLeft = 10;
        style.ContentMarginLeft = 14;
        style.ContentMarginRight = 14;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", CreateVariant(style, new Color("1D4C66"), new Color("A7F3D0")));
        button.AddThemeStyleboxOverride("pressed", CreateVariant(style, new Color("0F2436"), new Color("99F6E4")));
        button.AddThemeStyleboxOverride("focus", CreateVariant(style, new Color("1D4C66"), new Color("CCFBF1")));
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

    private static StyleBoxFlat CreateVariant(StyleBoxFlat source, Color background, Color border)
    {
        var clone = new StyleBoxFlat();
        clone.BgColor = background;
        clone.BorderColor = border;
        clone.BorderWidthLeft = source.BorderWidthLeft;
        clone.BorderWidthTop = source.BorderWidthTop;
        clone.BorderWidthRight = source.BorderWidthRight;
        clone.BorderWidthBottom = source.BorderWidthBottom;
        clone.CornerRadiusTopLeft = source.CornerRadiusTopLeft;
        clone.CornerRadiusTopRight = source.CornerRadiusTopRight;
        clone.CornerRadiusBottomRight = source.CornerRadiusBottomRight;
        clone.CornerRadiusBottomLeft = source.CornerRadiusBottomLeft;
        clone.ContentMarginLeft = source.ContentMarginLeft;
        clone.ContentMarginRight = source.ContentMarginRight;
        clone.ContentMarginTop = source.ContentMarginTop;
        clone.ContentMarginBottom = source.ContentMarginBottom;
        return clone;
    }
}
