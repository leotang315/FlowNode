using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode.node
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

    public static class PinTypeValidator
    {
        public static bool AreTypesCompatible(Type sourceType, Type targetType)
        {
            // 处理空值
            if (sourceType == null || targetType == null)
                return false;

            // 处理引用类型参数(ref/out)
            Type srcBaseType = sourceType.IsByRef ? sourceType.GetElementType() : sourceType;
            Type dstBaseType = targetType.IsByRef ? targetType.GetElementType() : targetType;

            // 检查直接兼容性
            if (dstBaseType == srcBaseType || dstBaseType.IsAssignableFrom(srcBaseType))
                return true;

            //// 检查运行时转换兼容性
            //try
            //{
            //    var testValue = Activator.CreateInstance(srcBaseType);
            //    Convert.ChangeType(testValue, dstBaseType);
            //    return true;
            //}
            //catch
            //{
            //    return false;
            //}
            return false;
        }
    }
}
