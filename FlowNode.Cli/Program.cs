using System;
using System.Collections.Generic;
using System.Globalization;
using FlowNode.app.serialization;

namespace FlowNode.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
            {
                PrintUsage();
                return args.Length == 0 ? GraphRunner.ExitUsage : GraphRunner.ExitOk;
            }

            string graphPath = null;
            var options = new GraphRunOptions { Variables = new Dictionary<string, object>() };

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--var" && i + 1 < args.Length)
                {
                    ParseVariable(args[++i], options.Variables);
                    continue;
                }

                if (graphPath == null && !args[i].StartsWith("-", StringComparison.Ordinal))
                    graphPath = args[i];
            }

            if (graphPath == null)
            {
                Console.Error.WriteLine("[错误] 缺少 graph.xml 路径");
                return GraphRunner.ExitUsage;
            }

            if (options.Variables.Count == 0)
                options = null;

            return GraphRunner.RunFile(
                graphPath,
                options,
                log: Console.WriteLine,
                error: message => Console.Error.WriteLine("[错误] " + message));
        }

        private static void ParseVariable(string text, IDictionary<string, object> variables)
        {
            var eq = text.IndexOf('=');
            if (eq <= 0)
                throw new FormatException("变量格式应为 name=value: " + text);

            var name = text.Substring(0, eq).Trim();
            var valueText = text.Substring(eq + 1);
            variables[name] = ParseValue(valueText);
        }

        private static object ParseValue(string text)
        {
            if (bool.TryParse(text, out bool b))
                return b;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
                return i;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                return d;
            return text;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("FlowNode.Cli — 无 UI 执行节点图");
            Console.WriteLine();
            Console.WriteLine("用法:");
            Console.WriteLine("  FlowNode.Cli.exe [--var name=value ...] <graph.xml>");
            Console.WriteLine();
            Console.WriteLine("选项:");
            Console.WriteLine("  --var name=value   执行前注入全局变量（覆盖 XML 同名项）");
            Console.WriteLine();
            Console.WriteLine("退出码:");
            Console.WriteLine("  0  执行成功");
            Console.WriteLine("  1  缺少参数");
            Console.WriteLine("  2  图校验失败");
            Console.WriteLine("  3  执行异常");
            Console.WriteLine("  4  文件不存在或加载失败");
        }
    }
}
