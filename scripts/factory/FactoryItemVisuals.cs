using Godot;
using System;
using System.Collections.Generic;

public enum FactoryTransportRenderMode
{
    Placeholder,
    TexturedBox,
    Billboard,
    ModelNode
}

public enum FactoryTransportRenderTier
{
    Near,
    Mid,
    Far
}

public sealed class FactoryTransportRenderDescriptor
{
    public FactoryTransportRenderDescriptor(
        string batchKey,
        FactoryTransportRenderMode mode,
        Color tint,
        Vector3 meshScale,
        Vector2 billboardScale,
        Texture2D? texture = null,
        Func<float, Node3D>? modelFactory = null,
        bool isBatchable = true)
    {
        BatchKey = batchKey;
        Mode = mode;
        Tint = tint;
        MeshScale = meshScale;
        BillboardScale = billboardScale;
        Texture = texture;
        ModelFactory = modelFactory;
        IsBatchable = isBatchable;
    }

    public string BatchKey { get; }
    public FactoryTransportRenderMode Mode { get; }
    public Color Tint { get; }
    public Vector3 MeshScale { get; }
    public Vector2 BillboardScale { get; }
    public Texture2D? Texture { get; }
    public Func<float, Node3D>? ModelFactory { get; }
    public bool IsBatchable { get; }
}

public sealed class FactoryTransportRenderDescriptorSet
{
    public FactoryTransportRenderDescriptorSet(
        FactoryTransportRenderDescriptor primary,
        FactoryTransportRenderDescriptor mid,
        FactoryTransportRenderDescriptor far)
    {
        Primary = primary;
        Mid = mid;
        Far = far;
    }

    public FactoryTransportRenderDescriptor Primary { get; }
    public FactoryTransportRenderDescriptor Mid { get; }
    public FactoryTransportRenderDescriptor Far { get; }

    public FactoryTransportRenderDescriptor ResolveForTier(FactoryTransportRenderTier tier)
    {
        return tier switch
        {
            FactoryTransportRenderTier.Far => Far,
            FactoryTransportRenderTier.Mid => Mid,
            _ => Primary
        };
    }

    public FactoryTransportRenderDescriptor ResolveBatchableForTier(FactoryTransportRenderTier tier)
    {
        var preferred = ResolveForTier(tier);
        if (preferred.IsBatchable)
        {
            return preferred;
        }

        if (Mid.IsBatchable)
        {
            return Mid;
        }

        if (Far.IsBatchable)
        {
            return Far;
        }

        return Primary;
    }
}

public sealed class FactoryTransportVisualProfile
{
    public FactoryTransportVisualProfile(
        Color tint,
        Vector3? placeholderScale = null,
        Vector3? texturedMeshScale = null,
        Vector2? billboardScale = null,
        Texture2D? texture = null,
        Func<float, Node3D>? modelFactory = null,
        bool allowTexturedMeshFallback = true,
        bool allowBillboardFallback = true)
    {
        Tint = tint;
        PlaceholderScale = placeholderScale ?? new Vector3(0.18f, 0.18f, 0.18f);
        TexturedMeshScale = texturedMeshScale ?? PlaceholderScale;
        BillboardScale = billboardScale ?? new Vector2(0.34f, 0.34f);
        Texture = texture;
        ModelFactory = modelFactory;
        AllowTexturedMeshFallback = allowTexturedMeshFallback;
        AllowBillboardFallback = allowBillboardFallback;
    }

    public Color Tint { get; }
    public Vector3 PlaceholderScale { get; }
    public Vector3 TexturedMeshScale { get; }
    public Vector2 BillboardScale { get; }
    public Texture2D? Texture { get; }
    public Func<float, Node3D>? ModelFactory { get; }
    public bool AllowTexturedMeshFallback { get; }
    public bool AllowBillboardFallback { get; }

    public FactoryTransportRenderDescriptorSet ResolveRenderDescriptors(FactoryItemKind itemKind, float cellSize)
    {
        return FactoryTransportVisualFactory.ResolveDescriptorSet(itemKind, cellSize);
    }
}

public sealed class FactoryItemDefinition
{
    public FactoryItemDefinition(
        FactoryItemKind itemKind,
        string displayName,
        Color accentColor,
        FactoryTransportVisualProfile visualProfile,
        Texture2D? iconTexture = null,
        int maxStackSize = 1,
        bool isFuel = false,
        float fuelValueSeconds = 0.0f)
    {
        ItemKind = itemKind;
        DisplayName = displayName;
        AccentColor = accentColor;
        VisualProfile = visualProfile;
        IconTexture = iconTexture ?? visualProfile.Texture;
        MaxStackSize = Mathf.Max(1, maxStackSize);
        IsFuel = isFuel;
        FuelValueSeconds = Mathf.Max(0.0f, fuelValueSeconds);
    }

    public FactoryItemKind ItemKind { get; }
    public string DisplayName { get; }
    public Color AccentColor { get; }
    public FactoryTransportVisualProfile VisualProfile { get; }
    public Texture2D? IconTexture { get; }
    public int MaxStackSize { get; }
    public bool IsFuel { get; }
    public float FuelValueSeconds { get; }
}

