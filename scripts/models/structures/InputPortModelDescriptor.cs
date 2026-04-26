using Godot;
using NetFactory.Models;

public static class InputPortModelDescriptor
{
    public static void BuildModel(IModelBuilder builder, FactorySiteKind siteKind)
    {
        var cs = builder.CellSize;

        BoundaryAttachmentModelDescriptor.BuildModel(builder, siteKind);

        var deckWidth = Mathf.Max(cs * 1.28f, cs * 1.28f * 0.90f);
        var deckDepth = Mathf.Max(cs * 1.86f, cs * 1.86f * 0.94f);
        var tipColor = new Color("F97316");

        builder.AddBox("InputReceiver", new Vector3(deckWidth * 0.24f, 0.16f, deckDepth * 0.42f), tipColor.Lightened(0.10f), new Vector3(-deckWidth * 0.24f, 0.34f, 0.0f));
    }
}
