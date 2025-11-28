using System.Diagnostics;
using XLib.Node;

namespace XNode.SubSystem.NodeLibSystem.Define.Controls
{
    /// <summary>
    /// 智能循环节点
    /// 支持无限循环、动态延迟、去重机制等高级功能
    /// 专为游戏辅助场景(如打地鼠)优化
    /// </summary>
    public class SmartLoopNode : NodeBase
    {
        #region 字段

        private bool _isInfinite = true;
        private int _maxIterations = 1000;
        private int _baseDelay = 100;
        private int _idleDelay = 200;
        private bool _enableDeduplication = true;
        private int _deduplicationWindow = 500;
        private int _deduplicationDistance = 30;
        private bool _enableBreak = false;

        private int _currentIteration = 0;
        private readonly Stopwatch _stopwatch = new();
        private readonly List<ClickRecord> _recentClicks = new();

        #endregion

        #region 引脚组索引

        private const int PIN_GROUP_EXECUTE_IN = 0;
        private const int PIN_GROUP_IS_INFINITE = 1;
        private const int PIN_GROUP_MAX_ITERATIONS = 2;
        private const int PIN_GROUP_BASE_DELAY = 3;
        private const int PIN_GROUP_IDLE_DELAY = 4;
        private const int PIN_GROUP_ENABLE_DEDUP = 5;
        private const int PIN_GROUP_DEDUP_WINDOW = 6;
        private const int PIN_GROUP_DEDUP_DISTANCE = 7;
        private const int PIN_GROUP_BREAK_SIGNAL = 8;
        private const int PIN_GROUP_CURRENT_ITERATION = 9;
        private const int PIN_GROUP_ELAPSED_TIME = 10;
        private const int PIN_GROUP_LOOP_BODY = 11;
        private const int PIN_GROUP_COMPLETED = 12;

        #endregion

        #region 生命周期

        public override void Init()
        {
            SetViewProperty(
                new NodeColor { r = 180, g = 100, b = 255 },
                "CPU",
                "智能循环"
            );

            PinGroupList.Clear();

            PinGroupList.Add(new ExecutePinGroup(this, "Enter"));

            PinGroupList.Add(new DataPinGroup(this, "bool", "无限循环", "True")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 80
            });

            PinGroupList.Add(new DataPinGroup(this, "int", "最大迭代次数", "1000")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 100
            });

