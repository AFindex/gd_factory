using Godot;
using System.Collections.Generic;

public static class MobileFactoryScenarioLibrary
{
    private static Vector2I[] CreateRectOffsets(int width, int height)
    {
        var offsets = new Vector2I[width * height];
        var index = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                offsets[index++] = new Vector2I(x, y);
            }
        }

        return offsets;
    }

    private static Vector2I[] CreateFootprintOffsets(int interiorWidth, int interiorHeight, float interiorCellSize, float interiorPlatformBorder)
    {
        var hullWidth = interiorWidth * interiorCellSize + interiorPlatformBorder;
        var hullHeight = interiorHeight * interiorCellSize + interiorPlatformBorder;
        var footprintWidth = Mathf.Max(2, Mathf.CeilToInt(hullWidth / FactoryConstants.CellSize));
        var footprintHeight = Mathf.Max(2, Mathf.CeilToInt(hullHeight / FactoryConstants.CellSize));
        return CreateRectOffsets(footprintWidth, footprintHeight);
    }

    private static MobileFactoryAttachmentMount CreateAttachmentMount(
        string id,
        Vector2I cell,
        FacingDirection facing,
        Vector2I worldPortOffsetEast,
        params BuildPrototypeKind[] allowedKinds)
    {
        return new MobileFactoryAttachmentMount(id, cell, facing, worldPortOffsetEast, allowedKinds);
    }

    public static MobileFactoryProfile CreateFocusedDemoProfile()
    {
        return new MobileFactoryProfile(
            id: "focused-standard",
            displayName: "标准移动工厂",
            interiorMinCell: new Vector2I(0, 0),
            interiorMaxCell: new Vector2I(7, 7),
            interiorCellSize: 0.72f,
            interiorFloorHeight: 0.36f,
            interiorPlatformBorder: 0.18f,
            footprintOffsetsEast: CreateFootprintOffsets(8, 8, 0.72f, 0.18f),
            portOffsetsEast: new[]
            {
                new Vector2I(2, 0)
            },
            outputBridgeCell: new Vector2I(7, 2),
            outputBridgeFacing: FacingDirection.East,
            transitParkingCenter: new Vector3(-11.0f, 0.0f, 7.0f),
            hullColor: new Color("1F2937"),
            cabColor: new Color("475569"),
            accentColor: new Color("F59E0B"),
            portColor: new Color("FB923C"),
            attachmentMounts: new[]
            {
                CreateAttachmentMount("west-input-main", new Vector2I(0, 3), FacingDirection.West, new Vector2I(-1, 1), BuildPrototypeKind.InputPort, BuildPrototypeKind.MiningInputPort),
                CreateAttachmentMount("east-output-main", new Vector2I(7, 1), FacingDirection.East, new Vector2I(3, 0), BuildPrototypeKind.OutputPort),
                CreateAttachmentMount("east-output-aux", new Vector2I(7, 4), FacingDirection.East, new Vector2I(3, 1), BuildPrototypeKind.OutputPort)
            });
    }

    public static MobileFactoryProfile CreateCompactProfile()
    {
        return new MobileFactoryProfile(
            id: "compact-scout",
            displayName: "紧凑型移动工厂",
            interiorMinCell: new Vector2I(0, 0),
            interiorMaxCell: new Vector2I(4, 4),
            interiorCellSize: 0.68f,
            interiorFloorHeight: 0.34f,
            interiorPlatformBorder: 0.16f,
            footprintOffsetsEast: CreateFootprintOffsets(5, 5, 0.68f, 0.16f),
            portOffsetsEast: new[]
            {
                new Vector2I(2, 0)
            },
            outputBridgeCell: new Vector2I(4, 2),
            outputBridgeFacing: FacingDirection.East,
            transitParkingCenter: new Vector3(-12.0f, 0.0f, 9.0f),
            hullColor: new Color("1E293B"),
            cabColor: new Color("0F766E"),
            accentColor: new Color("14B8A6"),
            portColor: new Color("5EEAD4"),
            attachmentMounts: new[]
            {
                CreateAttachmentMount("west-input-main", new Vector2I(0, 3), FacingDirection.West, new Vector2I(-1, 1), BuildPrototypeKind.InputPort, BuildPrototypeKind.MiningInputPort),
                CreateAttachmentMount("east-output-main", new Vector2I(4, 2), FacingDirection.East, new Vector2I(2, 0), BuildPrototypeKind.OutputPort),
                CreateAttachmentMount("east-output-aux", new Vector2I(4, 3), FacingDirection.East, new Vector2I(2, 1), BuildPrototypeKind.OutputPort)
            });
    }

    public static MobileFactoryProfile CreateMediumProfile()
    {
        return new MobileFactoryProfile(
            id: "medium-expedition",
            displayName: "远征型移动工厂",
            interiorMinCell: new Vector2I(0, 0),
            interiorMaxCell: new Vector2I(5, 4),
            interiorCellSize: 0.74f,
            interiorFloorHeight: 0.37f,
            interiorPlatformBorder: 0.18f,
            footprintOffsetsEast: CreateFootprintOffsets(6, 5, 0.74f, 0.18f),
            portOffsetsEast: new[]
            {
                new Vector2I(3, 0)
            },
            outputBridgeCell: new Vector2I(5, 2),
            outputBridgeFacing: FacingDirection.East,
            transitParkingCenter: new Vector3(-14.0f, 0.0f, 10.0f),
            hullColor: new Color("1F2937"),
            cabColor: new Color("1D4ED8"),
            accentColor: new Color("60A5FA"),
            portColor: new Color("BFDBFE"),
            attachmentMounts: new[]
            {
                CreateAttachmentMount("west-input-main", new Vector2I(0, 3), FacingDirection.West, new Vector2I(-1, 1), BuildPrototypeKind.InputPort, BuildPrototypeKind.MiningInputPort),
                CreateAttachmentMount("east-output-main", new Vector2I(5, 2), FacingDirection.East, new Vector2I(3, 0), BuildPrototypeKind.OutputPort),
                CreateAttachmentMount("east-output-aux", new Vector2I(5, 3), FacingDirection.East, new Vector2I(3, 1), BuildPrototypeKind.OutputPort)
            });
    }

    public static MobileFactoryProfile CreateHeavyProfile()
    {
        return new MobileFactoryProfile(
            id: "heavy-outpost",
            displayName: "重载移动工厂",
            interiorMinCell: new Vector2I(0, 0),
            interiorMaxCell: new Vector2I(6, 5),
            interiorCellSize: 0.76f,
            interiorFloorHeight: 0.39f,
            interiorPlatformBorder: 0.20f,
            footprintOffsetsEast: CreateFootprintOffsets(7, 6, 0.76f, 0.20f),
            portOffsetsEast: new[]
            {
                new Vector2I(4, 0)
            },
            outputBridgeCell: new Vector2I(6, 3),
            outputBridgeFacing: FacingDirection.East,
            transitParkingCenter: new Vector3(-16.0f, 0.0f, 11.0f),
            hullColor: new Color("292524"),
            cabColor: new Color("7C2D12"),
            accentColor: new Color("FB923C"),
            portColor: new Color("FED7AA"),
            attachmentMounts: new[]
            {
                CreateAttachmentMount("west-input-main", new Vector2I(0, 4), FacingDirection.West, new Vector2I(-1, 1), BuildPrototypeKind.InputPort, BuildPrototypeKind.MiningInputPort),
                CreateAttachmentMount("east-output-main", new Vector2I(6, 3), FacingDirection.East, new Vector2I(3, 0), BuildPrototypeKind.OutputPort),
                CreateAttachmentMount("east-output-aux", new Vector2I(6, 4), FacingDirection.East, new Vector2I(3, 1), BuildPrototypeKind.OutputPort)
            });
    }

    public static MobileFactoryInteriorPreset CreateFocusedDemoPreset()
    {
        var profile = CreateFocusedDemoProfile();
        return FactoryMapRuntimeLoader.LoadInteriorPreset(
            FactoryMapPaths.FocusedMobileInterior,
            presetId: "focused-dual-logistics",
            displayName: "野外加工转运样板",
            description: "玩家移动工厂把野外矿石导入熔炼与组装主线，同时在下层维持弹药补给支线，再经东侧双输出端口把产物送往接收站。",
            recoverySummary: "西侧输入端口接收矿石，中央熔炉把原矿送入组装机，主输出端口负责向站点外送；下层双仓储和弹药组装器组成内部防务补给支线。",
            profile: profile);
    }

    public static MobileFactoryInteriorPreset CreateExpeditionInputVerificationPreset()
    {
        return new MobileFactoryInteriorPreset(
            id: "expedition-input-verification",
            displayName: "远征熔炼装配线",
            description: "中型移动工厂把世界输入抬升进熔炉，再接入中段组装机和双输出端口，用于观察真实端口与配方链联动。",
            recoverySummary: "西侧输入先经北向抬升送入熔炉，熔炼产物再进入组装机，东侧双端口承担对外输出与站点交换。",
            placements: new[]
            {
                new FactoryPlacementSpec(BuildPrototypeKind.Generator, new Vector2I(1, 0), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.PowerPole, new Vector2I(3, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(1, 3), FacingDirection.North),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(1, 2), FacingDirection.North),
                new FactoryPlacementSpec(BuildPrototypeKind.Smelter, new Vector2I(2, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Assembler, new Vector2I(2, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Storage, new Vector2I(0, 0), FacingDirection.East)
            },
            attachmentPlacements: new[]
            {
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.InputPort, new Vector2I(0, 3), FacingDirection.West),
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.OutputPort, new Vector2I(5, 2), FacingDirection.East),
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.OutputPort, new Vector2I(5, 3), FacingDirection.East)
            });
    }

    public static MobileFactoryInteriorPreset CreateBranchAndMergePreset()
    {
        return new MobileFactoryInteriorPreset(
            id: "branch-and-merge",
            displayName: "缓冲回汇",
            description: "紧凑型工厂把输入端口接入缓存、熔炼和回汇主线，重点观察仓储与主输出的稳定性。",
            recoverySummary: "单输入在内部做一段缓冲后送入熔炉，主线最后汇入东侧输出端口，适合长时间观察节拍变化。",
            placements: new[]
            {
                new FactoryPlacementSpec(BuildPrototypeKind.Storage, new Vector2I(1, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Inserter, new Vector2I(2, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Smelter, new Vector2I(3, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(4, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Storage, new Vector2I(1, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Inserter, new Vector2I(2, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Sink, new Vector2I(1, 3), FacingDirection.East)
            },
            attachmentPlacements: new[]
            {
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.InputPort, new Vector2I(0, 3), FacingDirection.West),
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.OutputPort, new Vector2I(4, 2), FacingDirection.East)
            });
    }

    public static MobileFactoryInteriorPreset CreateBridgeRelayPreset()
    {
        return new MobileFactoryInteriorPreset(
            id: "bridge-relay",
            displayName: "跨桥中继",
            description: "使用仓储、卸载器、跨桥和装载器形成真实中继链，适合观察不同运输件的切换节奏。",
            recoverySummary: "输入端口进入仓储缓冲，再经卸载器和跨桥转发到输出端口，内部自带回收端防止长期堵死。",
            placements: new[]
            {
                new FactoryPlacementSpec(BuildPrototypeKind.Storage, new Vector2I(0, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Unloader, new Vector2I(1, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(2, 1), FacingDirection.South),
                new FactoryPlacementSpec(BuildPrototypeKind.Bridge, new Vector2I(2, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(2, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Loader, new Vector2I(3, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Sink, new Vector2I(4, 3), FacingDirection.East)
            },
            attachmentPlacements: new[]
            {
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.InputPort, new Vector2I(0, 3), FacingDirection.West),
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.OutputPort, new Vector2I(4, 2), FacingDirection.East)
            });
    }

    public static MobileFactoryInteriorPreset CreateDualFeedPreset()
    {
        return new MobileFactoryInteriorPreset(
            id: "dual-feed-balance",
            displayName: "双缓存汇流",
            description: "上下两组缓存和机械臂把同一输入端口的物资整理后汇入主线，适合观察合流与装卸稳定性。",
            recoverySummary: "双缓存经合并后统一接到世界侧输出端口，适合长时间对比不同路径的占用率。",
            placements: new[]
            {
                new FactoryPlacementSpec(BuildPrototypeKind.Storage, new Vector2I(1, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Inserter, new Vector2I(2, 1), FacingDirection.South),
                new FactoryPlacementSpec(BuildPrototypeKind.Storage, new Vector2I(1, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Inserter, new Vector2I(2, 3), FacingDirection.North),
                new FactoryPlacementSpec(BuildPrototypeKind.Merger, new Vector2I(2, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(3, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(4, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(5, 2), FacingDirection.East)
            },
            attachmentPlacements: new[]
            {
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.InputPort, new Vector2I(0, 3), FacingDirection.West),
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.OutputPort, new Vector2I(5, 2), FacingDirection.East)
            });
    }

    public static MobileFactoryInteriorPreset CreateWideLoopPreset()
    {
        return new MobileFactoryInteriorPreset(
            id: "wide-buffer-loop",
            displayName: "前线补给回路",
            description: "重载工厂在输入端口接收矿区物资后，内部用双缓冲与弹药组装器支撑防线补给，并保留对外输出能力。",
            recoverySummary: "左侧输入带和双仓储把物资送入主弹药组装器，东侧双输出端口负责向前线或站点外送，顶部炮塔展示防务支援角色。",
            placements: new[]
            {
                new FactoryPlacementSpec(BuildPrototypeKind.Generator, new Vector2I(1, 0), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.PowerPole, new Vector2I(3, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Storage, new Vector2I(0, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Inserter, new Vector2I(1, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(1, 4), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Storage, new Vector2I(4, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Inserter, new Vector2I(4, 2), FacingDirection.South),
                new FactoryPlacementSpec(BuildPrototypeKind.AmmoAssembler, new Vector2I(3, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.GunTurret, new Vector2I(5, 0), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Wall, new Vector2I(4, 0), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Storage, new Vector2I(4, 5), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Inserter, new Vector2I(5, 5), FacingDirection.East)
            },
            attachmentPlacements: new[]
            {
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.InputPort, new Vector2I(0, 4), FacingDirection.West),
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.OutputPort, new Vector2I(6, 3), FacingDirection.East),
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.OutputPort, new Vector2I(6, 4), FacingDirection.East)
            });
    }

    public static IReadOnlyList<MobileFactoryScenarioActorDefinition> CreateLargeScenarioActors()
    {
        var focused = CreateFocusedDemoProfile();
        var compact = CreateCompactProfile();
        var medium = CreateMediumProfile();
        var heavy = CreateHeavyProfile();

        var focusedDemo = CreateFocusedDemoPreset();
        var branch = CreateBranchAndMergePreset();
        var relay = CreateBridgeRelayPreset();
        var dualFeed = CreateDualFeedPreset();
        var wideLoop = CreateWideLoopPreset();

        return new[]
        {
            new MobileFactoryScenarioActorDefinition(
                actorId: "player-expedition",
                displayLabel: "玩家转运工厂",
                profile: focused,
                interiorPreset: focusedDemo,
                isPlayerControlled: true,
                transitPosition: new Vector3(-14.0f, 0.0f, 10.0f),
                transitFacing: FacingDirection.East,
                initialDeployAnchor: null,
                initialDeployFacing: FacingDirection.East,
                routePoints: null,
                labelColor: new Color("FDE68A")),
            new MobileFactoryScenarioActorDefinition(
                actorId: "heavy-output-east",
                displayLabel: "重载防务补给站",
                profile: heavy,
                interiorPreset: wideLoop,
                isPlayerControlled: false,
                transitPosition: new Vector3(-16.0f, 0.0f, -10.0f),
                transitFacing: FacingDirection.East,
                initialDeployAnchor: new Vector2I(-15, -6),
                initialDeployFacing: FacingDirection.East,
                routePoints: null,
                labelColor: new Color("FB923C")),
            new MobileFactoryScenarioActorDefinition(
                actorId: "compact-patrol",
                displayLabel: "石英巡航采样站",
                profile: compact,
                interiorPreset: relay,
                isPlayerControlled: false,
                transitPosition: new Vector3(6.0f, 0.0f, -9.0f),
                transitFacing: FacingDirection.West,
                initialDeployAnchor: null,
                initialDeployFacing: FacingDirection.East,
                routePoints: new[]
                {
                    new MobileFactoryRoutePoint(new Vector3(6.0f, 0.0f, -9.0f), FacingDirection.West, new Vector2I(6, -6), FacingDirection.East, 1.0f, 3.5f),
                    new MobileFactoryRoutePoint(new Vector3(13.0f, 0.0f, -1.0f), FacingDirection.North, new Vector2I(10, 2), FacingDirection.East, 1.25f, 3.2f)
                },
                labelColor: new Color("5EEAD4")),
            new MobileFactoryScenarioActorDefinition(
                actorId: "medium-output-north",
                displayLabel: "双源加工中继站",
                profile: medium,
                interiorPreset: dualFeed,
                isPlayerControlled: false,
                transitPosition: new Vector3(-6.0f, 0.0f, 8.0f),
                transitFacing: FacingDirection.East,
                initialDeployAnchor: new Vector2I(-4, 7),
                initialDeployFacing: FacingDirection.East,
                routePoints: null,
                labelColor: new Color("93C5FD")),
            new MobileFactoryScenarioActorDefinition(
                actorId: "compact-observer-loop",
                displayLabel: "轻型维修回收站",
                profile: compact,
                interiorPreset: branch,
                isPlayerControlled: false,
                transitPosition: new Vector3(2.0f, 0.0f, 12.0f),
                transitFacing: FacingDirection.South,
                initialDeployAnchor: null,
                initialDeployFacing: FacingDirection.East,
                routePoints: new[]
                {
                    new MobileFactoryRoutePoint(new Vector3(2.0f, 0.0f, 12.0f), FacingDirection.South, new Vector2I(1, 9), FacingDirection.East, 0.8f, 2.8f),
                    new MobileFactoryRoutePoint(new Vector3(-8.0f, 0.0f, 13.0f), FacingDirection.East, new Vector2I(-9, 10), FacingDirection.East, 1.0f, 2.8f)
                },
                labelColor: new Color("A7F3D0"))
        };
    }
}
