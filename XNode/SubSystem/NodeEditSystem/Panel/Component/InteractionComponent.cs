using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using XLib.Base;
using XLib.Base.UIComponent;
using XLib.Base.VirtualDisk;
using XLib.Node;
using XNode.SubSystem.NodeEditSystem.Control;
using XNode.SubSystem.NodeEditSystem.Define;
using XNode.SubSystem.ProjectSystem;
using XNode.SubSystem.ResourceSystem;
using XNode.SubSystem.WindowSystem;

namespace XNode.SubSystem.NodeEditSystem.Panel.Component
{
    /// <summary>
    /// 交互组件：处理键盘、鼠标事件
    /// </summary>
    public class InteractionComponent : Component<EditPanel>
    {
        #region 属性

        public NodeView? HoveredCard => _hoveredNodeView;

        #endregion

        #region 生命周期

        protected override void Init()
        {
            _tool = new SelectTool(this);
            _tool.Init();
            _hoverToolBar = new HoverToolBar
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            _host.LayerToolBarCanvas.Children.Add(_hoverToolBar);
            _hoverToolBar.UpdateLayout();
            _hoverToolBar.Visibility = Visibility.Collapsed;
            _hoverToolBar.Init();
            _hoverToolBar.ToolClick += HoverToolBar_ToolClick;

            // 监听清除拖放标志事件
            XNode.SubSystem.EventSystem.EM.Instance.Add(XNode.SubSystem.EventSystem.EventType.ClearDropFlags, OnClearDropFlags);
        }

        protected override void Enable()
        {
            _host.OperateAreaGrid.MouseMove += OperateArea_MouseMove;
            _host.OperateAreaGrid.MouseDown += OperateArea_MouseDown;
            _host.OperateAreaGrid.MouseUp += OperateArea_MouseUp;
        }

        protected override void Reset()
        {
            ResetComponent();
        }

        protected override void Disable()
        {
            ResetComponent();
            _host.OperateAreaGrid.MouseMove -= OperateArea_MouseMove;
            _host.OperateAreaGrid.MouseDown -= OperateArea_MouseDown;
            _host.OperateAreaGrid.MouseUp -= OperateArea_MouseUp;
            _hoverToolBar.Visibility = Visibility.Collapsed;
        }

        protected override void Remove()
        {
            ResetComponent();
            _host.OperateAreaGrid.MouseMove -= OperateArea_MouseMove;
            _host.OperateAreaGrid.MouseDown -= OperateArea_MouseDown;
            _host.OperateAreaGrid.MouseUp -= OperateArea_MouseUp;
            _hoverToolBar.ToolClick -= HoverToolBar_ToolClick;
            _host.LayerToolBarCanvas.Children.Remove(_hoverToolBar);
            _hoverToolBar = null;
        }

        #endregion

        #region 公开方法

        public void HandleKeyDown(KeyEventArgs e)
        {
            // 确认任何待确认的添加节点命令
            var coreEditer = GetCoreEditer();
            coreEditer?.CommandManager.ConfirmLastAddNodePosition();

            if (e.Key == Key.Delete)
            {
                List<NodeView> cardList = GetComponent<CardComponent>().SelectedCardList;
                if (cardList.Count == 0) return;

                bool? delete = WM.ShowAsk("确定删除选中节点吗？", "确定", false, TipLevel.Warning);
                if (delete != true) return;

                DeleteNode(cardList);
            }
            else if (e.Key == Key.Space)
            {
                List<NodeView> cardList = GetComponent<CardComponent>().SelectedCardList;
                if (cardList.Count == 0) return;

                foreach (NodeView card in cardList)
                {
                    if (card.NodeInstance.State == NodeState.Disable) card.NodeInstance.Start();
                    else card.NodeInstance.Stop();
                }
            }
        }

