using System.Windows;
using XNode.AppTool;
using XNode.SubSystem.WindowSystem;

namespace XNode
{
    public partial class App : Application
    {
        #region 构造方法

        public App()
        {
            Startup += App_Startup;
        }

        #endregion

        #region 应用程序事件

        private void App_Startup(object sender, StartupEventArgs e)
        {
            Console.WriteLine("应用程序启动开始...");
            
            // 启动异常
            bool startException = false;
            try
            {
                Console.WriteLine("初始化应用程序...");
                Init();
                Console.WriteLine("初始化完成，尝试创建并显示主窗口...");
                
                // 尝试直接创建和显示MainWindow
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                Console.WriteLine("主窗口已创建并显示");
            }
            catch (Exception ex)
            {
                startException = true;
                Console.WriteLine("启动异常：" + ex.Message);
                Console.WriteLine("异常堆栈：" + ex.StackTrace);
                
                // 尝试显示错误消息框
                try
                {
                    MessageBox.Show("软件启动异常：" + ex.Message + "\n\n详细信息已输出到控制台", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { }
            }
            finally
            {
                if (startException) 
                {
                    Console.WriteLine("发生异常，关闭应用程序...");
                    Shutdown();
                }
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化应用程序
        /// </summary>
        private void Init()
        {
            Console.WriteLine("开始初始化应用程序...");
            
            try
            {
                // 初始化应用程序代理
                Console.WriteLine("初始化应用程序代理...");
                AppDelegate.Init();
                Console.WriteLine("应用程序代理初始化完成");

                // 初始化系统数据
                Console.WriteLine("初始化系统数据...");
                SystemDataDelegate.Instance.Init();
                Console.WriteLine("系统数据初始化完成");
                
                // 启动系统服务
                Console.WriteLine("启动系统服务...");
                SystemServiceDelegate.Instance.Start();
                Console.WriteLine("系统服务启动完成");
                
                // 初始化系统工具
                Console.WriteLine("初始化系统工具...");
                SystemToolDelegate.Instance.Init();
                Console.WriteLine("系统工具初始化完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine("初始化过程中发生异常: " + ex.Message);
                Console.WriteLine("异常堆栈: " + ex.StackTrace);
                throw; // 重新抛出异常，让上层捕获处理
            }
            
            Console.WriteLine("应用程序初始化完成");
        }

        #endregion
    }
}