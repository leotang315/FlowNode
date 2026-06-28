using FlowNode.node.Attribute;

namespace FlowNode.node
{
    /// <summary>
    /// 常量节点基类：作为自动运行的纯数据源，输出一个可在属性面板编辑、可序列化的常量值。
    /// 子类通过暴露公有属性 Value（属性而非字段，才能被 PropertyGrid 与序列化识别）来设置常量。
    /// </summary>
    public abstract class ConstantNodeBase<T> : NodeBase
    {
        protected Pin pin_value;
        private T valueBacking;

        /// <summary>常量值（在属性面板中编辑）。</summary>
        public T Value
        {
            get => valueBacking;
            set
            {
                valueBacking = value;
                if (pin_value != null)
                    pin_value.data = value;
            }
        }

        protected ConstantNodeBase()
        {
            IsAutoRun = true;
        }

        public override void allocateDefaultPins()
        {
            pin_value = createPin("Value", PinDirection.Output, PinType.Data, typeof(T), Value);
        }

        public override void excute(INodeManager manager)
        {
            // 运行时输出当前（可能已被属性面板修改的）值
            pin_value.data = Value;
        }
    }

    [SystemNode("Constant/Int")]
    public class IntConstantNode : ConstantNodeBase<int>
    {
        public IntConstantNode() { Name = "Int"; }
    }

    [SystemNode("Constant/Float")]
    public class FloatConstantNode : ConstantNodeBase<float>
    {
        public FloatConstantNode() { Name = "Float"; }
    }

    [SystemNode("Constant/Bool")]
    public class BoolConstantNode : ConstantNodeBase<bool>
    {
        public BoolConstantNode() { Name = "Bool"; }
    }

    [SystemNode("Constant/String")]
    public class StringConstantNode : ConstantNodeBase<string>
    {
        public StringConstantNode() { Name = "String"; Value = ""; }
    }
}
