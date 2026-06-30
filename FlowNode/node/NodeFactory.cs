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
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                RegisterAssembly(assembly);
        }

        /// <summary>扫描程序集并注册节点；已存在的路径静默跳过（供宿主后加载程序集）。</summary>
        public static void RegisterAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            foreach (var type in GetLoadableTypes(assembly)
                .Where(t => t.GetCustomAttributes(typeof(SystemNodeAttribute), false).Any()))
            {
                var systemAttribute = (SystemNodeAttribute)type.GetCustomAttributes(typeof(SystemNodeAttribute), false).First();
                RegisterNodeInfo(new NodeInfo
                {
                    SystemNodeAttribute = systemAttribute,
                    NodeType = type
                }, skipIfExists: true);
            }

            foreach (var type in GetLoadableTypes(assembly)
                .Where(t => t.GetCustomAttributes(typeof(NodeAttribute), false).Any()))
            {
                var nodeAttribute = (NodeAttribute)type.GetCustomAttribute(typeof(NodeAttribute), false);
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.GetCustomAttributes(typeof(FunctionAttribute), false).Any()))
                {
                    var functionAttribute = (FunctionAttribute)method.GetCustomAttributes(typeof(FunctionAttribute), false).First();
                    RegisterNodeInfo(new NodeInfo
                    {
                        NodeAttribute = nodeAttribute,
                        FunctionAttribute = functionAttribute,
                        Method = method
                    }, skipIfExists: true);
                }
            }
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
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
            node.init();
            return node;
        }

        /// <summary>若节点为 Get/Set 变量节点，返回其变量元数据。</summary>
        public static bool TryGetVarNodeInfo(NodeBase node, out string varName, out Type varType, out bool isSet)
        {
            if (node is GetObjectNode getNode)
            {
                varName = getNode.VariableName;
                varType = getNode.VariableType;
                isSet = false;
                return true;
            }

            if (node is SetObjectNode setNode)
            {
                varName = setNode.VariableName;
                varType = setNode.VariableType;
                isSet = true;
                return true;
            }

            varName = null;
            varType = null;
            isSet = false;
            return false;
        }

        /// <summary>根据剪贴板/序列化中的变量元数据重建变量节点。</summary>
        public static NodeBase CreateVarNodeFromInfo(string varName, string varTypeName, bool isSet)
        {
            var varType = Type.GetType(varTypeName) ?? typeof(object);
            return CreateVarNode(varName, varType, isSet);
        }

        public static void RegisterNodeInfo(NodeInfo nodeInfo, bool skipIfExists = false)
        {
            string path;

            if (nodeInfo.IsSystemNode)
            {
                if (nodeInfo.SystemNodeAttribute == null)
                    throw new ArgumentException("System node must have SystemNodeAttribute");
                path = Path.Combine(_systemPath, nodeInfo.SystemNodeAttribute.Path);
            }
            else if (nodeInfo.IsFunctionNode)
            {
                if (nodeInfo.NodeAttribute == null || nodeInfo.FunctionAttribute == null)
                    throw new ArgumentException("Function node must have both NodeAttribute and FunctionAttribute");

                string functionName = string.IsNullOrEmpty(nodeInfo.FunctionAttribute.Name)
                    ? nodeInfo.Method.Name
                    : nodeInfo.FunctionAttribute.Name;

                path = Path.Combine(_customerPath, nodeInfo.NodeAttribute.Path, functionName);
            }
            else
            {
                throw new ArgumentException("Invalid node info type");
            }

            if (_nodeInfos.ContainsKey(path))
            {
                if (skipIfExists)
                    return;
                throw new ArgumentException($"Node path '{path}' is already registered");
            }

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