public static partial class FactoryItemCatalog
{
    private static readonly IReadOnlyDictionary<FactoryItemKind, FactoryItemDefinition> Definitions = CreateDefinitions();
    private static readonly Color GenericTint = new("7DD3FC");

    public static FactoryItemDefinition GetDefinition(FactoryItemKind itemKind)
    {
        return Definitions.TryGetValue(itemKind, out var definition)
            ? definition
            : Definitions[FactoryItemKind.GenericCargo];
    }

    public static string GetDisplayName(FactoryItemKind itemKind)
    {
        return GetDefinition(itemKind).DisplayName;
    }

    public static string GetDisplayName(FactoryItemKind itemKind, FactoryCargoForm cargoForm)
    {
        return $"{GetDisplayName(itemKind)}（{FactoryPresentation.GetCargoFormLabel(cargoForm)}）";
    }

    public static Color GetAccentColor(FactoryItemKind itemKind)
    {
        return GetDefinition(itemKind).AccentColor;
    }

    public static Color GetAccentColor(FactoryItemKind itemKind, FactoryCargoForm cargoForm)
    {
        var accent = GetAccentColor(itemKind);
        return cargoForm switch
        {
            FactoryCargoForm.WorldBulk => accent.Darkened(0.10f),
            FactoryCargoForm.WorldPacked => accent.Lightened(0.06f),
            FactoryCargoForm.InteriorFeed => accent.Lightened(0.18f),
            _ => accent
        };
    }

    public static bool IsFuel(FactoryItemKind itemKind)
    {
        return GetDefinition(itemKind).IsFuel;
    }

    public static Texture2D? GetIconTexture(FactoryItemKind itemKind)
    {
        return GetDefinition(itemKind).IconTexture;
    }

    public static int GetMaxStackSize(FactoryItemKind itemKind)
    {
        return GetDefinition(itemKind).MaxStackSize;
    }

    public static FactoryTransportVisualProfile ResolveVisualProfile(FactoryItem item)
    {
        return ResolveVisualProfile(item.ItemKind, item.CargoForm);
    }

    public static FactoryTransportVisualProfile ResolveVisualProfile(FactoryItemKind itemKind, FactoryCargoForm cargoForm)
    {
        var definition = GetDefinition(itemKind);
        var baseProfile = definition.VisualProfile;
        return cargoForm switch
        {
            FactoryCargoForm.WorldBulk => new FactoryTransportVisualProfile(
                GetAccentColor(itemKind, cargoForm),
                placeholderScale: baseProfile.PlaceholderScale * new Vector3(1.12f, 1.0f, 1.12f),
                texturedMeshScale: baseProfile.TexturedMeshScale * new Vector3(1.08f, 1.0f, 1.08f),
                billboardScale: baseProfile.BillboardScale * 1.08f,
                texture: baseProfile.Texture,
                modelFactory: baseProfile.ModelFactory,
                allowTexturedMeshFallback: baseProfile.AllowTexturedMeshFallback,
                allowBillboardFallback: baseProfile.AllowBillboardFallback),
            FactoryCargoForm.InteriorFeed => new FactoryTransportVisualProfile(
                GetAccentColor(itemKind, cargoForm),
                placeholderScale: baseProfile.PlaceholderScale * new Vector3(0.68f, 0.52f, 0.68f),
                texturedMeshScale: baseProfile.TexturedMeshScale * new Vector3(0.64f, 0.46f, 0.64f),
                billboardScale: baseProfile.BillboardScale * 0.72f,
                texture: baseProfile.Texture,
                modelFactory: baseProfile.ModelFactory,
                allowTexturedMeshFallback: baseProfile.AllowTexturedMeshFallback,
                allowBillboardFallback: baseProfile.AllowBillboardFallback),
            _ => new FactoryTransportVisualProfile(
                GetAccentColor(itemKind, cargoForm),
                placeholderScale: baseProfile.PlaceholderScale * new Vector3(0.96f, 0.84f, 0.96f),
                texturedMeshScale: baseProfile.TexturedMeshScale * new Vector3(1.12f, 0.88f, 1.12f),
                billboardScale: baseProfile.BillboardScale * 0.96f,
                texture: baseProfile.Texture,
                modelFactory: baseProfile.ModelFactory,
                allowTexturedMeshFallback: baseProfile.AllowTexturedMeshFallback,
                allowBillboardFallback: baseProfile.AllowBillboardFallback)
        };
    }

    public static bool TryGetFuelValueSeconds(FactoryItemKind itemKind, out float burnSeconds)
    {
        burnSeconds = GetDefinition(itemKind).FuelValueSeconds;
        return burnSeconds > 0.0f;
    }

