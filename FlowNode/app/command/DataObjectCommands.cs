using System;
using FlowNode.node;

namespace FlowNode.app.command
{
    /// <summary>
    /// 添加数据对象的命令
    /// </summary>
    public class AddDataObjectCommand : ICommand
    {
        private readonly NodeManager nodeManager;
        private readonly string key;
        private readonly object value;
        private readonly Type type;
        private object oldValue;
        private Type oldType;
        private bool hadOldValue;

        public AddDataObjectCommand(NodeManager nodeManager, string key, object value, Type type)
        {
            this.nodeManager = nodeManager;
            this.key = key;
            this.value = value;
            this.type = type;
        }

        public void Execute()
        {
            // 保存旧值（如果存在）
            oldValue = nodeManager.GetDataObject(key);
            oldType = nodeManager.GetDataObjectType(key);
            hadOldValue = oldValue != null || oldType != null;

            // 设置新值
            nodeManager.SetDataObject(key, value, type);
        }

        public void Undo()
        {
            if (hadOldValue)
            {
                nodeManager.SetDataObject(key, oldValue, oldType);
            }
            else
            {
                nodeManager.RemoveDataObject(key);
            }
        }
    }

    /// <summary>
    /// 移除数据对象的命令
    /// </summary>
    public class RemoveDataObjectCommand : ICommand
    {
        private readonly NodeManager nodeManager;
        private readonly string key;
        private object oldValue;
        private Type oldType;

        public RemoveDataObjectCommand(NodeManager nodeManager, string key)
        {
            this.nodeManager = nodeManager;
            this.key = key;
        }

        public void Execute()
        {
            // 保存旧值
            oldValue = nodeManager.GetDataObject(key);
            oldType = nodeManager.GetDataObjectType(key);

            // 移除数据
            nodeManager.RemoveDataObject(key);
        }

        public void Undo()
        {
            if (oldValue != null || oldType != null)
            {
                nodeManager.SetDataObject(key, oldValue, oldType);
            }
        }
    }

    /// <summary>
    /// 更新数据对象的命令
    /// </summary>
    public class UpdateDataObjectCommand : ICommand
    {
        private readonly NodeManager nodeManager;
        private readonly string key;
        private readonly object newValue;
        private readonly Type newType;
        private object oldValue;
        private Type oldType;

        public UpdateDataObjectCommand(NodeManager nodeManager, string key, object newValue, Type newType)
        {
            this.nodeManager = nodeManager;
            this.key = key;
            this.newValue = newValue;
            this.newType = newType;
        }

        public void Execute()
        {
            // 保存旧值
            oldValue = nodeManager.GetDataObject(key);
            oldType = nodeManager.GetDataObjectType(key);

            // 设置新值
            nodeManager.SetDataObject(key, newValue, newType);
        }

        public void Undo()
        {
            nodeManager.SetDataObject(key, oldValue, oldType);
        }
    }
} 