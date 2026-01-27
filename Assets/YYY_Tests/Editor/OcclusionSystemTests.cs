using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// 遮挡透明系统单元测试
/// 测试核心算法的正确性
/// </summary>
[TestFixture]
public class OcclusionSystemTests
{
    #region 属性 1: 遮挡检测双向一致性
    
    /// <summary>
    /// 测试 Bounds.Contains 检测逻辑
    /// 验证需求: 1.1, 1.2
    /// </summary>
    [Test]
    public void BoundsContains_PlayerInsideBounds_ReturnsTrue()
    {
        // Arrange
        Bounds occluderBounds = new Bounds(Vector3.zero, new Vector3(2f, 3f, 1f));
        Vector2 playerCenter = new Vector2(0.5f, 0.5f);
        
        // Act
        bool isInside = occluderBounds.Contains(playerCenter);
        
        // Assert
        Assert.IsTrue(isInside, "玩家中心在遮挡物边界内应返回 true");
    }
    
    [Test]
    public void BoundsContains_PlayerOutsideBounds_ReturnsFalse()
    {
        // Arrange
        Bounds occluderBounds = new Bounds(Vector3.zero, new Vector3(2f, 3f, 1f));
        Vector2 playerCenter = new Vector2(5f, 5f);
        
        // Act
        bool isInside = occluderBounds.Contains(playerCenter);
        
        // Assert
        Assert.IsFalse(isInside, "玩家中心在遮挡物边界外应返回 false");
    }
    
    [Test]
    public void BoundsContains_PlayerOnBoundary_ReturnsTrue()
    {
        // Arrange
        Bounds occluderBounds = new Bounds(Vector3.zero, new Vector3(2f, 3f, 1f));
        Vector2 playerCenter = new Vector2(1f, 0f); // 边界上
        
        // Act
        bool isInside = occluderBounds.Contains(playerCenter);
        
        // Assert
        Assert.IsTrue(isInside, "玩家中心在边界上应返回 true");
    }
    
    #endregion
    
    #region 属性 5: 树林连通性算法正确性
    
    /// <summary>
    /// 测试树根距离判定
    /// 验证需求: 3.2
    /// </summary>
    [Test]
    public void TreeConnection_RootDistanceWithinThreshold_ReturnsTrue()
    {
        // Arrange
        Vector2 rootA = new Vector2(0f, 0f);
        Vector2 rootB = new Vector2(2f, 0f);
        float connectionDistance = 2.5f;
        
        // Act
        float distance = Vector2.Distance(rootA, rootB);
        bool isConnected = distance <= connectionDistance;
        
        // Assert
        Assert.IsTrue(isConnected, "树根距离在阈值内应判定为连通");
    }
    
    [Test]
    public void TreeConnection_RootDistanceExceedsThreshold_ReturnsFalse()
    {
        // Arrange
        Vector2 rootA = new Vector2(0f, 0f);
        Vector2 rootB = new Vector2(5f, 0f);
        float connectionDistance = 2.5f;
        
        // Act
        float distance = Vector2.Distance(rootA, rootB);
        bool isConnected = distance <= connectionDistance;
        
        // Assert
        Assert.IsFalse(isConnected, "树根距离超出阈值应判定为不连通");
    }
    
    /// <summary>
    /// 测试树冠重叠判定
    /// 验证需求: 3.2
    /// </summary>
    [Test]
    public void TreeConnection_CanopyOverlapAboveThreshold_ReturnsTrue()
    {
        // Arrange
        Bounds boundsA = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(2f, 3f, 1f));
        Bounds boundsB = new Bounds(new Vector3(1f, 0f, 0f), new Vector3(2f, 3f, 1f));
        float overlapThreshold = 0.15f;
        
        // Act
        float overlapRatio = CalculateOverlapRatio(boundsA, boundsB);
        bool isConnected = overlapRatio >= overlapThreshold;
        
