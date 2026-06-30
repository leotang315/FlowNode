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

`FlowNode.Tests` 仅引用 **FlowNode.Core**（逻辑层）；画布/布局相关用例在 **FlowNode.Tests.Editor**（引用编辑器 WinExe）。一键构建并运行两者：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-tests.ps1
```

当前 **148** 个 Core 用例 + **16** 个 Editor 用例（合计 **164**）。

## 命令行执行（无 UI）

在编辑器中 Save 出 XML 后，可用 **FlowNode.Cli** 在无界面环境跑图：

```powershell
# 构建后
.\FlowNode.Cli\bin\Debug\net472\FlowNode.Cli.exe path\to\graph.xml
```

退出码：`0` 成功，`2` 校验失败，`3` 执行异常，`4` 文件/加载错误。Print 等节点日志输出到 stdout。

示例图见 [`samples/`](samples/)：

```powershell
.\FlowNode.Cli\bin\Debug\net472\FlowNode.Cli.exe samples\print-hello.xml
.\FlowNode.Cli\bin\Debug\net472\FlowNode.Cli.exe --var threshold=60 samples\score-check.xml
```

业务示例 **score-check**：读 `samples/score-input.txt`（分数）→ 与 `--var threshold=` 比较 → 分支 → 写 `samples/score-result.txt`。重新生成全部示例：`scripts/generate-samples.ps1`。

## 嵌入宿主（FlowNode.Core + HostDemo）

[`FlowNode.HostDemo`](FlowNode.HostDemo/) 演示在自有程序中引用 Core、注册自定义节点并跑图：

```csharp
using System.Collections.Generic;
using FlowNode.app.serialization;
using FlowNode.hosting;

var host = new GraphHost();
host.RegisterAssembly(typeof(MyNodes).Assembly);
var result = host.RunFile("samples/score-check.xml", new GraphRunOptions {
    WorkingDirectory = repoRoot,
    Variables = new Dictionary<string, object> { ["threshold"] = 60 }
});
return result.ExitCode;
```

构建并运行：

```powershell
.\FlowNode.HostDemo\bin\Debug\net472\FlowNode.HostDemo.exe
.\FlowNode.HostDemo\bin\Debug\net472\FlowNode.HostDemo.exe --threshold 90
```

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
├── FlowNode.Core/        # 逻辑层类库（node/、函数节点、XML 序列化/执行，无 WinForms）
├── FlowNode/             # WinForms 可视化编辑器（画布、命令、属性面板）
├── FlowNode.Cli/         # 命令行：加载 XML → 校验 → 执行（无 UI）
├── FlowNode.HostDemo/    # 嵌入示例：GraphHost + 自定义 [Function] 节点
├── FlowNode.Tests/       # Core 单元测试（148 用例，仅引 Core）
├── FlowNode.Tests.Editor/ # 编辑器 UI 测试（16 用例，引 WinExe）
├── FlowNode.sln
├── scripts/run-tests.ps1
└── docs/
```

### 逻辑与 UI 分离

| 程序集 | 职责 |
|--------|------|
| **FlowNode.Core** | `NodeManager` 执行引擎、`NodeFactory`、系统/函数节点、`NodeGraphSerializer`（节点/连线/变量，不含布局） |
| **FlowNode**（编辑器） | `NodeEditor` 画布、`NodeSerializationService`（在 Core 之上读写节点位置） |
| **FlowNode.Cli** | 无界面批处理执行，适合 CI / 脚本 |
| **FlowNode.HostDemo** | 宿主嵌入参考实现（仅引 Core） |

源码仍位于 `FlowNode/node/` 与 `FlowNode/app/node/`，由 **FlowNode.Core.csproj** 编译进 `FlowNode.Core.dll`。

### 编辑器目录（FlowNode/）

```
FlowNode/
├── node/                 # 【由 Core 编译】数据模型与执行引擎
├── app/
│   ├── view/             # 编辑器 UI 与交互
│   ├── command/          # 撤销 / 重做
│   ├── serialization/    # NodeSerializationService（布局 + Core 序列化）
│   └── node/             # 【由 Core 编译】[Function] 自定义节点
└── Program.cs            # 启动 DemoForm
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
