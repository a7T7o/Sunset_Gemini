using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sunset.Pool
{
    /// <summary>
    /// 对象池管理器
    /// 提供全局对象池管理功能
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        private static PoolManager _instance;
        
        public static PoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("[PoolManager]");
                    _instance = go.AddComponent<PoolManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        // 池容器（按预制体名称索引）
        private readonly Dictionary<string, object> _pools = new Dictionary<string, object>();
        
        // 池父物体
        private Transform _poolRoot;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            // 创建池根物体
            _poolRoot = new GameObject("PoolRoot").transform;
            _poolRoot.SetParent(transform);
        }
        
        /// <summary>
        /// 获取或创建对象池
        /// </summary>
        public ObjectPool<T> GetOrCreatePool<T>(T prefab, int initialSize = 10, int maxSize = 0) 
            where T : Component, IPoolable
        {
            string key = prefab.name;
            
            if (_pools.TryGetValue(key, out var existingPool))
            {
                return existingPool as ObjectPool<T>;
            }
            
            // 创建池容器
            Transform poolParent = new GameObject($"Pool_{key}").transform;
            poolParent.SetParent(_poolRoot);
            
            // 创建新池
            var pool = new ObjectPool<T>(prefab, poolParent, initialSize, maxSize);
            _pools[key] = pool;
            
            Debug.Log($"<color=cyan>[PoolManager] 创建对象池: {key} (初始:{initialSize}, 最大:{maxSize})</color>");
            
            return pool;
        }
        
        /// <summary>
        /// 获取对象池
        /// </summary>
        public ObjectPool<T> GetPool<T>(string prefabName) where T : Component, IPoolable
        {
            if (_pools.TryGetValue(prefabName, out var pool))
            {
                return pool as ObjectPool<T>;
            }
            return null;
        }
        
        /// <summary>
        /// 从池中获取对象（便捷方法）
        /// </summary>
        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, int initialSize = 10) 
            where T : Component, IPoolable
        {
            var pool = GetOrCreatePool(prefab, initialSize);
            return pool.Spawn(position, rotation);
        }
        
        /// <summary>
        /// 将对象返回池中（便捷方法）
        /// </summary>
        public void Despawn<T>(T obj) where T : Component, IPoolable
        {
            if (obj == null) return;
            
            // 尝试找到对应的池
            string baseName = obj.name;
            // 移除实例化后缀（如 "Prefab_0" -> "Prefab"）
            int underscoreIndex = baseName.LastIndexOf('_');
            if (underscoreIndex > 0)
            {
                string possibleNumber = baseName.Substring(underscoreIndex + 1);
                if (int.TryParse(possibleNumber, out _))
                {
                    baseName = baseName.Substring(0, underscoreIndex);
                }
            }
            
            if (_pools.TryGetValue(baseName, out var pool))
            {
                (pool as ObjectPool<T>)?.Despawn(obj);
            }
            else
            {
                // 没有找到池，直接销毁
                Destroy(obj.gameObject);
            }
        }
        
        /// <summary>
        /// 延迟返回池中
        /// </summary>
        public void DespawnDelayed<T>(ObjectPool<T> pool, T obj, float delay) 
            where T : Component, IPoolable
        {
            StartCoroutine(DespawnDelayedCoroutine(pool, obj, delay));
        }
        
        private IEnumerator DespawnDelayedCoroutine<T>(ObjectPool<T> pool, T obj, float delay) 
            where T : Component, IPoolable
        {
            yield return new WaitForSeconds(delay);
            pool.Despawn(obj);
        }
        
        /// <summary>
        /// 清空指定池
        /// </summary>
        public void ClearPool(string prefabName)
        {
            if (_pools.TryGetValue(prefabName, out var pool))
            {
                // 使用反射调用Clear方法
                var clearMethod = pool.GetType().GetMethod("Clear");
                clearMethod?.Invoke(pool, null);
                
                _pools.Remove(prefabName);
            }
        }
        
        /// <summary>
        /// 清空所有池
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var kvp in _pools)
            {
                var clearMethod = kvp.Value.GetType().GetMethod("Clear");
                clearMethod?.Invoke(kvp.Value, null);
            }
            
            _pools.Clear();
            
            // 清理池根物体下的所有子物体
            if (_poolRoot != null)
            {
                foreach (Transform child in _poolRoot)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        
        /// <summary>
        /// 获取池状态信息（调试用）
        /// </summary>
        public string GetPoolsInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== 对象池状态 ===");
            
            foreach (var kvp in _pools)
            {
                var availableProperty = kvp.Value.GetType().GetProperty("AvailableCount");
                var activeProperty = kvp.Value.GetType().GetProperty("ActiveCount");
                var totalProperty = kvp.Value.GetType().GetProperty("TotalCreated");
                
                int available = (int)(availableProperty?.GetValue(kvp.Value) ?? 0);
                int active = (int)(activeProperty?.GetValue(kvp.Value) ?? 0);
                int total = (int)(totalProperty?.GetValue(kvp.Value) ?? 0);
                
                sb.AppendLine($"  {kvp.Key}: 可用={available}, 活动={active}, 总计={total}");
            }
            
            return sb.ToString();
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                ClearAllPools();
                _instance = null;
            }
        }
    }
}
