namespace FlowNode.app.command
{
    public interface ICommand
    {
        void Execute();
        void Undo();
    }

    /// <summary>
    /// 组合命令，可以将多个命令组合成一个原子操作
    /// </summary>
    public class CompositeCommand : ICommand
    {
        private readonly System.Collections.Generic.List<ICommand> commands =
            new System.Collections.Generic.List<ICommand>();

        public bool HasCommands => commands.Count > 0;

        public void AddCommand(ICommand command)
        {
            commands.Add(command);
        }

        public void Clear()
        {
            commands.Clear();
        }

        public void Execute()
        {
            foreach (var command in commands)
                command.Execute();
        }

        public void Undo()
        {
            for (int i = commands.Count - 1; i >= 0; i--)
                commands[i].Undo();
        }
    }
}
