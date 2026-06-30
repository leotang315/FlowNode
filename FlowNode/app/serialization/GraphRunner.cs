using System;
using System.IO;
using FlowNode.node;

namespace FlowNode.app.serialization
{
    /// <summary>
    /// 无 UI 加载并执行节点图，供 CLI 与测试共用。
    /// </summary>
    public static class GraphRunner
    {
        public const int ExitOk = 0;
        public const int ExitUsage = 1;
        public const int ExitValidation = 2;
        public const int ExitExecution = 3;
        public const int ExitIo = 4;

        public static int RunFile(
            string filePath,
            Action<string> log = null,
            Action<string> error = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                error?.Invoke("缺少 graph.xml 路径");
                return ExitUsage;
            }

            var fullPath = Path.GetFullPath(filePath);
            if (!File.Exists(fullPath))
            {
                error?.Invoke("文件不存在: " + fullPath);
                return ExitIo;
            }

            var mgr = new NodeManager();
            var serializer = new NodeGraphSerializer(mgr);
            if (log != null)
                mgr.Log += log;

            try
            {
                serializer.LoadFromFile(fullPath);
            }
            catch (Exception ex)
            {
                error?.Invoke("加载失败: " + ex.Message);
                return ExitIo;
            }

            var errors = mgr.Validate();
            if (errors.Count > 0)
            {
                error?.Invoke("校验失败");
                foreach (var message in errors)
                    error?.Invoke("  " + message);
                return ExitValidation;
            }

            foreach (var warning in mgr.ValidateWarnings())
                log?.Invoke("[警告] " + warning);

            try
            {
                mgr.run();
            }
            catch (Exception ex)
            {
                error?.Invoke("执行失败: " + ex.Message);
                return ExitExecution;
            }

            return ExitOk;
        }
    }
}