            PinGroupList.Add(new DataPinGroup(this, "int", "基础延迟(ms)", "100")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 100
            });

            PinGroupList.Add(new DataPinGroup(this, "int", "空闲延迟(ms)", "200")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 100
            });

            PinGroupList.Add(new DataPinGroup(this, "bool", "启用去重", "True")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 80
            });

            PinGroupList.Add(new DataPinGroup(this, "int", "去重时间窗口(ms)", "500")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 120
            });

            PinGroupList.Add(new DataPinGroup(this, "int", "去重距离阈值(px)", "30")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 120
            });

            PinGroupList.Add(new DataPinGroup(this, "bool", "中断信号", "False")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 80
            });

            PinGroupList.Add(new DataPinGroup(this, "int", "当前迭代", "0")
            {
                Writeable = false,
                Readable = true,
                CanInput = false,
                BoxWidth = 80
            });

            PinGroupList.Add(new DataPinGroup(this, "double", "运行时间(s)", "0")
            {
                Writeable = false,
                Readable = true,
                CanInput = false,
                BoxWidth = 100
            });

            PinGroupList.Add(new ActionPinGroup(this, "LoopBody"));
            PinGroupList.Add(new ActionPinGroup(this, "Completed"));

            InitPinGroup();
        }

        #endregion

        #region 节点执行

        protected override void ExecuteNode()
        {
            try
            {
                UpdateInputParameters();

                _currentIteration = 0;
                _stopwatch.Restart();
                _recentClicks.Clear();

                Console.WriteLine($"[SmartLoopNode] 智能循环开始 - 模式: {(_isInfinite ? "无限" : $"{_maxIterations}次")}, 基础延迟: {_baseDelay}ms, 去重: {(_enableDeduplication ? "启用" : "禁用")}");

                while (ShouldContinueLoop())
                {
                    _currentIteration++;
                    SetData(PIN_GROUP_CURRENT_ITERATION, _currentIteration.ToString());
                    SetData(PIN_GROUP_ELAPSED_TIME, (_stopwatch.ElapsedMilliseconds / 1000.0).ToString("F2"));

                    CleanupOldClickRecords();

                    var loopStartTime = DateTime.Now;
                    ExecuteLoopBody();
                    var loopEndTime = DateTime.Now;

                    var executionTime = (loopEndTime - loopStartTime).TotalMilliseconds;
                    ApplySmartDelay(executionTime);
                }

                _stopwatch.Stop();
                Console.WriteLine($"[SmartLoopNode] 智能循环完成 - 总迭代: {_currentIteration}, 总时间: {_stopwatch.ElapsedMilliseconds / 1000.0:F2}秒");

                ExecuteCompleted();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmartLoopNode] 执行智能循环节点时发生错误: {ex.Message}");
                throw;
            }
        }

        private void UpdateInputParameters()
        {
            UpdateData(PIN_GROUP_IS_INFINITE);
            UpdateData(PIN_GROUP_MAX_ITERATIONS);
            UpdateData(PIN_GROUP_BASE_DELAY);
            UpdateData(PIN_GROUP_IDLE_DELAY);
            UpdateData(PIN_GROUP_ENABLE_DEDUP);
            UpdateData(PIN_GROUP_DEDUP_WINDOW);
            UpdateData(PIN_GROUP_DEDUP_DISTANCE);

            if (!bool.TryParse(GetData(PIN_GROUP_IS_INFINITE), out _isInfinite))
                _isInfinite = true;

            if (!int.TryParse(GetData(PIN_GROUP_MAX_ITERATIONS), out _maxIterations))
                _maxIterations = 1000;

            if (!int.TryParse(GetData(PIN_GROUP_BASE_DELAY), out _baseDelay))
                _baseDelay = 100;

            if (!int.TryParse(GetData(PIN_GROUP_IDLE_DELAY), out _idleDelay))
                _idleDelay = 200;

            if (!bool.TryParse(GetData(PIN_GROUP_ENABLE_DEDUP), out _enableDeduplication))
                _enableDeduplication = true;

            if (!int.TryParse(GetData(PIN_GROUP_DEDUP_WINDOW), out _deduplicationWindow))
                _deduplicationWindow = 500;

            if (!int.TryParse(GetData(PIN_GROUP_DEDUP_DISTANCE), out _deduplicationDistance))
                _deduplicationDistance = 30;

            _maxIterations = Math.Max(1, _maxIterations);
            _baseDelay = Math.Max(0, _baseDelay);
            _idleDelay = Math.Max(0, _idleDelay);
            _deduplicationWindow = Math.Max(0, _deduplicationWindow);
            _deduplicationDistance = Math.Max(0, _deduplicationDistance);
        }

        private bool ShouldContinueLoop()
        {
            // 检查中断信号
            UpdateData(PIN_GROUP_BREAK_SIGNAL);
            if (bool.TryParse(GetData(PIN_GROUP_BREAK_SIGNAL), out _enableBreak) && _enableBreak)
            {
                Console.WriteLine("[SmartLoopNode] 检测到中断信号，退出循环");
                return false;
            }

            if (_isInfinite)
                return true;

            return _currentIteration < _maxIterations;
        }

        private void ExecuteLoopBody()
        {
            var loopBodyGroup = GetPinGroup<ActionPinGroup>(PIN_GROUP_LOOP_BODY);
            loopBodyGroup.Invoke();
        }

        private void ExecuteCompleted()
        {
            var completedGroup = GetPinGroup<ActionPinGroup>(PIN_GROUP_COMPLETED);
            completedGroup.Invoke();
        }

        private void ApplySmartDelay(double executionTime)
        {
            int delay = _baseDelay;

            // 如果循环体执行很快(小于10ms),使用空闲延迟
            if (executionTime < 10)
            {
                delay = _idleDelay;
            }

            if (delay > 0)
            {
                Thread.Sleep(delay);
            }
        }

        private void CleanupOldClickRecords()
        {
            if (!_enableDeduplication || _deduplicationWindow <= 0)
            {
                _recentClicks.Clear();
                return;
            }

            var cutoffTime = DateTime.Now.AddMilliseconds(-_deduplicationWindow);
            _recentClicks.RemoveAll(record => record.Timestamp < cutoffTime);
        }

        #endregion

        #region 去重公共方法

        /// <summary>
        /// 检查坐标是否应该被去重（供其他节点调用）
        /// </summary>
        public bool ShouldDeduplicate(int x, int y)
        {
            if (!_enableDeduplication)
                return false;

            var now = DateTime.Now;
            var cutoffTime = now.AddMilliseconds(-_deduplicationWindow);

            foreach (var record in _recentClicks)
            {
                if (record.Timestamp < cutoffTime)
                    continue;

                var distance = Math.Sqrt(
                    Math.Pow(record.X - x, 2) +
                    Math.Pow(record.Y - y, 2)
                );

                if (distance <= _deduplicationDistance)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 记录点击坐标（供其他节点调用）
        /// </summary>
        public void RecordClick(int x, int y)
        {
            if (!_enableDeduplication)
                return;

            _recentClicks.Add(new ClickRecord
            {
                X = x,
                Y = y,
                Timestamp = DateTime.Now
            });
        }

        #endregion

        #region 序列化

        public override string GetTypeString() => nameof(SmartLoopNode);

        public override Dictionary<string, string> GetParaDict()
        {
            return new Dictionary<string, string>
            {
                { "IsInfinite", GetData(PIN_GROUP_IS_INFINITE) },
                { "MaxIterations", GetData(PIN_GROUP_MAX_ITERATIONS) },
                { "BaseDelay", GetData(PIN_GROUP_BASE_DELAY) },
                { "IdleDelay", GetData(PIN_GROUP_IDLE_DELAY) },
                { "EnableDeduplication", GetData(PIN_GROUP_ENABLE_DEDUP) },
                { "DeduplicationWindow", GetData(PIN_GROUP_DEDUP_WINDOW) },
                { "DeduplicationDistance", GetData(PIN_GROUP_DEDUP_DISTANCE) }
            };
        }

        protected override void LoadParaDictInternal(Dictionary<string, string> paraDict)
        {
            if (paraDict.TryGetValue("IsInfinite", out string? isInfinite))
                SetData(PIN_GROUP_IS_INFINITE, isInfinite);

            if (paraDict.TryGetValue("MaxIterations", out string? maxIterations))
                SetData(PIN_GROUP_MAX_ITERATIONS, maxIterations);

            if (paraDict.TryGetValue("BaseDelay", out string? baseDelay))
                SetData(PIN_GROUP_BASE_DELAY, baseDelay);

            if (paraDict.TryGetValue("IdleDelay", out string? idleDelay))
                SetData(PIN_GROUP_IDLE_DELAY, idleDelay);

            if (paraDict.TryGetValue("EnableDeduplication", out string? enableDedup))
                SetData(PIN_GROUP_ENABLE_DEDUP, enableDedup);

            if (paraDict.TryGetValue("DeduplicationWindow", out string? dedupWindow))
                SetData(PIN_GROUP_DEDUP_WINDOW, dedupWindow);

            if (paraDict.TryGetValue("DeduplicationDistance", out string? dedupDistance))
                SetData(PIN_GROUP_DEDUP_DISTANCE, dedupDistance);
        }

        protected override NodeBase CloneNode() => new SmartLoopNode();

        #endregion

        #region 内部类

        private class ClickRecord
        {
            public int X { get; set; }
            public int Y { get; set; }
            public DateTime Timestamp { get; set; }
        }

        #endregion
    }
}
