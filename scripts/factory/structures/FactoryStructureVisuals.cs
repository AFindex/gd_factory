using Godot;
using System;
using System.Collections.Generic;

public enum FactoryStructureVisualSourceKind
{
    AuthoredScene,
    Procedural,
    GenericPlaceholder
}

public readonly record struct FactoryStructureVisualState(
    bool IsVisible,
    bool IsHovered,
    bool IsSelected,
    bool IsUnderAttack,
    bool IsDestroyed,
    bool IsActive,
    bool IsProcessing,
    float ProcessRatio,
    bool HasBufferedOutput,
    FactoryPowerStatus PowerStatus,
    float PowerSatisfaction,
    double PresentationTimeSeconds);

public sealed class FactoryStructureVisualProfile
{
    public FactoryStructureVisualProfile(
        Action<FactoryStructureVisualController>? proceduralBuilder = null,
        Action<FactoryStructureVisualController, FactoryStructureVisualState, float>? stateUpdater = null,
        PackedScene? authoredScene = null,
        string? authoredScenePath = null,
        NodePath? animationPlayerPath = null,
        IReadOnlyDictionary<string, NodePath>? nodeAnchors = null,
        IReadOnlyDictionary<string, NodePath>? materialAnchors = null,
        IReadOnlyDictionary<string, string>? animationAliases = null)
    {
        ProceduralBuilder = proceduralBuilder;
        StateUpdater = stateUpdater;
        AuthoredScene = authoredScene;
        AuthoredScenePath = authoredScenePath;
        AnimationPlayerPath = animationPlayerPath;
        NodeAnchors = CopyNodePathDictionary(nodeAnchors);
        MaterialAnchors = CopyNodePathDictionary(materialAnchors);
        AnimationAliases = CopyStringDictionary(animationAliases);
    }

    public Action<FactoryStructureVisualController>? ProceduralBuilder { get; }
    public Action<FactoryStructureVisualController, FactoryStructureVisualState, float>? StateUpdater { get; }
    public PackedScene? AuthoredScene { get; }
    public string? AuthoredScenePath { get; }
    public NodePath? AnimationPlayerPath { get; }
    public IReadOnlyDictionary<string, NodePath> NodeAnchors { get; }
    public IReadOnlyDictionary<string, NodePath> MaterialAnchors { get; }
    public IReadOnlyDictionary<string, string> AnimationAliases { get; }

