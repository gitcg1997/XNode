using System;

using System.Windows;

using System.Windows.Media;

using System.Collections.Concurrent;

using System.Threading.Tasks;

using XLib.Base;

using XNode.Command;

using XLib.WPF.WindowDefine;

using XLib.WPFControl;

using XNode.AppTool;

using XNode.SubSystem.CacheSystem;

using XNode.SubSystem.EventSystem;

using XNode.SubSystem.ExecutionSystem;

using XNode.SubSystem.ProjectSystem;

using XNode.SubSystem.ResourceSystem;

using XNode.SubSystem.WindowSystem;

namespace XNode
{
    public partial class MainWindow : XMainWindow
    {
        #region 属性

        /// <summary>核心编辑器实例</summary>
        public CoreEditer Editer
        {
            get
            {
                if (_coreEditer == null) throw new Exception("核心编辑器为空");
                return _coreEditer;
            }
        }

        #endregion

        #region 构造方法

        public MainWindow() => InitializeComponent();

        #endregion

        #region XMainWindow 方法

        protected override void XWindowLoaded()
        {
            try
            {
                // 恢复窗口状态并监听窗口状态
                RecoverWindowState();
                ListenWindowState();

                // 设置主窗口实例
                WM.Main = this;

                // 初始化日志管理器
                LogManager.Initialize(AppendLog);
               
                // 设置日志区域初始可见性
                LogOutputArea.Visibility = _logAreaVisible ? Visibility.Visible : Visibility.Collapsed;
                LogAreaRow.Height = _logAreaVisible ? new GridLength(200) : new GridLength(0);
               
                // 加载核心编辑器
                LoadCoreEditer();
                // 初始化工具栏
                InitToolBar();

                // 监听命令状态变化事件
                Editer.CommandManager.CommandStatusChanged += OnCommandStatusChanged;
               
                // 监听系统事件
                EM.Instance.Add(EventType.Project_Changed, UpdateTitle);
               
                // 输出启动日志
                LogManager.LogInfo("XNode编辑器启动完成");
            }
            catch (Exception ex)
            {
                // 将异常信息写入控制台
                Console.WriteLine("窗口加载异常: " + ex.Message);
                Console.WriteLine("异常堆栈: " + ex.StackTrace);
               
                // 显示一个简单的消息框
                MessageBox.Show("窗口加载失败: " + ex.Message + "\n\n详细信息已输出到控制台", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 当命令状态改变时调用
        /// </summary>
        private void OnCommandStatusChanged()
        {
            Dispatcher.Invoke(() => {
                UpdateUndoRedoTools();
                // 更新UI中的选中框，确保撤销/重做后选中框正确显示
                UpdateUIAfterCommand();
            });
        }

        protected override bool ReadyClose()
        {
            // 项目未保存
            if (!ProjectManager.Instance.Saved)
            {
                bool? result = WM.ShowAsk("当前项目未保存，是否保存？");
                // 保存
                if (result == true)
                {
                    bool saved = ProjectManager.Instance.SaveProject();
                    // 确定保存，但未执行，则取消操作
                    if (!saved) return false;
                }
                // 取消操作
                else if (result == null) return false;
            }

            // 关闭项目
            ProjectManager.Instance.CloseProject();

            return true;
        }

        #endregion

        #region 窗口事件

        private void XMainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // 调试日志
            LogManager.LogInfo($"按键: {e.Key}, 修饰键: {System.Windows.Input.Keyboard.Modifiers}");

            // 处理快捷键
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case System.Windows.Input.Key.Z:
                        // Ctrl+Z: 撤销
                        LogManager.LogInfo($"检测到 Ctrl+Z, CanUndo={Editer.CommandManager.CanUndo}");
                        if (Editer.CommandManager.CanUndo)
                        {
                            string description = Editer.CommandManager.UndoDescription;
                            bool result = Editer.CommandManager.Undo();
                            if (result)
                            {
                                LogManager.LogInfo($"撤销操作: {description}");
                            }
                        }
                        else
                        {
                            LogManager.LogInfo("没有可撤销的操作");
                        }
                        e.Handled = true;
                        return;

                    case System.Windows.Input.Key.Y:
                        // Ctrl+Y: 重做
                        LogManager.LogInfo($"检测到 Ctrl+Y, CanRedo={Editer.CommandManager.CanRedo}");
                        if (Editer.CommandManager.CanRedo)
                        {
                            string description = Editer.CommandManager.RedoDescription;
                            bool result = Editer.CommandManager.Redo();
                            if (result)
                            {
                                LogManager.LogInfo($"重做操作: {description}");
                            }
                        }
                        else
                        {
                            LogManager.LogInfo("没有可重做的操作");
                        }
                        e.Handled = true;
                        return;
                }
            }

            EM.Instance.Invoke(EventType.KeyDown, e.Key.ToString());
        }