    private static IReadOnlyDictionary<FactoryItemKind, FactoryItemDefinition> CreateDefinitions()
    {
        var textures = FactoryGeneratedItemTextureLibrary.CreateTextures();
        return new Dictionary<FactoryItemKind, FactoryItemDefinition>
        {
            [FactoryItemKind.GenericCargo] = new FactoryItemDefinition(
                FactoryItemKind.GenericCargo,
                "基础货物",
                GenericTint,
                new FactoryTransportVisualProfile(GenericTint),
                iconTexture: textures["generic-cargo"],
                maxStackSize: 8),
            [FactoryItemKind.BuildingKit] = new FactoryItemDefinition(
                FactoryItemKind.BuildingKit,
                "建筑",
                new Color("60A5FA"),
                new FactoryTransportVisualProfile(new Color("60A5FA"), placeholderScale: new Vector3(0.28f, 0.20f, 0.28f)),
                iconTexture: textures["building-kit"],
                maxStackSize: 20),
            [FactoryItemKind.Coal] = new FactoryItemDefinition(
                FactoryItemKind.Coal,
                "煤矿",
                new Color("5B4636"),
                new FactoryTransportVisualProfile(
                    new Color("5B4636"),
                    texture: textures["coal"],
                    texturedMeshScale: new Vector3(0.22f, 0.18f, 0.20f)),
                iconTexture: textures["coal"],
                maxStackSize: 12,
                isFuel: true,
                fuelValueSeconds: 12.0f),
            [FactoryItemKind.IronOre] = new FactoryItemDefinition(
                FactoryItemKind.IronOre,
                "铁矿石",
                new Color("64748B"),
                new FactoryTransportVisualProfile(new Color("64748B"), placeholderScale: new Vector3(0.20f, 0.18f, 0.20f)),
                iconTexture: textures["iron-ore"],
                maxStackSize: 12),
            [FactoryItemKind.CopperOre] = new FactoryItemDefinition(
                FactoryItemKind.CopperOre,
                "铜矿石",
                new Color("C2410C"),
                new FactoryTransportVisualProfile(
                    new Color("C2410C"),
                    texture: textures["copper-ore"],
                    texturedMeshScale: new Vector3(0.20f, 0.18f, 0.20f)),
                iconTexture: textures["copper-ore"],
                maxStackSize: 12),
            [FactoryItemKind.StoneOre] = new FactoryItemDefinition(
                FactoryItemKind.StoneOre,
                "石矿石",
                new Color("78716C"),
                new FactoryTransportVisualProfile(
                    new Color("78716C"),
                    texture: textures["stone-ore"],
                    texturedMeshScale: new Vector3(0.22f, 0.18f, 0.20f)),
                iconTexture: textures["stone-ore"],
                maxStackSize: 12),
            [FactoryItemKind.SulfurOre] = new FactoryItemDefinition(
                FactoryItemKind.SulfurOre,
                "硫矿石",
                new Color("EAB308"),
                new FactoryTransportVisualProfile(
                    new Color("EAB308"),
                    texture: textures["sulfur-ore"],
                    texturedMeshScale: new Vector3(0.22f, 0.18f, 0.20f)),
                iconTexture: textures["sulfur-ore"],
                maxStackSize: 12),
            [FactoryItemKind.QuartzOre] = new FactoryItemDefinition(
                FactoryItemKind.QuartzOre,
                "石英矿石",
                new Color("7DD3FC"),
                new FactoryTransportVisualProfile(
                    new Color("7DD3FC"),
                    texture: textures["quartz-ore"],
                    texturedMeshScale: new Vector3(0.22f, 0.18f, 0.20f)),
                iconTexture: textures["quartz-ore"],
                maxStackSize: 12),
            [FactoryItemKind.IronPlate] = new FactoryItemDefinition(
                FactoryItemKind.IronPlate,
                "铁板",
                new Color("CBD5E1"),
                new FactoryTransportVisualProfile(new Color("CBD5E1"), placeholderScale: new Vector3(0.24f, 0.08f, 0.18f)),
                iconTexture: textures["iron-plate"],
                maxStackSize: 10),
            [FactoryItemKind.CopperPlate] = new FactoryItemDefinition(
                FactoryItemKind.CopperPlate,
                "铜板",
                new Color("FB923C"),
                new FactoryTransportVisualProfile(new Color("FB923C"), placeholderScale: new Vector3(0.24f, 0.08f, 0.18f)),
                iconTexture: textures["copper-plate"],
                maxStackSize: 10),
            [FactoryItemKind.StoneBrick] = new FactoryItemDefinition(
                FactoryItemKind.StoneBrick,
                "石砖",
                new Color("A8A29E"),
                new FactoryTransportVisualProfile(new Color("A8A29E"), placeholderScale: new Vector3(0.26f, 0.10f, 0.20f)),
                iconTexture: textures["stone-brick"],
                maxStackSize: 10),
            [FactoryItemKind.SulfurCrystal] = new FactoryItemDefinition(
                FactoryItemKind.SulfurCrystal,
                "硫晶",
                new Color("FDE047"),
                new FactoryTransportVisualProfile(
                    new Color("FDE047"),
                    texture: textures["sulfur-crystal"],
                    billboardScale: new Vector2(0.32f, 0.32f),
                    allowTexturedMeshFallback: false,
                    allowBillboardFallback: true),
                iconTexture: textures["sulfur-crystal"],
                maxStackSize: 12),
            [FactoryItemKind.Glass] = new FactoryItemDefinition(
                FactoryItemKind.Glass,
                "玻璃板",
                new Color("67E8F9"),
                new FactoryTransportVisualProfile(
                    new Color("67E8F9"),
                    texture: textures["glass"],
                    billboardScale: new Vector2(0.34f, 0.24f)),
                iconTexture: textures["glass"],
                maxStackSize: 10),
            [FactoryItemKind.SteelPlate] = new FactoryItemDefinition(
                FactoryItemKind.SteelPlate,
                "钢板",
                new Color("94A3B8"),
                new FactoryTransportVisualProfile(new Color("94A3B8"), placeholderScale: new Vector3(0.26f, 0.08f, 0.18f)),
                iconTexture: textures["steel-plate"],
                maxStackSize: 8),
            [FactoryItemKind.Gear] = new FactoryItemDefinition(
                FactoryItemKind.Gear,
                "齿轮",
                new Color("FBBF24"),
                new FactoryTransportVisualProfile(
                    new Color("FBBF24"),
                    texture: textures["gear"],
                    modelFactory: FactoryTransportModelLibrary.CreateGearModel),
                iconTexture: textures["gear"],
                maxStackSize: 8),
            [FactoryItemKind.CopperWire] = new FactoryItemDefinition(
                FactoryItemKind.CopperWire,
                "铜线",
                new Color("F97316"),
                new FactoryTransportVisualProfile(
                    new Color("F97316"),
                    texture: textures["copper-wire"],
                    billboardScale: new Vector2(0.30f, 0.30f),
                    allowTexturedMeshFallback: false,
                    allowBillboardFallback: true),
                iconTexture: textures["copper-wire"],
                maxStackSize: 16),
            [FactoryItemKind.CircuitBoard] = new FactoryItemDefinition(
                FactoryItemKind.CircuitBoard,
                "电路板",
                new Color("34D399"),
                new FactoryTransportVisualProfile(
                    new Color("34D399"),
                    texture: textures["circuit-board"],
                    billboardScale: new Vector2(0.32f, 0.28f),
                    allowTexturedMeshFallback: false,
                    allowBillboardFallback: true),
                iconTexture: textures["circuit-board"],
                maxStackSize: 8),
            [FactoryItemKind.BatteryPack] = new FactoryItemDefinition(
                FactoryItemKind.BatteryPack,
                "电池组",
                new Color("38BDF8"),
                new FactoryTransportVisualProfile(
                    new Color("38BDF8"),
                    texture: textures["battery-pack"],
                    texturedMeshScale: new Vector3(0.20f, 0.18f, 0.16f)),
                iconTexture: textures["battery-pack"],
                maxStackSize: 8),
            [FactoryItemKind.RepairKit] = new FactoryItemDefinition(
                FactoryItemKind.RepairKit,
                "维护包",
                new Color("22C55E"),
                new FactoryTransportVisualProfile(
                    new Color("22C55E"),
                    texture: textures["repair-kit"],
                    texturedMeshScale: new Vector3(0.20f, 0.18f, 0.20f)),
                iconTexture: textures["repair-kit"],
                maxStackSize: 6),
            [FactoryItemKind.MachinePart] = new FactoryItemDefinition(
                FactoryItemKind.MachinePart,
                "机加工件",
                new Color("8B5CF6"),
                new FactoryTransportVisualProfile(
                    new Color("8B5CF6"),
                    texture: textures["machine-part"],
                    modelFactory: FactoryTransportModelLibrary.CreateMachinePartModel),
                iconTexture: textures["machine-part"],
                maxStackSize: 6),
            [FactoryItemKind.AmmoMagazine] = new FactoryItemDefinition(
                FactoryItemKind.AmmoMagazine,
                "弹药",
                new Color("FACC15"),
                new FactoryTransportVisualProfile(
                    new Color("FACC15"),
                    texture: textures["ammo-magazine"],
                    modelFactory: FactoryTransportModelLibrary.CreateAmmoMagazineModel),
                iconTexture: textures["ammo-magazine"],
                maxStackSize: 12),
            [FactoryItemKind.HighVelocityAmmo] = new FactoryItemDefinition(
                FactoryItemKind.HighVelocityAmmo,
                "高速弹药",
                new Color("F97316"),
                new FactoryTransportVisualProfile(
                    new Color("F97316"),
                    texture: textures["high-velocity-ammo"],
                    texturedMeshScale: new Vector3(0.24f, 0.10f, 0.10f),
                    billboardScale: new Vector2(0.34f, 0.24f)),
                iconTexture: textures["high-velocity-ammo"],
                maxStackSize: 10),
        };
    }
}

