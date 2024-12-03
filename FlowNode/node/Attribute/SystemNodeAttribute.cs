using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode1.node
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class SystemNodeAttribute : System.Attribute
    {
        public string Path { get; }

        public SystemNodeAttribute(string path)
        {
            Path = path;
        }
    }
}