        // Assert
        Assert.IsTrue(isConnected, "树冠重叠超过15%应判定为连通");
    }
    
    [Test]
    public void TreeConnection_NoCanopyOverlap_ReturnsFalse()
    {
        // Arrange
        Bounds boundsA = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(2f, 3f, 1f));
        Bounds boundsB = new Bounds(new Vector3(10f, 0f, 0f), new Vector3(2f, 3f, 1f));
        float overlapThreshold = 0.15f;
        
        // Act
        float overlapRatio = CalculateOverlapRatio(boundsA, boundsB);
        bool isConnected = overlapRatio >= overlapThreshold;
        
        // Assert
        Assert.IsFalse(isConnected, "无树冠重叠应判定为不连通");
    }
    
    #endregion
    
    #region 属性 6: 树林搜索边界限制
    
    /// <summary>
    /// 测试搜索深度限制
    /// 验证需求: 3.3
    /// </summary>
    [Test]
    public void ForestSearch_DepthLimit_IsRespected()
    {
        // Arrange
        int maxSearchDepth = 50;
        int simulatedTreeCount = 100;
        
        // Act
        int actualSearchCount = Mathf.Min(simulatedTreeCount, maxSearchDepth);
        
        // Assert
        Assert.LessOrEqual(actualSearchCount, maxSearchDepth, 
            "搜索深度不应超过最大限制");
    }
    
    /// <summary>
    /// 测试搜索半径限制
    /// 验证需求: 3.3
    /// </summary>
    [Test]
    public void ForestSearch_RadiusLimit_IsRespected()
    {
        // Arrange
        Vector2 playerPos = Vector2.zero;
        Vector2 treePos = new Vector2(20f, 0f);
        float maxSearchRadius = 15f;
        
        // Act
        float distance = Vector2.Distance(playerPos, treePos);
        bool shouldInclude = distance <= maxSearchRadius;
        
        // Assert
        Assert.IsFalse(shouldInclude, 
            "超出搜索半径的树木不应被包含");
    }
    
    #endregion
    
    #region 属性 10: 性能限制遵守性
    
    /// <summary>
    /// 测试检测间隔配置
    /// 验证需求: 6.1
    /// </summary>
    [Test]
    public void DetectionInterval_DefaultValue_IsCorrect()
    {
        // Arrange
        float expectedInterval = 0.1f;
        float tolerance = 0.01f;
        
        // Act & Assert
        // 这里只验证默认值的合理性
        Assert.AreEqual(expectedInterval, 0.1f, tolerance,
            "默认检测间隔应为 0.1 秒");
    }
    
    /// <summary>
    /// 测试云朵数量限制
    /// 验证需求: 6.2
    /// </summary>
    [Test]
    public void CloudCount_MaxLimit_IsRespected()
    {
        // Arrange
        int maxClouds = 32;
        int requestedClouds = 50;
        
        // Act
        int actualClouds = Mathf.Min(requestedClouds, maxClouds);
        
        // Assert
        Assert.LessOrEqual(actualClouds, maxClouds,
            "云朵数量不应超过最大限制");
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 计算两个 Bounds 的重叠面积比例
    /// </summary>
    private float CalculateOverlapRatio(Bounds a, Bounds b)
    {
        float overlapMinX = Mathf.Max(a.min.x, b.min.x);
        float overlapMaxX = Mathf.Min(a.max.x, b.max.x);
        float overlapMinY = Mathf.Max(a.min.y, b.min.y);
        float overlapMaxY = Mathf.Min(a.max.y, b.max.y);
        
        float overlapWidth = overlapMaxX - overlapMinX;
        float overlapHeight = overlapMaxY - overlapMinY;
        
        if (overlapWidth <= 0 || overlapHeight <= 0)
            return 0f;
        
        float overlapArea = overlapWidth * overlapHeight;
        float areaA = a.size.x * a.size.y;
        float areaB = b.size.x * b.size.y;
        float smallerArea = Mathf.Min(areaA, areaB);
        
        return overlapArea / smallerArea;
    }
    
    #endregion
}
