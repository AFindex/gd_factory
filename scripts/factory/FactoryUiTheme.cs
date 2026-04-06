using Godot;

public static class FactoryUiTheme
{
    public const int RadiusNone = 0;
    public const int RadiusSoft = 1;
    public const int PaddingCompact = 6;
    public const int PaddingStandard = 10;
    public const int SpacingCompact = 4;
    public const int SpacingStandard = 8;

    public static readonly Color Canvas = new("050505");
    public static readonly Color SurfaceBase = new(0.06f, 0.06f, 0.06f, 0.96f);
    public static readonly Color SurfaceRaised = new(0.10f, 0.10f, 0.10f, 0.98f);
    public static readonly Color SurfaceInset = new(0.04f, 0.04f, 0.04f, 0.92f);
    public static readonly Color SurfaceOverlay = new(0.02f, 0.02f, 0.02f, 0.94f);
    public static readonly Color SurfaceInverse = new("F2F2F2");
    public static readonly Color BorderStrong = new("FAFAFA");
    public static readonly Color Border = new("B7B7B7");
    public static readonly Color BorderMuted = new("6A6A6A");
    public static readonly Color BorderSoft = new(1.0f, 1.0f, 1.0f, 0.18f);
    public static readonly Color Text = new("F5F5F5");
    public static readonly Color TextMuted = new("D0D0D0");
    public static readonly Color TextSubtle = new("A0A0A0");
    public static readonly Color TextFaint = new("767676");
    public static readonly Color TextInverse = new("080808");
    public static readonly Color StatusOk = new("F5F5F5");
    public static readonly Color StatusWarn = new("DADADA");
    public static readonly Color StatusError = new("8A8A8A");

    public static StyleBoxFlat ConfigurePanelStyle(
        StyleBoxFlat style,
        Color backgroundColor,
        Color borderColor,
        int borderWidth = 1,
        int cornerRadius = RadiusNone,
        int contentMargin = 0)
    {
        style.BgColor = backgroundColor;
        style.BorderColor = borderColor;
        style.BorderWidthBottom = borderWidth;
        style.BorderWidthLeft = borderWidth;
        style.BorderWidthRight = borderWidth;
        style.BorderWidthTop = borderWidth;
        style.CornerRadiusBottomLeft = cornerRadius;
        style.CornerRadiusBottomRight = cornerRadius;
        style.CornerRadiusTopLeft = cornerRadius;
        style.CornerRadiusTopRight = cornerRadius;
        style.ContentMarginBottom = contentMargin;
        style.ContentMarginLeft = contentMargin;
        style.ContentMarginRight = contentMargin;
        style.ContentMarginTop = contentMargin;
        return style;
    }

    public static StyleBoxFlat CreatePanelStyle(
        Color backgroundColor,
        Color borderColor,
        int borderWidth = 1,
        int cornerRadius = RadiusNone,
        int contentMargin = 0)
    {
        return ConfigurePanelStyle(new StyleBoxFlat(), backgroundColor, borderColor, borderWidth, cornerRadius, contentMargin);
    }

    public static StyleBoxFlat CreateChromePanelStyle()
    {
        return CreatePanelStyle(SurfaceOverlay, BorderStrong, borderWidth: 2);
    }

    public static StyleBoxFlat CreateWorkspaceBodyStyle()
    {
        return CreatePanelStyle(SurfaceInset, BorderSoft, borderWidth: 1);
    }

    public static StyleBoxFlat CreateTitleBarStyle()
    {
        return CreatePanelStyle(SurfaceRaised, BorderStrong, borderWidth: 1);
    }

    public static StyleBoxFlat ConfigureOutlineStyle(StyleBoxFlat style, Color borderColor, int borderWidth = 2, int contentMargin = 10)
    {
        return ConfigurePanelStyle(style, Colors.Transparent, borderColor, borderWidth, RadiusNone, contentMargin);
    }

    public static StyleBoxFlat CreateOutlineStyle(Color borderColor, int borderWidth = 2, int contentMargin = 10)
    {
        return ConfigureOutlineStyle(new StyleBoxFlat(), borderColor, borderWidth, contentMargin);
    }

    public static StyleBoxFlat CreateSlotStyle(Color backgroundColor, Color borderColor, int borderWidth = 1)
    {
        return CreatePanelStyle(backgroundColor, borderColor, borderWidth, RadiusNone, contentMargin: 1);
    }

