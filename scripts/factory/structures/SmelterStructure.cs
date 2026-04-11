using Godot;
using System.Collections.Generic;

public partial class SmelterStructure : FactoryRecipeMachineStructure
{
    private const string FurnaceBodyAlias = "furnace-body";
    private const string CoreGlowAlias = "core-glow";
    private const string FireboxGlowAlias = "firebox-glow";
    private const string ExhaustGlowAlias = "exhaust-glow";
    private const string EmberBandAlias = "ember-band";
    private const string HeatPlumeAlias = "heat-plume";

    public SmelterStructure()
        : base(2, 1, 1, 1)
    {
    }

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Smelter;
    public override string Description => "消耗矿石和电力，将原矿稳定炼成可制造的板材。";
    public override float MaxHealth => 68.0f;

    protected override IReadOnlyList<FactoryRecipeDefinition> AvailableRecipes => FactoryRecipeCatalog.SmelterRecipes;
    protected override string DetailSubtitle => "输入矿石、供电与冶炼缓存";
    protected override string InputSectionTitle => "矿石输入";
    protected override string OutputSectionTitle => "冶炼输出";
    protected override string InputInventoryId => "smelter-input";
    protected override string OutputInventoryId => "smelter-output";
    protected override string RecipeSectionTitle => "冶炼方案";
    protected override string RecipeSectionDescription => "切换当前熔炉的冶炼配方。";
    protected override int MachinePowerRangeCells => 3;
    protected override bool SupportsRecipeSelection => true;
    protected override float DispatchCooldownSeconds => 0.22f;

    protected override FactoryStructureVisualProfile CreateVisualProfile()
    {
        return new FactoryStructureVisualProfile(
            proceduralBuilder: BuildFurnaceVisualProfile,
            stateUpdater: UpdateFurnaceVisualProfile);
    }

