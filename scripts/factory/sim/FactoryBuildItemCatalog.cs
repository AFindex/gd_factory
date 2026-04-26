using Godot;
using System.Collections.Generic;

public static partial class FactoryItemCatalog
{
    private static readonly IReadOnlyDictionary<BuildPrototypeKind, Texture2D> StructureItemIcons = CreateStructureItemIcons();

    public static Texture2D GetStructureItemIcon(BuildPrototypeKind kind)
    {
        return StructureItemIcons.TryGetValue(kind, out var texture)
            ? texture
            : GetIconTexture(FactoryItemKind.BuildingKit)!;
    }

    private static IReadOnlyDictionary<BuildPrototypeKind, Texture2D> CreateStructureItemIcons()
    {
        var result = new Dictionary<BuildPrototypeKind, Texture2D>();
        foreach (BuildPrototypeKind kind in System.Enum.GetValues(typeof(BuildPrototypeKind)))
        {
            result[kind] = CreateStructureIcon(kind);
        }

        return result;
    }

    private static Texture2D CreateStructureIcon(BuildPrototypeKind kind)
    {
        var image = Image.CreateEmpty(32, 32, false, Image.Format.Rgba8);
        image.Fill(new Color(0.0f, 0.0f, 0.0f, 0.0f));

        var accent = FactoryPresentation.GetBuildPrototypeAccentColor(kind);
        var shadow = accent.Darkened(0.35f);
        var highlight = accent.Lightened(0.35f);

        FillRect(image, new Rect2I(4, 9, 24, 15), accent);
        FillRect(image, new Rect2I(7, 12, 18, 9), highlight);
        FillRect(image, new Rect2I(10, 6, 12, 4), shadow);

        var notchWidth = 3 + (((int)kind % 4) * 3);
        FillRect(image, new Rect2I(6, 23, notchWidth, 3), shadow);
        FillRect(image, new Rect2I(23 - notchWidth, 23, notchWidth, 3), shadow);

        var stripeY = 14 + (((int)kind % 3) * 2);
        FillRect(image, new Rect2I(9, stripeY, 14, 2), shadow);

        return ImageTexture.CreateFromImage(image);
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
}
