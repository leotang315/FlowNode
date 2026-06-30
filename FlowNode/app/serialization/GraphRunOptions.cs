using System;
using System.Collections.Generic;
using System.IO;
using FlowNode.node;

namespace FlowNode.app.serialization
{
    /// <summary>
    /// <see cref="GraphRunner"/> 的可选参数（CLI / 宿主注入变量等）。
    /// </summary>
    public sealed class GraphRunOptions
    {
        /// <summary>执行前写入 NodeManager 的全局变量（会覆盖 XML 中同名项）。</summary>
        public IDictionary<string, object> Variables { get; set; }

        /// <summary>执行前切换进程当前目录（相对路径读写文件时使用），执行后恢复。</summary>
        public string WorkingDirectory { get; set; }

        public void ApplyTo(NodeManager manager)
        {
            if (Variables == null || manager == null)
                return;

            foreach (var pair in Variables)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    continue;

                var value = pair.Value;
                var type = value?.GetType() ?? typeof(object);
                manager.SetDataObject(pair.Key, value, type);
            }
        }
    }

    /// <summary>
    /// 图执行结果，供宿主程序读取退出码与日志。
    /// </summary>
    public sealed class GraphRunResult
    {
        public int ExitCode { get; set; }
        public List<string> Logs { get; } = new List<string>();
        public List<string> Errors { get; } = new List<string>();

        public bool Success => ExitCode == GraphRunner.ExitOk;
    }
}
