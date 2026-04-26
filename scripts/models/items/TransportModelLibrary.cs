using Godot;

namespace NetFactory.Models;

internal static class TransportModelLibrary
{
    public static Node3D CreateInteriorCanisterModel(float cellSize, Color tint)
    {
        var root = new Node3D();
        root.AddChild(CreateMesh("CanisterBody", new CylinderMesh
        {
            TopRadius = cellSize * 0.08f,
            BottomRadius = cellSize * 0.08f,
            Height = cellSize * 0.22f
        }, tint.Darkened(0.08f), new Vector3(0.0f, 0.0f, 0.0f)));
        root.AddChild(CreateMesh("CanisterCap", new CylinderMesh
        {
            TopRadius = cellSize * 0.07f,
            BottomRadius = cellSize * 0.07f,
            Height = cellSize * 0.04f
        }, new Color("E2E8F0"), new Vector3(0.0f, cellSize * 0.12f, 0.0f)));
        root.AddChild(CreateMesh("CanisterBand", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.14f, cellSize * 0.04f, cellSize * 0.04f)
        }, tint.Lightened(0.28f), new Vector3(0.0f, 0.0f, cellSize * 0.08f)));
        return root;
    }

    public static Node3D CreateInteriorTrayModel(float cellSize, Color tint)
    {
        var root = new Node3D();
        root.AddChild(CreateMesh("TrayBase", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.22f, cellSize * 0.06f, cellSize * 0.16f)
        }, new Color("334155"), Vector3.Zero));
        root.AddChild(CreateMesh("TrayCargo", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.18f, cellSize * 0.04f, cellSize * 0.12f)
        }, tint, new Vector3(0.0f, cellSize * 0.04f, 0.0f)));
        root.AddChild(CreateMesh("TrayStripe", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.16f, cellSize * 0.02f, cellSize * 0.03f)
        }, tint.Lightened(0.24f), new Vector3(0.0f, cellSize * 0.08f, 0.0f)));
        return root;
    }

    public static Node3D CreateInteriorElectronicsCassetteModel(float cellSize, Color tint)
    {
        var root = new Node3D();
        root.AddChild(CreateMesh("CassetteBody", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.22f, cellSize * 0.12f, cellSize * 0.14f)
        }, new Color("1E293B"), Vector3.Zero));
        root.AddChild(CreateMesh("CassetteFace", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.16f, cellSize * 0.08f, cellSize * 0.02f)
        }, tint, new Vector3(0.0f, 0.0f, cellSize * 0.08f)));
        root.AddChild(CreateMesh("CassettePort", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.06f, cellSize * 0.03f, cellSize * 0.02f)
        }, new Color("E2E8F0"), new Vector3(0.0f, cellSize * 0.03f, cellSize * 0.09f)));
        return root;
    }

    public static Node3D CreateInteriorAmmoCassetteModel(float cellSize, Color tint)
    {
        var root = new Node3D();
        root.AddChild(CreateMesh("AmmoBody", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.18f, cellSize * 0.14f, cellSize * 0.12f)
        }, tint.Darkened(0.06f), Vector3.Zero));
        root.AddChild(CreateMesh("AmmoRack", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.14f, cellSize * 0.04f, cellSize * 0.10f)
        }, new Color("1F2937"), new Vector3(0.0f, cellSize * 0.05f, 0.0f)));
        root.AddChild(CreateMesh("AmmoStripe", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.12f, cellSize * 0.02f, cellSize * 0.02f)
        }, tint.Lightened(0.18f), new Vector3(0.0f, cellSize * 0.08f, cellSize * 0.06f)));
        return root;
    }

    public static Node3D CreateInteriorCrystalCaseModel(float cellSize, Color tint)
    {
        var root = new Node3D();
        root.AddChild(CreateMesh("CaseBody", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.18f, cellSize * 0.16f, cellSize * 0.18f)
        }, new Color("334155"), Vector3.Zero));
        root.AddChild(CreateMesh("CrystalCore", new PrismMesh
        {
            Size = new Vector3(cellSize * 0.08f, cellSize * 0.14f, cellSize * 0.08f)
        }, tint, new Vector3(0.0f, cellSize * 0.02f, 0.0f)));
        return root;
    }

    public static Node3D CreateInteriorUtilityCassetteModel(float cellSize, Color tint)
    {
        var root = new Node3D();
        root.AddChild(CreateMesh("UtilityBody", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.20f, cellSize * 0.14f, cellSize * 0.16f)
        }, new Color("334155"), Vector3.Zero));
        root.AddChild(CreateMesh("UtilityPanel", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.14f, cellSize * 0.08f, cellSize * 0.02f)
        }, tint, new Vector3(0.0f, 0.0f, cellSize * 0.09f)));
        root.AddChild(CreateMesh("UtilityLatch", new BoxMesh
        {
            Size = new Vector3(cellSize * 0.06f, cellSize * 0.04f, cellSize * 0.02f)
        }, new Color("E2E8F0"), new Vector3(0.0f, cellSize * 0.03f, cellSize * 0.10f)));
        return root;
    }

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
