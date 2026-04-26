using Godot;
using System.Collections.Generic;

namespace NetFactory.Models;

internal static class GeneratedItemTextureLibrary
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
