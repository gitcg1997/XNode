using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XLib.Node;

namespace XNode.SubSystem.ExecutionSystem
{
    /// <summary>
    /// 节点图执行器
    /// 负责执行节点图的逻辑流程
    /// </summary>
    public class NodeGraphExecutor
    {
        #region 字段

        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isExecuting;

        #endregion

        #region 属性

        /// <summary>
        /// 是否正在执行
        /// </summary>
        public bool IsExecuting => _isExecuting;

        #endregion

        #region 事件

        /// <summary>
        /// 执行开始事件
        /// </summary>
        public event Action? ExecutionStarted;

        /// <summary>
        /// 执行完成事件
        /// </summary>
        public event Action? ExecutionCompleted;

        /// <summary>
        /// 执行取消事件
        /// </summary>
        public event Action? ExecutionCancelled;

        /// <summary>
        /// 执行错误事件
        /// </summary>
        public event Action<Exception>? ExecutionError;

        /// <summary>
        /// 节点执行开始事件
        /// </summary>
        public event Action<NodeBase>? NodeExecutionStarted;

        /// <summary>
        /// 节点执行完成事件
        /// </summary>
        public event Action<NodeBase>? NodeExecutionCompleted;

        #endregion

        #region 公开方法

        /// <summary>
        /// 执行节点图
        /// </summary>
        /// <param name="nodes">节点列表</param>
        /// <param name="progress">进度报告</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task ExecuteAsync(
            List<NodeBase> nodes,
            IProgress<ExecutionProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (_isExecuting)
            {
                throw new InvalidOperationException("节点图正在执行中");
            }

            if (nodes == null || nodes.Count == 0)
            {
                throw new ArgumentException("节点列表为空", nameof(nodes));
            }

            try
            {
                _isExecuting = true;
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                Console.WriteLine($"[NodeGraphExecutor] 开始执行节点图，共 {nodes.Count} 个节点");

                // 查找起始节点
                var startNodes = nodes.Where(n =>
                    n.GetTypeString().Contains("Start") ||
                    n.GetTypeString().Equals("StartNode", StringComparison.OrdinalIgnoreCase)).ToList();

                if (startNodes.Count == 0)
                {
                    throw new InvalidOperationException("未找到起始节点（StartNode）。请添加一个开始节点作为执行入口。");
                }

                if (startNodes.Count > 1)
                {
                    Console.WriteLine("[NodeGraphExecutor] 警告: 发现多个起始节点，将使用第一个");
                }

                var startNode = startNodes[0];

                // 启用所有节点
                foreach (var node in nodes)
                {
                    node.Enable();
                }

                // 报告进度
                progress?.Report(new ExecutionProgress
                {
                    CurrentStep = 0,
                    TotalSteps = nodes.Count,
                    Message = "开始执行",
                    IsCompleted = false
                });

                // 触发执行开始事件
                ExecutionStarted?.Invoke();

                // 从起始节点开始执行
                await Task.Run(() =>
                {
                    try
                    {
                        // 使用递归执行方法，确保所有连接的节点都被执行
                        var visitedNodes = new HashSet<int>();
                        ExecuteNodeRecursive(startNode, visitedNodes, _cancellationTokenSource.Token);

                        progress?.Report(new ExecutionProgress
                        {
                            CurrentStep = nodes.Count,
                            TotalSteps = nodes.Count,
                            Message = "执行完成",
                            IsCompleted = true
                        });

                        // 触发执行完成事件
                        ExecutionCompleted?.Invoke();

                        Console.WriteLine("[NodeGraphExecutor] 节点图执行完成");
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("[NodeGraphExecutor] 节点图执行已取消");

                        progress?.Report(new ExecutionProgress
                        {
                            CurrentStep = 0,
                            TotalSteps = nodes.Count,
                            Message = "执行已取消",
                            IsCompleted = false,
                            IsCancelled = true
                        });

                        // 触发执行取消事件
                        ExecutionCancelled?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[NodeGraphExecutor] 执行节点图时发生错误: {ex.Message}");

                        progress?.Report(new ExecutionProgress
                        {
                            CurrentStep = 0,
                            TotalSteps = nodes.Count,
                            Message = $"执行出错: {ex.Message}",
                            IsCompleted = false,
                            Error = ex
                        });

                        // 触发执行错误事件
                        ExecutionError?.Invoke(ex);

                        throw;
                    }
                }, _cancellationTokenSource.Token);
            }
            finally
            {
                _isExecuting = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                // 禁用所有节点
                foreach (var node in nodes)
                {
                    node.Disable();
                }
            }
        }

