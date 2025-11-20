using System.Collections.Generic;
using System.Linq;

namespace XNode.Command
{
    /// <summary>
    /// 组合命令 - 将多个命令组合为一个批量操作
    /// </summary>
    /// <remarks>
    /// 使用组合模式实现批量撤销/重做功能
    /// 例如：删除多个节点、批量移动节点等
    /// </remarks>
    public class CompositeCommand : CommandBase
    {
        private readonly List<ICommand> _commands = new List<ICommand>();
        private readonly string _groupDescription;

        public override string Description => _groupDescription;

        /// <summary>
        /// 创建组合命令
        /// </summary>
        /// <param name="description">组合命令的描述</param>
        public CompositeCommand(string description)
        {
            _groupDescription = description;
        }

        /// <summary>
        /// 添加子命令到组合命令
        /// </summary>
        /// <param name="command">要添加的命令</param>
        public void Add(ICommand command)
        {
            _commands.Add(command);
        }

        /// <summary>
        /// 批量添加子命令
        /// </summary>
        /// <param name="commands">要添加的命令集合</param>
        public void AddRange(IEnumerable<ICommand> commands)
        {
            _commands.AddRange(commands);
        }

        /// <summary>
        /// 获取子命令数量
        /// </summary>
        public int Count => _commands.Count;

        /// <summary>
        /// 执行所有子命令
        /// </summary>
        public override void Execute()
        {
            LogInfo($"开始执行组合命令: {Description} (包含 {_commands.Count} 个子命令)");

            foreach (var command in _commands)
            {
                command.Execute();
            }

            LogInfo($"组合命令执行完成: {Description}");
        }

        /// <summary>
        /// 撤销所有子命令（逆序执行）
        /// </summary>
        public override void Undo()
        {
            LogInfo($"开始撤销组合命令: {Description} (包含 {_commands.Count} 个子命令)");

            // 逆序撤销
            for (int i = _commands.Count - 1; i >= 0; i--)
            {
                _commands[i].Undo();
            }

            LogInfo($"组合命令撤销完成: {Description}");

            // 通知UI更新
            NotifyUIUpdate();
        }

        /// <summary>
        /// 重做所有子命令
        /// </summary>
        public override void Redo()
        {
            LogInfo($"开始重做组合命令: {Description} (包含 {_commands.Count} 个子命令)");

            foreach (var command in _commands)
            {
                command.Redo();
            }

            LogInfo($"组合命令重做完成: {Description}");

            // 通知UI更新
            NotifyUIUpdate();
        }

        /// <summary>
        /// 获取所有子命令的描述（用于调试）
        /// </summary>
        public string GetDetailedDescription()
        {
            var descriptions = _commands.Select(cmd => cmd.Description);
            return $"{Description}\n  - {string.Join("\n  - ", descriptions)}";
        }
    }
}
