using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FlowNode.node
{
    public class NodeManager : INodeManager
    {
        private List<INode> nodes = new List<INode>();
        private List<Connector> connectors = new List<Connector>();
        private Stack<INode> executionStack = new Stack<INode>();

        /// <summary>
        /// 检查引脚方向
        /// </summary>
        /// <param name="src">源引脚</param>
        /// <param name="dst">目的引脚</param>
        /// <returns></returns>
        private bool ValidatePinDirection(Pin src, Pin dst)
        {

            if (src.direction != PinDirection.Output || dst.direction != PinDirection.Input)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 检查引脚类型
        /// </summary>
        /// <param name="src">源引脚</param>
        /// <param name="dst">目的引脚</param>
        /// <returns></returns>
        private bool ValidatePinType(Pin src, Pin dst)
        {
            if (src.pinType != dst.pinType)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 检查引脚数据兼容性
        /// </summary>
        /// <param name="srcDataType">源引脚数据类型</param>
        /// <param name="dstDataType">目的引脚数据类型</param>
        /// <returns></returns>
        private bool ValidatePinDataType(Type srcDataType, Type dstDataType)
        {
            return PinTypeValidator.AreTypesCompatible(srcDataType, dstDataType);
        }

        /// <summary>
        /// 检查有没有循环依赖
        /// </summary>
        /// <param name="src">添加连接的源节点</param>
        /// <param name="dst">添加连接的目标节点</param>
        /// <returns></returns>
        private bool ValidateCycleDependency(INode src, INode dst)
        {
            // 使用 DFS 检测环
            HashSet<INode> visited = new HashSet<INode>();
            return !hasPath(dst, src, visited);
        }

        /// <summary>
        /// 判断是否存在从当前节点到目标节点的路径
        /// </summary>
        /// <param name="current"></param>
        /// <param name="target"></param>
        /// <param name="visited"></param>
        /// <returns></returns>
        private bool hasPath(INode current, INode target, HashSet<INode> visited)
        {
            if (current == target)
            {
                return true; // 找到路径，���成环
            }

            if (visited.Contains(current))
            {
                return false; // 避免重复访问
            }

            visited.Add(current);

            // 找到当前节点的所有输出连接
            var outgoingConnectors = connectors.Where(c => c.src.host == current);
            foreach (var connector in outgoingConnectors)
            {
                if (hasPath(connector.dst.host, target, visited))
                {
                    return true;
                }
            }

            return false;
        }

        public List<Connector> getConnectors()
        {
            return connectors;
        }

        public List<INode> getNodes()
        {
            return nodes;
        }


        public void addNode(INode node)
        {
            nodes.Add(node);
        }

        public void removeNode(INode node)
        {
            nodes.Remove(node);
        }

        public void addConnector(Pin src, Pin dst)
        {

            if (!ValidatePinDirection(src, dst))
            {
                throw new InvalidOperationException("连接器只能从输出引脚连接到输入引脚");
            }

            if (!ValidatePinType(src, dst))
            {
                throw new InvalidOperationException("连接器数据引脚和执行引脚不能相连");
            }

            if (!ValidatePinDataType(src.dataType, dst.dataType))
            {
                throw new InvalidOperationException($"数据类型不兼容：源引脚类型为 {src.dataType}, 目标引脚类型为 {dst.dataType}");
            }


            var connection = new Connector { src = src, dst = dst };
            connectors.Add(connection);
        }

        public void removeConnector(Connector connector)
        {
            connectors.Remove(connector);
        }

        public Connector findConnector(Pin pin)
        {
            var connector = connectors.FirstOrDefault(c => c.dst == pin);
            return connector;
        }

        public void pushNextConnectNode(Pin pin)
        {
            var connector = connectors.FirstOrDefault(c => c.src == pin);

            if (connector != null)
            {
                pushNextNode(connector.dst.host);

            }
        }

        public void pushNextNode(INode node)
        {
            executionStack.Push(node);
        }

        /// <summary>
        /// 查找入口节点（没有输入执行引脚连接的节点）
        /// </summary>
        private List<INode> FindEntryNodes()
        {
            var entryNodes = new List<INode>();
            foreach (var node in nodes)
            {
                // 首先检查节点是否有执行类型的引脚
                bool hasExecutePin = node.Pins.Any(p => p.pinType == PinType.Execute);
                if (!hasExecutePin)
                {
                    continue; // 跳过没有执行类型引脚的节点
                }

                bool hasInputExecution = false;
                foreach (var pin in node.Pins)
                {
                    if (pin.direction == PinDirection.Input && pin.pinType == PinType.Execute)
                    {
                        // 检查这个输入执行引脚是否有连接
                        if (connectors.Any(c => c.dst == pin))
                        {
                            hasInputExecution = true;
                            break;
                        }
                    }
                }

                // 如果节点没有输入执行引脚的连接，它就是入口节点
                if (!hasInputExecution)
                {
                    entryNodes.Add(node);
                }
            }
            return entryNodes;
        }

        public void run()
        {
            try
            {
                // 清空执行堆栈
                executionStack.Clear();

                // 找到所有入口节点
                var entryNodes = FindEntryNodes();
                if (entryNodes.Count == 0)
                {
                    throw new InvalidOperationException("No entry nodes found in the graph");
                }

                // 将入口节点按照添加顺序推入堆栈（后添加的先执行）
                foreach (var node in entryNodes.AsEnumerable().Reverse())
                {
                    executionStack.Push(node);
                }

                //// 将所有根节点（没有输入依赖的节点）推入执行堆栈
                //executionStack.Push(nodes[0]);

                // 依次从堆栈中取出节点并执行
                while (executionStack.Count > 0)
                {
                    var node = executionStack.Pop();
                    node.run(this);
                }
            }
            catch (Exception ex)
            {
                // 清空执行堆栈
                executionStack.Clear();
                throw new InvalidOperationException($"Flow execution error: {ex.Message}", ex);
            }
        }
    }
}