public static class FactoryTransportVisualFactory
{
    private static readonly Dictionary<string, Mesh> SharedMeshes = new();
    private static readonly Dictionary<string, Material> SharedMaterials = new();

    public static Node3D CreateVisual(FactoryItem item, float cellSize)
    {
        return CreateVisual(item.ItemKind, cellSize);
    }

    public static Node3D CreateVisual(FactoryItemKind itemKind, float cellSize)
    {
        return CreateNodeForDescriptor(ResolveDescriptorSet(itemKind, cellSize).Primary, itemKind, cellSize);
    }

    public static FactoryTransportRenderDescriptorSet ResolveDescriptorSet(FactoryItem item, float cellSize)
    {
        var profile = FactoryItemCatalog.ResolveVisualProfile(item);
        var placeholder = CreatePlaceholderDescriptor(item.ItemKind, profile, cellSize);
        var billboard = CreateBillboardDescriptor(item.ItemKind, profile, cellSize) ?? placeholder;
        var textured = CreateTexturedDescriptor(item.ItemKind, profile, cellSize) ?? billboard;
        var primary = profile.AllowBillboardFallback && profile.Texture is not null
            ? billboard
            : profile.AllowTexturedMeshFallback && profile.Texture is not null
                ? textured
                : placeholder;
        var mid = primary.Mode == FactoryTransportRenderMode.Billboard
            ? billboard
            : profile.AllowTexturedMeshFallback && profile.Texture is not null
                ? textured
                : billboard;
        var far = profile.AllowBillboardFallback && profile.Texture is not null
            ? billboard
            : placeholder;
        return new FactoryTransportRenderDescriptorSet(primary, mid, far);
    }

