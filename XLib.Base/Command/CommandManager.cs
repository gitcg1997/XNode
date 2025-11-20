using System.Collections.Generic;

namespace XLib.Base.Command
{
    /// <summary>
    /// 撤销/重做管理器
    /// </summary>
    public class CommandManager
    {
        private readonly Stack<ICommand> _commandStack = new Stack<ICommand>();  // 已执行的命令栈
        private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();    // 撤销命令栈
        private const int _maxCommands = 50;  // 最大命令数量，防止内存溢出

        private static CommandManager _instance;
        public static CommandManager Instance => _instance ??= new CommandManager();

        /// <summary>
        /// 命令状态改变事件（执行、撤销、重做后触发）
        /// </summary>
        public event Action? CommandStatusChanged;

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="command">要执行的命令</param>
        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            _commandStack.Push(command);
            _undoStack.Clear();  // 执行新命令时清空撤销栈

            // 如果命令栈超过最大数量，移除最早的命令
            if (_commandStack.Count > _maxCommands)
            {
                var tempStack = new Stack<ICommand>();
                for (int i = 0; i < _maxCommands - 1; i++)
                {
                    tempStack.Push(_commandStack.Pop());
                }
                _commandStack.Clear();
                while (tempStack.Count > 0)
                {
                    _commandStack.Push(tempStack.Pop());
                }
            }
            
            // 触发命令状态改变事件
            CommandStatusChanged?.Invoke();
        }

        /// <summary>
        /// 撤销最后一个命令
        /// </summary>
        public bool Undo()
        {
            if (_undoStack.Count >= _maxCommands || _commandStack.Count == 0)
                return false;

            ICommand command = _commandStack.Pop();
            command.Undo();
            _undoStack.Push(command);
            
            // 触发命令状态改变事件
            CommandStatusChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 重做最后一个撤销的命令
        /// </summary>
        public bool Redo()
        {
            if (_commandStack.Count >= _maxCommands || _undoStack.Count == 0)
                return false;

            ICommand command = _undoStack.Pop();
            command.Redo();
            _commandStack.Push(command);
            
            // 触发命令状态改变事件
            CommandStatusChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 检查是否可以撤销
        /// </summary>
        public bool CanUndo => _commandStack.Count > 0;

        /// <summary>
        /// 检查是否可以重做
        /// </summary>
        public bool CanRedo => _undoStack.Count > 0;

        /// <summary>
        /// 获取撤销命令的描述
        /// </summary>
        public string UndoDescription
        {
            get
            {
                if (_commandStack.Count == 0) return string.Empty;
                return _commandStack.Peek().Description;
            }
        }

        /// <summary>
        /// 获取重做命令的描述
        /// </summary>
        public string RedoDescription
        {
            get
            {
                if (_undoStack.Count == 0) return string.Empty;
                return _undoStack.Peek().Description;
            }
        }

        /// <summary>
        /// 清空所有命令
        /// </summary>
        public void Clear()
        {
            _commandStack.Clear();
            _undoStack.Clear();
        }
    }
}