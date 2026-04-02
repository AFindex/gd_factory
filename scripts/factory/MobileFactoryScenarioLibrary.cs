using Godot;
using System.Collections.Generic;

public static class MobileFactoryScenarioLibrary
{
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
            interiorMaxCell: new Vector2I(4, 4),
            interiorCellSize: 0.72f,
            interiorFloorHeight: 0.36f,
            interiorPlatformBorder: 0.18f,
            footprintOffsetsEast: new[]
            {
                new Vector2I(0, 0),
                new Vector2I(1, 0),
                new Vector2I(0, 1),
                new Vector2I(1, 1)
            },
            portOffsetsEast: new[]
            {
                new Vector2I(2, 0)
            },
            outputBridgeCell: new Vector2I(4, 2),
            outputBridgeFacing: FacingDirection.East,
            transitParkingCenter: new Vector3(-11.0f, 0.0f, 7.0f),
            hullColor: new Color("1F2937"),
            cabColor: new Color("475569"),
            accentColor: new Color("F59E0B"),
            portColor: new Color("FB923C"),
            attachmentMounts: new[]
            {
                CreateAttachmentMount("west-input-main", new Vector2I(0, 3), FacingDirection.West, new Vector2I(-1, 1), BuildPrototypeKind.InputPort),
                CreateAttachmentMount("east-output-main", new Vector2I(4, 1), FacingDirection.East, new Vector2I(2, 0), BuildPrototypeKind.OutputPort),
                CreateAttachmentMount("east-output-aux", new Vector2I(4, 3), FacingDirection.East, new Vector2I(2, 1), BuildPrototypeKind.OutputPort)
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
            footprintOffsetsEast: new[]
            {
                new Vector2I(0, 0),
                new Vector2I(1, 0),
                new Vector2I(0, 1),
                new Vector2I(1, 1)
            },
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
                CreateAttachmentMount("west-input-main", new Vector2I(0, 3), FacingDirection.West, new Vector2I(-1, 1), BuildPrototypeKind.InputPort),
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
            footprintOffsetsEast: new[]
            {
                new Vector2I(0, 0),
                new Vector2I(1, 0),
                new Vector2I(2, 0),
                new Vector2I(0, 1),
                new Vector2I(1, 1),
                new Vector2I(2, 1)
            },
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
                CreateAttachmentMount("west-input-main", new Vector2I(0, 3), FacingDirection.West, new Vector2I(-1, 1), BuildPrototypeKind.InputPort),
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
            footprintOffsetsEast: new[]
            {
                new Vector2I(0, 0),
                new Vector2I(1, 0),
                new Vector2I(2, 0),
                new Vector2I(3, 0),
                new Vector2I(0, 1),
                new Vector2I(1, 1),
                new Vector2I(2, 1),
                new Vector2I(3, 1)
            },
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
                CreateAttachmentMount("west-input-main", new Vector2I(0, 4), FacingDirection.West, new Vector2I(-1, 1), BuildPrototypeKind.InputPort),
                CreateAttachmentMount("east-output-main", new Vector2I(6, 3), FacingDirection.East, new Vector2I(4, 0), BuildPrototypeKind.OutputPort),
                CreateAttachmentMount("east-output-aux", new Vector2I(6, 4), FacingDirection.East, new Vector2I(4, 1), BuildPrototypeKind.OutputPort)
            });
    }

    public static MobileFactoryInteriorPreset CreateFocusedDemoPreset()
    {
        return new MobileFactoryInteriorPreset(
            id: "focused-line",
            displayName: "基础直通线",
            description: "在厂内保留一条向外输出的主线，并增加一条从世界输入后进入内部回收的支线。",
            recoverySummary: "输出通过世界侧回收站长期消化，输入通过内部回收器稳定消费。",
            placements: new[]
            {
                new FactoryPlacementSpec(BuildPrototypeKind.Producer, new Vector2I(0, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(1, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(2, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(3, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Sink, new Vector2I(1, 3), FacingDirection.East)
            },
            attachmentPlacements: new[]
            {
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.InputPort, new Vector2I(0, 3), FacingDirection.West),
                new MobileFactoryAttachmentPlacementSpec(BuildPrototypeKind.OutputPort, new Vector2I(4, 1), FacingDirection.East)
            });
    }

    public static MobileFactoryInteriorPreset CreateExpeditionInputVerificationPreset()
    {
        return new MobileFactoryInteriorPreset(
            id: "expedition-input-verification",
            displayName: "远征输入验证线",
            description: "中型移动工厂保留一条稳定外输主线，并在西侧挂接输入端口，把世界物流直接送进内部回收端。",
            recoverySummary: "东侧输出接世界回收线，西侧输入持续喂给内部回收器，适合在大场景里验证输入端口功能。",
            placements: new[]
            {
                new FactoryPlacementSpec(BuildPrototypeKind.Producer, new Vector2I(0, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(1, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(2, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(3, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(4, 1), FacingDirection.South),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(4, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Sink, new Vector2I(1, 3), FacingDirection.East)
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
            displayName: "分流回汇",
            description: "主线分成上下两路，再汇回主输出，方便验证分流器、两路转向和合并器的组合。",
            recoverySummary: "最终通过输出桥接到世界回收线，由世界侧持续消化。",
            placements: new[]
            {
                new FactoryPlacementSpec(BuildPrototypeKind.Producer, new Vector2I(0, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Splitter, new Vector2I(1, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(1, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(2, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(1, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(2, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(3, 1), FacingDirection.South),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(3, 3), FacingDirection.North),
                new FactoryPlacementSpec(BuildPrototypeKind.Merger, new Vector2I(3, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(4, 2), FacingDirection.East)
            });
    }

    public static MobileFactoryInteriorPreset CreateBridgeRelayPreset()
    {
        return new MobileFactoryInteriorPreset(
            id: "bridge-relay",
            displayName: "跨桥中继",
            description: "使用卸载器、跨桥和装载器形成跨线中继，适合观察不同运输件的组合拓扑。",
            recoverySummary: "内部直接接入回收器，可长期运行而不会依赖外部线路。",
            placements: new[]
            {
                new FactoryPlacementSpec(BuildPrototypeKind.Producer, new Vector2I(0, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Unloader, new Vector2I(1, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(2, 1), FacingDirection.South),
                new FactoryPlacementSpec(BuildPrototypeKind.Bridge, new Vector2I(2, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(2, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Loader, new Vector2I(3, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Sink, new Vector2I(4, 3), FacingDirection.East)
            });
    }

    public static MobileFactoryInteriorPreset CreateDualFeedPreset()
    {
        return new MobileFactoryInteriorPreset(
            id: "dual-feed-balance",
            displayName: "双源汇流",
            description: "上下两条独立生产源汇入中心主线，再统一送往外部世界，适合观察多源争用。",
            recoverySummary: "经合并后的主线连到外部世界回收站，长期运行稳定。",
            placements: new[]
            {
                new FactoryPlacementSpec(BuildPrototypeKind.Producer, new Vector2I(0, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(1, 1), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(2, 1), FacingDirection.South),
                new FactoryPlacementSpec(BuildPrototypeKind.Producer, new Vector2I(0, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(1, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(2, 3), FacingDirection.North),
                new FactoryPlacementSpec(BuildPrototypeKind.Merger, new Vector2I(2, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(3, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(4, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(5, 2), FacingDirection.East)
            });
    }

    public static MobileFactoryInteriorPreset CreateWideLoopPreset()
    {
        return new MobileFactoryInteriorPreset(
            id: "wide-buffer-loop",
            displayName: "宽幅缓冲回路",
            description: "先分流到上下两层缓冲线，再回汇到主输出，拓扑更宽更密，适合长期观察。",
            recoverySummary: "最终输出连接世界侧回收线，同时保留多段缓冲，避免主干轻易卡死。",
            placements: new[]
            {
                new FactoryPlacementSpec(BuildPrototypeKind.Producer, new Vector2I(0, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Splitter, new Vector2I(1, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(1, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(2, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(3, 2), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(4, 2), FacingDirection.South),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(1, 4), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(2, 4), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(3, 4), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(4, 4), FacingDirection.North),
                new FactoryPlacementSpec(BuildPrototypeKind.Merger, new Vector2I(4, 3), FacingDirection.East),
                new FactoryPlacementSpec(BuildPrototypeKind.Belt, new Vector2I(5, 3), FacingDirection.East)
            });
    }

    public static IReadOnlyList<MobileFactoryScenarioActorDefinition> CreateLargeScenarioActors()
    {
        var compact = CreateCompactProfile();
        var medium = CreateMediumProfile();
        var heavy = CreateHeavyProfile();

        var branch = CreateBranchAndMergePreset();
        var expeditionInputVerification = CreateExpeditionInputVerificationPreset();
        var relay = CreateBridgeRelayPreset();
        var dualFeed = CreateDualFeedPreset();
        var wideLoop = CreateWideLoopPreset();

        return new[]
        {
            new MobileFactoryScenarioActorDefinition(
                actorId: "player-expedition",
                displayLabel: "玩家工厂",
                profile: medium,
                interiorPreset: expeditionInputVerification,
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
