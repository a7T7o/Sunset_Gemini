using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 树叶生成器 - 管理树叶粒子的生成和对象池
/// </summary>
public class LeafSpawner : MonoBehaviour
{
    #region 序列化字段
    
    [Header("树叶配置")]
    [Tooltip("树叶精灵数组")]
    public Sprite[] leafSprites;
    
    [Tooltip("每次命中生成的树叶数量")]
    [Range(1, 10)]
    public int leavesPerHit = 4;
    
    [Header("飘落参数")]
    [Tooltip("下落速度")]
    [Range(0.5f, 3f)]
    public float fallSpeed = 1.2f;
    
    [Tooltip("水平漂移速度")]
    [Range(0f, 1f)]
    public float driftSpeed = 0.3f;
    
    [Tooltip("旋转速度")]
    [Range(0f, 360f)]
    public float rotateSpeed = 90f;
    
    [Header("生成位置")]
    [Tooltip("从精灵顶部向下的比例（0=顶部，1=底部）")]
    [Range(0f, 0.5f)]
    public float spawnHeightRatio = 0.2f;
    
    [Tooltip("水平散布范围")]
    [Range(0f, 2f)]
    public float horizontalSpread = 0.5f;
    
    #endregion
    
    #region 私有字段
    
    private List<LeafFallEffect> _pool = new List<LeafFallEffect>();
    private Transform _poolParent;
    
    #endregion
    
    #region Unity 生命周期
    
    void Awake()
    {
        // 创建对象池父物体
        var poolObj = new GameObject("LeafPool");
        poolObj.transform.SetParent(transform);
        _poolParent = poolObj.transform;
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 生成树叶
    /// </summary>
    /// <param name="treeBounds">树木的精灵边界</param>
    public void SpawnLeaves(Bounds treeBounds)
    {
        if (leafSprites == null || leafSprites.Length == 0)
        {
            Debug.LogWarning("[LeafSpawner] 未配置树叶精灵");
            return;
        }
        
        // 计算生成区域
        float spawnY = treeBounds.max.y - treeBounds.size.y * spawnHeightRatio;
        float targetY = treeBounds.min.y; // 树根位置
        
        int count = Random.Range(leavesPerHit - 1, leavesPerHit + 2);
        
        for (int i = 0; i < count; i++)
        {
            var leaf = GetLeafFromPool();
            
            // 随机选择精灵
            Sprite sprite = leafSprites[Random.Range(0, leafSprites.Length)];
            
            // 随机生成位置
            float x = treeBounds.center.x + Random.Range(-horizontalSpread, horizontalSpread);
            float y = spawnY + Random.Range(-0.2f, 0.2f);
            Vector3 startPos = new Vector3(x, y, 0);
            
            // 随机参数
            float speed = fallSpeed * Random.Range(0.8f, 1.2f);
            float drift = driftSpeed * Random.Range(0.5f, 1.5f);
            float rotate = rotateSpeed * Random.Range(0.5f, 1.5f) * (Random.value > 0.5f ? 1 : -1);
            
            leaf.Activate(sprite, startPos, targetY, speed, drift, rotate);
        }
    }
    
    /// <summary>
    /// 回收树叶到对象池
    /// </summary>
    public void ReturnLeaf(LeafFallEffect leaf)
    {
        if (!_pool.Contains(leaf))
        {
            _pool.Add(leaf);
        }
    }
    
    #endregion
    
    #region 私有方法
    
    private LeafFallEffect GetLeafFromPool()
    {
        // 查找可用的
        foreach (var leaf in _pool)
        {
            if (!leaf.gameObject.activeInHierarchy)
            {
                return leaf;
            }
        }
        
        // 创建新的
        var newLeafObj = new GameObject("Leaf");
        newLeafObj.transform.SetParent(_poolParent);
        var newLeaf = newLeafObj.AddComponent<LeafFallEffect>();
        _pool.Add(newLeaf);
        
        return newLeaf;
    }
    
    #endregion
}
