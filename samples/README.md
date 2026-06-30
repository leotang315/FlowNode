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
| `input.txt` | 供 `read-transform-write.xml` 使用的输入文本 |

重新生成示例文件（修改节点默认值后）：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/generate-samples.ps1
```
