using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode1.node
{
    public interface INodeManager
    {
        Connector findConnector(Pin pin);
        void pushNextConnectNode(Pin pin);
        void pushNextNode(INode node);
        void run();
    }
}
