using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FlowNode1.node
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

            // 如果类型完全匹配，直接赋值
            if (dstDataType == srcDataType)
            {
                return true;

            }

            // 如果目标类型是源类型的父类型或相同类型，则兼容
            if (dstDataType.IsAssignableFrom(srcDataType))
            {
                return true;
            }

            // 如果可以进行安全的类型转换，也认为是兼容的
            try
            {
                var testValue = Activator.CreateInstance(srcDataType);
                Convert.ChangeType(testValue, dstDataType);
                return true;
            }
            catch
            {
                return false;
            }
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
                return true; // 找到路径，形成环
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

        public void run()
        {
            // 将所有根节点（没有输入依赖的节点）推入执行堆栈
            executionStack.Push(nodes[0]);

            // 依次从堆栈中取出节点并执行
            while (executionStack.Count > 0)
            {
                var node = executionStack.Pop();
                node.run(this);
            }
        }
    }
}
