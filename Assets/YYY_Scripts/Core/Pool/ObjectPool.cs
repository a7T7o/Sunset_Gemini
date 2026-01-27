using System.Collections.Generic;
using UnityEngine;

namespace Sunset.Pool
{
    /// <summary>
    /// 泛型对象池
    /// 用于管理频繁创建/销毁的对象，减少GC压力
    /// 
    /// 使用示例：
    /// var pool = new ObjectPool<DroppedItem>(prefab, parent, 10);
    /// var item = pool.Spawn(position, rotation);
    /// pool.Despawn(item);
    /// </summary>
    /// <typeparam name="T">池化对象类型，必须是Component且实现IPoolable</typeparam>
    public class ObjectPool<T> where T : Component, IPoolable
    {
        private readonly Queue<T> _pool = new Queue<T>();
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly int _initialSize;
        private readonly int _maxSize;
        
        private int _totalCreated = 0;
        private int _activeCount = 0;
        
        /// <summary>
        /// 创建对象池
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="parent">父物体（用于组织层级）</param>
        /// <param name="initialSize">初始预热数量</param>
        /// <param name="maxSize">最大容量（0表示无限制）</param>
        public ObjectPool(T prefab, Transform parent = null, int initialSize = 10, int maxSize = 0)
        {
            _prefab = prefab;
            _parent = parent;
            _initialSize = initialSize;
            _maxSize = maxSize;
            
            // 预热
            Prewarm(initialSize);
        }
        
        /// <summary>
        /// 预热对象池
        /// </summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                T obj = CreateNew();
                obj.gameObject.SetActive(false);
                _pool.Enqueue(obj);
            }
        }
        
        /// <summary>
        /// 从池中获取对象
        /// </summary>
        public T Spawn(Vector3 position, Quaternion rotation)
        {
            T obj;
            
            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
                
                // 检查对象是否被意外销毁
                if (obj == null)
                {
                    obj = CreateNew();
                }
            }
            else
            {
                // 检查是否达到最大容量
                if (_maxSize > 0 && _totalCreated >= _maxSize)
                {
                    Debug.LogWarning($"[ObjectPool<{typeof(T).Name}>] 达到最大容量: {_maxSize}");
                    return null;
                }
                
                obj = CreateNew();
            }
            
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.gameObject.SetActive(true);
            obj.OnSpawn();
            
            _activeCount++;
            
            return obj;
        }
        
        /// <summary>
        /// 从池中获取对象（使用默认位置和旋转）
        /// </summary>
        public T Spawn()
        {
            return Spawn(Vector3.zero, Quaternion.identity);
        }
        
        /// <summary>
        /// 将对象返回池中
        /// </summary>
        public void Despawn(T obj)
        {
            if (obj == null) return;
            
            obj.OnDespawn();
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(_parent);
            
            _pool.Enqueue(obj);
            _activeCount--;
        }
        
        /// <summary>
        /// 延迟返回池中
        /// </summary>
        public void DespawnDelayed(T obj, float delay)
        {
            if (obj == null) return;
            
            PoolManager.Instance.DespawnDelayed(this, obj, delay);
        }
        
        /// <summary>
        /// 回收所有活动对象
        /// </summary>
        public void DespawnAll()
        {
            // 注意：这需要追踪所有活动对象
            // 简化实现：由使用者自行管理活动对象列表
            Debug.LogWarning($"[ObjectPool<{typeof(T).Name}>] DespawnAll需要使用者自行追踪活动对象");
        }
        
        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                T obj = _pool.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj.gameObject);
                }
            }
            
            _totalCreated = 0;
            _activeCount = 0;
        }
        
        /// <summary>
        /// 创建新对象
        /// </summary>
        private T CreateNew()
        {
            T obj = Object.Instantiate(_prefab, _parent);
            obj.name = $"{_prefab.name}_{_totalCreated}";
            _totalCreated++;
            return obj;
        }
        
        #region 属性
        
        /// <summary>池中可用对象数量</summary>
        public int AvailableCount => _pool.Count;
        
        /// <summary>当前活动对象数量</summary>
        public int ActiveCount => _activeCount;
        
        /// <summary>总创建数量</summary>
        public int TotalCreated => _totalCreated;
        
        /// <summary>预制体</summary>
        public T Prefab => _prefab;
        
        #endregion
    }
}
