using UnityEngine;
using System.Collections.Generic;
using FarmGame.Combat;

/// <summary>
/// 资源节点注册表 - 管理所有可交互资源节点的全局注册表
/// 
/// 用途：
/// - 集中管理所有 IResourceNode（树木、矿石等）
/// - 提供高效的范围查询接口
/// - 避免使用 Physics2D 检测，直接使用 Sprite Bounds
/// </summary>
public class ResourceNodeRegistry : MonoBehaviour
{
    #region 单例
    
    public static ResourceNodeRegistry Instance { get; private set; }
    
    #endregion
    
    #region 私有字段
    
    // 使用 InstanceID 作为 key，支持快速查找
    private Dictionary<int, IResourceNode> _registeredNodes = new Dictionary<int, IResourceNode>();
    
    // 缓存列表，避免每次查询都分配内存
    private List<IResourceNode> _queryResultCache = new List<IResourceNode>(32);
    
    #endregion
    
    #region 调试
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = false;
    
    #endregion
    
    #region Unity 生命周期
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ResourceNodeRegistry] 已存在实例，销毁重复的");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        if (showDebugInfo)
        {
            Debug.Log("<color=cyan>[ResourceNodeRegistry] 初始化完成</color>");
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 注册资源节点
    /// </summary>
    /// <param name="node">要注册的节点</param>
    /// <param name="instanceId">节点的 InstanceID（通常是 GameObject.GetInstanceID()）</param>
    public void Register(IResourceNode node, int instanceId)
    {
        if (node == null) return;
        
        if (_registeredNodes.ContainsKey(instanceId))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"[ResourceNodeRegistry] 节点已注册: {instanceId}");
            }
            return;
        }
        
        _registeredNodes[instanceId] = node;
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=green>[ResourceNodeRegistry] 注册节点: {instanceId}, 总数: {_registeredNodes.Count}</color>");
        }
    }
    
    /// <summary>
    /// 注销资源节点
    /// </summary>
    /// <param name="instanceId">节点的 InstanceID</param>
    public void Unregister(int instanceId)
    {
        if (_registeredNodes.Remove(instanceId))
        {
            if (showDebugInfo)
            {
                Debug.Log($"<color=yellow>[ResourceNodeRegistry] 注销节点: {instanceId}, 剩余: {_registeredNodes.Count}</color>");
            }
        }
    }
    
    /// <summary>
    /// 获取指定范围内的所有资源节点
    /// </summary>
    /// <param name="center">中心点（世界坐标）</param>
    /// <param name="radius">搜索半径</param>
    /// <returns>范围内的节点列表（注意：返回的是缓存列表，不要长期持有）</returns>
    public List<IResourceNode> GetNodesInRange(Vector2 center, float radius)
    {
        _queryResultCache.Clear();
        
        float radiusSqr = radius * radius;
        
        foreach (var kvp in _registeredNodes)
        {
            var node = kvp.Value;
            if (node == null || node.IsDepleted) continue;
            
            // 使用位置进行初步距离过滤
            Vector2 nodePos = node.GetPosition();
            float distSqr = (nodePos - center).sqrMagnitude;
            
            if (distSqr <= radiusSqr)
            {
                _queryResultCache.Add(node);
            }
        }
        
        return _queryResultCache;
    }
    
    /// <summary>
    /// 获取指定范围内指定标签的资源节点
    /// </summary>
    /// <param name="center">中心点</param>
    /// <param name="radius">搜索半径</param>
    /// <param name="resourceTag">资源标签（如 "Tree", "Rock"）</param>
    /// <returns>符合条件的节点列表</returns>
    public List<IResourceNode> GetNodesInRange(Vector2 center, float radius, string resourceTag)
    {
        _queryResultCache.Clear();
        
        float radiusSqr = radius * radius;
        
        foreach (var kvp in _registeredNodes)
        {
            var node = kvp.Value;
            if (node == null || node.IsDepleted) continue;
            if (node.ResourceTag != resourceTag) continue;
            
            Vector2 nodePos = node.GetPosition();
            float distSqr = (nodePos - center).sqrMagnitude;
            
            if (distSqr <= radiusSqr)
            {
                _queryResultCache.Add(node);
            }
        }
        
        return _queryResultCache;
    }
    
    /// <summary>
    /// 获取已注册节点数量
    /// </summary>
    public int RegisteredCount => _registeredNodes.Count;
    
    #endregion
    
    #region 编辑器方法
    
    #if UNITY_EDITOR
    [ContextMenu("显示已注册节点")]
    private void DEBUG_ShowRegisteredNodes()
    {
        Debug.Log($"[ResourceNodeRegistry] 已注册节点数: {_registeredNodes.Count}");
        foreach (var kvp in _registeredNodes)
        {
            var node = kvp.Value;
            Debug.Log($"  - ID: {kvp.Key}, Tag: {node?.ResourceTag}, Depleted: {node?.IsDepleted}");
        }
    }
    #endif
    
    #endregion
}
