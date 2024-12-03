using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode.node
{
    public interface INode
    {
        bool isAutoRun();
        void init();
        void clearup();
        void run(INodeManager manager);
    }
}
