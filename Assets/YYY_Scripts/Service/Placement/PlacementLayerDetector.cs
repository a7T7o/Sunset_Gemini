using UnityEngine;

/// <summary>
/// 放置 Layer 检测器
/// 负责检测点击位置和玩家的 Layer
/// </summary>
public static class PlacementLayerDetector
{
    #region 常量
    
    /// <summary>默认 Layer 名称</summary>
    private const string DEFAULT_LAYER_NAME = "Default";
    
    /// <summary>地面检测的 Layer 名称列表</summary>
    private static readonly string[] GROUND_LAYER_NAMES = new string[]
    {
        "Ground",
        "Terrain",
        "LAYER 1",
        "LAYER 2",
        "LAYER 3"
    };
    
    #endregion
    
    #region Layer 检测
    
    /// <summary>
    /// 获取点击位置的 Layer（通过 Raycast 检测地面）
    /// </summary>
    /// <param name="worldPosition">世界坐标</param>
    /// <returns>检测到的 Layer，未检测到返回 Default Layer</returns>
    public static int GetLayerAtPosition(Vector3 worldPosition)
    {
        // 构建地面检测的 LayerMask
        int groundMask = 0;
        foreach (var layerName in GROUND_LAYER_NAMES)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                groundMask |= (1 << layer);
            }
        }
        
        // 如果没有有效的地面 Layer，返回默认
        if (groundMask == 0)
        {
            return LayerMask.NameToLayer(DEFAULT_LAYER_NAME);
        }
        
        // 使用 OverlapPoint 检测该位置的 Collider
        Collider2D hit = Physics2D.OverlapPoint(worldPosition, groundMask);
        if (hit != null)
        {
            return hit.gameObject.layer;
        }
        
        // 尝试使用 Raycast 从上方检测
        RaycastHit2D rayHit = Physics2D.Raycast(
            new Vector2(worldPosition.x, worldPosition.y + 10f),
            Vector2.down,
            20f,
            groundMask
        );
        
        if (rayHit.collider != null)
        {
            return rayHit.collider.gameObject.layer;
        }
        
        // 未检测到，返回默认 Layer
        return LayerMask.NameToLayer(DEFAULT_LAYER_NAME);
    }
    
    /// <summary>
    /// 获取点击位置的 Sorting Layer（通过检测 SpriteRenderer）
    /// </summary>
    /// <param name="worldPosition">世界坐标</param>
    /// <returns>Sorting Layer 名称</returns>
    public static string GetSortingLayerAtPosition(Vector3 worldPosition)
    {
        // 检测该位置的所有 Collider
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition);
        
        foreach (var hit in hits)
        {
            // 查找 SpriteRenderer
            var spriteRenderer = hit.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = hit.GetComponentInParent<SpriteRenderer>();
            }
            
            if (spriteRenderer != null)
            {
                return spriteRenderer.sortingLayerName;
            }
        }
        
        // 默认返回 "Default"
        return "Default";
    }
    
    #endregion
    
    #region 玩家 Layer 检测
    
    /// <summary>
    /// 获取玩家当前所在的 Layer
    /// 注意：这里返回的是玩家脚下地面的 Layer，而不是玩家 GameObject 的 Layer
    /// </summary>
    /// <param name="playerTransform">玩家 Transform</param>
    /// <returns>玩家脚下地面的 Layer</returns>
    public static int GetPlayerLayer(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            return LayerMask.NameToLayer(DEFAULT_LAYER_NAME);
        }
        
        // 获取玩家 Collider 的中心位置
        var playerCollider = playerTransform.GetComponent<Collider2D>();
        Vector3 playerCenter = playerCollider != null
            ? playerCollider.bounds.center
            : playerTransform.position;
        
        // 检测玩家脚下的地面 Layer
        return GetLayerAtPosition(playerCenter);
    }
    
    /// <summary>
    /// 获取玩家当前的 Sorting Layer
    /// </summary>
    /// <param name="playerTransform">玩家 Transform</param>
    /// <returns>Sorting Layer 名称</returns>
    public static string GetPlayerSortingLayer(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            return "Default";
        }
        
        var spriteRenderer = playerTransform.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            return spriteRenderer.sortingLayerName;
        }
        
        return "Default";
    }
    
    #endregion
    
    #region Layer 比较
    
    /// <summary>
    /// 检查两个 Layer 是否一致
    /// </summary>
    public static bool IsLayerMatch(int layerA, int layerB)
    {
        return layerA == layerB;
    }
    
    /// <summary>
    /// 检查放置位置的 Layer 是否与玩家一致
    /// </summary>
    /// <param name="worldPosition">放置位置</param>
    /// <param name="playerTransform">玩家 Transform</param>
    /// <returns>true 表示一致，可以放置</returns>
    public static bool IsPlacementLayerValid(Vector3 worldPosition, Transform playerTransform)
    {
        int positionLayer = GetLayerAtPosition(worldPosition);
        int playerLayer = GetPlayerLayer(playerTransform);
        
        return IsLayerMatch(positionLayer, playerLayer);
    }
    
    /// <summary>
    /// 检查 Sorting Layer 是否一致
    /// </summary>
    public static bool IsSortingLayerMatch(string layerA, string layerB)
    {
        return string.Equals(layerA, layerB, System.StringComparison.Ordinal);
    }
    
    #endregion
    
    #region Layer 同步
    
    /// <summary>
    /// 将 Layer 同步到 GameObject 及其所有子物体
    /// </summary>
    /// <param name="root">根物体</param>
    /// <param name="layer">目标 Layer</param>
    public static void SyncLayerToAllChildren(GameObject root, int layer)
    {
        if (root == null) return;
        
        // 设置根物体
        root.layer = layer;
        
        // 递归设置所有子物体
        foreach (Transform child in root.transform)
        {
            SyncLayerToAllChildren(child.gameObject, layer);
        }
    }
    
    /// <summary>
    /// 将 Sorting Layer 同步到所有 SpriteRenderer
    /// </summary>
    /// <param name="root">根物体</param>
    /// <param name="sortingLayerName">目标 Sorting Layer 名称</param>
    public static void SyncSortingLayerToAllRenderers(GameObject root, string sortingLayerName)
    {
        if (root == null) return;
        
        var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var renderer in renderers)
        {
            renderer.sortingLayerName = sortingLayerName;
        }
    }
    
    #endregion
}