    public static FactoryTransportRenderDescriptorSet ResolveDescriptorSet(FactoryItemKind itemKind, float cellSize)
    {
        var definition = FactoryItemCatalog.GetDefinition(itemKind);
        var profile = definition.VisualProfile;
        var placeholder = CreatePlaceholderDescriptor(itemKind, profile, cellSize);
        var billboard = CreateBillboardDescriptor(itemKind, profile, cellSize) ?? placeholder;
        var textured = CreateTexturedDescriptor(itemKind, profile, cellSize) ?? billboard;
        var primary = profile.AllowBillboardFallback && profile.Texture is not null
            ? billboard
            : profile.AllowTexturedMeshFallback && profile.Texture is not null
                ? textured
                : placeholder;
        var mid = primary.Mode == FactoryTransportRenderMode.Billboard
            ? billboard
            : profile.AllowTexturedMeshFallback && profile.Texture is not null
                ? textured
                : billboard;
        var far = profile.AllowBillboardFallback && profile.Texture is not null
            ? billboard
            : placeholder;
        return new FactoryTransportRenderDescriptorSet(primary, mid, far);
    }

    public static Mesh GetSharedMesh(FactoryTransportRenderDescriptor descriptor)
    {
        if (SharedMeshes.TryGetValue(descriptor.BatchKey, out var existing))
        {
            return existing;
        }

        Mesh mesh = descriptor.Mode switch
        {
            FactoryTransportRenderMode.Billboard => new QuadMesh { Size = descriptor.BillboardScale },
            FactoryTransportRenderMode.TexturedBox => new BoxMesh { Size = descriptor.MeshScale },
            _ => new BoxMesh { Size = descriptor.MeshScale }
        };
        SharedMeshes[descriptor.BatchKey] = mesh;
        return mesh;
    }

    public static Material GetSharedMaterial(FactoryTransportRenderDescriptor descriptor)
    {
        if (SharedMaterials.TryGetValue(descriptor.BatchKey, out var existing))
        {
            return existing;
        }

        Material material = descriptor.Mode switch
        {
            FactoryTransportRenderMode.Billboard => new StandardMaterial3D
            {
                AlbedoColor = Colors.White,
                AlbedoTexture = descriptor.Texture,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled
            },
            FactoryTransportRenderMode.TexturedBox => new StandardMaterial3D
            {
                AlbedoColor = Colors.White,
                AlbedoTexture = descriptor.Texture,
                Roughness = 0.62f,
                Metallic = 0.04f
            },
            _ => new StandardMaterial3D
            {
                AlbedoColor = descriptor.Tint,
                Roughness = 0.75f
            }
        };
        SharedMaterials[descriptor.BatchKey] = material;
        return material;
    }

    private static Node3D CreateNodeForDescriptor(FactoryTransportRenderDescriptor descriptor, FactoryItemKind itemKind, float cellSize)
    {
        if (descriptor.Mode == FactoryTransportRenderMode.ModelNode && descriptor.ModelFactory is not null)
        {
            var model = descriptor.ModelFactory(cellSize);
            model.Name = $"{itemKind}_Model";
            DisableShadowsRecursive(model);
            return model;
        }

        var mesh = new MeshInstance3D
        {
            Name = descriptor.Mode == FactoryTransportRenderMode.Billboard ? "BillboardQuad" : "TransportMesh",
            Mesh = GetSharedMesh(descriptor),
            MaterialOverride = GetSharedMaterial(descriptor),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };

        var root = new Node3D
        {
            Name = $"{itemKind}_{descriptor.Mode}"
        };
        root.AddChild(mesh);
        return root;
    }

    private static void DisableShadowsRecursive(Node node)
    {
        if (node is GeometryInstance3D geometry)
        {
            geometry.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        }

        foreach (var child in node.GetChildren())
        {
            DisableShadowsRecursive(child);
        }
    }

    private static FactoryTransportRenderDescriptor CreatePlaceholderDescriptor(FactoryItemKind itemKind, FactoryTransportVisualProfile profile, float cellSize)
    {
        return new FactoryTransportRenderDescriptor(
            $"placeholder:{itemKind}:{FormatVector3(profile.PlaceholderScale * cellSize)}:{profile.Tint.ToHtml()}",
            FactoryTransportRenderMode.Placeholder,
            profile.Tint,
            profile.PlaceholderScale * cellSize,
            profile.BillboardScale * cellSize);
    }

