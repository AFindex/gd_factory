using Godot;
using System;
using System.Collections.Generic;

public static class FactoryPreviewOverlaySupport
{
    public const float PowerDashBaseLength = FactoryConstants.CellSize * 0.52f;
    public const float PowerDashGapLength = FactoryConstants.CellSize * 0.28f;
    public const float PowerDashThickness = 0.08f;
    public const float PowerDashWidth = 0.11f;
    public const float PowerLinkEndpointInset = FactoryConstants.CellSize * 0.22f;
    public const float PreviewPowerPoleWireHeight = 1.44f;
    public const int PreviewPowerPoleConnectionRangeCells = 6;

    public static MeshInstance3D CreatePreviewCell(string name, float cellSize, float scale = 0.92f, float height = 0.08f, float y = 0.05f)
    {
        return new MeshInstance3D
        {
            Name = name,
            Mesh = new BoxMesh { Size = new Vector3(cellSize * scale, height, cellSize * scale) },
            Position = new Vector3(0.0f, y, 0.0f)
        };
    }

    public static Node3D CreateFacingArrow(string name, float bodyLength, float headRadius)
    {
        return FactoryPreviewVisuals.CreateFacingArrow(name, bodyLength, headRadius);
    }

    public static MeshInstance3D CreatePreviewPowerRange(string name)
    {
        return new MeshInstance3D
        {
            Name = name,
            Visible = false,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
    }

    public static MeshInstance3D CreatePortHintMesh(string name)
    {
        return new MeshInstance3D
        {
            Name = name,
            Visible = false,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
    }

    public static MeshInstance3D CreatePowerLinkDash(string name)
    {
        return new MeshInstance3D
        {
            Name = name,
            Visible = false,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Mesh = new BoxMesh
            {
                Size = new Vector3(PowerDashWidth, PowerDashThickness, PowerDashBaseLength)
            },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.99f, 0.93f, 0.62f, 0.92f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Roughness = 0.15f,
                EmissionEnabled = true,
                Emission = new Color(0.99f, 0.93f, 0.62f)
            }
        };
    }

    public static void EnsurePowerLinkDashCapacity(Node3D? root, List<MeshInstance3D> dashes, int count, string namePrefix)
    {
        if (root is null)
        {
            return;
        }

        while (dashes.Count < count)
        {
            var dash = CreatePowerLinkDash($"{namePrefix}_{dashes.Count}");
            root.AddChild(dash);
            dashes.Add(dash);
        }
    }

    public static int DrawDashedPowerLink(
        Vector3 start,
        Vector3 end,
        Color color,
        int dashIndex,
        Action<int> ensureCapacity,
        IReadOnlyList<MeshInstance3D> dashes,
        Action<MeshInstance3D, Color> applyColor)
    {
        var startFlat = new Vector3(start.X, 0.0f, start.Z);
        var endFlat = new Vector3(end.X, 0.0f, end.Z);
        var delta = endFlat - startFlat;
        var totalLength = delta.Length();
        if (totalLength <= PowerLinkEndpointInset * 2.0f)
        {
            return dashIndex;
        }

        var direction = delta / totalLength;
        var linkHeight = Mathf.Max(start.Y, end.Y);
        var dashStart = new Vector3(start.X, linkHeight, start.Z) + (direction * PowerLinkEndpointInset);
        var dashEnd = new Vector3(end.X, linkHeight, end.Z) - (direction * PowerLinkEndpointInset);
        var dashVector = dashEnd - dashStart;
        var dashDistance = dashVector.Length();
        if (dashDistance <= 0.05f)
        {
            return dashIndex;
        }

        var rotationY = Mathf.Atan2(direction.X, direction.Z);
        var step = PowerDashBaseLength + PowerDashGapLength;
        var progress = 0.0f;
        while (progress < dashDistance)
        {
            var dashLength = Mathf.Min(PowerDashBaseLength, dashDistance - progress);
            if (dashLength <= 0.02f)
            {
                break;
            }

            ensureCapacity(dashIndex + 1);
            var dash = dashes[dashIndex];
            dash.Visible = true;
            dash.Position = dashStart + (direction * (progress + (dashLength * 0.5f)));
            dash.Rotation = new Vector3(0.0f, rotationY, 0.0f);
            dash.Scale = new Vector3(1.0f, 1.0f, dashLength / PowerDashBaseLength);
            applyColor(dash, color);
            dashIndex++;
            progress += step;
        }

        return dashIndex;
    }

    public static void ApplyPreviewColor(MeshInstance3D meshInstance, Color color)
    {
        meshInstance.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = color,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            Roughness = 0.4f
        };
    }

    public static void ApplyPreviewColor(Node3D arrowRoot, Color color)
    {
        FactoryPreviewVisuals.ApplyArrowColor(arrowRoot, color);
    }

    public static void ApplyMiningPreviewColor(MeshInstance3D meshInstance, Color color)
    {
        meshInstance.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = color,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            Roughness = 0.28f,
            NoDepthTest = true,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            EmissionEnabled = true,
            Emission = new Color(color.R, color.G, color.B, 1.0f)
        };
    }
}
