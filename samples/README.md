# 示例节点图

在编辑器中 **File → Open** 加载，或用 CLI 无界面执行：

```powershell
.\FlowNode.Cli\bin\Debug\net472\FlowNode.Cli.exe samples\print-hello.xml
```

| 文件 | 说明 |
|------|------|
| `print-hello.xml` | 单个 Print 节点，输出 `hello-sample` 到日志 |
| `write-text.xml` | WriteText 节点，执行后写入 `output.txt`（相对当前工作目录） |
| `read-transform-write.xml` | 读 `input.txt` → 拼接后缀 → 写 `processed-output.txt` |
| `score-check.xml` | **业务示例**：读 `score-input.txt` → 与 **全局变量 `threshold`** 比较 → Branch → 写 `score-result.txt` |
| `score-input.txt` | 供 `score-check.xml` 使用的分数（默认 `85`） |
| `input.txt` | 供 `read-transform-write.xml` 使用的输入文本 |

CLI 跑分数检查（需在仓库根目录，或通过 `GraphRunOptions.WorkingDirectory`）：

```powershell
.\FlowNode.Cli\bin\Debug\net472\FlowNode.Cli.exe --var threshold=60 samples\score-check.xml
.\FlowNode.HostDemo\bin\Debug\net472\FlowNode.HostDemo.exe --threshold 90
```

**`threshold` 从哪来？**

| 场景 | 来源 |
|------|------|
| 编辑器打开 `score-check.xml` | XML 内 `DataObjects` 默认 **60**；**双击**左侧 Data Object Manager 中的 `threshold` 可改，或选中 **Get threshold** 后在右侧属性面板改 **threshold** |
| CLI / HostDemo | `--var threshold=60` 或 `--threshold 60`（覆盖 XML 默认值） |
| 画布 | **Get threshold** 节点副标题显示当前值；未定义时显示「未设置」 |

重新生成示例文件（修改节点默认值后）：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/generate-samples.ps1
```