    private static FactoryTransportRenderDescriptor? CreateTexturedDescriptor(FactoryItemKind itemKind, FactoryTransportVisualProfile profile, float cellSize)
    {
        if (profile.Texture is null)
        {
            return null;
        }

        return new FactoryTransportRenderDescriptor(
            $"textured:{itemKind}:{profile.Texture.GetInstanceId()}:{FormatVector3(profile.TexturedMeshScale * cellSize)}",
            FactoryTransportRenderMode.TexturedBox,
            profile.Tint,
            profile.TexturedMeshScale * cellSize,
            profile.BillboardScale * cellSize,
            texture: profile.Texture);
    }

    private static FactoryTransportRenderDescriptor? CreateBillboardDescriptor(FactoryItemKind itemKind, FactoryTransportVisualProfile profile, float cellSize)
    {
        if (profile.Texture is null)
        {
            return null;
        }

        return new FactoryTransportRenderDescriptor(
            $"billboard:{itemKind}:{profile.Texture.GetInstanceId()}:{FormatVector2(profile.BillboardScale * cellSize)}",
            FactoryTransportRenderMode.Billboard,
            profile.Tint,
            profile.PlaceholderScale * cellSize,
            profile.BillboardScale * cellSize,
            texture: profile.Texture);
    }

    private static FactoryTransportRenderDescriptor CreateModelDescriptor(FactoryItemKind itemKind, FactoryTransportVisualProfile profile, float cellSize)
    {
        return new FactoryTransportRenderDescriptor(
            $"model:{itemKind}:{FormatVector3(profile.PlaceholderScale * cellSize)}",
            FactoryTransportRenderMode.ModelNode,
            profile.Tint,
            profile.PlaceholderScale * cellSize,
            profile.BillboardScale * cellSize,
            texture: profile.Texture,
            modelFactory: profile.ModelFactory,
            isBatchable: false);
    }

    private static string FormatVector2(Vector2 value)
    {
        return $"{value.X:0.###},{value.Y:0.###}";
    }

    private static string FormatVector3(Vector3 value)
    {
        return $"{value.X:0.###},{value.Y:0.###},{value.Z:0.###}";
    }
}

internal static class FactoryGeneratedItemTextureLibrary
{
    public static IReadOnlyDictionary<string, Texture2D> CreateTextures()
    {
        return new Dictionary<string, Texture2D>
        {
            ["generic-cargo"] = CreateGenericCargoTexture(),
            ["building-kit"] = CreateBuildingKitTexture(),
            ["coal"] = CreateCoalTexture(),
            ["iron-ore"] = CreateIronOreTexture(),
            ["copper-ore"] = CreateCopperOreTexture(),
            ["stone-ore"] = CreateStoneOreTexture(),
            ["sulfur-ore"] = CreateSulfurOreTexture(),
            ["quartz-ore"] = CreateQuartzOreTexture(),
            ["iron-plate"] = CreatePlateTexture(new Color("CBD5E1"), new Color("64748B")),
            ["copper-plate"] = CreatePlateTexture(new Color("FB923C"), new Color("9A3412")),
            ["stone-brick"] = CreatePlateTexture(new Color("A8A29E"), new Color("57534E")),
            ["sulfur-crystal"] = CreateCrystalTexture(new Color("FDE047"), new Color("CA8A04")),
            ["glass"] = CreateGlassTexture(),
            ["steel-plate"] = CreatePlateTexture(new Color("94A3B8"), new Color("475569")),
            ["gear"] = CreateGearTexture(),
            ["copper-wire"] = CreateCopperWireTexture(),
            ["circuit-board"] = CreateCircuitBoardTexture(),
            ["battery-pack"] = CreateBatteryPackTexture(),
            ["repair-kit"] = CreateRepairKitTexture(),
            ["machine-part"] = CreateMachinePartTexture(),
            ["ammo-magazine"] = CreateAmmoTexture(new Color("FACC15"), new Color("78350F")),
            ["high-velocity-ammo"] = CreateAmmoTexture(new Color("F97316"), new Color("5B4636")),
        };
    }

