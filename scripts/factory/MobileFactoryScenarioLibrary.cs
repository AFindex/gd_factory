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
        return new MobileFactoryInteriorPreset(
            id: "focused-dual-logistics",
            displayName: "野外熔炼转运样板",
            description: "玩家移动工厂把世界侧输入矿石导入内部熔炉与组装工位，再经东侧输出端口把产物送往接收站。",
            recoverySummary: "西侧输入端口接收野外矿石，底部发电机维持内部供电，主线在熔炼后转入组装和仓储，再通过东侧输出端口外送。",
            placements: new[]
            {
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(1, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Smelter, new Vector2I(2, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(3, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Storage, new Vector2I(4, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Inserter, new Vector2I(5, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Assembler, new Vector2I(6, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(7, 3), FacingDirection.South),
                new FactoryPlacementSpec(BuildPrototypeKind.Generator, new Vector2I(1, 7), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.PowerPole, new Vector2I(3, 6), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Storage, new Vector2I(4, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Inserter, new Vector2I(5, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(6, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Sink, new Vector2I(1, 1), FacingDirection.East)
            },
            attachmentPlacements: new[]
            {
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.InputPort, new Vector2I(0, 3), FacingDirection.West),
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.OutputPort, new Vector2I(7, 1), FacingDirection.East),
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.OutputPort, new Vector2I(7, 4), FacingDirection.East)
            });
    }

    public static MobileFactoryInteriorPreset CreateExpeditionInputVerificationPreset()
    {
        return new MobileFactoryInteriorPreset(
            id: "expedition-input-verification",
            displayName: "远征输入熔炼线",
            description: "中型移动工厂把世界输入直接接入熔炼与转运链，用于观察输入端口、缓存和输出端口的联动。",
            recoverySummary: "主线在仓储缓冲后进入熔炉，再送入东侧端口；顶部电杆保证外出工位在部署后稳定运作。",
            placements: new[]
            {
                new FactoryPlacementSpec(BuildPrototypeKind.Generator, new Vector2I(1, 0), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.PowerPole, new Vector2I(3, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Storage, new Vector2I(1, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Inserter, new Vector2I(2, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Smelter, new Vector2I(3, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(4, 3), FacingDirection.North),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(4, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Assembler, new Vector2I(4, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Sink, new Vector2I(1, 4), FacingDirection.East)
            },
            attachmentPlacements: new[]
            {
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.InputPort, new Vector2I(0, 3), FacingDirection.West),
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.OutputPort, new Vector2I(5, 2), FacingDirection.East)
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
            description: "重载工厂在输入端口接收物资后，内部完成弹药缓存、组装和前线炮位补给，再把余量送往外部世界。",
            recoverySummary: "上层负责仓储缓冲和炮位补给，下层负责主线输出与回收，适合承担防线支援或大型站点转运角色。",
            placements: new[]
            {
                new FactoryPlacementSpec(BuildPrototypeKind.Generator, new Vector2I(1, 0), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.PowerPole, new Vector2I(3, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Storage, new Vector2I(1, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Inserter, new Vector2I(2, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.AmmoAssembler, new Vector2I(3, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(4, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Storage, new Vector2I(5, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Inserter, new Vector2I(5, 1), FacingDirection.North),
                new FactoryPlacementSpec(BuildPrototypeKind.GunTurret, new Vector2I(5, 0), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Wall, new Vector2I(4, 0), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Inserter, new Vector2I(5, 3), FacingDirection.South),
                new FactoryPlacementSpec(BuildPrototypeKind.GunTurret, new Vector2I(5, 4), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Wall, new Vector2I(4, 4), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(6, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Sink, new Vector2I(1, 4), FacingDirection.East)
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
                displayLabel: "玩家工厂",
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
                displayLabel: "重载外输站",
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
                displayLabel: "巡航采样站",
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
                displayLabel: "双源中继站",
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
                displayLabel: "轻型回路站",
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
