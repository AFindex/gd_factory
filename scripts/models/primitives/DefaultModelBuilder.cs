using Godot;

namespace NetFactory.Models;

public sealed class DefaultModelBuilder : IModelBuilder
{
    public float CellSize { get; }
    public Node3D Root { get; }

    public DefaultModelBuilder(Node3D root, float cellSize)
    {
        Root = root;
        CellSize = cellSize;
    }

    public MeshInstance3D AddBox(string name, Vector3 size, Color color, Vector3? position = null)
    {
        return AddBox(Root, name, size, color, position);
    }

    public MeshInstance3D AddBox(Node parent, string name, Vector3 size, Color color, Vector3? position = null)
    {
        var mesh = new MeshInstance3D
        {
            Name = name,
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = color,
                Roughness = 0.85f
            }
        };

        if (position is not null)
        {
            mesh.Position = position.Value;
        }

        parent.AddChild(mesh);
        return mesh;
    }

    public MeshInstance3D AddArmBox(Node parent, string name, Vector3 size, Color color, Vector3 position)
    {
        var mesh = new MeshInstance3D
        {
            Name = name,
            Mesh = new BoxMesh { Size = size },
            Position = position,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = color,
                Roughness = 0.82f
            }
        };
        parent.AddChild(mesh);
        return mesh;
    }

    public MeshInstance3D AddCombatBox(string name, Vector3 size, Color color, Vector3 position)
    {
        return AddArmBox(Root, name, size, color, position);
    }

    public MeshInstance3D AddDisc(string name, float radius, float height, Color color, Vector3? position = null)
    {
        return AddDisc(Root, name, radius, height, color, position);
    }

    public MeshInstance3D AddDisc(Node parent, string name, float radius, float height, Color color, Vector3? position = null)
    {
        var mesh = new MeshInstance3D
        {
            Name = name,
            Mesh = new CylinderMesh
            {
                TopRadius = radius,
                BottomRadius = radius,
                Height = height
            },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = color,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Roughness = 1.0f,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };

        if (position is not null)
        {
            mesh.Position = position.Value;
        }

        parent.AddChild(mesh);
        return mesh;
    }

    public MeshInstance3D AddCylinder(string name, float radius, float length, Color color, Vector3 position)
    {
        return AddCylinder(Root, name, radius, length, color, position);
    }

    public MeshInstance3D AddCylinder(Node parent, string name, float radius, float length, Color color, Vector3 position)
    {
        var mesh = new MeshInstance3D
        {
            Name = name,
            Mesh = new CylinderMesh
            {
                TopRadius = radius,
                BottomRadius = radius,
                Height = length
            },
            Position = position,
            Rotation = new Vector3(Mathf.Pi * 0.5f, 0.0f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = color,
                Roughness = 0.72f
            }
        };
        parent.AddChild(mesh);
        return mesh;
    }

    public void ConfigureGlowMaterial(MeshInstance3D mesh, Color emissionColor, float emissionEnergy)
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

    public void AddInteriorModuleShell(Node parent, string prefix, Vector3 shellSize, Color shellColor, Color trimColor, Vector3 position)
    {
        AddBox(parent, $"{prefix}Shell", shellSize, shellColor, position);
        AddBox(parent, $"{prefix}TopPlate", new Vector3(shellSize.X * 0.88f, Mathf.Max(CellSize * 0.06f, shellSize.Y * 0.10f), shellSize.Z * 0.88f), trimColor, position + new Vector3(0.0f, shellSize.Y * 0.42f, 0.0f));
        AddBox(parent, $"{prefix}SideSpine", new Vector3(Mathf.Max(CellSize * 0.08f, shellSize.X * 0.10f), shellSize.Y * 0.86f, shellSize.Z * 0.64f), trimColor.Darkened(0.05f), position + new Vector3((-shellSize.X * 0.36f), shellSize.Y * 0.02f, 0.0f));
    }

    public void AddInteriorTray(Node parent, string prefix, Vector3 traySize, Color trayColor, Color railColor, Vector3 position)
    {
        AddBox(parent, $"{prefix}Tray", traySize, trayColor, position);
        AddBox(parent, $"{prefix}RailNorth", new Vector3(traySize.X * 0.94f, traySize.Y * 0.68f, Mathf.Max(CellSize * 0.03f, traySize.Z * 0.10f)), railColor, position + new Vector3(0.0f, traySize.Y * 0.18f, -traySize.Z * 0.34f));
        AddBox(parent, $"{prefix}RailSouth", new Vector3(traySize.X * 0.94f, traySize.Y * 0.68f, Mathf.Max(CellSize * 0.03f, traySize.Z * 0.10f)), railColor, position + new Vector3(0.0f, traySize.Y * 0.18f, traySize.Z * 0.34f));
    }

    public void AddInteriorIndicatorLight(Node parent, string name, Color color, Vector3 position, float size)
    {
        var lamp = AddBox(parent, name, new Vector3(size, size, size), color, position);
        if (lamp.MaterialOverride is StandardMaterial3D material)
        {
            material.Roughness = 0.18f;
            material.EmissionEnabled = true;
            material.Emission = color;
            material.EmissionEnergyMultiplier = 1.45f;
        }
    }

    public void AddIndicatorLight(string name, Color color, Vector3 position, float size)
    {
        AddInteriorIndicatorLight(Root, name, color, position, size);
    }

    public void AddInteriorLabelPlate(Node parent, string prefix, string label, Color color, Vector3 position, float widthScale = 1.0f)
    {
        var plateWidth = Mathf.Max(CellSize * 0.24f, CellSize * 0.28f * widthScale);
        AddBox(parent, $"{prefix}Plate", new Vector3(plateWidth, 0.04f, CellSize * 0.12f), color.Darkened(0.38f), position);
        AddBox(parent, $"{prefix}Stripe", new Vector3(plateWidth * 0.82f, 0.02f, CellSize * 0.05f), color, position + new Vector3(0.0f, 0.018f, 0.0f));
        if (!string.IsNullOrWhiteSpace(label))
        {
            AddBox(parent, $"{prefix}_{label}", new Vector3(Mathf.Max(CellSize * 0.06f, plateWidth * 0.18f), 0.03f, CellSize * 0.03f), color.Lightened(0.18f), position + new Vector3(0.0f, 0.030f, 0.0f));
        }
    }

    public void AddLabelPlate(string name, string label, Color color, Vector3 position, float cellSize, float widthScale)
    {
        AddInteriorLabelPlate(Root, name, label, color, position, widthScale);
    }

    public Node3D AddPivotNode(string name, Vector3 position)
    {
        return AddPivotNode(Root, name, position);
    }

    public Node3D AddPivotNode(Node parent, string name, Vector3 position)
    {
        var pivot = new Node3D
        {
            Name = name,
            Position = position
        };
        parent.AddChild(pivot);
        return pivot;
    }
}