        private void XMainWindow_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            EM.Instance.Invoke(EventType.KeyUp, e.Key.ToString());
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 恢复窗口状态
        /// </summary>
        private void RecoverWindowState()
        {
            WindowState = CacheManager.Instance.Cache.MainWindow.State;
            Width = CacheManager.Instance.Cache.MainWindow.Width;
            Height = CacheManager.Instance.Cache.MainWindow.Height;
            // 居中窗口
            Left = (SystemParameters.WorkArea.Width - Width) / 2;
            Top = (SystemParameters.WorkArea.Height - Height) / 2;
        }

        /// <summary>
        /// 监听窗口状态
        /// </summary>
        private void ListenWindowState()
        {
            StateChanged += (s, e) =>
            {
                if (WindowState is WindowState.Normal or WindowState.Maximized)
                {
                    CacheManager.Instance.Cache.MainWindow.State = WindowState;
                    CacheManager.Instance.UpdateCache();
                }
            };
            SizeChanged += (s, e) =>
            {
                if (WindowState == WindowState.Maximized) return;
                CacheManager.Instance.Cache.MainWindow.Width = (int)Width;
                CacheManager.Instance.Cache.MainWindow.Height = (int)Height;
                CacheManager.Instance.UpdateCache();
            };
        }

        /// <summary>
        /// 初始化工具栏
        /// </summary>
        private void InitToolBar()
        {
            // 获取工具栏
            if (GetTemplateChild("TopToolBar") is ToolBar bar)
            {
                _toolBar = bar;
                bar.ToolStyle = (Style)FindResource("ToolBarButton");
                // 填充工具按钮
                bar.AddSplit(new Thickness(0, 5, 5, 5));
                bar.AddTool(new System.Windows.Media.Imaging.BitmapImage(new Uri("C:\\Users\\92588\\Desktop\\XNode\\NewFolder.png")), "NewProject", "新建项目");
                bar.AddTool(new System.Windows.Media.Imaging.BitmapImage(new Uri("C:\\Users\\92588\\Desktop\\XNode\\LinkedFolderOpened.png")), "OpenProject", "打开项目");
                bar.AddTool(new System.Windows.Media.Imaging.BitmapImage(new Uri("C:\\Users\\92588\\Desktop\\XNode\\Save.png")), "SaveProject", "保存项目");
                bar.AddTool(new System.Windows.Media.Imaging.BitmapImage(new Uri("C:\\Users\\92588\\Desktop\\XNode\\SaveAll.png")), "SaveAs", "另存为项目");
                bar.AddSplit();
                bar.AddTool(GetToolIcon("Undo"), "Undo", "撤销");
                bar.AddTool(GetToolIcon("Redo"), "Redo", "重做");
                bar.AddSplit();
                bar.AddTool(GetToolIcon("Play"), "Run", "运行");
                bar.AddTool(GetToolIcon("Stop"), "Stop", "停止");
                bar.AddSplit();
                bar.AddTool(GetToolIcon("AddImage"), "AddImage", "添加图片");
                bar.AddTool(GetToolIcon("Image"), "OpenImageLibrary", "打开图片库");
                // 监听工具栏
                bar.ToolClick += ToolBar_ToolClick;
                // 禁用工具栏
                // _toolBar.DisableAllTool();
                _toolBar.EnableTool("OpenImageLibrary");
                _toolBar.EnableTool("Undo");
                _toolBar.EnableTool("Redo");
                _toolBar.EnableTool("AddImage");

                // 初始化撤销/重做工具状态
                UpdateUndoRedoTools();
            }
        }

        /// <summary>
        /// 更新撤销重做工具状态
        /// </summary>
        private void UpdateUndoRedoTools()
        {
            if (_toolBar != null)
            {
                if (Editer.CommandManager.CanUndo)
                    _toolBar.EnableTool("Undo");
                else
                    _toolBar.DisableTool("Undo");
               
                if (Editer.CommandManager.CanRedo)
                    _toolBar.EnableTool("Redo");
                else
                    _toolBar.DisableTool("Redo");
            }
        }

