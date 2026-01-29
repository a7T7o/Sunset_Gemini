using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FarmGame.Data.Core
{
    /// <summary>
    /// 持久化对象注册中心
    /// 
    /// 管理场景中所有需要存档的对象。
    /// 使用单例模式，跨场景持久化。
    /// 
    /// 职责：
    /// - 注册/注销持久化对象
    /// - 根据 GUID 查找对象
    /// - 提供遍历接口供 SaveManager 使用
    /// </summary>
    public class PersistentObjectRegistry : MonoBehaviour, IPersistentObjectRegistry
    {
        #region 单例
        
        private static PersistentObjectRegistry _instance;
        
        public static PersistentObjectRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 尝试查找现有实例
                    _instance = FindFirstObjectByType<PersistentObjectRegistry>();
                    
                    // 如果没有，创建新实例
                    if (_instance == null)
                    {
                        var go = new GameObject("[PersistentObjectRegistry]");
                        _instance = go.AddComponent<PersistentObjectRegistry>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region 字段
        
        /// <summary>
        /// 已注册的持久化对象（GUID -> 对象）
        /// </summary>
        private Dictionary<string, IPersistentObject> _registry = new Dictionary<string, IPersistentObject>();
        
        /// <summary>
        /// 按类型分组的对象（用于快速查询）
        /// </summary>
        private Dictionary<string, HashSet<IPersistentObject>> _byType = new Dictionary<string, HashSet<IPersistentObject>>();
        
        [Header("调试")]
        [SerializeField] private bool showDebugInfo = false;
        
        #endregion
        
        #region 属性
        
        /// <summary>
        /// 已注册对象数量
        /// </summary>
        public int Count => _registry.Count;
        
        #endregion
        
        #region Unity 生命周期
        
        private void Awake()
        {
            // 单例检查
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (showDebugInfo)
                Debug.Log("[PersistentObjectRegistry] 初始化完成");
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion
        
        #region IPersistentObjectRegistry 实现
        
        /// <summary>
        /// 注册持久化对象
        /// </summary>
        public void Register(IPersistentObject obj)
        {
            if (obj == null) return;
            
            string guid = obj.PersistentId;
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"[PersistentObjectRegistry] 对象 {obj.ObjectType} 没有有效的 PersistentId");
                return;
            }
            
            // 检查重复注册
            if (_registry.ContainsKey(guid))
            {
                if (_registry[guid] == obj)
                {
                    // 同一对象重复注册，忽略
                    return;
                }
                
                Debug.LogWarning($"[PersistentObjectRegistry] GUID 冲突: {guid}, 类型: {obj.ObjectType}");
                // 覆盖旧对象
            }
            
            _registry[guid] = obj;
            
            // 按类型分组
            string objType = obj.ObjectType;
            if (!_byType.ContainsKey(objType))
            {
                _byType[objType] = new HashSet<IPersistentObject>();
            }
            _byType[objType].Add(obj);
            
            if (showDebugInfo)
                Debug.Log($"[PersistentObjectRegistry] 注册: {objType}, GUID: {guid}, 总数: {_registry.Count}");
        }
        
        /// <summary>
        /// 注销持久化对象
        /// </summary>
        public void Unregister(IPersistentObject obj)
        {
            if (obj == null) return;
            
            string guid = obj.PersistentId;
            if (string.IsNullOrEmpty(guid)) return;
            
            if (_registry.Remove(guid))
            {
                // 从类型分组中移除
                string objType = obj.ObjectType;
                if (_byType.ContainsKey(objType))
                {
                    _byType[objType].Remove(obj);
                }
                
                if (showDebugInfo)
                    Debug.Log($"[PersistentObjectRegistry] 注销: {objType}, GUID: {guid}, 剩余: {_registry.Count}");
            }
        }
        
        /// <summary>
        /// 根据 GUID 查找对象
        /// </summary>
        public IPersistentObject FindByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            
            _registry.TryGetValue(guid, out var obj);
            return obj;
        }
        
        /// <summary>
        /// 获取所有持久化对象
        /// </summary>
        public IEnumerable<IPersistentObject> GetAll()
        {
            return _registry.Values;
        }
        
        /// <summary>
        /// 获取指定类型的所有对象
        /// </summary>
        public IEnumerable<T> GetAllOfType<T>() where T : IPersistentObject
        {
            return _registry.Values.OfType<T>();
        }
        
        #endregion
        
        #region 扩展方法
        
        /// <summary>
        /// 获取指定类型标识的所有对象
        /// </summary>
        public IEnumerable<IPersistentObject> GetAllByObjectType(string objectType)
        {
            if (_byType.TryGetValue(objectType, out var set))
            {
                return set;
            }
            return Enumerable.Empty<IPersistentObject>();
        }
        
        /// <summary>
        /// 检查 GUID 是否已注册
        /// </summary>
        public bool IsRegistered(string guid)
        {
            return !string.IsNullOrEmpty(guid) && _registry.ContainsKey(guid);
        }
        
        /// <summary>
        /// 清空所有注册（场景切换时调用）
        /// </summary>
        public void Clear()
        {
            _registry.Clear();
            _byType.Clear();
            
            if (showDebugInfo)
                Debug.Log("[PersistentObjectRegistry] 已清空所有注册");
        }
        
        /// <summary>
        /// 获取所有需要保存的对象
        /// </summary>
        public IEnumerable<IPersistentObject> GetAllSaveable()
        {
            return _registry.Values.Where(obj => obj.ShouldSave);
        }
        
        /// <summary>
        /// 收集所有对象的存档数据
        /// </summary>
        public List<WorldObjectSaveData> CollectAllSaveData()
        {
            var result = new List<WorldObjectSaveData>();
            
            foreach (var obj in GetAllSaveable())
            {
                try
                {
                    var data = obj.Save();
                    if (data != null)
                    {
                        result.Add(data);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PersistentObjectRegistry] 保存对象失败: {obj.ObjectType}, GUID: {obj.PersistentId}, 错误: {e.Message}");
                }
            }
            
            if (showDebugInfo)
                Debug.Log($"[PersistentObjectRegistry] 收集存档数据: {result.Count} 个对象");
            
            return result;
        }
        
        /// <summary>
        /// 恢复所有对象的状态
        /// </summary>
        public void RestoreAllFromSaveData(List<WorldObjectSaveData> dataList)
        {
            if (dataList == null) return;
            
            int restored = 0;
            int notFound = 0;
            
            foreach (var data in dataList)
            {
                var obj = FindByGuid(data.guid);
                if (obj != null)
                {
                    try
                    {
                        obj.Load(data);
                        restored++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[PersistentObjectRegistry] 恢复对象失败: {data.objectType}, GUID: {data.guid}, 错误: {e.Message}");
                    }
                }
                else
                {
                    notFound++;
                    if (showDebugInfo)
                        Debug.LogWarning($"[PersistentObjectRegistry] 找不到对象: {data.objectType}, GUID: {data.guid}");
                }
            }
            
            if (showDebugInfo)
                Debug.Log($"[PersistentObjectRegistry] 恢复完成: 成功 {restored}, 未找到 {notFound}");
        }
        
        #endregion
        
        #region 调试
        
#if UNITY_EDITOR
        [ContextMenu("打印所有注册对象")]
        private void DebugPrintAll()
        {
            Debug.Log($"[PersistentObjectRegistry] 已注册对象: {_registry.Count}");
            foreach (var kvp in _registry)
            {
                Debug.Log($"  - {kvp.Value.ObjectType}: {kvp.Key}");
            }
        }
        
        [ContextMenu("按类型统计")]
        private void DebugPrintByType()
        {
            Debug.Log($"[PersistentObjectRegistry] 按类型统计:");
            foreach (var kvp in _byType)
            {
                Debug.Log($"  - {kvp.Key}: {kvp.Value.Count}");
            }
        }
#endif
        
        #endregion
    }
}
