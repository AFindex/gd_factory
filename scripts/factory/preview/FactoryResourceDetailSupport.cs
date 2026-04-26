using Godot;
using System.Collections.Generic;

public static class FactoryResourceDetailSupport
{
    public static bool TryGetDeposit(GridManager? grid, Vector2I cell, out FactoryResourceDepositDefinition? deposit)
    {
        deposit = null;
        return grid is not null && grid.TryGetResourceDeposit(cell, out deposit);
    }

    public static string GetSelectionTargetText(FactoryResourceDepositDefinition? deposit)
    {
        if (deposit is null)
        {
            return "未选中矿物";
        }

        var bounds = GetBounds(deposit);
        return $"{deposit.DisplayName} | {FactoryResourceCatalog.GetDisplayName(deposit.ResourceKind)} | 区域 ({bounds.Position.X}, {bounds.Position.Y}) - ({bounds.End.X - 1}, {bounds.End.Y - 1})";
    }

    public static void GetInspection(FactoryResourceDepositDefinition? deposit, out string? title, out string? body)
    {
        if (deposit is null)
        {
            title = null;
            body = null;
            return;
        }

        var outputKind = FactoryResourceCatalog.GetOutputItemKind(deposit.ResourceKind);
        var bounds = GetBounds(deposit);
        title = $"{deposit.DisplayName} 详情";
        body =
            $"矿种：{FactoryResourceCatalog.GetDisplayName(deposit.ResourceKind)}\n" +
            $"产出：{FactoryPresentation.GetItemKindLabel(outputKind)}\n" +
            $"覆盖：{deposit.Cells.Count} 格 | 区域 ({bounds.Position.X}, {bounds.Position.Y}) - ({bounds.End.X - 1}, {bounds.End.Y - 1})";
    }

    public static FactoryStructureDetailModel? BuildDetailModel(FactoryResourceDepositDefinition? deposit)
    {
        if (deposit is null)
        {
            return null;
        }

        var outputKind = FactoryResourceCatalog.GetOutputItemKind(deposit.ResourceKind);
        var bounds = GetBounds(deposit);
        var cells = new List<string>(deposit.Cells.Count);
        for (var index = 0; index < deposit.Cells.Count; index++)
        {
            var cell = deposit.Cells[index];
            cells.Add($"({cell.X}, {cell.Y})");
        }

        return new FactoryStructureDetailModel(
            deposit.DisplayName,
            $"矿物资源 | {FactoryResourceCatalog.GetDisplayName(deposit.ResourceKind)}",
            new[]
            {
                $"矿区 ID：{deposit.Id}",
                $"矿种：{FactoryResourceCatalog.GetDisplayName(deposit.ResourceKind)}",
                $"产出货物：{FactoryPresentation.GetItemKindLabel(outputKind)}",
                $"覆盖格数：{deposit.Cells.Count}",
                $"占地区域：({bounds.Position.X}, {bounds.Position.Y}) - ({bounds.End.X - 1}, {bounds.End.Y - 1})",
                $"可用开采器：采矿机 / 采矿输入端口",
                $"覆盖格列表：{string.Join(", ", cells)}"
            });
    }

    private static Rect2I GetBounds(FactoryResourceDepositDefinition deposit)
    {
        if (deposit.Cells.Count == 0)
        {
            return new Rect2I(Vector2I.Zero, Vector2I.One);
        }

        var minX = deposit.Cells[0].X;
        var minY = deposit.Cells[0].Y;
        var maxX = deposit.Cells[0].X;
        var maxY = deposit.Cells[0].Y;
        for (var index = 1; index < deposit.Cells.Count; index++)
        {
            var cell = deposit.Cells[index];
            minX = Mathf.Min(minX, cell.X);
            minY = Mathf.Min(minY, cell.Y);
            maxX = Mathf.Max(maxX, cell.X);
            maxY = Mathf.Max(maxY, cell.Y);
        }

        return new Rect2I(
            new Vector2I(minX, minY),
            new Vector2I(maxX - minX + 1, maxY - minY + 1));
    }
}
