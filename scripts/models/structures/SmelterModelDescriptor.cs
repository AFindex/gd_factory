using Godot;
using NetFactory.Models;

public static class SmelterModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind, FactoryInteriorVisualRole interiorRole)
    {
        var cs = builder.CellSize;
        var bodyRig = builder.AddPivotNode("BodyRig", Vector3.Zero);

        if (siteKind == FactorySiteKind.Interior)
        {
            builder.AddBox(bodyRig, "Base", new Vector3(cs * 0.98f, 0.14f, cs * 0.98f), new Color("1C1917"), new Vector3(0.0f, 0.07f, 0.0f));
            builder.AddInteriorModuleShell(bodyRig, "SmelterCabin", new Vector3(cs * 0.80f, 0.82f, cs * 0.82f), new Color("44403C"), new Color("A8A29E"), new Vector3(0.0f, 0.62f, 0.0f));
            builder.AddBox(bodyRig, "IntakeDrawer", new Vector3(cs * 0.44f, 0.14f, cs * 0.18f), new Color("A16207"), new Vector3(0.0f, 0.34f, cs * 0.30f));

            var cabinFurnaceCore = builder.AddBox(bodyRig, "FurnaceCore", new Vector3(cs * 0.34f, 0.34f, cs * 0.42f), new Color("FB923C"), new Vector3(0.0f, 0.62f, 0.0f));
            builder.ConfigureGlowMaterial(cabinFurnaceCore, new Color("EA580C"), 2.2f);

            var cabinFireboxGlow = builder.AddBox(bodyRig, "FireboxGlow", new Vector3(cs * 0.22f, 0.20f, cs * 0.10f), new Color("FDBA74"), new Vector3(0.0f, 0.54f, cs * 0.34f));
            builder.ConfigureGlowMaterial(cabinFireboxGlow, new Color("F97316"), 2.8f);

            var cabinEmberBand = builder.AddBox(bodyRig, "EmberBand", new Vector3(cs * 0.42f, 0.05f, cs * 0.50f), new Color(1.0f, 0.60f, 0.18f, 0.72f), new Vector3(0.0f, 0.92f, 0.0f));
            if (cabinEmberBand.MaterialOverride is StandardMaterial3D cabinEmberMaterial)
            {
                cabinEmberMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                cabinEmberMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
                cabinEmberMaterial.EmissionEnabled = true;
                cabinEmberMaterial.Emission = new Color("F97316");
                cabinEmberMaterial.EmissionEnergyMultiplier = 1.9f;
            }

            builder.AddBox(bodyRig, "ExhaustStack", new Vector3(cs * 0.12f, 0.44f, cs * 0.12f), new Color("71717A"), new Vector3(-cs * 0.22f, 1.06f, 0.0f));
            var cabinExhaustGlow = builder.AddBox(bodyRig, "ExhaustGlow", new Vector3(cs * 0.10f, 0.10f, cs * 0.10f), new Color("FDBA74"), new Vector3(-cs * 0.22f, 1.34f, 0.0f));
            builder.ConfigureGlowMaterial(cabinExhaustGlow, new Color("FB923C"), 2.0f);

            var cabinSmokeParticles = CreateSmokeParticles(cs);
            cabinSmokeParticles.Name = "HeatPlume";
            cabinSmokeParticles.Position = new Vector3(-cs * 0.22f, 1.42f, 0.0f);
            cabinSmokeParticles.Scale = new Vector3(0.72f, 0.72f, 0.72f);
            bodyRig.AddChild(cabinSmokeParticles);
            return;
        }

        builder.AddBox(bodyRig, "Base", new Vector3(cs * 0.98f, 0.18f, cs * 0.98f), new Color("292524"), new Vector3(0.0f, 0.09f, 0.0f));
        builder.AddBox(bodyRig, "Footing", new Vector3(cs * 0.88f, 0.16f, cs * 0.88f), new Color("44403C"), new Vector3(0.0f, 0.22f, 0.0f));
        builder.AddBox(bodyRig, "BodyShell", new Vector3(cs * 0.82f, 0.88f, cs * 0.82f), new Color("57534E"), new Vector3(0.0f, 0.70f, 0.0f));
        builder.AddBox(bodyRig, "TopCap", new Vector3(cs * 0.70f, 0.14f, cs * 0.70f), new Color("78716C"), new Vector3(0.0f, 1.18f, 0.0f));
        builder.AddBox(bodyRig, "IntakeHood", new Vector3(cs * 0.52f, 0.20f, cs * 0.24f), new Color("A16207"), new Vector3(0.0f, 0.78f, cs * 0.24f));
        builder.AddBox(bodyRig, "IntakeMouth", new Vector3(cs * 0.42f, 0.34f, cs * 0.10f), new Color("0F172A"), new Vector3(0.0f, 0.72f, cs * 0.36f));

        var furnaceCore = builder.AddBox(bodyRig, "FurnaceCore", new Vector3(cs * 0.42f, 0.44f, cs * 0.50f), new Color("FB923C"), new Vector3(0.0f, 0.66f, 0.0f));
        builder.ConfigureGlowMaterial(furnaceCore, new Color("EA580C"), 2.2f);

        builder.AddBox(bodyRig, "DoorFrame", new Vector3(cs * 0.46f, 0.54f, cs * 0.08f), new Color("B45309"), new Vector3(0.0f, 0.50f, cs * 0.33f));
        var fireboxGlow = builder.AddBox(bodyRig, "FireboxGlow", new Vector3(cs * 0.24f, 0.34f, cs * 0.09f), new Color("FDBA74"), new Vector3(0.0f, 0.49f, cs * 0.42f));
        builder.ConfigureGlowMaterial(fireboxGlow, new Color("F97316"), 2.8f);

        var emberBand = builder.AddBox(bodyRig, "EmberBand", new Vector3(cs * 0.48f, 0.06f, cs * 0.56f), new Color(1.0f, 0.60f, 0.18f, 0.72f), new Vector3(0.0f, 0.96f, 0.0f));
        if (emberBand.MaterialOverride is StandardMaterial3D emberMaterial)
        {
            emberMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            emberMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            emberMaterial.EmissionEnabled = true;
            emberMaterial.Emission = new Color("F97316");
            emberMaterial.EmissionEnergyMultiplier = 1.9f;
        }

        builder.AddBox(bodyRig, "ChimneyBase", new Vector3(cs * 0.24f, 0.12f, cs * 0.24f), new Color("44403C"), new Vector3(-cs * 0.18f, 1.22f, 0.0f));
        builder.AddBox(bodyRig, "ChimneyStack", new Vector3(cs * 0.18f, 0.62f, cs * 0.18f), new Color("71717A"), new Vector3(-cs * 0.18f, 1.54f, 0.0f));
        var exhaustGlow = builder.AddBox(bodyRig, "ExhaustGlow", new Vector3(cs * 0.11f, 0.12f, cs * 0.11f), new Color("FDBA74"), new Vector3(-cs * 0.18f, 1.90f, 0.0f));
        builder.ConfigureGlowMaterial(exhaustGlow, new Color("FB923C"), 2.0f);

        var smokeParticles = CreateSmokeParticles(cs);
        smokeParticles.Name = "HeatPlume";
        smokeParticles.Position = new Vector3(-cs * 0.18f, 2.02f, 0.0f);
        smokeParticles.Scale = new Vector3(0.85f, 0.85f, 0.85f);
        bodyRig.AddChild(smokeParticles);
    }

    private static GpuParticles3D CreateSmokeParticles(float cs)
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
            Size = new Vector2(cs * 0.26f, cs * 0.26f),
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