        /// <summary>
        /// 处理放下
        /// </summary>
        public void HandleDrop(List<ITreeItem> itemList)
        {
            MainWindow.LogManager.LogInfo($"开始处理拖放操作, 拖放项数量: {itemList.Count}");

            // 获取屏幕坐标
            var screenPoint = Mouse.GetPosition(_host.OperateAreaGrid);
            MainWindow.LogManager.LogInfo($"拖放屏幕坐标: ({screenPoint.X:F2}, {screenPoint.Y:F2})");

            // 获取节点组件
            var component = GetComponent<NodeComponent>();
            // 获取核心编辑器以执行命令
            var coreEditer = GetCoreEditer();
            // 放下节点
            bool added = false;
            foreach (var item in itemList)
            {
                if (item is File file && file.Instance is NodeType nodeType)
                {
                    MainWindow.LogManager.LogInfo($"正在添加节点: {file.Name}, 类型: {nodeType.ToString()}");

                    if (coreEditer != null)
                    {
                        // 创建节点实例
                        var nodeInstance = nodeType.NewInstance();
                        nodeInstance.PinBreaked += (start, end) => { /* 空实现,将在 LoadNode 中设置 */ };
                        nodeInstance.TypeID = file.ID;

                        // 获取世界坐标
                        var worldPoint = GetComponent<DrawingComponent>().ScreenToWorld(screenPoint);
                        // 设置节点编号、坐标
                        nodeInstance.ID = component.GetNextNodeId();
                        nodeInstance.Point = new NodePoint((int)worldPoint.X, (int)worldPoint.Y);

                        MainWindow.LogManager.LogInfo($"节点世界坐标: ({worldPoint.X:F2}, {worldPoint.Y:F2}), 节点ID: {nodeInstance.ID}");

                        // 执行添加节点命令,以支持撤销/重做
                        coreEditer.ExecuteAddNodeCommand(nodeInstance);
                        MainWindow.LogManager.LogInfo($"[调试] 拖放添加节点后 - {coreEditer.CommandManager.GetCommandStackStatus()}");

                        // 标记刚刚拖放了节点，用于合并后续的移动操作
                        _justDroppedNode = true;
                        _droppedNodeId = nodeInstance.ID;

                        added = true;
                    }
                    else
                    {
                        MainWindow.LogManager.LogWarning("无法获取核心编辑器,使用原始方法添加节点");

                        // 如果无法获取核心编辑器,则使用原始方法
                        NodeView? nodeView = component.DropNode(file.ID, nodeType, screenPoint);
                        if (nodeView != null)
                        {
                            nodeView.NodeBackMouseEnter = NodeBack_MouseEnter;
                            nodeView.NodeBackMouseLeave = NodeBack_MouseLeave;
                            nodeView.PinGroupListChanged = PinGroupListChanged;
                            nodeView.NodeChanged = NodeChanged;
                            added = true;
                        }
                    }
                }
                else
                {
                    MainWindow.LogManager.LogWarning($"拖放项不是节点类型: {item?.GetType()?.Name ?? "null"}");
                }
            }

            if (added)
            {
                ProjectManager.Instance.Saved = false;
                MainWindow.LogManager.LogInfo("节点添加成功, 项目状态已标记为未保存");
            }
            else
            {
                MainWindow.LogManager.LogWarning("未成功添加任何节点");
            }
        }

        /// <summary>
        /// 监听节点卡片
        /// </summary>
        public void ListenNodeCard(NodeView nodeView)
        {
            nodeView.NodeBackMouseEnter = NodeBack_MouseEnter;
            nodeView.NodeBackMouseLeave = NodeBack_MouseLeave;
            nodeView.PinGroupListChanged = PinGroupListChanged;
            nodeView.NodeChanged = NodeChanged;
        }

