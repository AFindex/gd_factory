using Godot;
using NetFactory.Models;

public static class GeneratorModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind, FactoryInteriorVisualRole interiorRole)
    {
        var cs = builder.CellSize;
        var root = builder.Root;

        if (siteKind == FactorySiteKind.Interior)
        {
            builder.AddDisc("PowerRange",
                cs * 5,
                0.03f,
                new Color(0.98f, 0.66f, 0.19f, 0.12f),
                new Vector3(0.0f, 0.02f, 0.0f));

            builder.AddBox("Base", new Vector3(cs * 0.96f, 0.16f, cs * 0.96f), new Color("1C1917"), new Vector3(0.0f, 0.08f, 0.0f));
            builder.AddInteriorModuleShell(root, "GeneratorCabin", new Vector3(cs * 0.78f, 0.74f, cs * 0.78f), new Color("44403C"), new Color("A8A29E"), new Vector3(0.0f, 0.56f, 0.0f));
            builder.AddBox("FuelDrawer", new Vector3(cs * 0.22f, 0.30f, cs * 0.26f), new Color("57534E"), new Vector3(cs * 0.28f, 0.32f, 0.18f));
            builder.AddBox("BusCoupler", new Vector3(cs * 0.18f, 0.18f, cs * 0.54f), new Color("FBBF24"), new Vector3(-cs * 0.28f, 0.72f, 0.0f));
            builder.AddBox("HeatStack", new Vector3(cs * 0.12f, 0.40f, cs * 0.12f), new Color("A8A29E"), new Vector3(-cs * 0.28f, 1.08f, cs * 0.20f));
            builder.AddBox("Beacon", new Vector3(cs * 0.12f, 0.12f, cs * 0.12f), new Color("FBBF24"), new Vector3(cs * 0.26f, 0.96f, cs * 0.18f));

            var rotorRig = builder.AddPivotNode("RotorRig", new Vector3(0.0f, 0.70f, 0.0f));
            builder.AddCylinder(rotorRig, "RotorCore", cs * 0.08f, cs * 0.18f, new Color("CBD5E1"), Vector3.Zero);
            CreateFanBlades(rotorRig, cs);
            builder.AddBox(rotorRig, "RotorMarker", new Vector3(cs * 0.05f, cs * 0.10f, cs * 0.06f), new Color("FDE047"), new Vector3(cs * 0.17f, 0.0f, 0.0f));

            var steamParticles = CreateSteamParticles(cs);
            steamParticles.Name = "SteamParticles";
            root.AddChild(steamParticles);
            return;
        }

        builder.AddDisc("PowerRange",
            cs * 5,
            0.03f,
            new Color(0.98f, 0.66f, 0.19f, 0.14f),
            new Vector3(0.0f, 0.02f, 0.0f));

        builder.AddBox("Base", new Vector3(cs * 0.96f, 0.20f, cs * 0.96f), new Color("292524"), new Vector3(0.0f, 0.10f, 0.0f));
        builder.AddBox("ServiceDeck", new Vector3(cs * 0.84f, 0.10f, cs * 0.84f), new Color("44403C"), new Vector3(0.0f, 0.24f, 0.0f));
        builder.AddBox("FuelCabinet", new Vector3(cs * 0.24f, 0.40f, cs * 0.28f), new Color("57534E"), new Vector3(cs * 0.28f, 0.46f, 0.18f));
        builder.AddBox("ExhaustStack", new Vector3(cs * 0.12f, 0.62f, cs * 0.12f), new Color("A8A29E"), new Vector3(-cs * 0.28f, 1.08f, cs * 0.20f));

        builder.AddCylinder("TurbineShell", cs * 0.26f, cs * 0.78f, new Color("71717A"), new Vector3(0.0f, 0.66f, 0.0f));
        CreateDashedBandRing(root, builder, "ShellBandFront", -cs * 0.14f, new Color("38BDF8"), cs);
        CreateDashedBandRing(root, builder, "ShellBandRear", cs * 0.16f, new Color("F59E0B"), cs);
        builder.AddBox("ShellMaintenancePanel", new Vector3(cs * 0.10f, cs * 0.18f, cs * 0.24f), new Color("FDE68A"), new Vector3(cs * 0.22f, 0.66f, 0.06f));
        builder.AddBox("ShellHeatPanel", new Vector3(cs * 0.10f, cs * 0.18f, cs * 0.22f), new Color("FB7185"), new Vector3(-cs * 0.22f, 0.66f, -0.04f));
        builder.AddCylinder("TurbineIntakeRing", cs * 0.31f, cs * 0.08f, new Color("D6D3D1"), new Vector3(0.0f, 0.66f, -cs * 0.34f));
        builder.AddCylinder("TurbineRearRing", cs * 0.22f, cs * 0.08f, new Color("A8A29E"), new Vector3(0.0f, 0.66f, cs * 0.34f));
        builder.AddCylinder("TurbineNozzle", cs * 0.17f, cs * 0.12f, new Color("78716C"), new Vector3(0.0f, 0.66f, cs * 0.46f));
        builder.AddBox("SupportLeft", new Vector3(cs * 0.08f, 0.50f, cs * 0.08f), new Color("78716C"), new Vector3(-cs * 0.20f, 0.46f, 0.0f));
        builder.AddBox("SupportRight", new Vector3(cs * 0.08f, 0.50f, cs * 0.08f), new Color("78716C"), new Vector3(cs * 0.20f, 0.46f, 0.0f));
        builder.AddBox("IntakeRingAccentTop", new Vector3(cs * 0.18f, cs * 0.06f, cs * 0.040f), new Color("F59E0B"), new Vector3(0.0f, 0.92f, -cs * 0.34f));
        builder.AddBox("IntakeRingAccentBottom", new Vector3(cs * 0.18f, cs * 0.06f, cs * 0.040f), new Color("F59E0B"), new Vector3(0.0f, 0.40f, -cs * 0.34f));
        builder.AddBox("IntakeRingAccentLeft", new Vector3(cs * 0.06f, cs * 0.18f, cs * 0.040f), new Color("38BDF8"), new Vector3(-cs * 0.26f, 0.66f, -cs * 0.34f));
        builder.AddBox("IntakeRingAccentRight", new Vector3(cs * 0.06f, cs * 0.18f, cs * 0.040f), new Color("38BDF8"), new Vector3(cs * 0.26f, 0.66f, -cs * 0.34f));
        builder.AddBox("IntakeRingAccentTopLeft", new Vector3(cs * 0.06f, cs * 0.06f, cs * 0.040f), new Color("FDE68A"), new Vector3(-cs * 0.18f, 0.84f, -cs * 0.34f));
        builder.AddBox("IntakeRingAccentBottomRight", new Vector3(cs * 0.06f, cs * 0.06f, cs * 0.040f), new Color("7DD3FC"), new Vector3(cs * 0.18f, 0.48f, -cs * 0.34f));

        var rotorRigW = builder.AddPivotNode("RotorRig", new Vector3(0.0f, 0.66f, -cs * 0.31f));
        builder.AddCylinder(rotorRigW, "RotorCore", cs * 0.08f, cs * 0.18f, new Color("CBD5E1"), new Vector3(0.0f, 0.0f, 0.0f));
        CreateFanBlades(rotorRigW, cs);
        builder.AddBox(rotorRigW, "RotorMarker", new Vector3(cs * 0.05f, cs * 0.10f, cs * 0.06f), new Color("FDE047"), new Vector3(cs * 0.17f, 0.0f, 0.0f));
        builder.AddBox("IntakeStrutTop", new Vector3(cs * 0.05f, 0.16f, cs * 0.05f), new Color("A8A29E"), new Vector3(0.0f, 0.87f, -cs * 0.31f));
        builder.AddBox("IntakeStrutBottom", new Vector3(cs * 0.05f, 0.16f, cs * 0.05f), new Color("A8A29E"), new Vector3(0.0f, 0.45f, -cs * 0.31f));
        builder.AddBox("Beacon", new Vector3(cs * 0.14f, 0.14f, cs * 0.14f), new Color("FBBF24"), new Vector3(cs * 0.28f, 1.06f, cs * 0.18f));

        var steamParticlesW = CreateSteamParticles(cs);
        steamParticlesW.Name = "SteamParticles";
        root.AddChild(steamParticlesW);
    }

    private static void CreateFanBlades(Node parent, float cs)
    {
        const int bladeCount = 6;
        for (var index = 0; index < bladeCount; index++)
        {
            var bladePivot = new Node3D
            {
                Name = $"BladePivot{index}",
                Rotation = new Vector3(0.0f, 0.0f, Mathf.Tau * index / bladeCount)
            };
            parent.AddChild(bladePivot);

            var bladeColor = index == 0
                ? new Color("F59E0B")
                : index % 2 == 0
                    ? new Color("E7E5E4")
                    : new Color("94A3B8");

            var blade = new MeshInstance3D
            {
                Name = $"Blade{index}",
                Mesh = new BoxMesh { Size = new Vector3(cs * 0.06f, cs * 0.24f, cs * 0.03f) },
                Position = new Vector3(0.0f, cs * 0.11f, 0.0f),
                Rotation = new Vector3(0.0f, 0.32f, 0.0f),
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = bladeColor,
                    Roughness = 0.72f
                }
            };
            bladePivot.AddChild(blade);

            var tipColor = index == 0
                ? new Color("FDE68A")
                : index % 2 == 0
                    ? new Color("475569")
                    : new Color("CBD5E1");

            var tip = new MeshInstance3D
            {
                Name = $"BladeTip{index}",
                Mesh = new BoxMesh { Size = new Vector3(cs * 0.07f, cs * 0.06f, cs * 0.035f) },
                Position = new Vector3(0.0f, cs * 0.22f, 0.0f),
                Rotation = new Vector3(0.0f, 0.32f, 0.0f),
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = tipColor,
                    Roughness = 0.72f
                }
            };
            bladePivot.AddChild(tip);

            if (index == 0)
            {
                if (blade.MaterialOverride is StandardMaterial3D bladeMaterial)
                {
                    bladeMaterial.EmissionEnabled = true;
                    bladeMaterial.Emission = new Color("F59E0B");
                    bladeMaterial.EmissionEnergyMultiplier = 0.55f;
                }

                if (tip.MaterialOverride is StandardMaterial3D tipMaterial)
                {
                    tipMaterial.EmissionEnabled = true;
                    tipMaterial.Emission = new Color("FDE68A");
                    tipMaterial.EmissionEnergyMultiplier = 0.75f;
                }
            }
        }
    }

    private static void CreateDashedBandRing(Node root, IModelBuilder builder, string prefix, float zPosition, Color color, float cs)
    {
        const int segmentCount = 8;
        var ringRadius = cs * 0.29f;
        var rig = builder.AddPivotNode($"{prefix}Rig", new Vector3(0.0f, 0.66f, zPosition));

        for (var index = 0; index < segmentCount; index++)
        {
            var angle = Mathf.Tau * index / segmentCount;
            var segment = builder.AddBox(
                rig,
                $"{prefix}_{index}",
                new Vector3(cs * 0.12f, cs * 0.045f, cs * 0.07f),
                color,
                new Vector3(
                    Mathf.Cos(angle) * ringRadius,
                    Mathf.Sin(angle) * ringRadius,
                    0.0f));
            segment.Rotation = new Vector3(0.0f, 0.0f, angle + (Mathf.Pi * 0.5f));
        }
    }

    private static GpuParticles3D CreateSteamParticles(float cs)
    {
        var particles = new GpuParticles3D
        {
            Name = "SteamParticles",
            Amount = 18,
            Lifetime = 1.4,
            OneShot = false,
            Explosiveness = 0.0f,
            Randomness = 0.35f,
            Emitting = true,
            DrawPasses = 1,
            VisibilityAabb = new Aabb(new Vector3(-0.45f, -0.2f, -0.45f), new Vector3(0.9f, 2.0f, 0.9f)),
            Position = new Vector3(-cs * 0.28f, 1.36f, cs * 0.20f)
        };

        var steamMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.86f, 0.90f, 0.96f, 0.20f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };
        var steamQuad = new QuadMesh
        {
            Size = new Vector2(cs * 0.20f, cs * 0.20f),
            Material = steamMaterial
        };
        particles.DrawPass1 = steamQuad;

        var processMaterial = new ParticleProcessMaterial
        {
            Direction = new Vector3(0.0f, 1.0f, 0.10f),
            Spread = 12.0f,
            Gravity = new Vector3(0.0f, 0.12f, 0.0f),
            InitialVelocityMin = 0.20f,
            InitialVelocityMax = 0.46f,
            ScaleMin = 0.28f,
            ScaleMax = 0.56f,
            DampingMin = 0.04f,
            DampingMax = 0.14f,
            AngleMin = -8.0f,
            AngleMax = 8.0f
        };
        particles.ProcessMaterial = processMaterial;

        return particles;
    }
}