    private void BuildFurnaceVisualProfile(FactoryStructureVisualController controller)
    {
        var bodyRig = new Node3D { Name = "BodyRig" };
        controller.Root.AddChild(bodyRig);
        controller.RegisterNodeAnchor(FurnaceBodyAlias, bodyRig);

        if (SiteKind == FactorySiteKind.Interior)
        {
            CreateBox(bodyRig, "Base", new Vector3(CellSize * 0.98f, 0.14f, CellSize * 0.98f), new Color("1C1917"), new Vector3(0.0f, 0.07f, 0.0f));
            CreateInteriorModuleShell(bodyRig, "SmelterCabin", new Vector3(CellSize * 0.80f, 0.82f, CellSize * 0.82f), new Color("44403C"), new Color("A8A29E"), new Vector3(0.0f, 0.62f, 0.0f));
            CreateBox(bodyRig, "IntakeDrawer", new Vector3(CellSize * 0.44f, 0.14f, CellSize * 0.18f), new Color("A16207"), new Vector3(0.0f, 0.34f, CellSize * 0.30f));

            var cabinFurnaceCore = CreateBox(bodyRig, "FurnaceCore", new Vector3(CellSize * 0.34f, 0.34f, CellSize * 0.42f), new Color("FB923C"), new Vector3(0.0f, 0.62f, 0.0f));
            ConfigureGlowMaterial(cabinFurnaceCore, new Color("EA580C"), 2.2f);
            RegisterMaterialAlias(controller, CoreGlowAlias, cabinFurnaceCore);

            var cabinFireboxGlow = CreateBox(bodyRig, "FireboxGlow", new Vector3(CellSize * 0.22f, 0.20f, CellSize * 0.10f), new Color("FDBA74"), new Vector3(0.0f, 0.54f, CellSize * 0.34f));
            ConfigureGlowMaterial(cabinFireboxGlow, new Color("F97316"), 2.8f);
            RegisterMaterialAlias(controller, FireboxGlowAlias, cabinFireboxGlow);

            var cabinEmberBand = CreateBox(bodyRig, "EmberBand", new Vector3(CellSize * 0.42f, 0.05f, CellSize * 0.50f), new Color(1.0f, 0.60f, 0.18f, 0.72f), new Vector3(0.0f, 0.92f, 0.0f));
            if (cabinEmberBand.MaterialOverride is StandardMaterial3D cabinEmberMaterial)
            {
                cabinEmberMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                cabinEmberMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
                cabinEmberMaterial.EmissionEnabled = true;
                cabinEmberMaterial.Emission = new Color("F97316");
                cabinEmberMaterial.EmissionEnergyMultiplier = 1.9f;
            }
            RegisterMaterialAlias(controller, EmberBandAlias, cabinEmberBand);

            CreateBox(bodyRig, "ExhaustStack", new Vector3(CellSize * 0.12f, 0.44f, CellSize * 0.12f), new Color("71717A"), new Vector3(-CellSize * 0.22f, 1.06f, 0.0f));
            var cabinExhaustGlow = CreateBox(bodyRig, "ExhaustGlow", new Vector3(CellSize * 0.10f, 0.10f, CellSize * 0.10f), new Color("FDBA74"), new Vector3(-CellSize * 0.22f, 1.34f, 0.0f));
            ConfigureGlowMaterial(cabinExhaustGlow, new Color("FB923C"), 2.0f);
            RegisterMaterialAlias(controller, ExhaustGlowAlias, cabinExhaustGlow);

            var cabinSmokeParticles = CreateSmokeParticles();
            cabinSmokeParticles.Name = "HeatPlume";
            cabinSmokeParticles.Position = new Vector3(-CellSize * 0.22f, 1.42f, 0.0f);
            cabinSmokeParticles.Scale = new Vector3(0.72f, 0.72f, 0.72f);
            bodyRig.AddChild(cabinSmokeParticles);
            controller.RegisterNodeAnchor(HeatPlumeAlias, cabinSmokeParticles);
            return;
        }

        CreateBox(bodyRig, "Base", new Vector3(CellSize * 0.98f, 0.18f, CellSize * 0.98f), new Color("292524"), new Vector3(0.0f, 0.09f, 0.0f));
        CreateBox(bodyRig, "Footing", new Vector3(CellSize * 0.88f, 0.16f, CellSize * 0.88f), new Color("44403C"), new Vector3(0.0f, 0.22f, 0.0f));
        CreateBox(bodyRig, "BodyShell", new Vector3(CellSize * 0.82f, 0.88f, CellSize * 0.82f), new Color("57534E"), new Vector3(0.0f, 0.70f, 0.0f));
        CreateBox(bodyRig, "TopCap", new Vector3(CellSize * 0.70f, 0.14f, CellSize * 0.70f), new Color("78716C"), new Vector3(0.0f, 1.18f, 0.0f));
        CreateBox(bodyRig, "IntakeHood", new Vector3(CellSize * 0.52f, 0.20f, CellSize * 0.24f), new Color("A16207"), new Vector3(0.0f, 0.78f, CellSize * 0.24f));
        CreateBox(bodyRig, "IntakeMouth", new Vector3(CellSize * 0.42f, 0.34f, CellSize * 0.10f), new Color("0F172A"), new Vector3(0.0f, 0.72f, CellSize * 0.36f));

        var furnaceCore = CreateBox(bodyRig, "FurnaceCore", new Vector3(CellSize * 0.42f, 0.44f, CellSize * 0.50f), new Color("FB923C"), new Vector3(0.0f, 0.66f, 0.0f));
        ConfigureGlowMaterial(furnaceCore, new Color("EA580C"), 2.2f);
        RegisterMaterialAlias(controller, CoreGlowAlias, furnaceCore);

        CreateBox(bodyRig, "DoorFrame", new Vector3(CellSize * 0.46f, 0.54f, CellSize * 0.08f), new Color("B45309"), new Vector3(0.0f, 0.50f, CellSize * 0.33f));
        var fireboxGlow = CreateBox(bodyRig, "FireboxGlow", new Vector3(CellSize * 0.24f, 0.34f, CellSize * 0.09f), new Color("FDBA74"), new Vector3(0.0f, 0.49f, CellSize * 0.42f));
        ConfigureGlowMaterial(fireboxGlow, new Color("F97316"), 2.8f);
        RegisterMaterialAlias(controller, FireboxGlowAlias, fireboxGlow);

        var emberBand = CreateBox(bodyRig, "EmberBand", new Vector3(CellSize * 0.48f, 0.06f, CellSize * 0.56f), new Color(1.0f, 0.60f, 0.18f, 0.72f), new Vector3(0.0f, 0.96f, 0.0f));
        if (emberBand.MaterialOverride is StandardMaterial3D emberMaterial)
        {
            emberMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            emberMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            emberMaterial.EmissionEnabled = true;
            emberMaterial.Emission = new Color("F97316");
            emberMaterial.EmissionEnergyMultiplier = 1.9f;
        }
        RegisterMaterialAlias(controller, EmberBandAlias, emberBand);

        CreateBox(bodyRig, "ChimneyBase", new Vector3(CellSize * 0.24f, 0.12f, CellSize * 0.24f), new Color("44403C"), new Vector3(-CellSize * 0.18f, 1.22f, 0.0f));
        CreateBox(bodyRig, "ChimneyStack", new Vector3(CellSize * 0.18f, 0.62f, CellSize * 0.18f), new Color("71717A"), new Vector3(-CellSize * 0.18f, 1.54f, 0.0f));
        var exhaustGlow = CreateBox(bodyRig, "ExhaustGlow", new Vector3(CellSize * 0.11f, 0.12f, CellSize * 0.11f), new Color("FDBA74"), new Vector3(-CellSize * 0.18f, 1.90f, 0.0f));
        ConfigureGlowMaterial(exhaustGlow, new Color("FB923C"), 2.0f);
        RegisterMaterialAlias(controller, ExhaustGlowAlias, exhaustGlow);

        var smokeParticles = CreateSmokeParticles();
        smokeParticles.Name = "HeatPlume";
        smokeParticles.Position = new Vector3(-CellSize * 0.18f, 2.02f, 0.0f);
        smokeParticles.Scale = new Vector3(0.85f, 0.85f, 0.85f);
        bodyRig.AddChild(smokeParticles);
        controller.RegisterNodeAnchor(HeatPlumeAlias, smokeParticles);
    }

