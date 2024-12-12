using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode.node
{
    public interface INode
    {
        string Name { get; }
        string NodePath { get; }
        List<Pin> Pins { get; }
        bool IsAutoRun { get; }
        void init();
        void clearup();
        Pin findPin(string name);
        void run(INodeManager manager);
    }
}