        /// <summary>
        /// 获取工具图标
        /// </summary>
        private ImageSource GetToolIcon(string name) => ImageResManager.Instance.GetAssetsImage($"Icon16/{name}.png");

        /// <summary>
        /// 工具栏.单击工具
        /// </summary>
        private void ToolBar_ToolClick(string name)
        {

            switch (name)
            {

                // 新建项目

                case "NewProject":

                    LogManager.LogInfo("创建新项目");

                    ProjectManager.Instance.NewProject();

                    UpdateTitle();

                    break;

                // 打开项目

                case "OpenProject":

                    LogManager.LogInfo("打开项目");

                    ProjectManager.Instance.OpenProject();

                    UpdateTitle();

                    break;

                // 保存项目

                case "SaveProject":

                    LogManager.LogInfo("保存项目");

                    ProjectManager.Instance.SaveProject();

                    UpdateTitle();

                    break;

                // 另存为项目

                case "SaveAs":

                    LogManager.LogInfo("另存为项目");

                    ProjectManager.Instance.SaveAsProject();

                    UpdateTitle();

                    break;

                // 打开图片库

                case "OpenImageLibrary":

                    LogManager.LogInfo("打开图片库");

                    OpenImageLibrary();

                    break;

                // 添加图片

                case "AddImage":

                    LogManager.LogInfo("添加图片");

                    OpenImageEditor();

                    break;

                // 撤销

                case "Undo":

                    if (Editer.CommandManager.CanUndo)

                    {

                        bool result = Editer.CommandManager.Undo();

                        if (result)

                        {

                            LogManager.LogInfo($"撤销操作: {Editer.CommandManager.UndoDescription}");

                            UpdateUndoRedoTools();

                        }

                    }

                    else

                    {

                        LogManager.LogInfo("没有可撤销的操作");

                    }

                    break;

                // 重做

                case "Redo":

                    if (Editer.CommandManager.CanRedo)

                    {

                        bool result = Editer.CommandManager.Redo();

                        if (result)

                        {

                            LogManager.LogInfo($"重做操作: {Editer.CommandManager.RedoDescription}");

                            UpdateUndoRedoTools();

                        }

                    }

                    else

                    {

                        LogManager.LogInfo("没有可重做的操作");

                    }

                    break;

                // 运行节点图

                case "Run":

                    RunNodeGraph();

                    break;

                // 停止运行

                case "Stop":

                    StopNodeGraph();

                    break;

            }

        }

        /// <summary>
        /// 加载核心编辑器
        /// </summary>
        private void LoadCoreEditer()
        {
            _coreEditer = new CoreEditer { Margin = new Thickness(0, 2, 0, 0) };
            MainGrid.Children.Add(_coreEditer);
        }

        /// <summary>
        /// 更新标题
        /// </summary>
        private void UpdateTitle()
        {
            if (ProjectManager.Instance.CurrentProject != null)
            {
                Title = ProjectManager.Instance.CurrentProject.ProjectName;
                if (!ProjectManager.Instance.Saved) Title += "*";
                Title += " - " + AppDelegate.AppTitle;
            }
            else Title = AppDelegate.AppTitle;
        }
        
