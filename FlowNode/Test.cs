using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using FlowNode.node;

namespace FlowNode
{
    public class DataNode : NodeBase
    {
        public Pin pin_data;
        public DataNode()
        {
            isAuto = true;
        }
        public override void allocateDefaultPins()
        {
            pin_data = createPin("value", PinDirection.Output, PinType.Data, typeof(bool), true);
        }

        public override void excute(INodeManager manager)
        {
        }
    }


    public class PrintNode : SequenceNode
    {
        public string Message { get; set; }
        public override void doWork()
        {
            Console.WriteLine($"{Message}");
        }
    }

    public class Print2Node : NodeBase
    {
        public Pin pin_input;
        public Pin pin_data;
        public Print2Node()
        {
            isAuto = true;
        }
        public override void allocateDefaultPins()
        {
            pin_input = createPin("Input", PinDirection.Input, PinType.Execute);
            pin_data = createPin("value", PinDirection.Input, PinType.Data, typeof(int), 0);
        }

        public override void excute(INodeManager manager)
        {
            Console.WriteLine($"{pin_data.data}");
        }
    }










    //public class Test
    //{
    //    [STAThread]
    //    static void Main()
    //    {

    //        var nodenames = NodeFactory.GetNodeTypes();
    //        var node0 = NodeFactory.CreateNode("Loop");


    //        // 创建节点实例
    //        var initNode = new SequenceNode { Name = "init" };
    //        var sequenceNode1 = new SequenceNode { Name = "Sequence Node 1" };
    //        var sequenceNode2 = new SequenceNode { Name = "Sequence Node 2" };
    //        var branchNode = new BranchNode { Name = "Branch Node" };
    //        var loopNode = new LoopNode { Name = "Loop Node", loopCount = 3 };
    //        var showNode = new PrintNode { Name = "PrintNode_x", Message = "loop run" };
    //        var showNode2 = new PrintNode { Name = "PrintNode", Message = "loop over" };
    //        var dataNode = new DataNode { Name = "data node" };
    //        var Print2Node = new Print2Node { Name = "Print2Node" };
    //        var funcNode = new FunctionNode(new MathOperator(), typeof(MathOperator).GetMethod("add"));


    //        initNode.init();
    //        sequenceNode1.init();
    //        sequenceNode2.init();
    //        branchNode.init();
    //        loopNode.init();
    //        showNode.init();
    //        showNode2.init();
    //        dataNode.init();
    //        Print2Node.init();
    //        funcNode.init();

    //        // 添加节点到 NodeManager
    //        var nodeManager = new NodeManager();
    //        nodeManager.addNode(initNode);
    //        nodeManager.addNode(sequenceNode1);
    //        nodeManager.addNode(sequenceNode2);
    //        nodeManager.addNode(branchNode);
    //        nodeManager.addNode(loopNode);
    //        nodeManager.addNode(showNode);
    //        nodeManager.addNode(showNode2);
    //        nodeManager.addNode(dataNode);
    //        nodeManager.addNode(Print2Node);




    //        // 连接节点
    //        nodeManager.connect(initNode.pin_output, sequenceNode1.pin_input);
    //        nodeManager.connect(sequenceNode1.pin_output, branchNode.pin_input);
    //        nodeManager.connect(dataNode.pin_data, branchNode.pin_condition);
    //        nodeManager.connect(branchNode.pin_true, loopNode.pin_input);
    //        nodeManager.connect(branchNode.pin_false, sequenceNode2.pin_input);
    //        nodeManager.connect(loopNode.pin_loopBody, Print2Node.pin_input);
    //        nodeManager.connect(loopNode.pin_index, Print2Node.pin_data);
    //        nodeManager.connect(loopNode.pin_completed, showNode2.pin_input); // 连接到结束节点

    //        nodeManager.run();
    //    }
    //}

}