    private static IReadOnlyDictionary<string, NodePath> CopyNodePathDictionary(IReadOnlyDictionary<string, NodePath>? source)
    {
        if (source is null || source.Count == 0)
        {
            return new Dictionary<string, NodePath>();
        }

        return new Dictionary<string, NodePath>(source, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, string> CopyStringDictionary(IReadOnlyDictionary<string, string>? source)
    {
        if (source is null || source.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        return new Dictionary<string, string>(source, StringComparer.OrdinalIgnoreCase);
    }
}

public sealed class FactoryStructureVisualController
{
    private readonly IReadOnlyDictionary<string, string> _animationAliases;
    private readonly Dictionary<string, Node> _nodeAnchors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, StandardMaterial3D> _materialAnchors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, float> _floatChannels = new(StringComparer.OrdinalIgnoreCase);

    public FactoryStructureVisualController(
        Node3D root,
        float cellSize,
        FactoryStructureVisualSourceKind sourceKind,
        IReadOnlyDictionary<string, string>? animationAliases = null)
    {
        Root = root;
        CellSize = cellSize;
        SourceKind = sourceKind;
        _animationAliases = animationAliases ?? new Dictionary<string, string>();
    }

    public Node3D Root { get; }
    public float CellSize { get; }
    public FactoryStructureVisualSourceKind SourceKind { get; }
    public AnimationPlayer? AnimationPlayer { get; private set; }

    public void SetAnimationPlayer(AnimationPlayer? animationPlayer)
    {
        AnimationPlayer = animationPlayer;
    }

    public void RegisterNodeAnchor(string alias, Node node)
    {
        if (!string.IsNullOrWhiteSpace(alias))
        {
            _nodeAnchors[alias] = node;
        }
    }

    public void RegisterMaterialAnchor(string alias, StandardMaterial3D material)
    {
        if (!string.IsNullOrWhiteSpace(alias))
        {
            _materialAnchors[alias] = material;
        }
    }

    public T? GetNodeAnchor<T>(string alias) where T : Node
    {
        return _nodeAnchors.TryGetValue(alias, out var node) ? node as T : null;
    }

    public StandardMaterial3D? GetMaterialAnchor(string alias)
    {
        return _materialAnchors.TryGetValue(alias, out var material) ? material : null;
    }

    public float AnimateFloat(string key, float target, float weight, float? initialValue = null)
    {
        var clampedWeight = Mathf.Clamp(weight, 0.0f, 1.0f);
        var current = _floatChannels.TryGetValue(key, out var value)
            ? value
            : initialValue ?? target;
        current = Mathf.Lerp(current, target, clampedWeight);
        _floatChannels[key] = current;
        return current;
    }

    public bool TryPlayAnimation(string alias)
    {
        if (AnimationPlayer is null
            || !_animationAliases.TryGetValue(alias, out var animationName)
            || !AnimationPlayer.HasAnimation(animationName))
        {
            return false;
        }

        if (!AnimationPlayer.IsPlaying() || !string.Equals(AnimationPlayer.CurrentAnimation, animationName, StringComparison.Ordinal))
        {
            AnimationPlayer.Play(animationName);
        }

        return true;
    }

    public void ApplyState(FactoryStructureVisualState state, FactoryStructureVisualProfile profile, float tickAlpha)
    {
        profile.StateUpdater?.Invoke(this, state, tickAlpha);
    }
}

public static class FactoryStructureVisualFactory
{
    public static FactoryStructureVisualController BuildForStructure(FactoryStructure owner, FactoryStructureVisualProfile profile, Node3D presentationRoot, float cellSize)
    {
        return BuildInternal(profile, presentationRoot, cellSize, owner);
    }

    public static FactoryStructureVisualController CreateDetachedController(FactoryStructureVisualProfile profile, float cellSize)
    {
        var presentationRoot = new Node3D { Name = "DetachedStructureVisualRoot" };
        return BuildInternal(profile, presentationRoot, cellSize, owner: null);
    }

    public static Node3D CreateGenericPlaceholderNode(float cellSize)
    {
        var root = new Node3D { Name = "GenericStructurePlaceholder" };

        root.AddChild(CreateBoxNode(
            "Base",
            new Vector3(cellSize * 0.90f, 0.20f, cellSize * 0.90f),
            new Color("334155"),
            new Vector3(0.0f, 0.10f, 0.0f)));
        root.AddChild(CreateBoxNode(
            "Body",
            new Vector3(cellSize * 0.62f, 0.72f, cellSize * 0.62f),
            new Color("64748B"),
            new Vector3(0.0f, 0.56f, 0.0f)));
        root.AddChild(CreateBoxNode(
            "Marker",
            new Vector3(cellSize * 0.16f, 0.16f, cellSize * 0.16f),
            new Color("38BDF8"),
            new Vector3(cellSize * 0.22f, 1.00f, 0.0f)));

        return root;
    }

    private static FactoryStructureVisualController BuildInternal(FactoryStructureVisualProfile profile, Node3D presentationRoot, float cellSize, FactoryStructure? owner)
    {
        var sceneRoot = ResolveAuthoredSceneRoot(profile);
        var sourceKind = sceneRoot is not null
            ? FactoryStructureVisualSourceKind.AuthoredScene
            : profile.ProceduralBuilder is not null
                ? FactoryStructureVisualSourceKind.Procedural
                : FactoryStructureVisualSourceKind.GenericPlaceholder;
        var controller = new FactoryStructureVisualController(presentationRoot, cellSize, sourceKind, profile.AnimationAliases);

        if (sceneRoot is not null)
        {
            presentationRoot.AddChild(sceneRoot);
            ResolveSceneBindings(controller, sceneRoot, profile);
            return controller;
        }

        if (profile.ProceduralBuilder is not null)
        {
            if (owner is not null)
            {
                owner.BeginVisualBuildScope(presentationRoot);
            }

            try
            {
                profile.ProceduralBuilder(controller);
            }
            finally
            {
                owner?.EndVisualBuildScope();
            }

            return controller;
        }

        presentationRoot.AddChild(CreateGenericPlaceholderNode(cellSize));
        return controller;
    }

    private static Node3D? ResolveAuthoredSceneRoot(FactoryStructureVisualProfile profile)
    {
        var scene = profile.AuthoredScene;
        if (scene is null && !string.IsNullOrWhiteSpace(profile.AuthoredScenePath) && ResourceLoader.Exists(profile.AuthoredScenePath))
        {
            scene = ResourceLoader.Load<PackedScene>(profile.AuthoredScenePath);
        }

        if (scene?.Instantiate() is not Node sceneNode)
        {
            return null;
        }

        return sceneNode as Node3D;
    }

    private static void ResolveSceneBindings(FactoryStructureVisualController controller, Node3D sceneRoot, FactoryStructureVisualProfile profile)
    {
        if (profile.AnimationPlayerPath is not null && sceneRoot.GetNodeOrNull<AnimationPlayer>(profile.AnimationPlayerPath) is AnimationPlayer animationPlayer)
        {
            controller.SetAnimationPlayer(animationPlayer);
        }

        foreach (var pair in profile.NodeAnchors)
        {
            if (sceneRoot.GetNodeOrNull(pair.Value) is Node node)
            {
                controller.RegisterNodeAnchor(pair.Key, node);
            }
        }

        foreach (var pair in profile.MaterialAnchors)
        {
            if (sceneRoot.GetNodeOrNull<MeshInstance3D>(pair.Value) is MeshInstance3D mesh
                && mesh.MaterialOverride is StandardMaterial3D material)
            {
                controller.RegisterMaterialAnchor(pair.Key, material);
            }
        }
    }

    private static MeshInstance3D CreateBoxNode(string name, Vector3 size, Color color, Vector3 localPosition)
    {
        var mesh = new MeshInstance3D
        {
            Name = name,
            Mesh = new BoxMesh { Size = size },
            Position = localPosition,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = color,
                Roughness = 0.85f
            }
        };
        return mesh;
    }
}
