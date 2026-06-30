namespace FlowNode.node
{
    /// <summary>
    /// 将文本写入文件（执行流节点）。路径无效或 IO 失败时写入日志但不中断执行。
    /// </summary>
    [SystemNode("Io/WriteText")]
    public class WriteTextNode : NodeBase
    {
        private Pin pin_input;
        private Pin pin_output;
        private Pin pin_path;
        private Pin pin_content;

        /// <summary>为 true 时追加写入，否则覆盖。</summary>
        public bool Append { get; set; }

        public WriteTextNode()
        {
            Name = "Write Text";
        }

        public override void allocateDefaultPins()
        {
            pin_input = createPin("Input", PinDirection.Input, PinType.Execute);
            pin_output = createPin("Output", PinDirection.Output, PinType.Execute);
            pin_path = createPin("Path", PinDirection.Input, PinType.Data, typeof(string), string.Empty);
            pin_content = createPin("Content", PinDirection.Input, PinType.Data, typeof(string), string.Empty);
        }

        public override void excute(INodeManager manager)
        {
            var path = pin_path.data as string;
            var content = pin_content.data as string ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    var dir = System.IO.Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir))
                        System.IO.Directory.CreateDirectory(dir);

                    if (Append)
                        System.IO.File.AppendAllText(path, content);
                    else
                        System.IO.File.WriteAllText(path, content);

                    manager.WriteLog("[WriteText] " + path);
                }
                catch (System.Exception ex)
                {
                    manager.WriteLog("[WriteText] 失败: " + ex.Message);
                }
            }

            manager.pushNextConnectNode(pin_output);
        }
    }
}