    private void UpdateFurnaceVisualProfile(FactoryStructureVisualController controller, FactoryStructureVisualState state, float tickAlpha)
    {
        var targetHeat = state.PowerStatus switch
        {
            FactoryPowerStatus.Powered when state.IsProcessing => 1.0f,
            FactoryPowerStatus.Powered => state.HasBufferedOutput ? 0.58f : 0.34f,
            FactoryPowerStatus.Underpowered => state.IsProcessing ? 0.56f : 0.28f,
            _ => 0.06f
        };
        var heat = controller.AnimateFloat("smelter-heat", targetHeat, 0.10f + (tickAlpha * 0.24f), initialValue: 0.08f);
        var pulseAmplitude = state.IsProcessing
            ? 0.18f + (state.ProcessRatio * 0.14f)
            : state.HasBufferedOutput
                ? 0.08f
                : 0.03f;
        var pulse = 0.5f + (0.5f * Mathf.Sin((float)(state.PresentationTimeSeconds * (state.IsProcessing ? 8.0 : 3.5))));
        var flicker = heat + (pulseAmplitude * pulse * heat);

        if (controller.GetMaterialAnchor(CoreGlowAlias) is StandardMaterial3D coreMaterial)
        {
            coreMaterial.AlbedoColor = new Color(0.24f + (0.62f * heat), 0.10f + (0.30f * heat), 0.05f + (0.10f * heat));
            coreMaterial.Emission = new Color(0.92f, 0.34f + (0.26f * flicker), 0.10f);
            coreMaterial.EmissionEnergyMultiplier = 0.25f + (3.6f * flicker);
        }

        if (controller.GetMaterialAnchor(FireboxGlowAlias) is StandardMaterial3D fireboxMaterial)
        {
            fireboxMaterial.AlbedoColor = new Color(0.42f + (0.48f * flicker), 0.15f + (0.24f * flicker), 0.08f);
            fireboxMaterial.Emission = new Color(1.0f, 0.52f + (0.20f * pulse), 0.12f);
            fireboxMaterial.EmissionEnergyMultiplier = 0.18f + (4.2f * flicker);
        }

        if (controller.GetMaterialAnchor(ExhaustGlowAlias) is StandardMaterial3D exhaustMaterial)
        {
            var exhaustHeat = heat * (state.IsProcessing ? 1.0f : 0.72f);
            exhaustMaterial.AlbedoColor = new Color(0.30f + (0.54f * exhaustHeat), 0.16f + (0.28f * exhaustHeat), 0.10f);
            exhaustMaterial.Emission = new Color(1.0f, 0.58f, 0.16f);
            exhaustMaterial.EmissionEnergyMultiplier = 0.12f + (2.8f * exhaustHeat);
        }

        if (controller.GetMaterialAnchor(EmberBandAlias) is StandardMaterial3D emberMaterial)
        {
            var emberAlpha = 0.10f + (0.55f * heat);
            emberMaterial.AlbedoColor = new Color(1.0f, 0.65f, 0.24f, emberAlpha);
            emberMaterial.Emission = new Color(1.0f, 0.48f + (0.16f * pulse), 0.12f);
            emberMaterial.EmissionEnergyMultiplier = 0.08f + (2.4f * flicker);
        }

        if (controller.GetNodeAnchor<GpuParticles3D>(HeatPlumeAlias) is GpuParticles3D smokeParticles)
        {
            smokeParticles.Visible = heat > 0.08f;
            smokeParticles.Emitting = heat > 0.08f;
            smokeParticles.SpeedScale = 0.42f + (heat * 1.10f);
            var plumeScale = 0.65f + (heat * 1.05f) + (pulse * 0.12f * heat);
            smokeParticles.Scale = new Vector3(0.72f + (plumeScale * 0.22f), 0.72f + (plumeScale * 0.42f), 0.72f + (plumeScale * 0.22f));
            smokeParticles.Position = new Vector3(-CellSize * 0.18f, 2.02f + (pulse * 0.04f * heat), 0.0f);
        }

        controller.TryPlayAnimation(state.IsProcessing ? "working" : "idle");
    }

