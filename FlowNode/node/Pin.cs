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

        /// <summary>校验两引脚是否可连线，并返回可读的错误原因。</summary>
        public static bool CanConnect(Pin source, Pin target, out string error)
        {
            error = null;
            if (source == null || target == null)
            {
                error = "未选中目标引脚";
                return false;
            }

            if (source.host == target.host)
            {
                error = "不能连接同一节点上的引脚";
                return false;
            }

            if (source.direction == target.direction)
            {
                error = "需要连接方向相反的引脚（输出 → 输入）";
                return false;
            }

            if (source.pinType != target.pinType)
            {
                error = "引脚类型不匹配（Execute 与 Data 不能互连）";
                return false;
            }

            if (source.pinType == PinType.Execute)
                return true;

            Pin outputPin = source.direction == PinDirection.Output ? source : target;
            Pin inputPin = source.direction == PinDirection.Output ? target : source;

            if (AreTypesCompatible(outputPin.dataType, inputPin.dataType))
                return true;

            string srcType = outputPin.dataType?.Name ?? "?";
            string dstType = inputPin.dataType?.Name ?? "?";
            error = $"数据类型不兼容：{srcType} → {dstType}";
            return false;
        }
    }
}
