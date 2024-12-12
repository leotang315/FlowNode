using System;
using System.Collections.Generic;
using System.Linq;
using FlowNode.node;

namespace FlowNode.app.command
{
    /// <summary>
    /// 节点数据操作的命令
    /// </summary>
    public class AddNodeDataCommand : ICommand
    {
        private readonly NodeManager nodeManager;
        private readonly INode node;

        public AddNodeDataCommand(NodeManager nodeManager, INode node)
        {
            this.nodeManager = nodeManager;
            this.node = node;
        }

        public void Execute()
        {
            nodeManager.addNode(node);
        }

        public void Undo()
        {
            nodeManager.removeNode(node);
        }
    }

    public class RemoveNodeDataCommand : ICommand
    {
        private readonly NodeManager nodeManager;
        private readonly INode node;
        private readonly List<Connector> relatedConnectors;

        public RemoveNodeDataCommand(NodeManager nodeManager, INode node)
        {
            this.nodeManager = nodeManager;
            this.node = node;
            this.relatedConnectors = nodeManager.getConnectors()
                .Where(c => c.src.host == node || c.dst.host == node)
                .ToList();
        }

        public void Execute()
        {
            foreach (var connector in relatedConnectors)
            {
                nodeManager.removeConnector(connector);
            }
            nodeManager.removeNode(node);
        }

        public void Undo()
        {
            nodeManager.addNode(node);
            foreach (var connector in relatedConnectors)
            {
                nodeManager.addConnector(connector.src, connector.dst);
            }
        }
    }

    public class AddConnectorDataCommand : ICommand
    {
        private readonly NodeManager nodeManager;
        private readonly Pin sourcePin;
        private readonly Pin targetPin;

        public AddConnectorDataCommand(NodeManager nodeManager, Pin sourcePin, Pin targetPin)
        {
            this.nodeManager = nodeManager;
            this.sourcePin = sourcePin;
            this.targetPin = targetPin;
        }

        public void Execute()
        {
            nodeManager.addConnector(sourcePin, targetPin);
        }

        public void Undo()
        {
            var connector = nodeManager.getConnectors()
                .First(c => c.src == sourcePin && c.dst == targetPin);
            nodeManager.removeConnector(connector);
        }
    }

    public class RemoveConnectorDataCommand : ICommand
    {
        private readonly NodeManager nodeManager;
        private readonly Connector connector;

        public RemoveConnectorDataCommand(NodeManager nodeManager, Connector connector)
        {
            this.nodeManager = nodeManager;
            this.connector = connector;
        }

        public void Execute()
        {
            nodeManager.removeConnector(connector);
        }

        public void Undo()
        {
            nodeManager.addConnector(connector.src, connector.dst);
        }
    }
} 