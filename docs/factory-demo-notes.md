# 工厂 Demo 说明

## 场景划分

- `scenes/factory_demo.tscn` 仍然是默认主入口，用来保留原本的静态工厂建造与物流 baseline
- `scenes/mobile_factory_demo.tscn` 是新增的概念验证场景，专门演示“移动工厂”在世界中的部署、回收与再部署

## 当前版本包含

- 固定俯角的 3D 工厂镜头，支持边界内平移、缩放和分段旋转
- 鼠标驱动的网格指向、建造预览、放置与拆除
- 生产器、可自动形成直线/拐角的传送带、回收站三种基础原型建筑
- 分流器、合并器、跨桥、装载器、卸载器五种物流拓扑件
- 基于固定 Tick 的模拟循环，以及一条开场即可运行的生产演示线
- 传送带的连续物品堆积、阻塞回压和逐段传递
- 一套移动工厂 runtime 骨架：site-based 结构宿主、移动工厂 footprint 预留、部署端口桥接，以及可保留内部状态的 deploy / recall / redeploy 流程
- 一个独立的移动工厂 demo：左键部署、`R` 回收，能在两条外部物流线之间重新接入同一座移动工厂

## 当前已知限制

- 这一版仅支持 1x1 建筑
- 传送带目前仍是单通道简化模型，还没有实现异星工厂那种完整双边 lane 逻辑
- 分流器和合并器目前是 1x1 简化拓扑，不是异星工厂原版的双格宽结构
- 装载器/卸载器当前是机器口与带网之间的轻量适配件，不包含机械臂式抓取动作
- 当前代码与中文口径已统一：装载器是“传送带 -> 机器”，卸载器是“机器 -> 传送带”。
- 还没有存档、成长系统、分流物流、机械臂或配方生产
- 输入映射由 demo 场景在运行时注册，暂未完全转到编辑器配置里
- 移动工厂当前只有一个固定朝向和单一对外输出端口，主要用于验证机制，不是完整载具系统
- 移动工厂 demo 目前使用预设好的外部线路和部署锚点，尚未开放完整的移动平台建造编辑体验

## 冒烟测试

可以用下面的命令跑默认静态 demo 的冒烟测试：

```powershell
& 'D:\Godot\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe' --headless --path 'D:\Godot\projs\net-factory' -- --factory-smoke-test
```

这个测试会验证项目启动、临时放置/拆除，以及默认生产线上的物品送达流程。

可以用下面的命令跑独立移动工厂 demo 的冒烟测试：

```powershell
& 'D:\Godot\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe' --headless --path 'D:\Godot\projs\net-factory' 'res://scenes/mobile_factory_demo.tscn' -- --mobile-factory-smoke-test
```

这个测试会验证：
- 一个无效部署点会被正确拦截
- 移动工厂能成功部署到第一条外部物流线并送达回收站
- 回收后 footprint 和端口预留会被正确释放
- 再部署到第二条线路后，同一座移动工厂能够恢复物流输出