    private static Texture2D CreateGenericCargoTexture()
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillRect(image, new Rect2I(5, 6, 22, 20), new Color("7DD3FC"));
        FillRect(image, new Rect2I(9, 10, 14, 12), new Color("E0F2FE"));
        FillRect(image, new Rect2I(12, 13, 8, 6), new Color("0C4A6E"));
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateCoalTexture()
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillCircle(image, 10, 13, 6, new Color("3F3A37"));
        FillCircle(image, 18, 14, 7, new Color("5B4636"));
        FillCircle(image, 14, 20, 6, new Color("1F1B18"));
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateBuildingKitTexture()
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillRect(image, new Rect2I(5, 8, 22, 16), new Color("60A5FA"));
        FillRect(image, new Rect2I(8, 11, 16, 10), new Color("DBEAFE"));
        FillRect(image, new Rect2I(12, 4, 8, 6), new Color("1D4ED8"));
        FillRect(image, new Rect2I(10, 16, 12, 4), new Color("1E3A8A"));
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateCopperOreTexture()
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillCircle(image, 10, 12, 6, new Color("9A3412"));
        FillCircle(image, 18, 12, 6, new Color("EA580C"));
        FillCircle(image, 14, 19, 7, new Color("C2410C"));
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateIronOreTexture()
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillCircle(image, 10, 12, 6, new Color("475569"));
        FillCircle(image, 18, 12, 6, new Color("64748B"));
        FillCircle(image, 14, 19, 7, new Color("94A3B8"));
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateStoneOreTexture()
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillCircle(image, 10, 13, 6, new Color("57534E"));
        FillCircle(image, 18, 12, 6, new Color("78716C"));
        FillCircle(image, 15, 20, 7, new Color("A8A29E"));
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateSulfurOreTexture()
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillCircle(image, 10, 12, 6, new Color("CA8A04"));
        FillCircle(image, 18, 13, 6, new Color("EAB308"));
        FillCircle(image, 14, 20, 7, new Color("FDE047"));
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateQuartzOreTexture()
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillCircle(image, 10, 12, 6, new Color("7DD3FC"));
        FillCircle(image, 18, 12, 6, new Color("BAE6FD"));
        FillCircle(image, 14, 19, 7, new Color("E0F2FE"));
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreatePlateTexture(Color plateColor, Color accentColor)
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillRect(image, new Rect2I(5, 10, 22, 12), plateColor);
        FillRect(image, new Rect2I(7, 8, 18, 2), accentColor);
        FillRect(image, new Rect2I(7, 22, 18, 2), accentColor.Darkened(0.1f));
        FillRect(image, new Rect2I(9, 13, 14, 6), plateColor.Lightened(0.18f));
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateGearTexture()
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillRect(image, new Rect2I(13, 3, 6, 6), new Color("FACC15"));
        FillRect(image, new Rect2I(13, 23, 6, 6), new Color("FACC15"));
        FillRect(image, new Rect2I(3, 13, 6, 6), new Color("FACC15"));
        FillRect(image, new Rect2I(23, 13, 6, 6), new Color("FACC15"));
        FillCircle(image, 16, 16, 9, new Color("EAB308"));
        FillCircle(image, 16, 16, 4, new Color("78350F"));
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateCopperWireTexture()
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillCircle(image, 9, 16, 5, new Color("FB923C"));
        FillCircle(image, 16, 12, 5, new Color("F97316"));
        FillCircle(image, 22, 18, 5, new Color("FDBA74"));
        DrawLine(image, new Vector2I(9, 16), new Vector2I(16, 12), new Color("7C2D12"));
        DrawLine(image, new Vector2I(16, 12), new Vector2I(22, 18), new Color("7C2D12"));
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateCircuitBoardTexture()
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillRect(image, new Rect2I(4, 6, 24, 18), new Color("10B981"));
        FillRect(image, new Rect2I(8, 10, 6, 4), new Color("D1FAE5"));
        FillRect(image, new Rect2I(18, 10, 6, 4), new Color("A7F3D0"));
        FillRect(image, new Rect2I(11, 17, 10, 4), new Color("FDE68A"));
        DrawLine(image, new Vector2I(14, 12), new Vector2I(16, 19), new Color("ECFCCB"));
        DrawLine(image, new Vector2I(21, 12), new Vector2I(16, 19), new Color("ECFCCB"));
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateMachinePartTexture()
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillRect(image, new Rect2I(5, 8, 22, 16), new Color("8B5CF6"));
        FillRect(image, new Rect2I(11, 4, 10, 24), new Color("A78BFA"));
        FillRect(image, new Rect2I(13, 10, 6, 12), new Color("EDE9FE"));
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateCrystalTexture(Color body, Color accent)
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillTriangleUp(image, new Vector2I(16, 4), 6, body);
        FillRect(image, new Rect2I(11, 10, 10, 10), body);
        FillRect(image, new Rect2I(13, 12, 6, 8), accent);
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateGlassTexture()
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillRect(image, new Rect2I(5, 7, 22, 18), new Color("CFFAFE"));
        FillRect(image, new Rect2I(8, 10, 16, 12), new Color("67E8F9"));
        FillRect(image, new Rect2I(11, 6, 4, 20), new Color("ECFEFF"));
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateBatteryPackTexture()
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillRect(image, new Rect2I(7, 8, 18, 16), new Color("38BDF8"));
        FillRect(image, new Rect2I(12, 4, 8, 6), new Color("0F172A"));
        FillRect(image, new Rect2I(11, 12, 10, 8), new Color("E0F2FE"));
        FillRect(image, new Rect2I(14, 14, 4, 4), new Color("22C55E"));
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateRepairKitTexture()
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillRect(image, new Rect2I(6, 10, 20, 12), new Color("22C55E"));
        FillRect(image, new Rect2I(13, 5, 6, 22), new Color("DCFCE7"));
        FillRect(image, new Rect2I(9, 13, 14, 6), new Color("DCFCE7"));
        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateAmmoTexture(Color casing, Color tip)
    {
        var image = CreateCanvas(new Color(0.0f, 0.0f, 0.0f, 0.0f));
        FillRect(image, new Rect2I(9, 8, 14, 14), casing);
        FillTriangleUp(image, new Vector2I(16, 3), 7, tip);
        FillRect(image, new Rect2I(12, 22, 8, 4), new Color("1F2937"));
        return ImageTexture.CreateFromImage(image);
    }

    private static Image CreateCanvas(Color color)
    {
        var image = Image.CreateEmpty(32, 32, false, Image.Format.Rgba8);
        image.Fill(color);
        return image;
    }

