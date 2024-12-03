using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode.node.Attribute
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    class NodeAttribute: System.Attribute
    {
        public string Path { get; }
        public NodeAttribute(string path="/custom/")
        {
            Path = path;
        }
    }
}
