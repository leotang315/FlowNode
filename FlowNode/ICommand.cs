using FlowNode;
using FlowNode.node;
using System.Collections.Generic;
using System.Drawing;
public interface ICommand
{
    void Execute();
    void Undo();
}

public class CommandManager
{
    private Stack<ICommand> undoStack = new Stack<ICommand>();
    private Stack<ICommand> redoStack = new Stack<ICommand>();

    public void ExecuteCommand(ICommand command)
    {
        command.Execute();
        undoStack.Push(command);
        redoStack.Clear();
    }

    public void Undo()
    {
        if (undoStack.Count > 0)
        {
            var command = undoStack.Pop();
            command.Undo();
            redoStack.Push(command);
        }
    }

    public void Redo()
    {
        if (redoStack.Count > 0)
        {
            var command = redoStack.Pop();
            command.Execute();
            undoStack.Push(command);
        }
    }
}

// 示例命令类
public class AddNodeCommand : ICommand
{
    private NodeEditor editor;
    private NodeBase node;
    private Point position;

    public AddNodeCommand(NodeEditor editor, NodeBase node, Point position)
    {
        this.editor = editor;
        this.node = node;
        this.position = position;
    }

    public void Execute()
    {
        editor.AddNode(node, position);
    }

    public void Undo()
    {
        editor.RemoveNode(node);
    }
} 