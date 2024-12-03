using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode.node
{
    [AttributeUsage(AttributeTargets.Method)]
    public class FunctionAttribute : System.Attribute
    {
        public string Name { get; set; }
        public bool IsAutoRun { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }


        public FunctionAttribute(string name, bool isAutoRun, string category = "", string desc = "")
        {
            Name = name;
            IsAutoRun = isAutoRun;
            Category = category;
            Description = desc;
        }
    }
}
