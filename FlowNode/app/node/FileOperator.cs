using System.IO;
using FlowNode.node.Attribute;

namespace FlowNode.node
{
    /// <summary>
    /// 文件读取：自动运行的纯数据节点（路径 → 文本内容）。
    /// </summary>
    [Node("Io")]
    public class FileOperator
    {
        [Function("readTextFile", true)]
        public static void ReadTextFile(string path, out string result)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                result = string.Empty;
                return;
            }

            try
            {
                result = File.ReadAllText(path);
            }
            catch
            {
                result = string.Empty;
            }
        }
    }
}
