using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode1.node
{
    public enum PinDirection
    {
        Input,
        Output,
    }

    public enum PinType
    {
        Execute,
        Data,
    }

    public class DataChangedArgs : EventArgs
    {
        public object Data { get; }
        public DataChangedArgs(object data)
        {
            Data = data;
        }
    }

    public class Pin
    {
        public event EventHandler<DataChangedArgs> DataChanged;
        public string Name { get; set; }
        public INode host { get; }
        public PinDirection direction;
        public PinType pinType;
        public object data;
        public Type dataType;

        public object Data
        {
            get => data;
            set
            {
                if (!Equals(data, value))
                {
                    data = value;
                    DataChanged?.Invoke(this, new DataChangedArgs(data));
                }
            }
        }
        public Pin(INode node)
        {
            host = node;
        }

        public static void copyData(Pin dst, Pin src)
        {
            dst.data = src.data;
        }

    }
}
