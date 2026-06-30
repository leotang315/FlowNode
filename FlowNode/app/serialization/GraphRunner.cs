using System;
using System.IO;
using FlowNode.node;

namespace FlowNode.app.serialization
{
    /// <summary>
    /// 无 UI 加载并执行节点图，供 CLI、宿主与测试共用。
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
            return RunFile(filePath, null, log, error);
        }

        public static int RunFile(
            string filePath,
            GraphRunOptions options,
            Action<string> log = null,
            Action<string> error = null)
        {
            var mgr = new NodeManager();
            return RunLoadedGraph(mgr, filePath, options, log, error);
        }

        public static GraphRunResult RunFileInto(
            NodeManager manager,
            string filePath,
            GraphRunOptions options = null)
        {
            var result = new GraphRunResult();
            result.ExitCode = RunLoadedGraph(
                manager,
                filePath,
                options,
                message => result.Logs.Add(message),
                message => result.Errors.Add(message));
            return result;
        }

        /// <summary>对已加载的图校验并执行（不再读文件）。</summary>
        public static int Run(
            NodeManager manager,
            Action<string> log = null,
            Action<string> error = null)
        {
            if (manager == null)
            {
                error?.Invoke("NodeManager 为空");
                return ExitUsage;
            }

            if (log != null)
                manager.Log += log;

            return ExecuteManager(manager, error, log);
        }

        private static int ExecuteManager(NodeManager manager, Action<string> error, Action<string> log)
        {
            return ExecuteManager(manager, error, log, workingDirectory: null);
        }

        private static int RunLoadedGraph(
            NodeManager manager,
            string filePath,
            GraphRunOptions options,
            Action<string> log,
            Action<string> error)
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

            var serializer = new NodeGraphSerializer(manager);
            if (log != null)
                manager.Log += log;

            try
            {
                serializer.LoadFromFile(fullPath);
            }
            catch (Exception ex)
            {
                error?.Invoke("加载失败: " + ex.Message);
                return ExitIo;
            }

            options?.ApplyTo(manager);
            return ExecuteManager(manager, error, log, options?.WorkingDirectory);
        }

        private static int ExecuteManager(
            NodeManager manager,
            Action<string> error,
            Action<string> log,
            string workingDirectory = null)
        {
            string previousDirectory = null;
            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                previousDirectory = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(Path.GetFullPath(workingDirectory));
            }

            try
            {
                return ExecuteManagerCore(manager, error, log);
            }
            finally
            {
                if (previousDirectory != null)
                    Directory.SetCurrentDirectory(previousDirectory);
            }
        }

        private static int ExecuteManagerCore(NodeManager manager, Action<string> error, Action<string> log)
        {
            var errors = manager.Validate();
            if (errors.Count > 0)
            {
                error?.Invoke("校验失败");
                foreach (var message in errors)
                    error?.Invoke("  " + message);
                return ExitValidation;
            }

            foreach (var warning in manager.ValidateWarnings())
                log?.Invoke("[警告] " + warning);

            try
            {
                manager.run();
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