    public static StyleBoxFlat CreateButtonStyle(
        Color backgroundColor,
        Color borderColor,
        int borderWidth = 1,
        int horizontalPadding = 12,
        int verticalPadding = 8)
    {
        var style = CreatePanelStyle(backgroundColor, borderColor, borderWidth);
        style.ContentMarginLeft = horizontalPadding;
        style.ContentMarginRight = horizontalPadding;
        style.ContentMarginTop = verticalPadding;
        style.ContentMarginBottom = verticalPadding;
        return style;
    }

    public static void ApplyButtonTheme(BaseButton button, bool compact = false, bool invertPressed = true)
    {
        var horizontalPadding = compact ? 10 : 12;
        var verticalPadding = compact ? 6 : 8;
        button.AddThemeStyleboxOverride("normal", CreateButtonStyle(SurfaceBase, Border, 1, horizontalPadding, verticalPadding));
        button.AddThemeStyleboxOverride("hover", CreateButtonStyle(SurfaceRaised, BorderStrong, 1, horizontalPadding, verticalPadding));
        button.AddThemeStyleboxOverride(
            "pressed",
            CreateButtonStyle(invertPressed ? SurfaceInverse : SurfaceInset, BorderStrong, invertPressed ? 2 : 1, horizontalPadding, verticalPadding));
        button.AddThemeStyleboxOverride("focus", CreateButtonStyle(SurfaceRaised, BorderStrong, 2, horizontalPadding, verticalPadding));
        button.AddThemeStyleboxOverride("disabled", CreateButtonStyle(SurfaceInset, BorderMuted, 1, horizontalPadding, verticalPadding));
        button.AddThemeColorOverride("font_color", TextMuted);
        button.AddThemeColorOverride("font_hover_color", Text);
        button.AddThemeColorOverride("font_focus_color", Text);
        button.AddThemeColorOverride("font_pressed_color", invertPressed ? TextInverse : Text);
        button.AddThemeColorOverride("font_disabled_color", TextFaint);
    }

    public static void ApplyLineEditTheme(LineEdit lineEdit)
    {
        lineEdit.AddThemeStyleboxOverride("normal", CreateButtonStyle(SurfaceInset, Border, 1, 10, 8));
        lineEdit.AddThemeStyleboxOverride("focus", CreateButtonStyle(SurfaceBase, BorderStrong, 2, 10, 8));
        lineEdit.AddThemeStyleboxOverride("read_only", CreateButtonStyle(SurfaceInset, BorderMuted, 1, 10, 8));
        lineEdit.AddThemeColorOverride("font_color", Text);
        lineEdit.AddThemeColorOverride("font_placeholder_color", TextFaint);
        lineEdit.AddThemeColorOverride("font_uneditable_color", TextSubtle);
    }

    public static void ApplyItemListTheme(ItemList list)
    {
        list.AddThemeStyleboxOverride("panel", CreatePanelStyle(SurfaceInset, BorderMuted, 1, RadiusNone, 4));
        list.AddThemeColorOverride("font_color", TextMuted);
        list.AddThemeColorOverride("font_hovered_color", Text);
        list.AddThemeColorOverride("font_selected_color", TextInverse);
        list.AddThemeColorOverride("font_hovered_selected_color", TextInverse);
        list.AddThemeColorOverride("guide_color", BorderSoft);
    }

    public static void ApplyTabContainerTheme(TabContainer tabs)
    {
        tabs.AddThemeColorOverride("font_selected_color", TextInverse);
        tabs.AddThemeColorOverride("font_unselected_color", TextMuted);
        tabs.AddThemeColorOverride("font_hovered_color", Text);
    }

    public static void ApplyProgressBarTheme(ProgressBar bar)
    {
        bar.AddThemeStyleboxOverride("fill", CreatePanelStyle(SurfaceInverse, BorderStrong, 1));
        bar.AddThemeStyleboxOverride("background", CreatePanelStyle(SurfaceInset, BorderMuted, 1));
    }

    public static ColorRect CreateDivider()
    {
        return new ColorRect
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(0.0f, 1.0f),
            Color = BorderSoft
        };
    }

    public static Color GetStatusTone(bool positive)
    {
        return positive ? StatusOk : StatusError;
    }

    public static Color GetStatusTone(FactoryStatusTone tone)
    {
        return tone switch
        {
            FactoryStatusTone.Positive => StatusOk,
            FactoryStatusTone.Warning => StatusWarn,
            _ => StatusError
        };
    }
}
