using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public partial class FactoryPlayerController : CharacterBody3D, IFactoryInventoryEndpointProvider
{
    public const string BackpackInventoryId = "player-backpack";
    public const int HotbarSlotCount = 10;
    private const float MoveSpeed = 6.8f;
    private const float BodyRadius = 0.28f;
    private const float BodyHeight = 1.18f;

    private readonly FactorySlottedItemInventory _inventory = new(10, 4);
    private int _activeHotbarIndex;
    private bool _hotbarPlacementArmed = true;

    public int ActiveHotbarIndex => _activeHotbarIndex;
    public bool IsHotbarPlacementArmed => _hotbarPlacementArmed;
    public FactorySlottedItemInventory BackpackInventory => _inventory;

    public override void _Ready()
    {
        Name = "FactoryPlayer";

        var collision = new CollisionShape3D
        {
            Name = "Collision",
            Shape = new CapsuleShape3D
            {
                Radius = BodyRadius,
                Height = BodyHeight
            }
        };
        collision.Position = new Vector3(0.0f, BodyHeight * 0.5f, 0.0f);
        AddChild(collision);

        var body = new MeshInstance3D
        {
            Name = "CapsuleBody",
            Mesh = new CapsuleMesh
            {
                Radius = BodyRadius,
                Height = BodyHeight
            },
            Position = new Vector3(0.0f, BodyHeight * 0.5f, 0.0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color("FDE68A"),
                Roughness = 0.62f
            }
        };
        AddChild(body);

        var marker = new MeshInstance3D
        {
            Name = "FacingMarker",
            Mesh = new BoxMesh { Size = new Vector3(0.18f, 0.10f, 0.36f) },
            Position = new Vector3(0.0f, BodyHeight + 0.24f, -0.20f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color("FB923C"),
                Roughness = 0.35f
            }
        };
        AddChild(marker);
    }

    public void EnsureStarterLoadout(SimulationController? simulation)
    {
        if (simulation is null || !_inventory.IsEmpty)
        {
            RefreshActiveSlotState();
            return;
        }

        AddStructureKitStack(simulation, BuildPrototypeKind.Belt, 24);
        AddStructureKitStack(simulation, BuildPrototypeKind.Sink, 8);
        AddStructureKitStack(simulation, BuildPrototypeKind.Splitter, 8);
        AddStructureKitStack(simulation, BuildPrototypeKind.Merger, 8);
        AddStructureKitStack(simulation, BuildPrototypeKind.Bridge, 6);
        AddStructureKitStack(simulation, BuildPrototypeKind.Loader, 6);
        AddStructureKitStack(simulation, BuildPrototypeKind.Unloader, 6);
        AddStructureKitStack(simulation, BuildPrototypeKind.Storage, 12);
        AddStructureKitStack(simulation, BuildPrototypeKind.Inserter, 12);
        AddStructureKitStack(simulation, BuildPrototypeKind.Generator, 6);
        AddStructureKitStack(simulation, BuildPrototypeKind.PowerPole, 12);
        AddStructureKitStack(simulation, BuildPrototypeKind.MiningDrill, 8);
        AddStructureKitStack(simulation, BuildPrototypeKind.Smelter, 8);
        AddStructureKitStack(simulation, BuildPrototypeKind.Assembler, 8);
        AddStructureKitStack(simulation, BuildPrototypeKind.Wall, 20);
        AddStructureKitStack(simulation, BuildPrototypeKind.AmmoAssembler, 6);
        AddStructureKitStack(simulation, BuildPrototypeKind.GunTurret, 6);
        AddStructureKitStack(simulation, BuildPrototypeKind.HeavyGunTurret, 4);
        AddStructureKitStack(simulation, BuildPrototypeKind.LargeStorageDepot, 4);

        RefreshActiveSlotState();
    }

    public void ApplyMovement(Rect2 movementBounds, double delta, bool allowInput)
    {
        var input = Vector2.Zero;
        if (allowInput)
        {
            if (Input.IsActionPressed("player_move_left"))
            {
                input.X -= 1.0f;
            }

            if (Input.IsActionPressed("player_move_right"))
            {
                input.X += 1.0f;
            }

            if (Input.IsActionPressed("player_move_forward"))
            {
                input.Y -= 1.0f;
            }

            if (Input.IsActionPressed("player_move_backward"))
            {
                input.Y += 1.0f;
            }
        }

        input = input == Vector2.Zero ? input : input.Normalized();
        Velocity = new Vector3(input.X * MoveSpeed, 0.0f, input.Y * MoveSpeed);
        MoveAndSlide();

        var clamped = GlobalPosition;
        clamped.X = Mathf.Clamp(clamped.X, movementBounds.Position.X, movementBounds.End.X);
        clamped.Z = Mathf.Clamp(clamped.Z, movementBounds.Position.Y, movementBounds.End.Y);
        clamped.Y = 0.0f;
        GlobalPosition = clamped;

        if (input != Vector2.Zero)
        {
            Rotation = new Vector3(0.0f, Mathf.Atan2(input.X, input.Y), 0.0f);
        }
    }

    public bool TryResolveInventoryEndpoint(string inventoryId, out FactoryInventoryTransferEndpoint endpoint)
    {
        if (inventoryId == BackpackInventoryId)
        {
            endpoint = new FactoryInventoryTransferEndpoint(inventoryId, _inventory);
            return true;
        }

        endpoint = default;
        return false;
    }

    public bool SelectHotbarIndex(int index)
    {
        if (index < 0 || index >= HotbarSlotCount)
        {
            return false;
        }

        _activeHotbarIndex = index;
        _hotbarPlacementArmed = GetActiveHotbarItem() is FactoryItem activeItem
            && FactoryPresentation.IsPlaceableStructureItem(activeItem);
        RefreshActiveSlotState();
        return true;
    }

    public bool ToggleHotbarIndex(int index)
    {
        if (index < 0 || index >= HotbarSlotCount)
        {
            return false;
        }

        if (_activeHotbarIndex == index)
        {
            if (_hotbarPlacementArmed)
            {
                DisarmHotbarPlacement();
            }
            else
            {
                RearmHotbarPlacement();
            }

            return true;
        }

        return SelectHotbarIndex(index);
    }

    public void DisarmHotbarPlacement()
    {
        _hotbarPlacementArmed = false;
    }

    public void RearmHotbarPlacement()
    {
        _hotbarPlacementArmed = true;
        RefreshActiveSlotState();
    }

    public FactoryItem? GetActiveHotbarItem()
    {
        var slot = new Vector2I(_activeHotbarIndex, 0);
        return _inventory.GetItemOrDefault(slot);
    }

    public FactoryItem? GetHotbarItem(int index)
    {
        if (index < 0 || index >= HotbarSlotCount)
        {
            return null;
        }

        return _inventory.GetItemOrDefault(new Vector2I(index, 0));
    }

    public BuildPrototypeKind? GetArmedPlaceablePrototype()
    {
        return _hotbarPlacementArmed && FactoryPresentation.TryGetPlaceableStructureKind(GetActiveHotbarItem(), out var kind)
            ? kind
            : null;
    }

    public bool TryConsumeActivePlaceable(out BuildPrototypeKind kind)
    {
        kind = default;
        var slot = new Vector2I(_activeHotbarIndex, 0);
        if (!_hotbarPlacementArmed
            || !_inventory.TryPeekSlot(slot, out var item)
            || item is null
            || !FactoryPresentation.TryGetPlaceableStructureKind(item, out kind)
            || !_inventory.TryTakeFromSlot(slot, out _))
        {
            return false;
        }

        RefreshActiveSlotState();
        return true;
    }

    public void RefreshActiveSlotState()
    {
        if (GetActiveHotbarItem() is not FactoryItem activeItem || !FactoryPresentation.IsPlaceableStructureItem(activeItem))
        {
            _hotbarPlacementArmed = false;
        }
    }

    public FactoryStructureDetailModel BuildBackpackDetailModel(FactoryStructureDetailModel? linkedStructureModel = null)
    {
        var summaryLines = new List<string>
        {
            $"快捷栏：{GetHotbarSlotLabel(_activeHotbarIndex)} 号槽位{(GetArmedPlaceablePrototype().HasValue ? $"，已就绪 {FactoryPresentation.GetBuildPrototypeDisplayName(GetArmedPlaceablePrototype()!.Value)}" : "，当前未就绪建筑放置")}",
            $"背包物品总数：{_inventory.Count} | 占用槽位：{_inventory.OccupiedSlotCount}/{_inventory.Capacity}"
        };

        var sections = new List<FactoryInventorySectionModel>
        {
            BuildBackpackSection()
        };

        return new FactoryStructureDetailModel(
            "玩家背包",
            "主角基础携行栏位",
            summaryLines,
            sections);
    }

    public string BuildBackpackUiSignature(FactoryStructureDetailModel? linkedStructureModel = null)
    {
        var builder = new StringBuilder();
        builder.Append(_activeHotbarIndex)
            .Append('|')
            .Append(_hotbarPlacementArmed)
            .Append('|')
            .Append(_inventory.Count)
            .Append('|')
            .Append(_inventory.OccupiedSlotCount)
            .Append('|');

        var snapshot = _inventory.Snapshot();
        for (var index = 0; index < snapshot.Length; index++)
        {
            var slot = snapshot[index];
            builder.Append(slot.Position.X)
                .Append(',')
                .Append(slot.Position.Y)
                .Append('=')
                .Append(slot.Item?.ItemKind.ToString() ?? "empty")
                .Append(':')
                .Append(slot.Item?.SourceKind.ToString() ?? "none")
                .Append(':')
                .Append(slot.StackCount)
                .Append(';');
        }

        return builder.ToString();
    }

    public FactoryStructureDetailModel BuildItemInfoDetailModel()
    {
        return BuildItemInfoDetailModel(GetActiveHotbarItem());
    }

    public FactoryStructureDetailModel BuildItemInfoDetailModel(FactoryItem? item)
    {
        if (item is null)
        {
            return new FactoryStructureDetailModel(
                "物品信息",
                "当前未选择物品",
                new[]
                {
                    "快捷栏当前槽位为空，或未持有可展示的物品。",
                    "点击底部物品栏或按数字键切换快捷栏槽位。"
                });
        }

        var lines = new List<string>
        {
            $"名称：{FactoryPresentation.GetItemDisplayName(item)}",
            $"来源：{FactoryPresentation.GetBuildPrototypeDisplayName(item.SourceKind)}",
            FactoryPresentation.IsPlaceableStructureItem(item)
                ? "说明：可直接放置。"
                : "说明：可在容器间转移。"
        };

        return new FactoryStructureDetailModel(
            "物品信息",
            "当前选中物品",
            lines,
            new[]
            {
                new FactoryInventorySectionModel(
                    "player-item-info",
                    "选中物品",
                    new Vector2I(1, 1),
                    new[]
                    {
                        new FactoryInventorySlotModel(
                            Vector2I.Zero,
                            item.ItemKind,
                            item.Id.ToString(),
                            FactoryPresentation.GetItemDisplayName(item),
                            string.Join(" | ", lines),
                            FactoryPresentation.GetItemAccentColor(item),
                            1,
                            FactoryItemCatalog.GetMaxStackSize(item.ItemKind),
                            FactoryPresentation.GetItemIcon(item))
                    },
                    false)
            });
    }

    public string BuildItemInfoSignature(FactoryItem? item)
    {
        return item is null
            ? "none"
            : $"{item.Id}|{item.ItemKind}|{item.SourceKind}";
    }

    public FactoryStructureDetailModel BuildStatsDetailModel()
    {
        return new FactoryStructureDetailModel(
            "个人属性",
            "主角基础状态",
            new[]
            {
                "状态：可移动胶囊占位主角",
                $"位置：X {GlobalPosition.X:0.0} | Z {GlobalPosition.Z:0.0}",
                $"背包占用：{_inventory.OccupiedSlotCount}/{_inventory.Capacity}",
                $"当前快捷栏：{GetHotbarSlotLabel(_activeHotbarIndex)} 号槽位"
            });
    }

    public string BuildStatsSignature()
    {
        return $"{Mathf.RoundToInt(GlobalPosition.X * 10.0f)}|{Mathf.RoundToInt(GlobalPosition.Z * 10.0f)}|{_inventory.OccupiedSlotCount}|{_activeHotbarIndex}|{_hotbarPlacementArmed}";
    }

    public FactoryPlayerRuntimeSnapshot CaptureRuntimeSnapshot()
    {
        return new FactoryPlayerRuntimeSnapshot
        {
            Position = FactoryRuntimeVec3.FromVector3(GlobalPosition),
            ActiveHotbarIndex = _activeHotbarIndex,
            IsHotbarPlacementArmed = _hotbarPlacementArmed,
            SelectedInventoryId = BackpackInventoryId,
            SelectedSlot = FactoryRuntimeInt2.FromVector2I(new Vector2I(_activeHotbarIndex, 0)),
            Inventory = FactoryRuntimeSnapshotValues.CaptureInventory(BackpackInventoryId, _inventory)
        };
    }

    public void ApplyRuntimeSnapshot(FactoryPlayerRuntimeSnapshot snapshot, SimulationController simulation)
    {
        if (!FactoryRuntimeSnapshotValues.TryRestoreInventory(_inventory, snapshot.Inventory, simulation))
        {
            throw new InvalidOperationException("Player backpack snapshot could not be restored.");
        }

        GlobalPosition = snapshot.Position.ToVector3();
        SelectHotbarIndex(Mathf.Clamp(snapshot.ActiveHotbarIndex, 0, HotbarSlotCount - 1));
        if (snapshot.IsHotbarPlacementArmed)
        {
            RearmHotbarPlacement();
        }
        else
        {
            DisarmHotbarPlacement();
        }

        RefreshActiveSlotState();
    }

    private FactoryInventorySectionModel BuildBackpackSection()
    {
        var slots = new List<FactoryInventorySlotModel>();
        var snapshot = _inventory.Snapshot();
        for (var index = 0; index < snapshot.Length; index++)
        {
            var state = snapshot[index];
            var item = state.Item;
            slots.Add(new FactoryInventorySlotModel(
                state.Position,
                item?.ItemKind,
                item is null ? null : item.Id.ToString(),
                item is null ? null : FactoryPresentation.GetItemDisplayName(item),
                item is null
                    ? "空槽位"
                    : $"{FactoryPresentation.GetItemDisplayName(item)} {state.StackCount}/{state.MaxStackSize} | 槽位 ({state.Position.X}, {state.Position.Y})",
                item is null ? new Color("475569") : FactoryPresentation.GetItemAccentColor(item),
                state.StackCount,
                state.MaxStackSize,
                item is null ? null : FactoryPresentation.GetItemIcon(item)));
        }

        return new FactoryInventorySectionModel(BackpackInventoryId, "玩家背包", _inventory.GridSize, slots, true);
    }

    private void AddStructureKitStack(SimulationController simulation, BuildPrototypeKind kind, int count)
    {
        for (var index = 0; index < count; index++)
        {
            _inventory.TryAddItem(simulation.CreateItem(kind, FactoryItemKind.BuildingKit));
        }
    }

    public static string GetHotbarSlotLabel(int index)
    {
        return index == HotbarSlotCount - 1
            ? "0"
            : $"{index + 1}";
    }
}
