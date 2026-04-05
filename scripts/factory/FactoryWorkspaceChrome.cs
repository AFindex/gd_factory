using Godot;
using System;
using System.Collections.Generic;

public sealed class FactoryWorkspaceDescriptor
{
    public FactoryWorkspaceDescriptor(string id, string label)
    {
        Id = id;
        Label = label;
    }

    public string Id { get; }
    public string Label { get; }
}

public partial class FactoryWorkspaceChrome : PanelContainer
{
    private readonly Dictionary<string, Button> _workspaceButtons = new();
    private readonly List<FactoryWorkspaceDescriptor> _workspaces = new();

    private MarginContainer? _margin;
    private VBoxContainer? _body;
    private Label? _titleLabel;
    private Label? _subtitleLabel;
    private HBoxContainer? _workspaceRow;
    private string _activeWorkspaceId = string.Empty;
    private string _pendingTitle = string.Empty;
    private string _pendingSubtitle = string.Empty;
    private string _pendingActiveWorkspaceId = string.Empty;

    public event Action<string>? WorkspaceSelected;

    public string ActiveWorkspaceId => _activeWorkspaceId;

    public override void _Ready()
    {
        MouseFilter = Control.MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel", CreatePanelStyle());

        var margin = new MarginContainer();
        margin.MouseFilter = Control.MouseFilterEnum.Ignore;
        margin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        AddChild(margin);
        _margin = margin;

        var body = new VBoxContainer();
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.AddThemeConstantOverride("separation", 8);
        margin.AddChild(body);
        _body = body;

        _titleLabel = CreateLabel(string.Empty, 18, Colors.White);
        body.AddChild(_titleLabel);

        _subtitleLabel = CreateLabel(string.Empty, 11, new Color("A8B8C6"));
        body.AddChild(_subtitleLabel);

        _workspaceRow = new HBoxContainer();
        _workspaceRow.MouseFilter = Control.MouseFilterEnum.Stop;
        _workspaceRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _workspaceRow.AddThemeConstantOverride("separation", 6);
        body.AddChild(_workspaceRow);

        ApplyConfiguration();
    }

    public void Configure(string title, string subtitle, IReadOnlyList<FactoryWorkspaceDescriptor> workspaces, string activeWorkspaceId)
    {
        _pendingTitle = title;
        _pendingSubtitle = subtitle;
        _pendingActiveWorkspaceId = activeWorkspaceId;

        _workspaces.Clear();
        _workspaces.AddRange(workspaces);
        ApplyConfiguration();
    }

    public IReadOnlyList<string> GetWorkspaceIds()
    {
        var ids = new List<string>(_workspaces.Count);
        for (var i = 0; i < _workspaces.Count; i++)
        {
            ids.Add(_workspaces[i].Id);
        }

        return ids;
    }

    public bool HasWorkspace(string workspaceId)
    {
        return _workspaceButtons.ContainsKey(workspaceId);
    }

    public void SetActiveWorkspace(string workspaceId, bool emitSignal = true)
    {
        if (_workspaceButtons.Count == 0)
        {
            _activeWorkspaceId = string.Empty;
            return;
        }

        if (!_workspaceButtons.ContainsKey(workspaceId))
        {
            workspaceId = _workspaces.Count > 0 ? _workspaces[0].Id : string.Empty;
        }

        if (string.IsNullOrWhiteSpace(workspaceId))
        {
            return;
        }

        _activeWorkspaceId = workspaceId;
        foreach (var pair in _workspaceButtons)
        {
            var isActive = pair.Key == workspaceId;
            pair.Value.ButtonPressed = isActive;
            pair.Value.Modulate = isActive ? Colors.White : new Color("C6D4E1");
        }

        if (emitSignal)
        {
            WorkspaceSelected?.Invoke(workspaceId);
        }
    }

    private void RebuildWorkspaceButtons()
    {
        if (_workspaceRow is null)
        {
            return;
        }

        var isCompact = IsCompactLayout();
        foreach (var child in _workspaceRow.GetChildren())
        {
            child.QueueFree();
        }

        _workspaceButtons.Clear();
        for (var index = 0; index < _workspaces.Count; index++)
        {
            var descriptor = _workspaces[index];
            var workspaceId = descriptor.Id;
            var button = new Button
            {
                Text = descriptor.Label,
                ToggleMode = true,
                MouseFilter = Control.MouseFilterEnum.Stop,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(0.0f, isCompact ? 26.0f : 30.0f)
            };
            button.AddThemeFontSizeOverride("font_size", isCompact ? 10 : 11);
            button.Pressed += () => SetActiveWorkspace(workspaceId);
            _workspaceRow.AddChild(button);
            _workspaceButtons[workspaceId] = button;
        }
    }

    private void ApplyConfiguration()
    {
        if (_titleLabel is null || _subtitleLabel is null || _body is null || _margin is null)
        {
            return;
        }

        var hasTitle = !string.IsNullOrWhiteSpace(_pendingTitle);
        var hasSubtitle = !string.IsNullOrWhiteSpace(_pendingSubtitle);
        var isCompact = !hasTitle && !hasSubtitle;

        _titleLabel.Text = _pendingTitle;
        _titleLabel.Visible = hasTitle;
        _subtitleLabel.Text = _pendingSubtitle;
        _subtitleLabel.Visible = hasSubtitle;
        _body.AddThemeConstantOverride("separation", isCompact ? 4 : 8);
        _margin.AddThemeConstantOverride("margin_left", isCompact ? 8 : 12);
        _margin.AddThemeConstantOverride("margin_top", isCompact ? 6 : 10);
        _margin.AddThemeConstantOverride("margin_right", isCompact ? 8 : 12);
        _margin.AddThemeConstantOverride("margin_bottom", isCompact ? 6 : 10);
        RebuildWorkspaceButtons();
        SetActiveWorkspace(_pendingActiveWorkspaceId, emitSignal: false);
    }

    private bool IsCompactLayout()
    {
        return string.IsNullOrWhiteSpace(_pendingTitle) && string.IsNullOrWhiteSpace(_pendingSubtitle);
    }

    private static Label CreateLabel(string text, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.Modulate = color;
        return label;
    }

    private static StyleBoxFlat CreatePanelStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.08f, 0.12f, 0.92f),
            BorderColor = new Color("4DA8DA"),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusBottomLeft = 10
        };
    }
}
