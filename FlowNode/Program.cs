using FlowNode.node.Attribute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using FlowNode.app.view;
using FlowNode.node;
namespace FlowNode
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 显示命令行窗口
            AllocConsole();
           // Console.WriteLine("这是一个命令行窗口");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 在应用程序启动时注册
            NodeViewFactory.RegisterNodeView<TestNode, TestNodeView>();

            Application.Run(new DemoForm());

            // 关闭命令行窗口（可选）
            FreeConsole();
        }
    }
}
