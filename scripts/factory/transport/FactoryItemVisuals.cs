using Godot;
using System;
using System.Collections.Generic;
using NetFactory.Models;

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

public enum FactoryCargoPresentationStandard
{
    WorldPayload,
    CabinCarrier
}

public enum FactoryTransportVisualContext
{
    Default,
    WorldRoute,
    BoundaryHandoff,
    InteriorRail,
    InteriorConversion,
    InteriorStaging
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
        bool isBatchable = true,
        FactoryCargoPresentationStandard presentationStandard = FactoryCargoPresentationStandard.WorldPayload,
        FactoryTransportVisualContext visualContext = FactoryTransportVisualContext.Default,
        bool keepsWorldScaleInsideCabin = false)
    {
        BatchKey = batchKey;
        Mode = mode;
        Tint = tint;
        MeshScale = meshScale;
        BillboardScale = billboardScale;
        Texture = texture;
        ModelFactory = modelFactory;
        IsBatchable = isBatchable;
        PresentationStandard = presentationStandard;
        VisualContext = visualContext;
        KeepsWorldScaleInsideCabin = keepsWorldScaleInsideCabin;
    }

    public string BatchKey { get; }
    public FactoryTransportRenderMode Mode { get; }
    public Color Tint { get; }
    public Vector3 MeshScale { get; }
    public Vector2 BillboardScale { get; }
    public Texture2D? Texture { get; }
    public Func<float, Node3D>? ModelFactory { get; }
    public bool IsBatchable { get; }
    public FactoryCargoPresentationStandard PresentationStandard { get; }
    public FactoryTransportVisualContext VisualContext { get; }
    public bool KeepsWorldScaleInsideCabin { get; }
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
        bool allowBillboardFallback = true,
        string? profileId = null,
        bool preferModelPrimary = false,
        FactoryCargoPresentationStandard presentationStandard = FactoryCargoPresentationStandard.WorldPayload,
        FactoryTransportVisualContext visualContext = FactoryTransportVisualContext.Default,
        bool keepsWorldScaleInsideCabin = false)
    {
        Tint = tint;
        PlaceholderScale = placeholderScale ?? new Vector3(0.18f, 0.18f, 0.18f);
        TexturedMeshScale = texturedMeshScale ?? PlaceholderScale;
        BillboardScale = billboardScale ?? new Vector2(0.34f, 0.34f);
        Texture = texture;
        ModelFactory = modelFactory;
        AllowTexturedMeshFallback = allowTexturedMeshFallback;
        AllowBillboardFallback = allowBillboardFallback;
        ProfileId = string.IsNullOrWhiteSpace(profileId) ? "default" : profileId;
        PreferModelPrimary = preferModelPrimary;
        PresentationStandard = presentationStandard;
        VisualContext = visualContext;
        KeepsWorldScaleInsideCabin = keepsWorldScaleInsideCabin;
    }

    public Color Tint { get; }
    public Vector3 PlaceholderScale { get; }
    public Vector3 TexturedMeshScale { get; }
    public Vector2 BillboardScale { get; }
    public Texture2D? Texture { get; }
    public Func<float, Node3D>? ModelFactory { get; }
    public bool AllowTexturedMeshFallback { get; }
    public bool AllowBillboardFallback { get; }
    public string ProfileId { get; }
    public bool PreferModelPrimary { get; }
    public FactoryCargoPresentationStandard PresentationStandard { get; }
    public FactoryTransportVisualContext VisualContext { get; }
    public bool KeepsWorldScaleInsideCabin { get; }

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
        return FactoryIndustrialStandards.GetCargoPresentationLabel(itemKind, cargoForm);
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
        return ResolveVisualProfile(item, ResolveDefaultVisualContext(item.CargoForm));
    }

    public static FactoryTransportVisualProfile ResolveVisualProfile(FactoryItem item, FactoryTransportVisualContext visualContext)
    {
        if (item.CargoForm == FactoryCargoForm.InteriorFeed)
        {
            return ResolveVisualProfile(item.ItemKind, item.CargoForm, visualContext);
        }

        var definition = GetDefinition(item.ItemKind);
        return CreateWorldBundleProfile(item, definition.VisualProfile, visualContext);
    }

    public static FactoryTransportVisualProfile ResolveVisualProfile(FactoryItemKind itemKind, FactoryCargoForm cargoForm)
    {
        return ResolveVisualProfile(itemKind, cargoForm, ResolveDefaultVisualContext(cargoForm));
    }

    public static FactoryTransportVisualProfile ResolveVisualProfile(
        FactoryItemKind itemKind,
        FactoryCargoForm cargoForm,
        FactoryTransportVisualContext visualContext)
    {
        var definition = GetDefinition(itemKind);
        var baseProfile = definition.VisualProfile;
        return cargoForm switch
            {
            FactoryCargoForm.WorldBulk => CreateWorldBundleProfile(
                new FactoryItem(-1, BuildPrototypeKind.Storage, itemKind, cargoForm),
                baseProfile,
                visualContext),
            FactoryCargoForm.InteriorFeed => CreateInteriorFeedProfile(itemKind, baseProfile, visualContext),
            _ => CreateWorldBundleProfile(
                new FactoryItem(-1, BuildPrototypeKind.Storage, itemKind, cargoForm),
                baseProfile,
                visualContext)
        };
    }

    private static FactoryTransportVisualProfile CreateWorldBundleProfile(
        FactoryItem item,
        FactoryTransportVisualProfile baseProfile,
        FactoryTransportVisualContext visualContext)
    {
        var tier = FactoryBundleCatalog.ResolveSizeTier(item);
        var resolvedContext = visualContext == FactoryTransportVisualContext.Default
            ? FactoryTransportVisualContext.WorldRoute
            : visualContext;
        var sizeMultiplier = tier switch
        {
            FactoryBundleSizeTier.Compact => new Vector3(1.86f, 1.54f, 1.86f),
            FactoryBundleSizeTier.Wide => new Vector3(2.78f, 1.88f, 2.38f),
            _ => new Vector3(2.28f, 1.72f, 2.10f)
        };
        var minimumScale = tier switch
        {
            FactoryBundleSizeTier.Compact => new Vector3(0.34f, 0.26f, 0.34f),
            FactoryBundleSizeTier.Wide => new Vector3(0.60f, 0.42f, 0.52f),
            _ => new Vector3(0.46f, 0.34f, 0.42f)
        };

        return new FactoryTransportVisualProfile(
            GetAccentColor(item.ItemKind, item.CargoForm),
            placeholderScale: MaxVector3(baseProfile.PlaceholderScale * sizeMultiplier, minimumScale),
            texturedMeshScale: MaxVector3(baseProfile.TexturedMeshScale * sizeMultiplier, minimumScale),
            billboardScale: MaxVector2(baseProfile.BillboardScale * 1.12f, new Vector2(0.42f, 0.36f)),
            texture: baseProfile.Texture,
            modelFactory: null,
            allowTexturedMeshFallback: true,
            allowBillboardFallback: false,
            profileId: $"world-bundle:{item.BundleTemplateId}:{item.ItemKind}:{resolvedContext}:{tier}",
            preferModelPrimary: false,
            presentationStandard: FactoryCargoPresentationStandard.WorldPayload,
            visualContext: resolvedContext,
            keepsWorldScaleInsideCabin: true);
    }

    private static FactoryTransportVisualProfile CreateInteriorFeedProfile(
        FactoryItemKind itemKind,
        FactoryTransportVisualProfile baseProfile,
        FactoryTransportVisualContext visualContext)
    {
        var tint = GetAccentColor(itemKind, FactoryCargoForm.InteriorFeed);
        var carrierLabel = FactoryIndustrialStandards.GetInteriorCarrierLabel(itemKind);
        var resolvedContext = visualContext == FactoryTransportVisualContext.Default
            ? FactoryTransportVisualContext.InteriorRail
            : visualContext;
        return itemKind switch
        {
            FactoryItemKind.Coal or FactoryItemKind.IronOre or FactoryItemKind.CopperOre or FactoryItemKind.StoneOre or FactoryItemKind.SulfurOre or FactoryItemKind.QuartzOre
                => new FactoryTransportVisualProfile(
                    tint,
                    placeholderScale: new Vector3(0.20f, 0.24f, 0.20f),
                    texturedMeshScale: new Vector3(0.18f, 0.22f, 0.18f),
                    billboardScale: baseProfile.BillboardScale * 0.74f,
                    texture: baseProfile.Texture,
                    modelFactory: cellSize => TransportModelLibrary.CreateInteriorCanisterModel(cellSize, tint),
                    allowTexturedMeshFallback: true,
                    allowBillboardFallback: true,
                    profileId: $"interior:{carrierLabel}:{itemKind}:{resolvedContext}",
                    preferModelPrimary: true,
                    presentationStandard: FactoryCargoPresentationStandard.CabinCarrier,
                    visualContext: resolvedContext),
            FactoryItemKind.IronPlate or FactoryItemKind.CopperPlate or FactoryItemKind.SteelPlate or FactoryItemKind.StoneBrick or FactoryItemKind.Glass
                => new FactoryTransportVisualProfile(
                    tint,
                    placeholderScale: new Vector3(0.26f, 0.12f, 0.20f),
                    texturedMeshScale: new Vector3(0.22f, 0.12f, 0.18f),
                    billboardScale: baseProfile.BillboardScale * 0.78f,
                    texture: baseProfile.Texture,
                    modelFactory: cellSize => TransportModelLibrary.CreateInteriorTrayModel(cellSize, tint),
                    allowTexturedMeshFallback: true,
                    allowBillboardFallback: true,
                    profileId: $"interior:{carrierLabel}:{itemKind}:{resolvedContext}",
                    preferModelPrimary: true,
                    presentationStandard: FactoryCargoPresentationStandard.CabinCarrier,
                    visualContext: resolvedContext),
            FactoryItemKind.CopperWire or FactoryItemKind.CircuitBoard
                => new FactoryTransportVisualProfile(
                    tint,
                    placeholderScale: new Vector3(0.24f, 0.14f, 0.18f),
                    texturedMeshScale: new Vector3(0.20f, 0.12f, 0.16f),
                    billboardScale: baseProfile.BillboardScale * 0.76f,
                    texture: baseProfile.Texture,
                    modelFactory: cellSize => TransportModelLibrary.CreateInteriorElectronicsCassetteModel(cellSize, tint),
                    allowTexturedMeshFallback: true,
                    allowBillboardFallback: true,
                    profileId: $"interior:{carrierLabel}:{itemKind}:{resolvedContext}",
                    preferModelPrimary: true,
                    presentationStandard: FactoryCargoPresentationStandard.CabinCarrier,
                    visualContext: resolvedContext),
            FactoryItemKind.AmmoMagazine or FactoryItemKind.HighVelocityAmmo
                => new FactoryTransportVisualProfile(
                    tint,
                    placeholderScale: new Vector3(0.22f, 0.16f, 0.16f),
                    texturedMeshScale: new Vector3(0.20f, 0.14f, 0.14f),
                    billboardScale: baseProfile.BillboardScale * 0.74f,
                    texture: baseProfile.Texture,
                    modelFactory: cellSize => TransportModelLibrary.CreateInteriorAmmoCassetteModel(cellSize, tint),
                    allowTexturedMeshFallback: true,
                    allowBillboardFallback: true,
                    profileId: $"interior:{carrierLabel}:{itemKind}:{resolvedContext}",
                    preferModelPrimary: true,
                    presentationStandard: FactoryCargoPresentationStandard.CabinCarrier,
                    visualContext: resolvedContext),
            FactoryItemKind.SulfurCrystal
                => new FactoryTransportVisualProfile(
                    tint,
                    placeholderScale: new Vector3(0.18f, 0.22f, 0.18f),
                    texturedMeshScale: new Vector3(0.16f, 0.20f, 0.16f),
                    billboardScale: baseProfile.BillboardScale * 0.72f,
                    texture: baseProfile.Texture,
                    modelFactory: cellSize => TransportModelLibrary.CreateInteriorCrystalCaseModel(cellSize, tint),
                    allowTexturedMeshFallback: true,
                    allowBillboardFallback: true,
                    profileId: $"interior:{carrierLabel}:{itemKind}:{resolvedContext}",
                    preferModelPrimary: true,
                    presentationStandard: FactoryCargoPresentationStandard.CabinCarrier,
                    visualContext: resolvedContext),
            _ => new FactoryTransportVisualProfile(
                tint,
                placeholderScale: new Vector3(0.22f, 0.16f, 0.18f),
                texturedMeshScale: new Vector3(0.20f, 0.14f, 0.16f),
                billboardScale: baseProfile.BillboardScale * 0.76f,
                texture: baseProfile.Texture,
                modelFactory: cellSize => TransportModelLibrary.CreateInteriorUtilityCassetteModel(cellSize, tint),
                allowTexturedMeshFallback: true,
                allowBillboardFallback: true,
                profileId: $"interior:{carrierLabel}:{itemKind}:{resolvedContext}",
                preferModelPrimary: true,
                presentationStandard: FactoryCargoPresentationStandard.CabinCarrier,
                visualContext: resolvedContext)
        };
    }

    private static FactoryTransportVisualContext ResolveDefaultVisualContext(FactoryCargoForm cargoForm)
    {
        return cargoForm switch
        {
            FactoryCargoForm.InteriorFeed => FactoryTransportVisualContext.InteriorRail,
            FactoryCargoForm.WorldBulk or FactoryCargoForm.WorldPacked => FactoryTransportVisualContext.WorldRoute,
            _ => FactoryTransportVisualContext.Default
        };
    }

    private static Vector2 MaxVector2(Vector2 value, Vector2 minimum)
    {
        return new Vector2(
            Mathf.Max(value.X, minimum.X),
            Mathf.Max(value.Y, minimum.Y));
    }

    private static Vector3 MaxVector3(Vector3 value, Vector3 minimum)
    {
        return new Vector3(
            Mathf.Max(value.X, minimum.X),
            Mathf.Max(value.Y, minimum.Y),
            Mathf.Max(value.Z, minimum.Z));
    }

    public static bool TryGetFuelValueSeconds(FactoryItemKind itemKind, out float burnSeconds)
    {
        burnSeconds = GetDefinition(itemKind).FuelValueSeconds;
        return burnSeconds > 0.0f;
    }

    private static IReadOnlyDictionary<FactoryItemKind, FactoryItemDefinition> CreateDefinitions()
    {
        var textures = GeneratedItemTextureLibrary.CreateTextures();
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
                    modelFactory: TransportModelLibrary.CreateGearModel),
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
                    modelFactory: TransportModelLibrary.CreateMachinePartModel),
                iconTexture: textures["machine-part"],
                maxStackSize: 6),
            [FactoryItemKind.AmmoMagazine] = new FactoryItemDefinition(
                FactoryItemKind.AmmoMagazine,
                "弹药",
                new Color("FACC15"),
                new FactoryTransportVisualProfile(
                    new Color("FACC15"),
                    texture: textures["ammo-magazine"],
                    modelFactory: TransportModelLibrary.CreateAmmoMagazineModel),
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
        return CreateVisual(item, cellSize, FactoryTransportVisualContext.Default);
    }

    public static Node3D CreateVisual(FactoryItem item, float cellSize, FactoryTransportVisualContext visualContext)
    {
        return CreateNodeForDescriptor(ResolveDescriptorSet(item, cellSize, visualContext).Primary, item.ItemKind, cellSize);
    }

    public static Node3D CreateVisual(FactoryItemKind itemKind, float cellSize)
    {
        return CreateNodeForDescriptor(ResolveDescriptorSet(itemKind, cellSize).Primary, itemKind, cellSize);
    }

    public static FactoryTransportRenderDescriptorSet ResolveDescriptorSet(FactoryItem item, float cellSize)
    {
        return ResolveDescriptorSet(item, cellSize, FactoryTransportVisualContext.Default);
    }

    public static FactoryTransportRenderDescriptorSet ResolveDescriptorSet(
        FactoryItem item,
        float cellSize,
        FactoryTransportVisualContext visualContext)
    {
        var profile = FactoryItemCatalog.ResolveVisualProfile(item, visualContext);
        return ResolveDescriptorSet(item.ItemKind, profile, cellSize);
    }

    public static FactoryTransportRenderDescriptorSet ResolveDescriptorSet(FactoryItemKind itemKind, float cellSize)
    {
        var definition = FactoryItemCatalog.GetDefinition(itemKind);
        return ResolveDescriptorSet(itemKind, definition.VisualProfile, cellSize);
    }

    public static float EstimateOccupiedLengthProgress(FactoryTransportRenderDescriptorSet descriptors, float cellSize)
    {
        return EstimateOccupiedLengthProgress(descriptors.Primary, cellSize);
    }

    public static float EstimateOccupiedLengthProgress(FactoryTransportRenderDescriptor descriptor, float cellSize)
    {
        if (cellSize <= 0.001f)
        {
            return 0.18f;
        }

        var footprintWorldUnits = descriptor.Mode switch
        {
            FactoryTransportRenderMode.Billboard => Mathf.Max(descriptor.BillboardScale.X, descriptor.BillboardScale.Y * 0.58f),
            _ => Mathf.Max(descriptor.MeshScale.X, descriptor.MeshScale.Z)
        };

        if (descriptor.PresentationStandard == FactoryCargoPresentationStandard.WorldPayload)
        {
            footprintWorldUnits = Mathf.Max(footprintWorldUnits + (cellSize * 0.08f), cellSize * 0.44f);
            return Mathf.Clamp(footprintWorldUnits / cellSize, 0.24f, 0.92f);
        }

        footprintWorldUnits = Mathf.Max(footprintWorldUnits, cellSize * 0.14f);
        return Mathf.Clamp(footprintWorldUnits / cellSize, 0.14f, 0.52f);
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

    private static FactoryTransportRenderDescriptorSet ResolveDescriptorSet(FactoryItemKind itemKind, FactoryTransportVisualProfile profile, float cellSize)
    {
        var renderCellSize = ResolveRenderCellSize(profile, cellSize);
        var placeholder = CreatePlaceholderDescriptor(itemKind, profile, renderCellSize);
        var billboard = CreateBillboardDescriptor(itemKind, profile, renderCellSize) ?? placeholder;
        var textured = CreateTexturedDescriptor(itemKind, profile, renderCellSize) ?? billboard;
        var model = profile.ModelFactory is not null ? CreateModelDescriptor(itemKind, profile, renderCellSize) : null;
        var primary = profile.PreferModelPrimary && model is not null
            ? model
            : profile.AllowBillboardFallback && profile.Texture is not null
                ? billboard
                : profile.AllowTexturedMeshFallback && profile.Texture is not null
                    ? textured
                    : model ?? placeholder;
        var mid = primary.Mode == FactoryTransportRenderMode.ModelNode
            ? (profile.AllowTexturedMeshFallback && profile.Texture is not null ? textured : billboard)
            : primary.Mode == FactoryTransportRenderMode.Billboard
                ? billboard
                : profile.AllowTexturedMeshFallback && profile.Texture is not null
                    ? textured
                    : billboard;
        var far = profile.AllowBillboardFallback && profile.Texture is not null
            ? billboard
            : placeholder;
        return new FactoryTransportRenderDescriptorSet(primary, mid, far);
    }

    private static float ResolveRenderCellSize(FactoryTransportVisualProfile profile, float cellSize)
    {
        if (!profile.KeepsWorldScaleInsideCabin)
        {
            return cellSize;
        }

        return Mathf.Max(cellSize, FactoryConstants.CellSize);
    }

    private static FactoryTransportRenderDescriptor CreatePlaceholderDescriptor(FactoryItemKind itemKind, FactoryTransportVisualProfile profile, float cellSize)
    {
        return new FactoryTransportRenderDescriptor(
            $"placeholder:{itemKind}:{profile.ProfileId}:{FormatVector3(profile.PlaceholderScale * cellSize)}:{profile.Tint.ToHtml()}",
            FactoryTransportRenderMode.Placeholder,
            profile.Tint,
            profile.PlaceholderScale * cellSize,
            profile.BillboardScale * cellSize,
            presentationStandard: profile.PresentationStandard,
            visualContext: profile.VisualContext,
            keepsWorldScaleInsideCabin: profile.KeepsWorldScaleInsideCabin);
    }

    private static FactoryTransportRenderDescriptor? CreateTexturedDescriptor(FactoryItemKind itemKind, FactoryTransportVisualProfile profile, float cellSize)
    {
        if (profile.Texture is null)
        {
            return null;
        }

        return new FactoryTransportRenderDescriptor(
            $"textured:{itemKind}:{profile.ProfileId}:{profile.Texture.GetInstanceId()}:{FormatVector3(profile.TexturedMeshScale * cellSize)}",
            FactoryTransportRenderMode.TexturedBox,
            profile.Tint,
            profile.TexturedMeshScale * cellSize,
            profile.BillboardScale * cellSize,
            texture: profile.Texture,
            presentationStandard: profile.PresentationStandard,
            visualContext: profile.VisualContext,
            keepsWorldScaleInsideCabin: profile.KeepsWorldScaleInsideCabin);
    }

    private static FactoryTransportRenderDescriptor? CreateBillboardDescriptor(FactoryItemKind itemKind, FactoryTransportVisualProfile profile, float cellSize)
    {
        if (profile.Texture is null)
        {
            return null;
        }

        return new FactoryTransportRenderDescriptor(
            $"billboard:{itemKind}:{profile.ProfileId}:{profile.Texture.GetInstanceId()}:{FormatVector2(profile.BillboardScale * cellSize)}",
            FactoryTransportRenderMode.Billboard,
            profile.Tint,
            profile.PlaceholderScale * cellSize,
            profile.BillboardScale * cellSize,
            texture: profile.Texture,
            presentationStandard: profile.PresentationStandard,
            visualContext: profile.VisualContext,
            keepsWorldScaleInsideCabin: profile.KeepsWorldScaleInsideCabin);
    }

    private static FactoryTransportRenderDescriptor CreateModelDescriptor(FactoryItemKind itemKind, FactoryTransportVisualProfile profile, float cellSize)
    {
        return new FactoryTransportRenderDescriptor(
            $"model:{itemKind}:{profile.ProfileId}:{FormatVector3(profile.PlaceholderScale * cellSize)}",
            FactoryTransportRenderMode.ModelNode,
            profile.Tint,
            profile.PlaceholderScale * cellSize,
            profile.BillboardScale * cellSize,
            texture: profile.Texture,
            modelFactory: profile.ModelFactory,
            isBatchable: false,
            presentationStandard: profile.PresentationStandard,
            visualContext: profile.VisualContext,
            keepsWorldScaleInsideCabin: profile.KeepsWorldScaleInsideCabin);
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