        /// <summary>
        /// 同步执行节点图（简化版）
        /// </summary>
        /// <param name="nodes">节点列表</param>
        public void Execute(List<NodeBase> nodes)
        {
            if (_isExecuting)
            {
                throw new InvalidOperationException("节点图正在执行中");
            }

            if (nodes == null || nodes.Count == 0)
            {
                throw new ArgumentException("节点列表为空", nameof(nodes));
            }

            try
            {
                _isExecuting = true;
                Console.WriteLine($"[NodeGraphExecutor] 开始执行节点图，共 {nodes.Count} 个节点");

                // 查找起始节点
                var startNode = nodes.FirstOrDefault(n =>
                    n.GetTypeString().Contains("Start") ||
                    n.GetTypeString().Equals("StartNode", StringComparison.OrdinalIgnoreCase));

                if (startNode == null)
                {
                    throw new InvalidOperationException("未找到起始节点（StartNode）。请添加一个开始节点作为执行入口。");
                }

                // 启用所有节点
                foreach (var node in nodes)
                {
                    node.Enable();
                }

                // 触发执行开始事件
                ExecutionStarted?.Invoke();

                try
                {
                    // 使用递归执行方法，确保所有连接的节点都被执行
                    var visitedNodes = new HashSet<int>();
                    ExecuteNodeRecursive(startNode, visitedNodes, CancellationToken.None);

                    // 触发执行完成事件
                    ExecutionCompleted?.Invoke();

                    Console.WriteLine("[NodeGraphExecutor] 节点图执行完成");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NodeGraphExecutor] 执行节点图时发生错误: {ex.Message}");

                    // 触发执行错误事件
                    ExecutionError?.Invoke(ex);

                    throw;
                }
            }
            finally
            {
                _isExecuting = false;

                // 禁用所有节点
                foreach (var node in nodes)
                {
                    node.Disable();
                }
            }
        }

        /// <summary>
        /// 取消执行
        /// </summary>
        public void Cancel()
        {
            if (_isExecuting && _cancellationTokenSource != null)
            {
                Console.WriteLine("[NodeGraphExecutor] 请求取消节点图执行");
                _cancellationTokenSource.Cancel();
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 执行单个节点
        /// </summary>
        /// <remarks>
        /// 通过执行起始节点的输出引脚，触发整个节点图的执行流。
        /// XNode的执行机制基于引脚连接系统自动传播。
        /// </remarks>
        private void ExecuteNode(NodeBase node, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                Console.WriteLine($"[NodeGraphExecutor] 从节点开始执行: {node.Title} (ID: {node.ID})");

                // 触发节点执行开始事件
                Console.WriteLine($"[NodeGraphExecutor] 触发节点执行开始事件: {node.Title} (ID: {node.ID})");
                NodeExecutionStarted?.Invoke(node);

                node.Execute();

                // 触发节点执行完成事件
                Console.WriteLine($"[NodeGraphExecutor] 触发节点执行完成事件: {node.Title} (ID: {node.ID})");
                NodeExecutionCompleted?.Invoke(node);

                Console.WriteLine("[NodeGraphExecutor] 节点图执行流程完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NodeGraphExecutor] 执行节点 {node.Title} 时发生错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 递归执行节点及其连接的节点
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <param name="visitedNodes">已访问的节点集合，防止循环执行</param>
        /// <param name="cancellationToken">取消令牌</param>
        private void ExecuteNodeRecursive(NodeBase node, HashSet<int> visitedNodes, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 检查是否已访问过该节点，防止循环执行
            if (visitedNodes.Contains(node.ID))
            {
                Console.WriteLine($"[NodeGraphExecutor] 节点 {node.Title} (ID: {node.ID}) 已执行过，跳过");
                return;
            }

            // 标记为已访问
            visitedNodes.Add(node.ID);

            try
            {
                Console.WriteLine($"[NodeGraphExecutor] 开始执行节点: {node.Title} (ID: {node.ID})");

                // 触发节点执行开始事件
                NodeExecutionStarted?.Invoke(node);

                // 执行节点
                node.Execute();

                // 触发节点执行完成事件
                NodeExecutionCompleted?.Invoke(node);

                Console.WriteLine($"[NodeGraphExecutor] 节点执行完成: {node.Title} (ID: {node.ID})");

                // 获取该节点的所有输出引脚
                foreach (var pinGroup in node.PinGroupList)
                {
                    var outputPin = pinGroup.GetOutputPin();
                    if (outputPin != null)
                    {
                        // 检查该输出引脚是否有连接
                        if (outputPin.TargetList != null && outputPin.TargetList.Count > 0)
                        {
                            foreach (var connectedPin in outputPin.TargetList)
                            {
                                // 获取连接的节点
                                var connectedNode = connectedPin.OwnerGroup.OwnerNode;
                                if (connectedNode != null && !visitedNodes.Contains(connectedNode.ID))
                                {
                                    // 递归执行连接的节点
                                    ExecuteNodeRecursive(connectedNode, visitedNodes, cancellationToken);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NodeGraphExecutor] 执行节点 {node.Title} 时发生错误: {ex.Message}");
                throw;
            }
        }

        #endregion
    }

    /// <summary>
    /// 执行进度信息
    /// </summary>
    public class ExecutionProgress
    {
        /// <summary>
        /// 当前步骤
        /// </summary>
        public int CurrentStep { get; set; }

        /// <summary>
        /// 总步骤数
        /// </summary>
        public int TotalSteps { get; set; }

        /// <summary>
        /// 进度消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 是否完成
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// 是否取消
        /// </summary>
        public bool IsCancelled { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public Exception? Error { get; set; }

        /// <summary>
        /// 进度百分比 (0-100)
        /// </summary>
        public double ProgressPercentage =>
            TotalSteps > 0 ? (double)CurrentStep / TotalSteps * 100 : 0;
    }
}