        /// <summary>
        /// 更新全部引脚图标
        /// </summary>
        public void UpdateAllPinIcon()
        {
            foreach (var card in GetComponent<CardComponent>().AllCard) card.UpdateAllPinIcon();
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取核心编辑器
        /// </summary>
        private CoreEditer? GetCoreEditer()
        {
            // 从EditPanel向上查找CoreEditer
            DependencyObject current = _host;
            while (current != null)
            {
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
                if (current is CoreEditer coreEditer)
                {
                    return coreEditer;
                }
            }
            return null;
        }

        /// <summary>
        /// 捕获操作图层
        /// </summary>
        public void CaptureOperationLayer() => _host.OperateAreaGrid.CaptureMouse();

        /// <summary>
        /// 释放操作图层
        /// </summary>
        public void ReleaseOperationLayer() => _host.OperateAreaGrid.ReleaseMouseCapture();

        /// <summary>
        /// 获取鼠标命中区域
        /// </summary>
        public MouseHitedArea GetHitedArea()
        {
            // 如果悬停节点不为空
            if (_hoveredNodeView != null)
                return _hoveredNodeView.HoveredPin == null ? MouseHitedArea.Node : MouseHitedArea.Pin;
            // 如果悬停于连接线
            else if (GetComponent<DrawingComponent>().HoveredConnectLine != null) return MouseHitedArea.ConnectLine;
            // 返回空白区域
            return MouseHitedArea.Space;
        }

        /// <summary>
        /// 清除悬停框
        /// </summary>
        public void ClearHoverBox()
        {
            GetComponent<DrawingComponent>().HoverBox = null;
            GetComponent<DrawingComponent>().UpdateHoverBox();
        }

        /// <summary>
        /// 处理鼠标移动
        /// </summary>
        public void HandleMouseMove()
        {
            // 重置光标
            _tool.Cursor = CursorManager.Instance.Select;
            // 悬停在引脚上：切换光标
            if (_hoveredNodeView != null && _hoveredNodeView.HoveredPin != null)
                _tool.Cursor = CursorManager.Instance.Cross;
            // 设置光标
            _host.OperateAreaGrid.Cursor = _tool.Cursor;

            // 更新悬停连接线
            GetComponent<DrawingComponent>().UpdateHoveredConnectLine(Mouse.GetPosition(_host.OperateAreaGrid));
        }

        /// <summary>
        /// 移除节点焦点
        /// </summary>
        public void RemoveNodeFocus() => _host.OperateAreaGrid.Focus();

        /// <summary>
        /// 启动并执行节点
        /// </summary>
        public void StartAndExecute()
        {
            _hoveredNodeView.NodeInstance.Start();
            _hoveredNodeView.NodeInstance.Execute();
        }



        #region 选择

        public bool CurrentNodeSelected() =>
            GetComponent<CardComponent>().SelectedCardList.Contains(_hoveredNodeView);

        public void SetTop() => GetComponent<CardComponent>().SetTop(_hoveredNodeView);

        public void AddSelect()
        {
            MainWindow.LogManager.LogInfo($"[调试] 添加选择 - 悬停节点: {_hoveredNodeView?.NodeInstance.Title ?? "null"}");
            GetComponent<CardComponent>().AddSelect(_hoveredNodeView);
            GetComponent<DrawingComponent>().UpdateSelectedBox();
            UpdateHoverToolBar();
            UpdatePropertyPanel();
        }

        /// <summary>
        /// 添加指定节点到选择列表
        /// </summary>
        public void AddSelect(NodeView nodeView)
        {
            GetComponent<CardComponent>().AddSelect(nodeView);
            GetComponent<DrawingComponent>().UpdateSelectedBox();
            UpdateHoverToolBar();
            UpdatePropertyPanel();
        }

        public void RemoveSelect()
        {
            GetComponent<CardComponent>().RemoveSelect(_hoveredNodeView);
            GetComponent<DrawingComponent>().UpdateSelectedBox();
            UpdateHoverToolBar();
            UpdatePropertyPanel();
        }

        public void ClearSelect()
        {
            // 注意：不在这里确认节点位置，因为拖放后选中节点时会调用此方法
            // 确认逻辑移到其他更合适的地方

            GetComponent<CardComponent>().ClearSelect();
            GetComponent<DrawingComponent>().UpdateSelectedBox();
            UpdateHoverToolBar();
            UpdatePropertyPanel();
        }

        #endregion

        #region 选框

        public void BeginDrawSelectBox() => _mouseDown = Mouse.GetPosition(_host.OperateAreaGrid);

        public void CancelDrawSelectBox() => GetComponent<DrawingComponent>().ClearSelectBox();

        public void DrawSelectBox() =>
            GetComponent<DrawingComponent>().UpdateSelectBox(_mouseDown, Mouse.GetPosition(_host.OperateAreaGrid));

        public void EndDrawSelectBox()
        {
            // 清除选框
            GetComponent<DrawingComponent>().ClearSelectBox();
            // 获取选框区域与选择方式
            Rect rect = GetComponent<DrawingComponent>().GetSelectBoxRect();
            SelectType type = GetComponent<DrawingComponent>().GetSelectType();
            // 选择节点视图
            foreach (var card in GetComponent<CardComponent>().AllCard)
            {
                Rect nodeRect = card.GetHittableRect();
                switch (type)
                {
                    case SelectType.Box:
                        if (rect.Contains(nodeRect)) GetComponent<CardComponent>().AddSelect(card);
                        break;
                    case SelectType.Cross:
                        if (rect.IntersectsWith(nodeRect)) GetComponent<CardComponent>().AddSelect(card);
                        break;
                }
            }
            // 更新选中框
            GetComponent<DrawingComponent>().UpdateSelectedBox();
            // 更新工具栏
            UpdateHoverToolBar();
            // 更新属性面板
            UpdatePropertyPanel();
        }

        #endregion

        #region 拖动节点

        public void BeginDragNode()
        {
            _host.OperateAreaGrid.Cursor = CursorManager.Instance.SelectAndMove;
            _mouseDown = Mouse.GetPosition(_host.OperateAreaGrid);
        }

        public void CancelDragNode()
        {
            _host.OperateAreaGrid.Cursor = _tool.Cursor;
        }

        public void DragNode()
        {
            // 更新当前坐标
            _mousePoint = Mouse.GetPosition(_host.OperateAreaGrid);
            // 计算偏移
            Point offset = new Point(_mousePoint.X - _mouseDown.X, _mousePoint.Y - _mouseDown.Y);
            // 对齐网格
            offset.X = Math.Round(offset.X / 10) * 10;
            offset.Y = Math.Round(offset.Y / 10) * 10;
            // 获取世界中心
            Point center = GetComponent<DrawingComponent>().WorldCenter;
            // 设置节点偏移并更新坐标
            foreach (var card in GetComponent<CardComponent>().SelectedCardList)
            {
                card.SetOffset(new Point(offset.X, offset.Y));
                Canvas.SetLeft(card, center.X + card.Point.X - 12);
                Canvas.SetTop(card, center.Y + card.Point.Y - 1);
            }
            // 更新选中框、连接线
            GetComponent<DrawingComponent>().UpdateSelectedBox();
            GetComponent<DrawingComponent>().UpdateConnectLine();
            // 更新悬浮工具栏
            UpdateHoverToolBar();

            ProjectManager.Instance.Saved = false;
        }

        public void EndDragNode()
        {
            _host.OperateAreaGrid.Cursor = _tool.Cursor;

            // 获取核心编辑器以执行命令
            var coreEditer = GetCoreEditer();
            if (coreEditer != null)
            {
                // 智能节点移动处理
                MainWindow.LogManager.LogInfo($"[调试] EndDragNode - _justDroppedNode: {_justDroppedNode}, SelectedCount: {GetComponent<CardComponent>().SelectedCardList.Count}, _droppedNodeId: {_droppedNodeId}");

                bool smartUpdateSucceeded = false; // 跟踪智能更新是否成功

                // 为每个移动的节点处理位置更新
                foreach (var card in GetComponent<CardComponent>().SelectedCardList)
                {
                    // 保存移动前的节点位置(在应用偏移前)
                    var oldPosition = new XLib.Node.NodePoint(card.NodeInstance.Point.X, card.NodeInstance.Point.Y);

                    // 应用偏移，这将更新节点的真实坐标
                    card.ApplyOffset();

                    // 获取移动后的位置
                    var newPosition = new XLib.Node.NodePoint(card.NodeInstance.Point.X, card.NodeInstance.Point.Y);

                    // 只有在位置真正发生变化时才处理
                    if (oldPosition.X != newPosition.X || oldPosition.Y != newPosition.Y)
                    {
                        // 尝试智能更新最后一个添加节点命令的位置（如果是拖放后的移动）
                        if (_justDroppedNode && _droppedNodeId == card.NodeInstance.ID)
                        {
                            bool updated = coreEditer.CommandManager.TryUpdateLastAddNodePosition(card.NodeInstance.ID, newPosition);
                            if (updated)
                            {
                                MainWindow.LogManager.LogInfo($"智能更新拖放节点位置: {card.NodeInstance.Title} -> ({newPosition.X}, {newPosition.Y})");
                                smartUpdateSucceeded = true;
                                continue; // 跳过创建移动命令
                            }
                            else
                            {
                                MainWindow.LogManager.LogInfo($"智能更新失败，节点已被确认: {card.NodeInstance.Title}");
                                // 智能更新失败，清除拖放标志
                                _justDroppedNode = false;
                                _droppedNodeId = -1;
                            }
                        }
                        // 注意：移动其他节点时不清除拖放标志，让拖放的节点仍然可以进行智能更新

                        // 创建普通移动命令
                        // 先恢复到旧位置
                        card.NodeInstance.Point.X = oldPosition.X;
                        card.NodeInstance.Point.Y = oldPosition.Y;

                        // 执行移动节点命令，以支持撤销/重做
                        coreEditer.ExecuteMoveNodeCommand(card.NodeInstance, oldPosition, newPosition);
                        MainWindow.LogManager.LogInfo($"执行移动节点命令: {card.NodeInstance.Title} 从 ({oldPosition.X},{oldPosition.Y}) 到 ({newPosition.X},{newPosition.Y})");
                    }
                }

                // 不再清除拖放标志，让智能合并系统持续工作
                // 拖放标志只会在执行其他类型命令时被CommandManager自动清除
            }
            else
            {
                // 如果无法获取核心编辑器，则直接应用偏移
                foreach (var card in GetComponent<CardComponent>().SelectedCardList) card.ApplyOffset();

                // 清除拖放标志
                _justDroppedNode = false;
                _droppedNodeId = -1;
            }
        }

        #endregion

        #region 连接线

        public void BeginDrawConnectLine()
        {
            // 设置起始引脚
            _startPin = _hoveredNodeView.HoveredPin;
            // 设置鼠标坐标
            _mouseDown = Mouse.GetPosition(_host.OperateAreaGrid);
            // 获取引脚与鼠标的偏移量
            Point offset = _hoveredNodeView.GetHoveredPinOffset();
            // 计算引脚连接点坐标
            Point pinPoint = new Point(_mouseDown.X + offset.X, _mouseDown.Y + offset.Y);
            // 开始绘制连接线
            GetComponent<DrawingComponent>().BeginDrawTempConnectLine(pinPoint);
        }

        public void CancelDrawConnectLine()
        {
            GetComponent<DrawingComponent>().ClearTempLine();
        }

        public void DrawConnectLine()
        {
            // 更新鼠标坐标
            _mousePoint = Mouse.GetPosition(_host.OperateAreaGrid);
            if (_hoveredNodeView != null && _hoveredNodeView.HoveredPin != null)
            {
                // 获取引脚与鼠标的偏移量
                Point offset = _hoveredNodeView.GetHoveredPinOffset();
                // 计算引脚连接点坐标
                _mousePoint = new Point(_mousePoint.X + offset.X, _mousePoint.Y + offset.Y);
            }
            // 根据起始引脚类型更新连接线的起点或终点
            if (_startPin.Flow == PinFlow.Input)
                GetComponent<DrawingComponent>().UpdateTempLineStart(_mousePoint);
            else
                GetComponent<DrawingComponent>().UpdateTempLineEnd(_mousePoint);
        }

        public void EndDrawConnectLine()
        {
            // 清除临时连接线
            GetComponent<DrawingComponent>().ClearTempLine();
            // 悬停引脚不为空
            if (_hoveredNodeView != null && _hoveredNodeView.HoveredPin != null)
            {
                PinBase endPin = _hoveredNodeView.HoveredPin;
                // 无法连接
                if (!CanConnect(_startPin, endPin)) return;

                // 获取核心编辑器
                var coreEditer = GetCoreEditer();

                // 确认任何待确认的添加节点命令
                coreEditer?.CommandManager.ConfirmLastAddNodePosition();

                // 确定源引脚和目标引脚
                PinBase sourcePin, targetPin;
                if (_startPin.Flow == PinFlow.Input)
                {
                    sourcePin = endPin;
                    targetPin = _startPin;
                }
                else
                {
                    sourcePin = _startPin;
                    targetPin = endPin;
                }

                // 使用命令系统执行连接
                if (coreEditer != null)
                {
                    coreEditer.ExecuteConnectPinCommand(sourcePin, targetPin);
                }
                else
                {
                    // 如果无法获取核心编辑器,使用原始方法
                    // 连接引脚：将结束引脚写入起始引脚
                    if (_startPin.Flow == PinFlow.Input)
                    {
                        // 如果是数据引脚，先移除原有的连接线
                        if (_startPin is DataPin && _startPin.SourceList.Count > 0)
                            GetComponent<DrawingComponent>().RemoveConnectLine(_startPin.SourceList[0], _startPin);
                        // 连接源与目标。数据引脚会自动断开原有连接
                        _startPin.AddSource(endPin);
                        endPin.AddTarget(_startPin);
                        // 添加连接线
                        GetComponent<DrawingComponent>().AddConnectLine(endPin, _startPin);
                    }
                    else
                    {
                        // 如果是数据引脚，先移除原有的连接线
                        if (endPin is DataPin && endPin.SourceList.Count > 0)
                            GetComponent<DrawingComponent>().RemoveConnectLine(endPin.SourceList[0], endPin);
                        // 连接源与目标。数据引脚会自动断开原有连接
                        _startPin.AddTarget(endPin);
                        endPin.AddSource(_startPin);
                        // 添加连接线
                        GetComponent<DrawingComponent>().AddConnectLine(_startPin, endPin);
                    }
                    // 更新引脚图标
                    UpdateAllPinIcon();

                    ProjectManager.Instance.Saved = false;
                }
            }
            _startPin = null;
        }

        #endregion

        #region 断开引脚

        public void BeginBreakPin() => _rightHitedPin = _hoveredNodeView.HoveredPin;

        public void CancelBreakPin() => _rightHitedPin = null;

        public void EndBreakPin()
        {
            // 获取核心编辑器
            var coreEditer = GetCoreEditer();

            // 命中输入节点，则与源断开
            if (_rightHitedPin.Flow == PinFlow.Input)
            {
                List<PinBase> sourceList = new List<PinBase>(_rightHitedPin.SourceList);
                foreach (var source in sourceList)
                {
                    if (coreEditer != null)
                    {
                        coreEditer.ExecuteDisconnectPinCommand(source, _rightHitedPin);
                    }
                    else
                    {
                        BreakPin(source, _rightHitedPin);
                    }
                }
            }
            // 否则与目标断开
            else
            {
                List<PinBase> targetList = new List<PinBase>(_rightHitedPin.TargetList);
                foreach (var target in targetList)
                {
                    if (coreEditer != null)
                    {
                        coreEditer.ExecuteDisconnectPinCommand(_rightHitedPin, target);
                    }
                    else
                    {
                        BreakPin(_rightHitedPin, target);
                    }
                }
            }

            // 更新引脚图标
            if (coreEditer == null)
            {
                UpdateAllPinIcon();
                ProjectManager.Instance.Saved = false;
            }

            _rightHitedPin = null;
        }

        #endregion

        #region 移除连接线

        public void RemoveConnectLine()
        {
            // 获取连接线
            Layer.VisualConnectLine? connectLine = GetComponent<DrawingComponent>().HoveredConnectLine;
            if (connectLine == null) return;

            // 获取核心编辑器
            var coreEditer = GetCoreEditer();

            if (coreEditer != null)
            {
                // 使用命令系统断开引脚
                coreEditer.ExecuteDisconnectPinCommand(connectLine.StartPin, connectLine.EndPin);
            }
            else
            {
                // 断开引脚
                BreakPin(connectLine.StartPin, connectLine.EndPin);
                // 更新引脚图标
                UpdateAllPinIcon();
            }
        }

        #endregion

        #region 拖动视口

        public void BeginDragViewport()
        {
            _host.OperateAreaGrid.Cursor = CursorManager.Instance.Move;
            _mouseDown = Mouse.GetPosition(_host.OperateAreaGrid);
        }

        public void CancelDragViewport()
        {
            _host.OperateAreaGrid.Cursor = _tool.Cursor;
        }

        public void DragViewport()
        {
            _mousePoint = Mouse.GetPosition(_host.OperateAreaGrid);
            GetComponent<DrawingComponent>().DragViewport(new Point(_mousePoint.X - _mouseDown.X, _mousePoint.Y - _mouseDown.Y));
            UpdateHoverToolBar();
        }

        public void EndDragViewport()
        {
            _host.OperateAreaGrid.Cursor = _tool.Cursor;
            GetComponent<DrawingComponent>().EndDrag();
        }

        #endregion

        #endregion

        #region 节点事件

        private void NodeBack_MouseEnter(NodeView nodeView) => SwitchHoverTarget(nodeView);

        private void NodeBack_MouseLeave(NodeView nodeView) => SwitchHoverTarget(null);

        private void PinGroupListChanged()
        {
            // 更新选中框
            GetComponent<DrawingComponent>().UpdateSelectedBox();
            // 更新连接线
            GetComponent<DrawingComponent>().UpdateConnectLine();
            // 更新引脚图标
            UpdateAllPinIcon();
        }

        private void NodeChanged()
        {
            ProjectManager.Instance.Saved = false;
        }

        #endregion

        #region 控件事件

        private void OperateArea_MouseMove(object sender, MouseEventArgs e) => _tool?.OnMouseMove();

        private void OperateArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1) _tool?.OnMouseDown(e.ChangedButton);
            else if (e.ChangedButton == MouseButton.Left) _tool?.OnDoubleClick();
        }

