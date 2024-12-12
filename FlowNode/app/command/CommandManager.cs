using System;
using System.Collections.Generic;

namespace FlowNode.app.command
{
    /// <summary>
    /// 命令管理器，负责管理命令的执行、撤销和重做
    /// </summary>
    public class CommandManager
    {
        private readonly Stack<ICommand> undoStack = new Stack<ICommand>();
        private readonly Stack<ICommand> redoStack = new Stack<ICommand>();
        private const int MaxUndoCount = 100; // 最大撤销次数
        private CommandGroup currentGroup = null;

        /// <summary>
        /// 是否可以撤销
        /// </summary>
        public bool CanUndo => undoStack.Count > 0;

        /// <summary>
        /// 是否可以重做
        /// </summary>
        public bool CanRedo => redoStack.Count > 0;

        /// <summary>
        /// 执行命令
        /// </summary>
        public void ExecuteCommand(ICommand command)
        {
            try
            {
                if (currentGroup != null)
                {
                    // 在命令组中执行命令
                    currentGroup.TryExecuteCommand(command);
                }
                else
                {
                    // 正常执行单个命令
                    command.Execute();
                    undoStack.Push(command);
                    redoStack.Clear();

                    // 限制撤销栈的大小
                    while (undoStack.Count > MaxUndoCount)
                    {
                        undoStack.Pop();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Command execution failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 撤销操作
        /// </summary>
        public void Undo()
        {
            if (!CanUndo)
                return;

            try
            {
                var command = undoStack.Pop();
                command.Undo();
                redoStack.Push(command);
            }
            catch (Exception ex)
            {
                // 如果撤销失败，尝试恢复到之前的状态
                if (redoStack.Count > 0)
                {
                    var command = redoStack.Pop();
                    undoStack.Push(command);
                }
                throw new InvalidOperationException($"Undo operation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 重做操作
        /// </summary>
        public void Redo()
        {
            if (!CanRedo)
                return;

            try
            {
                var command = redoStack.Pop();
                command.Execute();
                undoStack.Push(command);
            }
            catch (Exception ex)
            {
                // 如果重做失败，尝试恢复到之前的状态
                if (undoStack.Count > 0)
                {
                    var command = undoStack.Pop();
                    redoStack.Push(command);
                }
                throw new InvalidOperationException($"Redo operation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 清空所有命令历史
        /// </summary>
        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
        }

        /// <summary>
        /// 开始一个命令组
        /// </summary>
        public IDisposable BeginCommandGroup()
        {
            currentGroup = new CommandGroup(this);
            return currentGroup;
        }

        private class CommandGroup : IDisposable
        {
            private readonly CommandManager manager;
            private readonly CompositeCommand compositeCommand;
            private readonly List<ICommand> executedCommands;
            private bool isRollingBack;

            public CommandGroup(CommandManager manager)
            {
                this.manager = manager;
                this.compositeCommand = new CompositeCommand();
                this.executedCommands = new List<ICommand>();
                this.isRollingBack = false;
            }

            public void TryExecuteCommand(ICommand command)
            {
                try
                {
                    // 执行新命令
                    command.Execute();
                    // 执行成功后添加到已执行列表和组合命令中
                    executedCommands.Add(command);
                    compositeCommand.AddCommand(command);
                }
                catch (Exception)
                {
                    // 如果执行失败，回滚所有已执行的命令
                    RollbackExecutedCommands();
                    throw;
                }
            }

            private void RollbackExecutedCommands()
            {
                if (isRollingBack) return; // 防止回滚过程中的异常导致递归

                isRollingBack = true;
                try
                {
                    // 反向遍历已执行的命令并撤销
                    for (int i = executedCommands.Count - 1; i >= 0; i--)
                    {
                        try
                        {
                            executedCommands[i].Undo();
                        }
                        catch (Exception ex)
                        {
                            // 记录回滚过程中的错误，但继续回滚其他命令
                            System.Diagnostics.Debug.WriteLine($"Error during rollback: {ex.Message}");
                        }
                    }
                }
                finally
                {
                    isRollingBack = false;
                    executedCommands.Clear();
                    compositeCommand.Clear();
                }
            }

            public void Dispose()
            {
                try
                {
                    if (compositeCommand.HasCommands)
                    {
                        // 只有所有命令都成功执行，才将组合命令添加到撤销栈
                        manager.undoStack.Push(compositeCommand);
                        manager.redoStack.Clear();
                    }
                }
                finally
                {
                    manager.currentGroup = null;
                }
            }
        }
    }
} 