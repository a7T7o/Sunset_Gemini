using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sunset.Events
{
    /// <summary>
    /// 事件订阅者信息
    /// </summary>
    internal class EventSubscription
    {
        public Delegate Handler { get; set; }
        public int Priority { get; set; }
        public object Owner { get; set; } // 用于自动取消订阅
    }
    
    /// <summary>
    /// 统一事件总线系统
    /// 提供类型安全的事件发布/订阅机制
    /// 
    /// 特性：
    /// - 泛型事件类型支持
    /// - 事件优先级（0-100，数字越大优先级越高）
    /// - 自动管理订阅生命周期（MonoBehaviour销毁时自动取消订阅）
    /// - 事件历史记录（调试用）
    /// - 线程安全
    /// </summary>
    public static class EventBus
    {
        // 事件订阅者字典：事件类型 -> 订阅者列表
        private static readonly Dictionary<Type, List<EventSubscription>> _subscriptions 
            = new Dictionary<Type, List<EventSubscription>>();
        
        // 事件历史记录（调试用）
        private static readonly Queue<EventHistoryEntry> _eventHistory = new Queue<EventHistoryEntry>();
        private static int _maxHistorySize = 100;
        
        // 是否启用调试日志
        private static bool _enableDebugLog = false;
        
        // 锁对象
        private static readonly object _lock = new object();
        
        #region 订阅方法
        
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        /// <param name="priority">优先级（0-100，默认50）</param>
        /// <param name="owner">所有者（用于自动取消订阅，通常传this）</param>
        public static void Subscribe<T>(Action<T> handler, int priority = 50, object owner = null) 
            where T : IGameEvent
        {
            if (handler == null)
            {
                Debug.LogWarning("[EventBus] 尝试订阅空处理器");
                return;
            }
            
            lock (_lock)
            {
                Type eventType = typeof(T);
                
                if (!_subscriptions.ContainsKey(eventType))
                {
                    _subscriptions[eventType] = new List<EventSubscription>();
                }
                
                // 检查是否已订阅（避免重复订阅）
                var existingSubscription = _subscriptions[eventType].Find(
                    s => s.Handler.Equals(handler) && s.Owner == owner);
                
                if (existingSubscription != null)
                {
                    if (_enableDebugLog)
                    {
                        Debug.LogWarning($"[EventBus] 重复订阅: {eventType.Name}");
                    }
                    return;
                }
                
                var subscription = new EventSubscription
                {
                    Handler = handler,
                    Priority = Mathf.Clamp(priority, 0, 100),
                    Owner = owner
                };
                
                _subscriptions[eventType].Add(subscription);
                
                // 按优先级排序（高优先级在前）
                _subscriptions[eventType].Sort((a, b) => b.Priority.CompareTo(a.Priority));
                
                if (_enableDebugLog)
                {
                    Debug.Log($"<color=cyan>[EventBus] 订阅: {eventType.Name} (优先级:{priority})</color>");
                }
            }
        }
        
        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null) return;
            
            lock (_lock)
            {
                Type eventType = typeof(T);
                
                if (!_subscriptions.ContainsKey(eventType)) return;
                
                _subscriptions[eventType].RemoveAll(s => s.Handler.Equals(handler));
                
                if (_enableDebugLog)
                {
                    Debug.Log($"<color=yellow>[EventBus] 取消订阅: {eventType.Name}</color>");
                }
            }
        }
        
        /// <summary>
        /// 取消指定所有者的所有订阅
        /// </summary>
        public static void UnsubscribeAll(object owner)
        {
            if (owner == null) return;
            
            lock (_lock)
            {
                foreach (var kvp in _subscriptions)
                {
                    kvp.Value.RemoveAll(s => s.Owner == owner);
                }
                
                if (_enableDebugLog)
                {
                    Debug.Log($"<color=yellow>[EventBus] 取消所有订阅: {owner.GetType().Name}</color>");
                }
            }
        }
        
        #endregion
        
        #region 发布方法
        
        /// <summary>
        /// 发布事件
        /// </summary>
        public static void Publish<T>(T eventData) where T : IGameEvent
        {
            if (eventData == null)
            {
                Debug.LogWarning("[EventBus] 尝试发布空事件");
                return;
            }
            
            Type eventType = typeof(T);
            List<EventSubscription> subscriptionsCopy;
            
            lock (_lock)
            {
                if (!_subscriptions.ContainsKey(eventType) || _subscriptions[eventType].Count == 0)
                {
                    if (_enableDebugLog)
                    {
                        Debug.Log($"<color=gray>[EventBus] 无订阅者: {eventType.Name}</color>");
                    }
                    return;
                }
                
                // 复制订阅列表，避免在遍历时修改
                subscriptionsCopy = new List<EventSubscription>(_subscriptions[eventType]);
            }
            
            // 记录事件历史
            RecordEventHistory(eventType.Name, subscriptionsCopy.Count);
            
            if (_enableDebugLog)
            {
                Debug.Log($"<color=green>[EventBus] 发布: {eventType.Name} → {subscriptionsCopy.Count}个订阅者</color>");
            }
            
            // 按优先级顺序调用处理器
            foreach (var subscription in subscriptionsCopy)
            {
                try
                {
                    // 检查所有者是否已销毁（MonoBehaviour）
                    if (subscription.Owner is MonoBehaviour mb && mb == null)
                    {
                        // 所有者已销毁，跳过并稍后清理
                        continue;
                    }
                    
                    var handler = subscription.Handler as Action<T>;
                    handler?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] 事件处理器异常: {eventType.Name}\n{ex}");
                }
            }
            
            // 清理已销毁的订阅者
            CleanupDestroyedSubscribers(eventType);
        }
        
        /// <summary>
        /// 延迟发布事件（下一帧执行）
        /// </summary>
        public static void PublishDelayed<T>(T eventData, float delay = 0f) where T : IGameEvent
        {
            if (delay <= 0f)
            {
                // 使用协程在下一帧发布
                EventBusRunner.Instance.PublishNextFrame(eventData);
            }
            else
            {
                // 使用协程延迟发布
                EventBusRunner.Instance.PublishAfterDelay(eventData, delay);
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 清理已销毁的订阅者
        /// </summary>
        private static void CleanupDestroyedSubscribers(Type eventType)
        {
            lock (_lock)
            {
                if (!_subscriptions.ContainsKey(eventType)) return;
                
                _subscriptions[eventType].RemoveAll(s =>
                {
                    if (s.Owner is MonoBehaviour mb)
                    {
                        return mb == null;
                    }
                    return false;
                });
            }
        }
        
        /// <summary>
        /// 记录事件历史
        /// </summary>
        private static void RecordEventHistory(string eventName, int subscriberCount)
        {
            lock (_lock)
            {
                _eventHistory.Enqueue(new EventHistoryEntry
                {
                    EventName = eventName,
                    Timestamp = Time.time,
                    SubscriberCount = subscriberCount
                });
                
                while (_eventHistory.Count > _maxHistorySize)
                {
                    _eventHistory.Dequeue();
                }
            }
        }
        
        /// <summary>
        /// 获取事件历史（调试用）
        /// </summary>
        public static EventHistoryEntry[] GetEventHistory()
        {
            lock (_lock)
            {
                return _eventHistory.ToArray();
            }
        }
        
        /// <summary>
        /// 清空所有订阅
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock)
            {
                _subscriptions.Clear();
                _eventHistory.Clear();
                
                if (_enableDebugLog)
                {
                    Debug.Log("<color=red>[EventBus] 清空所有订阅</color>");
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
        /// 获取订阅者数量
        /// </summary>
        public static int GetSubscriberCount<T>() where T : IGameEvent
        {
            lock (_lock)
            {
                Type eventType = typeof(T);
                if (_subscriptions.ContainsKey(eventType))
                {
                    return _subscriptions[eventType].Count;
                }
                return 0;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// 事件历史记录条目
    /// </summary>
    public struct EventHistoryEntry
    {
        public string EventName;
        public float Timestamp;
        public int SubscriberCount;
    }
}