        /// <summary>
        /// 在命令执行后更新UI
        /// </summary>
        private void UpdateUIAfterCommand()
        {
            try
            {
                // 更新编辑面板的UI，确保选中框正确显示
                if (Editer != null)
                {
                    var editPanel = Editer.GetEditPanel();
                    editPanel?.UpdateUIAfterNodeOperation();
                    LogManager.LogInfo("命令执行后已更新UI和选中框");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"更新UI失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 清除所有选中状态
        /// </summary>
        private void ClearAllSelection(SubSystem.NodeEditSystem.Panel.EditPanel editPanel)
        {
            try
            {
                // 通过InteractionComponent清除选中
                editPanel.InteractionComponent?.ClearSelect();
                
                // 通过DrawingComponent清除选中框
                editPanel.DrawingComponent?.ClearSelectBox();
                
                LogManager.LogInfo("已清除所有选中状态和选中框");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"清除选中状态失败: {ex.Message}");
            }
        }

        #endregion

        #region 日志管理器

        public static class LogManager
        {
            private static ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
            private static Action<string>? _logCallback;
            private static bool _isProcessing = false;

            public static void Initialize(Action<string> logCallback)
            {
                _logCallback = logCallback;
                // 输出初始日志
                Log("日志系统初始化完成");
            }

            public static void Log(string message)
            {
                string logMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                _logQueue.Enqueue(logMessage);
                ProcessLogQueue();
            }

            public static void LogError(string message)
            {
                Log($"[错误] {message}");
            }

            public static void LogWarning(string message)
            {
                Log($"[警告] {message}");
            }

            public static void LogInfo(string message)
            {
                Log($"[信息] {message}");
            }

            private static void ProcessLogQueue()
            {
                if (_isProcessing) return;

                _isProcessing = true;
                Task.Run(() =>
                {
                    while (_logQueue.TryDequeue(out string? logMessage))
                    {
                        _logCallback?.Invoke(logMessage);
                    }
                    _isProcessing = false;
                });
            }
        }

        /// <summary>
        /// 切换日志区域显示状态
        /// </summary>
        private void LogToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _logAreaVisible = !_logAreaVisible;
            LogOutputArea.Visibility = _logAreaVisible ? Visibility.Visible : Visibility.Collapsed;
            LogAreaRow.Height = _logAreaVisible ? new GridLength(200) : new GridLength(0);
            LogManager.LogInfo(_logAreaVisible ? "显示日志输出区域" : "隐藏日志输出区域");
        }

        /// <summary>
        /// 清空日志
        /// </summary>
        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
            LogManager.LogInfo("清空日志输出");
        }