        private void OperateArea_MouseUp(object sender, MouseButtonEventArgs e) => _tool?.OnMouseUp(e.ChangedButton);

        private void HoverToolBar_ToolClick(string name)
        {
            switch (name)
            {
                case "Tool_Start":
                    foreach (var card in GetComponent<CardComponent>().SelectedCardList)
                        card.NodeInstance.Start();
                    break;
                case "Tool_Stop":
                    foreach (var card in GetComponent<CardComponent>().SelectedCardList)
                        card.NodeInstance.Stop();
                    break;
                case "Tool_Delete":
                    DeleteNode(GetComponent<CardComponent>().SelectedCardList);
                    break;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 切换悬停目标
        /// </summary>
        private void SwitchHoverTarget(NodeView? target)
        {
            // 与当前目标一致，忽略
            if (_hoveredNodeView == target) return;
            // 更新目标
            _hoveredNodeView = target;

            // 当前目标为空，清空悬停框
            if (_hoveredNodeView == null)
            {
                GetComponent<DrawingComponent>().HoverBox = null;
                GetComponent<DrawingComponent>().UpdateHoverBox();
                return;
            }
            // 当前目标已选中，不绘制悬停框
            if (GetComponent<CardComponent>().SelectedCardList.Contains(_hoveredNodeView)) return;

            // 设置悬停框
            GetComponent<DrawingComponent>().HoverBox = new TargetBox
            {
                ScreenPoint = new Point(Canvas.GetLeft(_hoveredNodeView) + 9, Canvas.GetTop(_hoveredNodeView) - 2),
                Width = _hoveredNodeView.ActualWidth - 18,
                Height = _hoveredNodeView.ActualHeight + 4
            };
        }

        /// <summary>
        /// 更新悬停工具栏
        /// </summary>
        private void UpdateHoverToolBar()
        {
            // 选中数量
            int selectedCount = GetComponent<CardComponent>().SelectedCardList.Count;
            // 显隐工具栏
            _hoverToolBar.Visibility = selectedCount == 0 ? Visibility.Collapsed : Visibility.Visible;

            // 无选中
            if (selectedCount == 0) { }
            // 选中一个
            else if (selectedCount == 1)
            {
                Rect cardRect = GetComponent<CardComponent>().SelectedCardList[0].GetHittableRect();
                double left = cardRect.Right - _hoverToolBar.ActualWidth;
                double top = cardRect.Top - 10 - _hoverToolBar.ActualHeight;
                Canvas.SetLeft(_hoverToolBar, left + 1);
                Canvas.SetTop(_hoverToolBar, top + 1);
            }
            // 选中多个
            else
            {
                List<NodeView> selectedList = GetComponent<CardComponent>().SelectedCardList;
                Rect rect = selectedList[0].GetHittableRect();
                for (int index = 1; index < selectedList.Count; index++)
                {
                    rect.Union(selectedList[index].GetHittableRect());
                }
                double left = Math.Round((rect.Right - rect.Left - _hoverToolBar.ActualWidth) / 2) + rect.Left;
                double top = rect.Top - 10 - _hoverToolBar.ActualHeight;
                Canvas.SetLeft(_hoverToolBar, left + 1);
                Canvas.SetTop(_hoverToolBar, top + 1);
            }
        }

        /// <summary>
        /// 更新属性面板
        /// </summary>
        private void UpdatePropertyPanel()
        {
            // 选中数量
            int selectedCount = GetComponent<CardComponent>().SelectedCardList.Count;
            // 显隐属性面板
            var propertyArea = (Border)_host.FindName("PropertyArea");
            var propertyPanel = (XNode.SubSystem.NodeEditSystem.Control.NodePropertyPanel)_host.FindName("PropertyPanel");
            
            propertyArea.Visibility = selectedCount == 0 ? Visibility.Collapsed : Visibility.Visible;

            if (selectedCount == 0)
            {
                propertyPanel.Instance = null;
                propertyPanel.ClearPropertyBar();
            }
            else
            {
                // 获取第一个选中的节点
                NodeView firstCard = GetComponent<CardComponent>().SelectedCardList[0];
                if (firstCard.NodeInstance.PropertyList.Count == 0)
                {
                    propertyArea.Visibility = Visibility.Collapsed;
                    return;
                }

                // 加载属性条
                propertyPanel.Instance = firstCard.NodeInstance;
                propertyPanel.LoadPropertyBar();
            }
        }

        /// <summary>
        /// 判断两个引脚能否连接
        /// </summary>
        private bool CanConnect(PinBase start, PinBase end)
        {
            // 不能连接自己
            if (end == start) return false;
            // 不能处于同一节点下
            if (end.OwnerGroup.OwnerNode == start.OwnerGroup.OwnerNode) return false;
            // 流向不能一致
            if (end.Flow == start.Flow) return false;
            // 类型必须一致
            if (end.GetType() != start.GetType()) return false;
            // 已连接
            if (start.TargetList.Contains(end)) return false;

            return true;
        }

        /// <summary>
        /// 断开引脚
        /// </summary>
        private void BreakPin(PinBase source, PinBase target)
        {
            source.TargetList.Remove(target);
            target.SourceList.Remove(source);
            GetComponent<DrawingComponent>().RemoveConnectLine(source, target);
        }

        /// <summary>
        /// 删除节点
        /// </summary>
        private void DeleteNode(List<NodeView> cardList)
        {
            // 获取核心编辑器
            var coreEditer = GetCoreEditer();

            if (coreEditer != null)
            {
                // 使用命令系统删除节点
                foreach (var card in cardList)
                {
                    coreEditer.ExecuteDeleteNodeCommand(card.NodeInstance);
                }
            }
            else
            {
                // 删除节点实例与卡片
                foreach (var card in cardList)
                {
                    GetComponent<NodeComponent>().DeleteNode(card.NodeInstance);
                    GetComponent<CardComponent>().DeleteNodeCard(card);
                }
                // 更新引脚图标
                UpdateAllPinIcon();

                ProjectManager.Instance.Saved = false;
            }

            // 清空选择
            GetComponent<CardComponent>().ClearSelect();
            // 更新选中框
            GetComponent<DrawingComponent>().UpdateSelectedBox();
            // 更新悬浮工具栏
            UpdateHoverToolBar();
            // 更新属性面板
            UpdatePropertyPanel();
            // 更新鼠标悬停
            HandleMouseMove();
        }

        /// <summary>
        /// 重置组件
        /// </summary>
        private void ResetComponent()
        {
            ReleaseOperationLayer();
            Host.Cursor = null;
            _tool.Reset();
            _hoveredNodeView = null;

            _mouseDown = new Point();
            _mousePoint = new Point();

            _startPin = null;
            _rightHitedPin = null;

            _hoverToolBar.Visibility = Visibility.Collapsed;
            var propertyArea = (Border)_host.FindName("PropertyArea");
            var propertyPanel = (XNode.SubSystem.NodeEditSystem.Control.NodePropertyPanel)_host.FindName("PropertyPanel");
            
            propertyPanel.Instance = null;
            propertyPanel.ClearPropertyBar();
            propertyArea.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region 字段

        private SelectTool _tool;

        /// <summary>当前悬停的节点视图</summary>
        private NodeView? _hoveredNodeView = null;

        /// <summary>当前鼠标坐标</summary>
        private Point _mousePoint = new Point();
        /// <summary>鼠标按下坐标</summary>
        private Point _mouseDown = new Point();

        /// <summary>起始引脚</summary>
        private PinBase? _startPin;
        /// <summary>右键命中引脚</summary>
        private PinBase? _rightHitedPin = null;

        /// <summary>悬浮工具栏</summary>
        private HoverToolBar _hoverToolBar;

        /// <summary>标记刚刚拖放了节点</summary>
        private bool _justDroppedNode = false;
        /// <summary>拖放的节点ID</summary>
        private int _droppedNodeId = -1;

        #endregion

        #region 事件处理

        /// <summary>
        /// 清除拖放标志事件处理
        /// </summary>
        private void OnClearDropFlags()
        {
            if (_justDroppedNode)
            {
                MainWindow.LogManager.LogInfo($"通过事件系统清除拖放标志，节点ID: {_droppedNodeId}");
                _justDroppedNode = false;
                _droppedNodeId = -1;
            }
        }

        #endregion
    }
}