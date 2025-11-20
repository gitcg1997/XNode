using System.Collections.Generic;
using XLib.Node;

namespace XNode.Command
{
    /// <summary>
    /// 撤销/重做管理器 - 优化后的内存管理版本
    /// </summary>
    public class CommandManager
    {
        private readonly LinkedList<ICommand> _history = new LinkedList<ICommand>();  // 命令历史
        private LinkedListNode<ICommand>? _current = null;  // 当前命令节点
        private const int _maxCommands = 50;  // 最大命令数量，防止内存溢出

        private readonly List<ICommandInterceptor> _interceptors = new List<ICommandInterceptor>();  // 命令拦截器列表

        private static CommandManager? _instance;
        private static readonly object _lock = new object();

        // 线程安全的单例模式
        public static CommandManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new CommandManager();
                            // 默认注册添加节点位置确认拦截器
                            _instance.RegisterInterceptor(new AddNodePositionInterceptor(_instance));
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 命令状态改变事件（执行、撤销、重做后触发）
        /// </summary>
        public event Action? CommandStatusChanged;

        /// <summary>
        /// 注册命令拦截器
        /// </summary>
        /// <param name="interceptor">拦截器实例</param>
        public void RegisterInterceptor(ICommandInterceptor interceptor)
        {
            if (!_interceptors.Contains(interceptor))
            {
                _interceptors.Add(interceptor);
                MainWindow.LogManager.LogInfo($"命令拦截器已注册: {interceptor.GetType().Name}");
            }
        }

        /// <summary>
        /// 移除命令拦截器
        /// </summary>
        /// <param name="interceptor">拦截器实例</param>
        public void UnregisterInterceptor(ICommandInterceptor interceptor)
        {
            if (_interceptors.Remove(interceptor))
            {
                MainWindow.LogManager.LogInfo($"命令拦截器已移除: {interceptor.GetType().Name}");
            }
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="command">要执行的命令</param>
        public void ExecuteCommand(ICommand command)
        {
            // 执行拦截器的前置逻辑
            foreach (var interceptor in _interceptors)
            {
                if (!interceptor.OnBeforeExecute(command))
                {
                    MainWindow.LogManager.LogWarning($"命令执行被拦截器阻止: {command.Description}");
                    return; // 拦截器拒绝执行
                }
            }

            // 尝试与最后一个命令合并
            if (_current != null && _current.Value is IMergeable mergeable)
            {
                if (mergeable.CanMergeWith(command))
                {
                    // 合并命令：替换当前命令为合并后的命令
                    var mergedCommand = mergeable.MergeWith(command);

                    // 执行新命令（从旧位置到新位置的完整移动）
                    command.Execute();

                    // 替换历史中的当前命令
                    _current.Value = mergedCommand;

                    MainWindow.LogManager.LogInfo($"命令已合并: {mergedCommand.Description}");

                    // 执行拦截器的后置逻辑
                    foreach (var interceptor in _interceptors)
                    {
                        interceptor.OnAfterExecute(mergedCommand);
                    }

                    // 触发命令状态改变事件
                    CommandStatusChanged?.Invoke();
                    return;
                }
            }

            // 无法合并，正常执行
            command.Execute();

            // 如果当前不在历史末尾,删除后面的所有命令
            if (_current != null)
            {
                var nodeToRemove = _current.Next;
                while (nodeToRemove != null)
                {
                    var next = nodeToRemove.Next;
                    _history.Remove(nodeToRemove);
                    nodeToRemove = next;
                }
            }

            // 添加新命令到历史
            _history.AddLast(command);
            _current = _history.Last;

            // 如果命令数量超过最大限制,移除最早的命令
            while (_history.Count > _maxCommands)
            {
                _history.RemoveFirst();
                // 如果移除的是当前节点,重置当前指针
                if (_current?.List == null)
                {
                    _current = _history.Last;
                }
            }

            // 执行拦截器的后置逻辑
            foreach (var interceptor in _interceptors)
            {
                interceptor.OnAfterExecute(command);
            }

            // 触发命令状态改变事件
            CommandStatusChanged?.Invoke();
        }

        /// <summary>
        /// 撤销最后一个命令
        /// </summary>
        public bool Undo()
        {
            if (_current == null)
                return false;

            _current.Value.Undo();
            _current = _current.Previous;

            // 触发命令状态改变事件
            CommandStatusChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 重做最后一个撤销的命令
        /// </summary>
        public bool Redo()
        {
            // 如果当前为空,从头开始
            var nodeToRedo = _current?.Next ?? _history.First;

            if (nodeToRedo == null)
                return false;

            nodeToRedo.Value.Redo();
            _current = nodeToRedo;

            // 触发命令状态改变事件
            CommandStatusChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 检查是否可以撤销
        /// </summary>
        public bool CanUndo => _current != null;

        /// <summary>
        /// 检查是否可以重做
        /// </summary>
        public bool CanRedo => _current?.Next != null || (_current == null && _history.Count > 0);

        /// <summary>
        /// 获取撤销命令的描述
        /// </summary>
        public string UndoDescription
        {
            get
            {
                if (_current == null) return string.Empty;
                return _current.Value.Description;
            }
        }

        /// <summary>
        /// 获取重做命令的描述
        /// </summary>
        public string RedoDescription
        {
            get
            {
                var nodeToRedo = _current?.Next ?? _history.First;
                if (nodeToRedo == null) return string.Empty;
                return nodeToRedo.Value.Description;
            }
        }

        /// <summary>
        /// 清空所有命令
        /// </summary>
        public void Clear()
        {
            _history.Clear();
            _current = null;
        }

        /// <summary>
        /// 尝试更新最后一个添加节点命令的位置
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="newPosition">新位置</param>
        /// <returns>是否成功更新</returns>
        public bool TryUpdateLastAddNodePosition(int nodeId, NodePoint newPosition)
        {
            if (_current != null)
            {
                if (_current.Value is AddNodeCommand addCommand &&
                    addCommand.Node.ID == nodeId &&
                    addCommand.IsPending)
                {
                    return addCommand.TryUpdatePosition(newPosition);
                }
            }
            return false;
        }

        /// <summary>
        /// 确认最后一个添加节点命令的位置
        /// </summary>
        public void ConfirmLastAddNodePosition()
        {
            if (_current != null)
            {
                if (_current.Value is AddNodeCommand addCommand)
                {
                    addCommand.ConfirmPosition();
                }
            }
        }

        /// <summary>
        /// 获取命令栈状态（用于调试）
        /// </summary>
        public string GetCommandStackStatus()
        {
            var commandDescriptions = new List<string>();
            foreach (var command in _history)
            {
                commandDescriptions.Add(command.Description);
            }

            return $"命令历史数量: {_history.Count}, 当前位置: {GetCurrentIndex()}, 命令: [{string.Join(", ", commandDescriptions)}]";
        }

        /// <summary>
        /// 获取当前命令在历史中的位置索引（用于调试）
        /// </summary>
        private int GetCurrentIndex()
        {
            if (_current == null) return -1;

            int index = 0;
            var node = _history.First;
            while (node != null)
            {
                if (node == _current) return index;
                index++;
                node = node.Next;
            }
            return -1;
        }
    }
}