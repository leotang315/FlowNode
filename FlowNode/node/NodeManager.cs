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
        private Dictionary<string, (object value, Type type)> dataObjects = new Dictionary<string, (object, Type)>();

        // 执行步数上限，防止意外死循环把界面卡死
        private const int MaxExecutionSteps = 1000000;

        /// <summary>每个节点开始执行前触发，供 UI 高亮当前节点。</summary>
        public event Action<INode> NodeExecuting;

        /// <summary>执行过程中的日志消息，供日志面板显示。</summary>
        public event Action<string> Log;

        /// <summary>向执行日志写一条消息（供 Print 等节点输出到日志面板）。</summary>
        public void WriteLog(string message)
        {
            Log?.Invoke(message);
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
            if (src.pinType == PinType.Data)
            {
                if (!ValidatePinDataType(src.dataType, dst.dataType))
                {
                    throw new InvalidOperationException($"数据类型不兼容：源引脚类型为 {src.dataType}, 目标引脚类型为 {dst.dataType}");
                }
            }
            var connection = new Connector { src = src, dst = dst };
            connectors.Add(connection);
        }

        public void removeConnector(Connector connector)
        {
            connectors.Remove(connector);
        }

        public void clear()
        {
            nodes.Clear();
            connectors.Clear();
            executionStack.Clear();
            dataObjects.Clear();
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
        /// 执行前校验图，返回错误列表（为空表示校验通过）。
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (nodes.Count == 0)
            {
                errors.Add("图中没有任何节点。");
                return errors;
            }

            var entryNodes = FindEntryNodes();
            if (entryNodes.Count == 0)
            {
                errors.Add("没有找到入口节点：所有带执行引脚的节点其输入执行引脚都已被连接（可能存在执行流环）。");
            }

            if (HasExecuteCycle())
            {
                errors.Add("执行流中存在环：节点通过执行引脚相互连接形成闭环，会导致死循环。");
            }

            return errors;
        }

        /// <summary>
        /// 执行前的告警检查（不阻断执行）：孤立节点、未连接且无默认值的数据输入引脚。
        /// </summary>
        public List<string> ValidateWarnings()
        {
            var warnings = new List<string>();

            foreach (var node in nodes)
            {
                bool hasAnyConnection = connectors.Any(c => c.src.host == node || c.dst.host == node);
                if (!hasAnyConnection)
                {
                    warnings.Add($"节点 \"{node.Name}\" 未连接任何引脚（孤立节点，不会参与执行）。");
                    continue;
                }

                foreach (var pin in node.Pins.Where(p => p.direction == PinDirection.Input && p.pinType == PinType.Data))
                {
                    bool connected = connectors.Any(c => c.dst == pin);
                    if (!connected && pin.data == null)
                    {
                        warnings.Add($"节点 \"{node.Name}\" 的数据输入引脚 \"{pin.Name}\" 未连接且无默认值。");
                    }
                }
            }

            return warnings;
        }

        /// <summary>
        /// 检测执行引脚连接是否形成有向环（循环节点通过运行时重新压栈实现，不会产生连接环，故不会被误报）。
        /// </summary>
        private bool HasExecuteCycle()
        {
            var visited = new HashSet<INode>();
            var inStack = new HashSet<INode>();
            foreach (var node in nodes)
            {
                if (ExecuteCycleDfs(node, visited, inStack))
                    return true;
            }
            return false;
        }

        private bool ExecuteCycleDfs(INode node, HashSet<INode> visited, HashSet<INode> inStack)
        {
            if (inStack.Contains(node))
                return true;
            if (visited.Contains(node))
                return false;

            visited.Add(node);
            inStack.Add(node);

            foreach (var connector in connectors.Where(c => c.src.host == node && c.src.pinType == PinType.Execute))
            {
                if (ExecuteCycleDfs(connector.dst.host, visited, inStack))
                    return true;
            }

            inStack.Remove(node);
            return false;
        }

        private bool isRunning;
        private int stepCounter;

        /// <summary>是否处于执行（含暂停于断点）状态。</summary>
        public bool IsRunning => isRunning;

        /// <summary>当前是否还有待执行的节点。</summary>
        public bool HasMoreSteps => isRunning && executionStack.Count > 0;

        /// <summary>查看下一个将要执行的节点（不出栈），用于断点判断。</summary>
        public INode PeekNext()
        {
            return executionStack.Count > 0 ? executionStack.Peek() : null;
        }

        /// <summary>
        /// 开始一次执行：校验、清栈、压入入口节点。需配合 Step() 驱动。
        /// </summary>
        public void BeginRun()
        {
            var errors = Validate();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("\n", errors));
            }

            executionStack.Clear();
            foreach (var node in FindEntryNodes().AsEnumerable().Reverse())
            {
                executionStack.Push(node);
            }

            stepCounter = 0;
            isRunning = true;
            Log?.Invoke("=== 开始执行 ===");
        }

        /// <summary>
        /// 执行栈顶一个节点。返回 true 表示执行了一个节点，false 表示无可执行节点。
        /// </summary>
        public bool Step()
        {
            if (!isRunning)
                return false;

            if (executionStack.Count == 0)
            {
                EndRun();
                return false;
            }

            if (++stepCounter > MaxExecutionSteps)
            {
                executionStack.Clear();
                EndRun();
                throw new InvalidOperationException($"执行步数超过上限 {MaxExecutionSteps}，可能存在死循环。");
            }

            var node = executionStack.Pop();
            NodeExecuting?.Invoke(node);
            Log?.Invoke($"执行节点: {node.Name}");
            node.run(this);

            // 节点执行后若栈空，说明流程自然结束
            if (executionStack.Count == 0)
            {
                EndRun();
            }
            return true;
        }

        /// <summary>主动中止执行。silent=true 时不输出停止日志（用于出错复位）。</summary>
        public void StopRun(bool silent = false)
        {
            if (!isRunning)
                return;
            executionStack.Clear();
            isRunning = false;
            if (!silent)
            {
                Log?.Invoke("=== 执行已停止 ===");
            }
        }

        private void EndRun()
        {
            if (!isRunning)
                return;
            isRunning = false;
            Log?.Invoke("=== 执行完成 ===");
        }

        /// <summary>
        /// 一次性执行整个流程（无 UI 分步驱动时使用）。
        /// </summary>
        public void run()
        {
            try
            {
                BeginRun();
                while (Step()) { }
            }
            catch (Exception ex)
            {
                executionStack.Clear();
                isRunning = false;
                throw new InvalidOperationException($"Flow execution error: {ex.Message}", ex);
            }
        }






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
                return true; // 找到路径，成环
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

        public void SetDataObject(string key, object obj, Type type)
        {
            dataObjects[key] = (obj, type);
        }

        public object GetDataObject(string key)
        {
            return dataObjects.TryGetValue(key, out var obj) ? obj.value : null;
        }

        public Type GetDataObjectType(string key)
        {
            return dataObjects.TryGetValue(key, out var obj) ? obj.type : null;
        }
        public List<string>  GetAllDataObjectKeys()
        {
           return dataObjects.Keys.ToList();
        }

        public void RemoveDataObject(string key)
        {
            dataObjects.Remove(key);
        }

        /// <summary>将所有 Get 变量节点的输出引脚与全局变量同步（改值后刷新画布副标题）。</summary>
        public void SyncGetObjectOutputPins()
        {
            foreach (var node in getNodes())
            {
                if (node is GetObjectNode getNode)
                    getNode.RefreshOutputFrom(this);
            }
        }

    }
}