    private static void ConfigureGlowMaterial(MeshInstance3D mesh, Color emissionColor, float emissionEnergy)
    {
        if (mesh.MaterialOverride is not StandardMaterial3D material)
        {
            return;
        }

        material.Roughness = 0.18f;
        material.EmissionEnabled = true;
        material.Emission = emissionColor;
        material.EmissionEnergyMultiplier = emissionEnergy;
    }

    private static void RegisterMaterialAlias(FactoryStructureVisualController controller, string alias, MeshInstance3D mesh)
    {
        if (mesh.MaterialOverride is StandardMaterial3D material)
        {
            controller.RegisterMaterialAnchor(alias, material);
        }
    }

    private GpuParticles3D CreateSmokeParticles()
    {
        var smokeParticles = new GpuParticles3D
        {
            Amount = 10,
            Lifetime = 1.8,
            OneShot = false,
            Explosiveness = 0.0f,
            Randomness = 0.45f,
            Emitting = true,
            DrawPasses = 1,
            VisibilityAabb = new Aabb(new Vector3(-0.45f, -0.1f, -0.45f), new Vector3(0.9f, 2.1f, 0.9f))
        };

        var smokeMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.42f, 0.42f, 0.42f, 0.24f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };
        var smokeQuad = new QuadMesh
        {
            Size = new Vector2(CellSize * 0.26f, CellSize * 0.26f),
            Material = smokeMaterial
        };
        smokeParticles.DrawPass1 = smokeQuad;

        var processMaterial = new ParticleProcessMaterial
        {
            Direction = Vector3.Up,
            Spread = 18.0f,
            Gravity = new Vector3(0.0f, 0.16f, 0.0f),
            InitialVelocityMin = 0.20f,
            InitialVelocityMax = 0.55f,
            ScaleMin = 0.35f,
            ScaleMax = 0.72f,
            DampingMin = 0.06f,
            DampingMax = 0.18f
        };
        smokeParticles.ProcessMaterial = processMaterial;

        return smokeParticles;
    }
}
