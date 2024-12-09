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
            var nodeTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.GetCustomAttributes(typeof(SystemNodeAttribute), false).Any());

            foreach (var type in nodeTypes)
            {
                var attribute = (SystemNodeAttribute)type.GetCustomAttributes(typeof(SystemNodeAttribute), false).First();
                string path = Path.Combine(_systemPath, attribute.Path);
                
                _nodeInfos[path] = new NodeInfo
                {
                    NodeType = type,
                    SystemNodeAttribute = attribute
                };
            }

            // 函数节点，使用反射找出具有Node的节点类,并在其内部具有Function属性的函数
            var nodes = AppDomain.CurrentDomain.GetAssemblies()
                     .SelectMany(assembly => assembly.GetTypes())
                      .Where(type => type.GetCustomAttributes(typeof(NodeAttribute), false).Any());
            foreach (var node in nodes)
            {
                var nodeAttribute = (NodeAttribute)node.GetCustomAttribute(typeof(NodeAttribute), false);
                var methods = node.GetMethods(BindingFlags.Public | BindingFlags.Static)
                  .Where(method => method.GetCustomAttributes(typeof(FunctionAttribute), false).Any());
                
                foreach (var method in methods)
                {
                    var functionAttribute = (FunctionAttribute)method.GetCustomAttributes(typeof(FunctionAttribute), false).First();
                    string path;
                    if (string.IsNullOrEmpty(functionAttribute.Name))
                    {
                        path = Path.Combine(_customerPath, nodeAttribute.Path, method.Name);
                    }
                    else
                    {
                        path = Path.Combine(_customerPath, nodeAttribute.Path, functionAttribute.Name);
                    }

                    _nodeInfos[path] = new NodeInfo
                    {
                        Method = method,
                        FunctionAttribute = functionAttribute,
                        NodeAttribute = nodeAttribute
                    };
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
                node.isAuto = nodeInfo.FunctionAttribute.IsAutoRun;
            }
            else
            {
                throw new InvalidOperationException("Invalid node info");
            }

            node.Name = name;
            node.init();
            return node;
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

        //// 按类别获取节点路径
        //public static Dictionary<string, List<string>> GetNodePathsByCategory()
        //{
        //    var categories = new Dictionary<string, List<string>>();
            
        //    foreach (var pair in _nodeInfos)
        //    {
        //        string category;
        //        if (pair.Value.IsSystemNode)
        //        {
        //            category = pair.Value.SystemNodeAttribute.Category ?? "System";
        //        }
        //        else
        //        {
        //            category = pair.Value.NodeAttribute?.Category ?? "Functions";
        //        }

        //        if (!categories.ContainsKey(category))
        //        {
        //            categories[category] = new List<string>();
        //        }
        //        categories[category].Add(pair.Key);
        //    }

        //    return categories;
        //}
    }
}