    private static void FillRect(Image image, Rect2I rect, Color color)
    {
        for (var y = rect.Position.Y; y < rect.End.Y; y++)
        {
            for (var x = rect.Position.X; x < rect.End.X; x++)
            {
                if (x >= 0 && x < image.GetWidth() && y >= 0 && y < image.GetHeight())
                {
                    image.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void FillCircle(Image image, int centerX, int centerY, int radius, Color color)
    {
        var radiusSquared = radius * radius;
        for (var y = centerY - radius; y <= centerY + radius; y++)
        {
            for (var x = centerX - radius; x <= centerX + radius; x++)
            {
                var deltaX = x - centerX;
                var deltaY = y - centerY;
                if ((deltaX * deltaX) + (deltaY * deltaY) <= radiusSquared
                    && x >= 0 && x < image.GetWidth()
                    && y >= 0 && y < image.GetHeight())
                {
                    image.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void DrawLine(Image image, Vector2I from, Vector2I to, Color color)
    {
        var steps = Mathf.Max(Mathf.Abs(to.X - from.X), Mathf.Abs(to.Y - from.Y));
        if (steps == 0)
        {
            image.SetPixel(from.X, from.Y, color);
            return;
        }

        for (var step = 0; step <= steps; step++)
        {
            var ratio = step / (float)steps;
            var x = Mathf.RoundToInt(Mathf.Lerp(from.X, to.X, ratio));
            var y = Mathf.RoundToInt(Mathf.Lerp(from.Y, to.Y, ratio));
            if (x >= 0 && x < image.GetWidth() && y >= 0 && y < image.GetHeight())
            {
                image.SetPixel(x, y, color);
            }
        }
    }

    private static void FillTriangleUp(Image image, Vector2I tip, int halfWidth, Color color)
    {
        for (var y = 0; y <= halfWidth; y++)
        {
            var width = Mathf.Max(1, halfWidth - y);
            FillRect(image, new Rect2I(tip.X - width, tip.Y + y, (width * 2) + 1, 1), color);
        }
    }
}

internal static class FactoryTransportModelLibrary
{
    public static Node3D CreateGearModel(float cellSize)
    {
        var root = new Node3D();
        root.AddChild(CreateMesh("GearBody", new CylinderMesh
        {
            TopRadius = cellSize * 0.10f,
            BottomRadius = cellSize * 0.10f,
            Height = cellSize * 0.10f
        }, new Color("EAB308"), new Vector3(0.0f, 0.0f, 0.0f)));

        root.AddChild(CreateMesh("GearToothNorth", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.06f, cellSize * 0.08f, cellSize * 0.16f)
        }, new Color("FACC15"), new Vector3(0.0f, 0.0f, cellSize * 0.11f)));
        root.AddChild(CreateMesh("GearToothSouth", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.06f, cellSize * 0.08f, cellSize * 0.16f)
        }, new Color("FACC15"), new Vector3(0.0f, 0.0f, -cellSize * 0.11f)));
        root.AddChild(CreateMesh("GearToothWest", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.16f, cellSize * 0.08f, cellSize * 0.06f)
        }, new Color("FACC15"), new Vector3(-cellSize * 0.11f, 0.0f, 0.0f)));
        root.AddChild(CreateMesh("GearToothEast", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.16f, cellSize * 0.08f, cellSize * 0.06f)
        }, new Color("FACC15"), new Vector3(cellSize * 0.11f, 0.0f, 0.0f)));
        return root;
    }

    public static Node3D CreateMachinePartModel(float cellSize)
    {
        var root = new Node3D();
        root.AddChild(CreateMesh("Core", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.24f, cellSize * 0.10f, cellSize * 0.14f)
        }, new Color("8B5CF6"), new Vector3(0.0f, 0.0f, 0.0f)));
        root.AddChild(CreateMesh("Rib", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.10f, cellSize * 0.16f, cellSize * 0.16f)
        }, new Color("A78BFA"), new Vector3(-cellSize * 0.05f, cellSize * 0.04f, 0.0f)));
        root.AddChild(CreateMesh("Head", new CylinderMesh
        {
            TopRadius = cellSize * 0.04f,
            BottomRadius = cellSize * 0.04f,
            Height = cellSize * 0.12f
        }, new Color("EDE9FE"), new Vector3(cellSize * 0.10f, 0.0f, 0.0f)));
        return root;
    }

    public static Node3D CreateAmmoMagazineModel(float cellSize)
    {
        var root = new Node3D();
        root.AddChild(CreateMesh("Casing", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.14f, cellSize * 0.20f, cellSize * 0.10f)
        }, new Color("FACC15"), new Vector3(0.0f, 0.0f, 0.0f)));
        root.AddChild(CreateMesh("Tip", new PrismMesh
        {
            Size = new Vector3(cellSize * 0.10f, cellSize * 0.08f, cellSize * 0.10f)
        }, new Color("78350F"), new Vector3(0.0f, cellSize * 0.14f, 0.0f)));
        return root;
    }

    private static MeshInstance3D CreateMesh(string name, Mesh mesh, Color color, Vector3 position)
    {
        return new MeshInstance3D
        {
            Name = name,
            Mesh = mesh,
            Position = position,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = color,
                Roughness = 0.65f
            }
        };
    }
}
