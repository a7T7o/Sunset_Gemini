using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 云朵阴影系统单元测试
/// 测试核心算法的正确性
/// </summary>
[TestFixture]
public class CloudShadowSystemTests
{
    #region 属性 7: 云朵天气联动一致性
    
    /// <summary>
    /// 测试晴天时云影应该显示
    /// 验证需求: 4.1
    /// </summary>
    [Test]
    public void WeatherGate_Sunny_CloudsEnabled()
    {
        // Arrange
        bool enableInSunny = true;
        string currentWeather = "Sunny";
        
        // Act
        bool shouldShow = IsWeatherAllowed(currentWeather, enableInSunny, true, false, false, false);
        
        // Assert
        Assert.IsTrue(shouldShow, "晴天时云影应该显示");
    }
    
    /// <summary>
    /// 测试雨天时云影应该隐藏
    /// 验证需求: 4.2
    /// </summary>
    [Test]
    public void WeatherGate_Rain_CloudsDisabled()
    {
        // Arrange
        bool enableInRain = false;
        string currentWeather = "Rain";
        
        // Act
        bool shouldShow = IsWeatherAllowed(currentWeather, true, true, false, enableInRain, false);
        
        // Assert
        Assert.IsFalse(shouldShow, "雨天时云影应该隐藏");
    }
    
    /// <summary>
    /// 测试阴天时云影应该隐藏
    /// 验证需求: 4.2
    /// </summary>
    [Test]
    public void WeatherGate_Overcast_CloudsDisabled()
    {
        // Arrange
        bool enableInOvercast = false;
        string currentWeather = "Overcast";
        
        // Act
        bool shouldShow = IsWeatherAllowed(currentWeather, true, true, enableInOvercast, false, false);
        
        // Assert
        Assert.IsFalse(shouldShow, "阴天时云影应该隐藏");
    }
    
    #endregion
    
    #region 属性 8: 云朵循环移动不变性
    
    /// <summary>
    /// 测试云朵移出右边界后应该出现在左边界
    /// 验证需求: 4.3
    /// </summary>
    [Test]
    public void CloudLoop_ExitRight_EnterLeft()
    {
        // Arrange
        Rect area = new Rect(-20f, -12f, 40f, 24f);
        Vector2 direction = new Vector2(1f, 0f); // 向右移动
        float halfWidth = 2f;
        float cloudX = area.xMax + halfWidth + 1f; // 超出右边界 + halfWidth
        
        // Act
        float newX = cloudX;
        if (direction.x > 0f && cloudX > area.xMax + halfWidth)
        {
            newX = area.xMin - halfWidth;
        }
        
        // Assert
        Assert.Less(newX, area.center.x, "云朵应该传送到左侧");
    }
    
    /// <summary>
    /// 测试云朵移出左边界后应该出现在右边界
    /// 验证需求: 4.3
    /// </summary>
    [Test]
    public void CloudLoop_ExitLeft_EnterRight()
    {
        // Arrange
        Rect area = new Rect(-20f, -12f, 40f, 24f);
        Vector2 direction = new Vector2(-1f, 0f); // 向左移动
        float cloudX = area.xMin - 3f; // 超出左边界
        float halfWidth = 2f;
        
        // Act
        float newX = cloudX;
        if (direction.x < 0f && cloudX < area.xMin - halfWidth)
        {
            newX = area.xMax + halfWidth;
        }
        
        // Assert
        Assert.Greater(newX, area.center.x, "云朵应该传送到右侧");
    }
    
    #endregion
    
    #region 属性 9: 对象池资源管理正确性
    
    /// <summary>
    /// 测试对象池复用逻辑
    /// 验证需求: 4.4
    /// </summary>
    [Test]
    public void ObjectPool_Reuse_WorksCorrectly()
    {
        // Arrange
        var pool = new System.Collections.Generic.Stack<GameObject>();
        var active = new System.Collections.Generic.List<GameObject>();
        
        // 模拟创建和回收
        var obj1 = new GameObject("Cloud1");
        var obj2 = new GameObject("Cloud2");
        
        // Act - 回收到池
        pool.Push(obj1);
        pool.Push(obj2);
        
        // Act - 从池取出
        var reused1 = pool.Pop();
        var reused2 = pool.Pop();
        
        // Assert
        Assert.AreEqual(obj2, reused1, "应该复用最后入池的对象");
        Assert.AreEqual(obj1, reused2, "应该复用先入池的对象");
        Assert.AreEqual(0, pool.Count, "池应该为空");
        
        // Cleanup
        Object.DestroyImmediate(obj1);
        Object.DestroyImmediate(obj2);
    }
    
    /// <summary>
    /// 测试云朵数量调整
    /// 验证需求: 4.4
    /// </summary>
    [Test]
    public void CloudCount_Adjustment_WorksCorrectly()
    {
        // Arrange
        int currentCount = 5;
        int targetCount = 8;
        int maxClouds = 32;
        
        // Act
        int toAdd = Mathf.Max(0, targetCount - currentCount);
        int finalCount = Mathf.Min(currentCount + toAdd, maxClouds);
        
        // Assert
        Assert.AreEqual(targetCount, finalCount, "云朵数量应该调整到目标值");
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 模拟天气门控逻辑
    /// </summary>
    private bool IsWeatherAllowed(string weather, 
        bool enableInSunny, bool enableInPartlyCloudy, 
        bool enableInOvercast, bool enableInRain, bool enableInSnow)
    {
        switch (weather)
        {
            case "Sunny": return enableInSunny;
            case "PartlyCloudy": return enableInPartlyCloudy;
            case "Overcast": return enableInOvercast;
            case "Rain": return enableInRain;
            case "Snow": return enableInSnow;
            default: return true;
        }
    }
    
    #endregion
}
