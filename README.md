# FlowNode

一个基于 **C# WinForms + .NET Framework 4.7.2** 的可视化节点 / 流程图编辑器，设计思路接近 Unreal Blueprint 与 Node-RED：通过拖拽节点、连线来搭建可执行的逻辑图。

## 功能特性

- 可视化画布：拖拽建节点、贝塞尔连线、缩放、平移、框选、右键搜索建节点
- 撤销 / 重做（命令模式）；属性与 Pin 修改可 Undo
- 复制 / 粘贴（含变量 Get/Set 节点元数据）；多选对齐与等距
- 双轨 Pin 系统：Execute（控制流）+ Data（数据流）
- 内置系统节点：Sequence / Branch / Loop / 常量 / Print / Delay / Comment 等
- 反射注册函数节点与自定义系统节点；Get/Set 全局变量
- 栈式执行引擎：校验、高亮、断点（F9）、单步 / 继续 / 停止、日志面板
- XML 存盘：节点、连线、布局、引脚默认值、变量节点、全局变量取值
- 未保存提示（标题栏 `*`）；Undo 回到已保存状态时自动清除 dirty

完整快捷键见应用内 **Help → Keyboard Shortcuts**。

## 环境要求

- Windows
- .NET Framework 4.7.2
- Visual Studio 2019 或更高版本（含 .NET 桌面开发工作负载）

## 构建与运行

1. 用 Visual Studio 打开 `FlowNode.sln`
2. 选择 `Debug` 或 `Release` 配置，按 F5 运行
3. Debug 构建会附带一个控制台窗口用于查看日志（Release 不会）

命令行构建：

```bash
msbuild FlowNode.sln /p:Configuration=Release
```

## 运行测试

`node/` 纯逻辑层由 `FlowNode.Tests` 工程覆盖（NUnit 风格用例）。一键构建并运行：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-tests.ps1
```

脚本使用 Visual Studio 自带的 MSBuild 构建（以正确处理 .NET Framework 的 `.resx`），并通过工程内置的轻量 runner 执行用例（规避旧版 NUnit3 引擎在本机枚举 .NET 7 运行时目录时的已知崩溃），全部通过时退出码为 0。当前 **135** 个用例。

## 快捷键（摘要）

| 操作 | 快捷键 |
|------|--------|
| 撤销 / 重做 | Ctrl+Z / Ctrl+Y |
| 全选 / 复制 / 粘贴 | Ctrl+A / Ctrl+C / Ctrl+V |
| 删除选中节点 | Delete |
| 对齐（左/右/上/下） | Ctrl+Shift+L / R / T / B |
| 等距（水平/垂直，≥3 节点） | Ctrl+Shift+H / J |
| 适应全部 / 缩放至选中 | Ctrl+0 / Ctrl+Shift+0 |
| 断点（单选节点） | F9 |

画布：右键空白建节点、右键连线删除、中键平移、滚轮缩放。详见 **Help → Keyboard Shortcuts**。

## XML 文件格式

Save / Save As 输出 `NodeGraphData` 的 XML（`XmlSerializer`），根元素下主要包含：

| 区块 | 内容 |
|------|------|
| `Nodes` | 节点：`NodePath`、属性、`Pins` 默认值；Get/Set 变量节点含 `VarName` / `VarTypeName` / `VarIsSet` |
| `Connectors` | 连线：源/目标节点 Id 与引脚名 |
| `ViewData` | 节点在画布上的位置与大小 |
| `DataObjects` | 全局变量名、类型（`TypeName`）、当前值 |

旧文件若无 `DataObjects` 仍可加载；加载时会清空当前图再恢复。类型名使用 `AssemblyQualifiedName` / `FullName`，数值用 `InvariantCulture` 字符串存储。

## 项目结构

```
FlowNode/
├── node/                 # 数据模型与执行引擎（与 UI 解耦）
│   ├── NodeBase / Pin / Connector / NodeManager / NodeFactory
│   ├── Attribute/        # 节点注册特性
│   └── SystemNode/       # 内置系统节点（含 FlowControl）
├── app/
│   ├── view/             # 编辑器 UI 与交互
│   │   ├── NodeEditor.cs # 画布核心
│   │   ├── NodeView.cs   # 单个节点视图
│   │   ├── control/      # 节点内嵌 GDI+ 控件
│   │   └── states/       # 交互状态机
│   ├── command/          # 撤销 / 重做命令
│   ├── serialization/    # XML 序列化
│   └── node/             # 用户自定义函数节点
└── Program.cs            # 应用入口（启动 DemoForm）
```

更详细的架构说明、技术栈分析与开发计划见 [`docs/项目分析与开发计划.md`](docs/项目分析与开发计划.md)。

## 添加自定义节点

### 方式一：函数节点（最简单）

用 `[Function]` 特性标注静态方法，参数会自动映射为 Data 引脚：

```csharp
public class MathOperator
{
    [Function("Math/Add")]
    public static void Add(int a, int b, out int result)
    {
        result = a + b;
    }
}
```

### 方式二：系统节点

继承 `NodeBase`（或 `SequenceNode`）并用 `[SystemNode("路径")]` 标注，在 `allocateDefaultPins()` 中创建引脚、在 `excute()` 中实现逻辑：

```csharp
[SystemNode("MyCategory/MyNode")]
public class MyNode : NodeBase
{
    public override void allocateDefaultPins()
    {
        createPin("Input",  PinDirection.Input,  PinType.Execute);
        createPin("Output", PinDirection.Output, PinType.Execute);
    }

    public override void excute(INodeManager manager)
    {
        // 业务逻辑
    }
}
```

### 自定义节点外观（可选）

继承 `NodeView` 实现自定义绘制，并在 `Program.cs` 启动时注册映射：

```csharp
NodeViewFactory.RegisterNodeView<MyNode, MyNodeView>();
```

未注册的节点类型将使用 `DefaultNodeView`。

## 许可证

[MIT License](LICENSE) © 2024 Lei Qian
