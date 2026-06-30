using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode.node
{
    public interface INodeManager
    {
        Connector findConnector(Pin pin);
        void pushNextConnectNode(Pin pin);
        void pushNextNode(INode node);
        void SetDataObject(string key, object obj, Type type);
        object GetDataObject(string key);
        Type GetDataObjectType(string key);
        void run();

        /// <summary>向执行日志写一条消息（供 Print 等节点输出到日志面板）。</summary>
        void WriteLog(string message);
    }
}