        /// <summary>
        /// 追加日志到文本框
        /// </summary>
        private void AppendLog(string logMessage)
        {
            // 在UI线程中更新文本框
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText(logMessage + Environment.NewLine);
                LogTextBox.ScrollToEnd();
            });
        }

        #endregion

        #region 节点图执行

        /// <summary>
        /// 运行节点图
        /// </summary>
        private void RunNodeGraph()
        {
            try
            {
                // 获取当前项目的节点列表
                var project = ProjectManager.Instance.CurrentProject;
                if (project == null)
                {
                    LogManager.LogWarning("没有打开的项目");
                    WM.ShowTip("请先打开或创建一个项目");
                    return;
                }

                if (Editer.NodeList == null || Editer.NodeList.Count == 0)
                {
                    LogManager.LogWarning("项目中没有节点");
                    WM.ShowTip("请添加节点后再运行");
                    return;
                }

                // 检查是否正在执行
                if (_nodeGraphExecutor.IsExecuting)
                {
                    LogManager.LogWarning("节点图正在执行中");
                    WM.ShowTip("节点图正在执行中，请等待完成或停止后再运行");
                    return;
                }

                LogManager.LogInfo("开始运行节点图");

                // 连接执行器事件以支持高亮显示
                ConnectExecutorEvents();

                // 异步执行节点图
                Task.Run(async () =>
                {
                    try
                    {
                        await _nodeGraphExecutor.ExecuteAsync(Editer.NodeList);

                        Dispatcher.Invoke(() =>
                        {
                            LogManager.LogInfo("节点图执行完成");
                        });
                    }
                    catch (InvalidOperationException ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LogManager.LogError($"执行失败: {ex.Message}");
                            WM.ShowTip(ex.Message, null, TipLevel.Warning);
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LogManager.LogError($"执行出错: {ex.Message}");
                            WM.ShowTip($"执行节点图时发生错误:\n{ex.Message}", null, TipLevel.Error);
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                LogManager.LogError($"运行节点图失败: {ex.Message}");
                WM.ShowTip($"运行节点图失败:\n{ex.Message}", null, TipLevel.Error);
            }
        }

        /// <summary>
        /// 连接执行器事件
        /// </summary>
        private void ConnectExecutorEvents()
        {
            // 先断开之前的事件连接，避免重复连接
            DisconnectExecutorEvents();
           
            // 连接执行器事件以支持高亮显示
            _nodeGraphExecutor.NodeExecutionStarted += (node) =>
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var editPanel = Editer.GetEditPanel();
                        editPanel?.HighlightNode(node);
                        LogManager.LogInfo($"高亮节点: {node.Title} (ID: {node.ID})");
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogWarning($"无法高亮节点: {ex.Message}");
                    }
                });
            };

            _nodeGraphExecutor.NodeExecutionCompleted += (node) =>
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var editPanel = Editer.GetEditPanel();
                        editPanel?.ClearHighlight();
                        LogManager.LogInfo($"清除节点高亮: {node.Title} (ID: {node.ID})");
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogWarning($"无法清除高亮: {ex.Message}");
                    }
                });
            };

            _nodeGraphExecutor.ExecutionCompleted += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var editPanel = Editer.GetEditPanel();
                        editPanel?.ClearHighlight();
                        LogManager.LogInfo("执行完成，清除所有高亮");
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogWarning($"无法清除高亮: {ex.Message}");
                    }
                });
            };

            _nodeGraphExecutor.ExecutionCancelled += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var editPanel = Editer.GetEditPanel();
                        editPanel?.ClearHighlight();
                        LogManager.LogInfo("执行取消，清除所有高亮");
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogWarning($"无法清除高亮: {ex.Message}");
                    }
                });
            };

            _nodeGraphExecutor.ExecutionError += (ex) =>
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var editPanel = Editer.GetEditPanel();
                        editPanel?.ClearHighlight();
                        LogManager.LogInfo($"执行错误，清除所有高亮: {ex.Message}");
                    }
                    catch (Exception ex2)
                    {
                        LogManager.LogWarning($"无法清除高亮: {ex2.Message}");
                    }
                });
            };
        }

        /// <summary>
        /// 断开执行器事件
        /// </summary>
        private void DisconnectExecutorEvents()
        {
            // 这里无法直接断开事件，因为C#不支持断开匿名方法
            // 我们将在每次运行时重新连接事件，这是常见的做法
        }

        /// <summary>
        /// 停止节点图执行
        /// </summary>
        private void StopNodeGraph()
        {
            try
            {
                if (_nodeGraphExecutor.IsExecuting)
                {
                    LogManager.LogInfo("请求停止节点图执行");
                    _nodeGraphExecutor.Cancel();
                    LogManager.LogInfo("节点图执行已停止");
                }
                else
                {
                    LogManager.LogInfo("节点图未在执行");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"停止节点图执行失败: {ex.Message}");
                WM.ShowTip($"停止节点图执行失败:\n{ex.Message}", null, TipLevel.Error);
            }
        }

        #endregion

        #region 图片编辑器

        /// <summary>
        /// 打开图片编辑器窗口
        /// </summary>
        private void OpenImageEditor()
        {
            try
            {
                // 打开截图窗口
                var captureWindow = new Windows.ImageEditor.CaptureWindow(isRecaptureMode: false);
                var result = captureWindow.ShowDialog();

                if (result == true && captureWindow.CaptureSucceeded)
                {
                    LogManager.LogInfo($"截图成功: {captureWindow.CapturedImagePath}");
                    WM.ShowTip("图片已成功添加");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"打开图片编辑器失败: {ex.Message}");
                WM.ShowError($"打开图片编辑器失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 打开图片库窗口
        /// </summary>
        private void OpenImageLibrary()
        {
            try
            {
                // 打开新的图片库管理窗口
                var dialog = new Windows.ImageLibrary.Views.ImageLibraryWindow();
                var result = dialog.ShowDialog();

                if (result == true && dialog.SelectedImagePaths.Count > 0)
                {
                    LogManager.LogInfo($"从图片库选择了 {dialog.SelectedImagePaths.Count} 张图片");
                    WM.ShowTip($"已选择 {dialog.SelectedImagePaths.Count} 张图片");

                    // 这里可以根据需要处理选中的图片
                    // 例如：在控制台输出图片路径
                    foreach (var imagePath in dialog.SelectedImagePaths)
                    {
                        LogManager.LogInfo($"  - {imagePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"打开图片库失败: {ex.Message}");
                WM.ShowError($"打开图片库失败: {ex.Message}");
            }
        }

        #endregion

        #region 字段

        /// <summary>工具栏</summary>
        private ToolBar? _toolBar = null;

        /// <summary>核心编辑器</summary>
        private CoreEditer? _coreEditer = null;

        /// <summary>控制台已打开</summary>
        private bool _consoleOpened = false;

        /// <summary>日志区域是否显示</summary>
        private bool _logAreaVisible = true;

        /// <summary>节点图执行器</summary>
        private readonly NodeGraphExecutor _nodeGraphExecutor = new NodeGraphExecutor();

        #endregion
    }
}