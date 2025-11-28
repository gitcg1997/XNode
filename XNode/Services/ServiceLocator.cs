namespace XNode.Services
{
    /// <summary>
    /// 简单的服务定位器模式实现
    /// 用于在 MVVM 过渡期间提供依赖注入功能
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();
        private static readonly Dictionary<Type, Func<object>> _factories = new();

        /// <summary>
        /// 注册单例服务
        /// </summary>
        public static void RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            _services[typeof(TInterface)] = new TImplementation();
        }

        /// <summary>
        /// 注册单例服务实例
        /// </summary>
        public static void RegisterSingleton<TInterface>(TInterface instance)
            where TInterface : class
        {
            _services[typeof(TInterface)] = instance;
        }

        /// <summary>
        /// 注册工厂方法（每次调用创建新实例）
        /// </summary>
        public static void RegisterTransient<TInterface>(Func<TInterface> factory)
            where TInterface : class
        {
            _factories[typeof(TInterface)] = () => factory();
        }

        /// <summary>
        /// 获取服务
        /// </summary>
        public static T GetService<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }

            if (_factories.TryGetValue(typeof(T), out var factory))
            {
                return (T)factory();
            }

            throw new InvalidOperationException($"服务 {typeof(T).Name} 未注册");
        }

        /// <summary>
        /// 尝试获取服务
        /// </summary>
        public static T? TryGetService<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }

            if (_factories.TryGetValue(typeof(T), out var factory))
            {
                return (T)factory();
            }

            return null;
        }

        /// <summary>
        /// 初始化所有服务
        /// </summary>
        public static void Initialize()
        {
            // 注册项目服务
            RegisterSingleton<IProjectService>(ProjectService.Instance);
        }

        /// <summary>
        /// 清理所有服务
        /// </summary>
        public static void Cleanup()
        {
            _services.Clear();
            _factories.Clear();
        }
    }
}
