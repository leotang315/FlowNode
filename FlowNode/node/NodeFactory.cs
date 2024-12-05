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
    public static class NodeFactory
    {
        private static readonly Dictionary<string, Type> _nodeTypes = new Dictionary<string, Type>();
        private static readonly Dictionary<string, MethodInfo> _methods = new Dictionary<string, MethodInfo>();
        private static readonly string _customerPath = "/custom/";
        private static readonly string _systemPath = "/system/";

        static NodeFactory()
        {
            // 类节点，使用反射所有具有SystemNode属性的节点，并将类型其注册到_nodeTypes中
            var nodeTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.GetCustomAttributes(typeof(SystemNodeAttribute), false).Any());

            foreach (var type in nodeTypes)
            {
                var attribute = (SystemNodeAttribute)type.GetCustomAttributes(typeof(SystemNodeAttribute), false).First();

                string path = Path.Combine(_systemPath, attribute.Path);
                _nodeTypes[path] = type;
            }

            // 函数节点，使用反射找出具有Node的节点类,并在其内部具有Function属性的函数注册到_methods中
            var nodes = AppDomain.CurrentDomain.GetAssemblies()
                     .SelectMany(assembly => assembly.GetTypes())
                      .Where(type => type.GetCustomAttributes(typeof(NodeAttribute), false).Any());
            foreach (var node in nodes)
            {
                var nodeAtrribute = (NodeAttribute)node.GetCustomAttribute(typeof(NodeAttribute), false);
                var methods = node.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                  .Where(method => method.GetCustomAttributes(typeof(FunctionAttribute), false).Any());
                foreach (var method in methods)
                {
                    var FunctionAttribute = (FunctionAttribute)method.GetCustomAttributes(typeof(FunctionAttribute), false).First();
                    string path = "";
                    if (FunctionAttribute.Name == null || FunctionAttribute.Name.Equals(""))
                    {
                        path = Path.Combine(_customerPath, nodeAtrribute.Path, method.Name);
                    }
                    else
                    {
                        path = Path.Combine(_customerPath, nodeAtrribute.Path, FunctionAttribute.Name);
                    }
                    _methods[path] = method;
                }
            }


        }

        public static List<string> GetNodePath()
        {
            return _nodeTypes.Keys.Concat(_methods.Keys).ToList();
        }

        public static NodeBase CreateNode(string path)
        {
            // 类节点创建
            NodeBase node = null;
            Type nodeType;
            if (_nodeTypes.TryGetValue(path, out nodeType))
            {
                node = (NodeBase)Activator.CreateInstance(nodeType);
                node.init();
                return node;
            }

            // 函数节点创建
            MethodInfo method;
            if (_methods.TryGetValue(path, out method))
            {
                node = new FunctionNode(null, method);
                node.init();
                return node;
            }

            throw new ArgumentException("Invalid node type");
        }
    }
}
