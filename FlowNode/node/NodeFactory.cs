using FlowNode.node.Attribute;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode.node
{
    public class NodeInfo
    {
        // 系统节点信息
        public Type NodeType { get; set; }
        public SystemNodeAttribute SystemNodeAttribute { get; set; }

        // 函数节点信息
        public MethodInfo Method { get; set; }
        public FunctionAttribute FunctionAttribute { get; set; }
        public NodeAttribute NodeAttribute { get; set; }

        // 节点类型
        public bool IsSystemNode => NodeType != null;
        public bool IsFunctionNode => Method != null;
    }

    public static class NodeFactory
    {
        private static readonly Dictionary<string, NodeInfo> _nodeInfos = new Dictionary<string, NodeInfo>();
        private static readonly string _customerPath = "/custom/";
        private static readonly string _systemPath = "/system/";

        static NodeFactory()
        {
            // 系统节点，使用反射所有具有SystemNode属性的节点
            var systemNodeTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.GetCustomAttributes(typeof(SystemNodeAttribute), false).Any());

            foreach (var type in systemNodeTypes)
            {
                var systemAttribute = (SystemNodeAttribute)type.GetCustomAttributes(typeof(SystemNodeAttribute), false).First();
                NodeInfo nodeInfo = new NodeInfo
                {
                    SystemNodeAttribute = systemAttribute,
                    NodeType = type
                };
                RegisterNodeInfo(nodeInfo);
            }

            // 函数节点，使用反射找出具有Node的节点类,并在其内部具有Function属性的函数
            var nodeTypes = AppDomain.CurrentDomain.GetAssemblies()
                     .SelectMany(assembly => assembly.GetTypes())
                      .Where(type => type.GetCustomAttributes(typeof(NodeAttribute), false).Any());
            foreach (var type in nodeTypes)
            {
                var nodeAttribute = (NodeAttribute)type.GetCustomAttribute(typeof(NodeAttribute), false);
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                  .Where(method => method.GetCustomAttributes(typeof(FunctionAttribute), false).Any());

                foreach (var method in methods)
                {
                    var functionAttribute = (FunctionAttribute)method.GetCustomAttributes(typeof(FunctionAttribute), false).First();
                    NodeInfo nodeInfo = new NodeInfo
                    {
                        NodeAttribute = nodeAttribute,
                        FunctionAttribute = functionAttribute,
                        Method = method
                    };
                    RegisterNodeInfo(nodeInfo);
                }
            }
        }

        public static List<string> GetNodePath()
        {
            return _nodeInfos.Keys.ToList();
        }

        public static NodeBase CreateNode(string path)
        {
            if (!_nodeInfos.TryGetValue(path, out NodeInfo nodeInfo))
            {
                throw new ArgumentException("Invalid node path");
            }

            NodeBase node = null;
            var name = Path.GetFileName(path);

            if (nodeInfo.IsSystemNode)
            {
                // 创建系统节点
                node = (NodeBase)Activator.CreateInstance(nodeInfo.NodeType);
            }
            else if (nodeInfo.IsFunctionNode)
            {
                // 创建函数节点
                node = new FunctionNode(null, nodeInfo.Method);
                node.IsAutoRun = nodeInfo.FunctionAttribute.IsAutoRun;
            }
            else
            {
                throw new InvalidOperationException("Invalid node info");
            }

            node.Name = name;
            node.NodePath = path;
            node.init();
            return node;
        }

        public static NodeBase CreateVarNode(string varName, Type varType, bool isSet)
        {
            NodeBase node = isSet ? (NodeBase)new SetObjectNode(varName, varType) : new GetObjectNode(varName, varType);

            return node;
        }

        public static void RegisterNodeInfo(NodeInfo nodeInfo)
        {
            string path;

            if (nodeInfo.IsSystemNode)
            {
                // 系统节点的路径处理
                if (nodeInfo.SystemNodeAttribute == null)
                {
                    throw new ArgumentException("System node must have SystemNodeAttribute");
                }
                path = Path.Combine(_systemPath, nodeInfo.SystemNodeAttribute.Path);
            }
            else if (nodeInfo.IsFunctionNode)
            {
                // 函数节点的路径处理
                if (nodeInfo.NodeAttribute == null || nodeInfo.FunctionAttribute == null)
                {
                    throw new ArgumentException("Function node must have both NodeAttribute and FunctionAttribute");
                }

                string functionName = string.IsNullOrEmpty(nodeInfo.FunctionAttribute.Name)
                    ? nodeInfo.Method.Name
                    : nodeInfo.FunctionAttribute.Name;

                path = Path.Combine(_customerPath, nodeInfo.NodeAttribute.Path, functionName);
            }
            else
            {
                throw new ArgumentException("Invalid node info type");
            }

            // 检查路径是否已存在
            if (_nodeInfos.ContainsKey(path))
            {
                throw new ArgumentException($"Node path '{path}' is already registered");
            }

            // 注册节点信息
            _nodeInfos[path] = nodeInfo;
        }


        // 添加一些辅助方法来获取节点信息
        public static NodeInfo GetNodeInfo(string path)
        {
            return _nodeInfos.TryGetValue(path, out NodeInfo info) ? info : null;
        }

        public static List<string> GetSystemNodePaths()
        {
            return _nodeInfos.Where(kv => kv.Value.IsSystemNode)
                            .Select(kv => kv.Key)
                            .ToList();
        }

        public static List<string> GetFunctionNodePaths()
        {
            return _nodeInfos.Where(kv => kv.Value.IsFunctionNode)
                            .Select(kv => kv.Key)
                            .ToList();
        }

    }
}
