using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sunset.Services
{
    /// <summary>
    /// 服务作用域
    /// </summary>
    public enum ServiceScope
    {
        /// <summary>全局服务，跨场景保持</summary>
        Global,
        /// <summary>场景服务，场景切换时清理</summary>
        Scene,
        /// <summary>临时服务，手动管理生命周期</summary>
        Temporary
    }
    
    /// <summary>
    /// 服务注册信息
    /// </summary>
    internal class ServiceRegistration
    {
        public object Instance { get; set; }
        public ServiceScope Scope { get; set; }
        public Type ImplementationType { get; set; }
    }
    
    /// <summary>
    /// 服务定位器
    /// 提供统一的服务注册、获取、管理功能
    /// 
    /// 特性：
    /// - 接口化服务注册
    /// - 支持服务作用域（Global/Scene/Temporary）
    /// - 支持服务替换（用于测试和调试）
    /// - 自动清理场景级服务
    /// 
    /// 使用示例：
    /// // 注册服务
    /// ServiceLocator.Register<ITimeService>(timeManager, ServiceScope.Global);
    /// 
    /// // 获取服务
    /// var timeService = ServiceLocator.Get<ITimeService>();
    /// 
    /// // 场景切换时清理
    /// ServiceLocator.ClearScope(ServiceScope.Scene);
    /// </summary>
    public static class ServiceLocator
    {
        // 服务注册表
        private static readonly Dictionary<Type, ServiceRegistration> _services 
            = new Dictionary<Type, ServiceRegistration>();
        
        // 是否启用调试日志
        private static bool _enableDebugLog = false;
        
        // 锁对象
        private static readonly object _lock = new object();
        
        #region 注册方法
        
        /// <summary>
        /// 注册服务
        /// </summary>
        /// <typeparam name="TInterface">服务接口类型</typeparam>
        /// <param name="instance">服务实例</param>
        /// <param name="scope">服务作用域</param>
        public static void Register<TInterface>(TInterface instance, ServiceScope scope = ServiceScope.Global)
        {
            if (instance == null)
            {
                Debug.LogWarning($"[ServiceLocator] 尝试注册空服务: {typeof(TInterface).Name}");
                return;
            }
            
            lock (_lock)
            {
                Type interfaceType = typeof(TInterface);
                
                if (_services.ContainsKey(interfaceType))
                {
                    if (_enableDebugLog)
                    {
                        Debug.LogWarning($"[ServiceLocator] 服务已存在，将被替换: {interfaceType.Name}");
                    }
                }
                
                _services[interfaceType] = new ServiceRegistration
                {
                    Instance = instance,
                    Scope = scope,
                    ImplementationType = instance.GetType()
                };
                
                if (_enableDebugLog)
                {
                    Debug.Log($"<color=cyan>[ServiceLocator] 注册服务: {interfaceType.Name} → {instance.GetType().Name} ({scope})</color>");
                }
            }
        }
        
        /// <summary>
        /// 注册服务（使用现有单例）
        /// 便于从现有单例模式迁移
        /// </summary>
        public static void RegisterSingleton<TInterface, TImplementation>(ServiceScope scope = ServiceScope.Global)
            where TImplementation : MonoBehaviour, TInterface
        {
            var instance = UnityEngine.Object.FindFirstObjectByType<TImplementation>();
            if (instance != null)
            {
                Register<TInterface>(instance, scope);
            }
            else
            {
                Debug.LogWarning($"[ServiceLocator] 未找到单例: {typeof(TImplementation).Name}");
            }
        }
        
        #endregion
        
        #region 获取方法
        
        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="TInterface">服务接口类型</typeparam>
        /// <returns>服务实例，如果未注册则返回default</returns>
        public static TInterface Get<TInterface>()
        {
            lock (_lock)
            {
                Type interfaceType = typeof(TInterface);
                
                if (_services.TryGetValue(interfaceType, out var registration))
                {
                    // 检查MonoBehaviour是否已销毁
                    if (registration.Instance is MonoBehaviour mb && mb == null)
                    {
                        _services.Remove(interfaceType);
                        Debug.LogWarning($"[ServiceLocator] 服务已销毁: {interfaceType.Name}");
                        return default;
                    }
                    
                    return (TInterface)registration.Instance;
                }
                
                if (_enableDebugLog)
                {
                    Debug.LogWarning($"[ServiceLocator] 服务未注册: {interfaceType.Name}");
                }
                
                return default;
            }
        }
        
        /// <summary>
        /// 尝试获取服务
        /// </summary>
        public static bool TryGet<TInterface>(out TInterface service)
        {
            service = Get<TInterface>();
            return service != null;
        }
        
        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        public static bool IsRegistered<TInterface>()
        {
            lock (_lock)
            {
                return _services.ContainsKey(typeof(TInterface));
            }
        }
        
        #endregion
        
        #region 管理方法
        
        /// <summary>
        /// 注销服务
        /// </summary>
        public static void Unregister<TInterface>()
        {
            lock (_lock)
            {
                Type interfaceType = typeof(TInterface);
                
                if (_services.Remove(interfaceType))
                {
                    if (_enableDebugLog)
                    {
                        Debug.Log($"<color=yellow>[ServiceLocator] 注销服务: {interfaceType.Name}</color>");
                    }
                }
            }
        }
        
        /// <summary>
        /// 清理指定作用域的所有服务
        /// </summary>
        public static void ClearScope(ServiceScope scope)
        {
            lock (_lock)
            {
                var toRemove = new List<Type>();
                
                foreach (var kvp in _services)
                {
                    if (kvp.Value.Scope == scope)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var type in toRemove)
                {
                    _services.Remove(type);
                }
                
                if (_enableDebugLog && toRemove.Count > 0)
                {
                    Debug.Log($"<color=yellow>[ServiceLocator] 清理 {scope} 作用域: {toRemove.Count} 个服务</color>");
                }
            }
        }
        
        /// <summary>
        /// 清理所有服务
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock)
            {
                _services.Clear();
                
                if (_enableDebugLog)
                {
                    Debug.Log("<color=red>[ServiceLocator] 清空所有服务</color>");
                }
            }
        }
        
        /// <summary>
        /// 替换服务（用于测试）
        /// </summary>
        public static void Replace<TInterface>(TInterface newInstance)
        {
            lock (_lock)
            {
                Type interfaceType = typeof(TInterface);
                
                if (_services.TryGetValue(interfaceType, out var registration))
                {
                    registration.Instance = newInstance;
                    registration.ImplementationType = newInstance?.GetType();
                    
                    if (_enableDebugLog)
                    {
                        Debug.Log($"<color=orange>[ServiceLocator] 替换服务: {interfaceType.Name}</color>");
                    }
                }
                else
                {
                    Register(newInstance);
                }
            }
        }
        
        /// <summary>
        /// 设置调试模式
        /// </summary>
        public static void SetDebugMode(bool enabled)
        {
            _enableDebugLog = enabled;
        }
        
        /// <summary>
        /// 获取所有已注册服务的信息（调试用）
        /// </summary>
        public static string GetRegisteredServicesInfo()
        {
            lock (_lock)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== 已注册服务 ===");
                
                foreach (var kvp in _services)
                {
                    sb.AppendLine($"  {kvp.Key.Name} → {kvp.Value.ImplementationType.Name} ({kvp.Value.Scope})");
                }
                
                return sb.ToString();
            }
        }
        
        #endregion
    }
}
