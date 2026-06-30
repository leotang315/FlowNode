using System;
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

            return GraphRunner.RunFile(
                args[0],
                log: Console.WriteLine,
                error: message => Console.Error.WriteLine("[错误] " + message));
        }

        private static void PrintUsage()
        {
            Console.WriteLine("FlowNode.Cli — 无 UI 执行节点图");
            Console.WriteLine();
            Console.WriteLine("用法:");
            Console.WriteLine("  FlowNode.Cli.exe <graph.xml>");
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
