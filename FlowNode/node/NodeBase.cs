using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode.node
{
 

    public abstract class NodeBase : INode
    {
        public string Name { get; set; }
        public List<Pin> Pins = new List<Pin>();
        public bool isAuto = false;


        public NodeBase()
        {
        }

        public bool isAutoRun()
        {
            return isAuto;
        }

        public void init()
        {
            allocateDefaultPins();
        }

        public void clearup()
        {

        }

        public void run(INodeManager manager)
        {
            // 执行当前节点的逻辑
            Console.WriteLine($"{Name}");
            resolve(manager);
            excute(manager);
        }

        public void resolve(INodeManager manager)
        {
            // 找出所有输入依赖节点
            var inputPins = Pins.FindAll(n => (n.direction == PinDirection.Input) && (n.pinType == PinType.Data));

            // 找出依赖节点所连接的对端节点
            foreach (var pin in inputPins)
            {
                var connector = manager.findConnector(pin);
                if (connector != null)
                {
                    if (connector.src.host.isAutoRun())
                    {
                        connector.src.host.run(manager);
                    }
                    Pin.copyData(connector.dst, connector.src);
                }
            }
        }

        public abstract void excute(INodeManager manager);

        public abstract void allocateDefaultPins();

        public Pin createPin(string name, PinDirection dir, PinType type)
        {
            Pin pin = new Pin(this);
            pin.Name = name;
            pin.direction = dir;
            pin.pinType = type;
            Pins.Add(pin);
            return pin;
        }

        public Pin createPin(string name, PinDirection dir, PinType type, Type dataType, object data)
        {
            Pin pin = new Pin(this);
            pin.Name = name;
            pin.direction = dir;
            pin.pinType = type;
            pin.data = data;
            pin.dataType = dataType;
            Pins.Add(pin);
            return pin;
        }

        public Pin findPin(string name, PinDirection dir)
        {
            for (int i = 0; i < Pins.Count; i++)
            {
                if (Pins[i].Name == name && Pins[i].direction == dir)
                {
                    return Pins[i];
                }
            }
            return null;
        }

        public Pin findPin(string name)
        {
            for (int i = 0; i < Pins.Count; i++)
            {
                if (Pins[i].Name == name)
                {
                    return Pins[i];
                }
            }
            return null;
        }

    
    }

}
