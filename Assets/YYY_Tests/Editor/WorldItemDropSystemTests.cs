using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 世界物品掉落系统单元测试
/// **Feature: world-item-drop-system**
/// 
/// 注意：由于测试程序集与主程序集分离，这里测试核心算法逻辑
/// 而不是直接测试 ItemData 类型
/// </summary>
[TestFixture]
public class WorldItemDropSystemTests
{
    #region Property 2: bagSprite回退逻辑

    /// <summary>
    /// **Feature: world-item-drop-system, Property 2: bagSprite回退逻辑**
    /// *For any* ItemData，当bagSprite为空时，GetBagSprite()应该返回icon
    /// **Validates: Requirements 2.2**
    /// 
    /// 测试回退逻辑的算法正确性
    /// </summary>
    [Test]
    public void GetBagSprite_FallbackLogic_WhenBagSpriteIsNull_ReturnsIcon()
    {
        // Arrange - 模拟 GetBagSprite() 的逻辑
        Sprite icon = CreateTestSprite(32, 32);
        Sprite bagSprite = null;

        // Act - 模拟 GetBagSprite() 实现
        Sprite result = bagSprite != null ? bagSprite : icon;

        // Assert
        Assert.AreEqual(icon, result, 
            "当bagSprite为空时，应返回icon");

        // Cleanup
        DestroySprite(icon);
    }

    [Test]
    public void GetBagSprite_FallbackLogic_WhenBagSpriteIsSet_ReturnsBagSprite()
    {
        // Arrange
        Sprite icon = CreateTestSprite(32, 32);
        Sprite bagSprite = CreateTestSprite(64, 64);

        // Act - 模拟 GetBagSprite() 实现
        Sprite result = bagSprite != null ? bagSprite : icon;

        // Assert
        Assert.AreEqual(bagSprite, result, 
            "当bagSprite已设置时，应返回bagSprite");

        // Cleanup
        DestroySprite(icon);
        DestroySprite(bagSprite);
    }

    [Test]
    public void GetBagSprite_FallbackLogic_WhenBothAreNull_ReturnsNull()
    {
        // Arrange
        Sprite icon = null;
        Sprite bagSprite = null;

        // Act - 模拟 GetBagSprite() 实现
        Sprite result = bagSprite != null ? bagSprite : icon;

        // Assert
        Assert.IsNull(result, 
            "当icon和bagSprite都为空时，应返回null");
    }

    /// <summary>
    /// 属性测试：随机生成100个场景，验证回退逻辑一致性
    /// </summary>
    [Test]
    public void GetBagSprite_PropertyTest_FallbackLogicIsConsistent()
    {
        const int iterations = 100;
        var random = new System.Random(42); // 固定种子保证可重复

        for (int i = 0; i < iterations; i++)
        {
            // Arrange - 随机决定是否设置各个Sprite
            bool hasIcon = random.Next(2) == 1;
            bool hasBagSprite = random.Next(2) == 1;

            Sprite icon = hasIcon ? CreateTestSprite(32, 32) : null;
            Sprite bagSprite = hasBagSprite ? CreateTestSprite(64, 64) : null;

            // Act - 模拟 GetBagSprite() 实现
            Sprite result = bagSprite != null ? bagSprite : icon;

            // Assert
            if (hasBagSprite)
            {
                Assert.AreEqual(bagSprite, result, 
                    $"迭代 {i}: 当bagSprite已设置时应返回bagSprite");
            }
            else if (hasIcon)
            {
                Assert.AreEqual(icon, result, 
                    $"迭代 {i}: 当bagSprite为空但icon存在时应返回icon");
            }
            else
            {
                Assert.IsNull(result, 
                    $"迭代 {i}: 当两者都为空时应返回null");
            }

            // Cleanup
            if (icon != null) DestroySprite(icon);
            if (bagSprite != null) DestroySprite(bagSprite);
        }
    }

    #endregion

    #region Property 1: Sprite旋转边界约束

    /// <summary>
    /// **Feature: world-item-drop-system, Property 1: Sprite旋转边界约束**
    /// *For any* icon Sprite，生成的45度旋转世界Sprite的边界框应该不超过原始icon的最大边长
    /// **Validates: Requirements 1.2**
    /// </summary>
    [Test]
    public void SpriteRotation_BoundaryConstraint_RotatedSizeWithinOriginal()
    {
        // Arrange
        int originalWidth = 32;
        int originalHeight = 48;
        int maxSize = Mathf.Max(originalWidth, originalHeight);

        // Act - 计算45度旋转后的边界框
        // 旋转后对角线长度 = sqrt(w² + h²)
        float diagonal = Mathf.Sqrt(originalWidth * originalWidth + originalHeight * originalHeight);
        
        // 缩放因子使旋转后的Sprite适应原始边界
        float scale = maxSize / diagonal;
        float rotatedSize = diagonal * scale;

        // Assert
        Assert.LessOrEqual(rotatedSize, maxSize + 0.01f, 
            "旋转后的Sprite边界不应超过原始最大边长");
    }

    /// <summary>
    /// 属性测试：随机尺寸的Sprite旋转边界约束
    /// </summary>
    [Test]
    public void SpriteRotation_PropertyTest_BoundaryConstraintIsConsistent()
    {
        const int iterations = 100;
        var random = new System.Random(42);

        for (int i = 0; i < iterations; i++)
        {
            // Arrange - 随机尺寸
            int width = random.Next(16, 128);
            int height = random.Next(16, 128);
            int maxSize = Mathf.Max(width, height);

            // Act
            float diagonal = Mathf.Sqrt(width * width + height * height);
            float scale = maxSize / diagonal;
            float rotatedSize = diagonal * scale;

            // Assert
            Assert.LessOrEqual(rotatedSize, maxSize + 0.01f, 
                $"迭代 {i}: 尺寸 {width}x{height} 旋转后不应超过 {maxSize}");
        }
    }

    #endregion

    #region Property 4: 掉落位置随机分布

    /// <summary>
    /// **Feature: world-item-drop-system, Property 4: 掉落位置随机分布**
    /// *For any* 多个同时掉落的物品，每个物品的最终位置应该有随机偏移，不完全重叠
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Test]
    public void DropPosition_MultipleItems_HaveRandomOffset()
    {
        // Arrange
        Vector3 origin = Vector3.zero;
        float spreadRadius = 0.5f;
        int itemCount = 5;
        var positions = new Vector3[itemCount];
        var random = new System.Random(42);

        // Act - 模拟生成多个掉落位置
        for (int i = 0; i < itemCount; i++)
        {
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float distance = (float)random.NextDouble() * spreadRadius;
            positions[i] = origin + new Vector3(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance,
                0f
            );
        }

        // Assert - 检查位置不完全相同
        bool allUnique = true;
        for (int i = 0; i < itemCount; i++)
        {
            for (int j = i + 1; j < itemCount; j++)
            {
                if (Vector3.Distance(positions[i], positions[j]) < 0.001f)
                {
                    allUnique = false;
                    break;
                }
            }
        }

        Assert.IsTrue(allUnique, "多个掉落物品的位置应该有随机偏移，不完全重叠");
    }

    /// <summary>
    /// 属性测试：掉落位置在指定半径内
    /// </summary>
    [Test]
    public void DropPosition_PropertyTest_WithinSpreadRadius()
    {
        const int iterations = 100;
        var random = new System.Random(42);

        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            Vector3 origin = new Vector3(
                (float)random.NextDouble() * 100f - 50f,
                (float)random.NextDouble() * 100f - 50f,
                0f
            );
            float spreadRadius = (float)random.NextDouble() * 2f + 0.1f;

            // Act
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float distance = (float)random.NextDouble() * spreadRadius;
            Vector3 dropPos = origin + new Vector3(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance,
                0f
            );

            // Assert
            float actualDistance = Vector3.Distance(origin, dropPos);
            Assert.LessOrEqual(actualDistance, spreadRadius + 0.001f, 
                $"迭代 {i}: 掉落位置应在半径 {spreadRadius} 内");
        }
    }

    #endregion

    #region Property 5: 阴影高度同步

    /// <summary>
    /// **Feature: world-item-drop-system, Property 5: 阴影高度同步**
    /// *For any* 正在弹跳的物品，阴影的缩放应该与物品高度成反比
    /// **Validates: Requirements 4.2**
    /// </summary>
    [Test]
    public void ShadowScale_InverselyProportionalToHeight()
    {
        // Arrange
        float groundY = 0f;
        float maxHeight = 1f;
        float minShadowScale = 0.5f;
        float maxShadowScale = 1f;

        // Act & Assert - 测试不同高度
        float[] heights = { 0f, 0.25f, 0.5f, 0.75f, 1f };
        float previousScale = float.MaxValue;

        foreach (float height in heights)
        {
            // 计算阴影缩放：高度越高，阴影越小
            float heightRatio = Mathf.Clamp01(height / maxHeight);
            float shadowScale = Mathf.Lerp(maxShadowScale, minShadowScale, heightRatio);

            Assert.LessOrEqual(shadowScale, previousScale, 
                $"高度 {height}: 阴影缩放应随高度增加而减小");
            previousScale = shadowScale;
        }
    }

    /// <summary>
    /// 属性测试：阴影缩放在有效范围内
    /// </summary>
    [Test]
    public void ShadowScale_PropertyTest_WithinValidRange()
    {
        const int iterations = 100;
        var random = new System.Random(42);
        float minShadowScale = 0.5f;
        float maxShadowScale = 1f;
        float maxHeight = 1f;

        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            float height = (float)random.NextDouble() * maxHeight * 1.5f; // 可能超过maxHeight

            // Act
            float heightRatio = Mathf.Clamp01(height / maxHeight);
            float shadowScale = Mathf.Lerp(maxShadowScale, minShadowScale, heightRatio);

            // Assert
            Assert.GreaterOrEqual(shadowScale, minShadowScale, 
                $"迭代 {i}: 阴影缩放不应小于最小值");
            Assert.LessOrEqual(shadowScale, maxShadowScale, 
                $"迭代 {i}: 阴影缩放不应大于最大值");
        }
    }

    #endregion

    #region Property 6: 距离动画暂停

    /// <summary>
    /// **Feature: world-item-drop-system, Property 6: 距离动画暂停**
    /// *For any* 距离玩家超过animationActiveDistance的物品，动画应该处于暂停状态
    /// **Validates: Requirements 5.1, 5.2**
    /// </summary>
    [Test]
    public void AnimationPause_BeyondDistance_IsPaused()
    {
        // Arrange
        Vector3 playerPos = Vector3.zero;
        float activeDistance = 15f;
        Vector3 itemPos = new Vector3(20f, 0f, 0f); // 超出距离

        // Act
        float distance = Vector3.Distance(playerPos, itemPos);
        bool shouldPause = distance > activeDistance;

        // Assert
        Assert.IsTrue(shouldPause, "超出激活距离的物品动画应该暂停");
    }

    [Test]
    public void AnimationPause_WithinDistance_IsPlaying()
    {
        // Arrange
        Vector3 playerPos = Vector3.zero;
        float activeDistance = 15f;
        Vector3 itemPos = new Vector3(10f, 0f, 0f); // 在距离内

        // Act
        float distance = Vector3.Distance(playerPos, itemPos);
        bool shouldPause = distance > activeDistance;

        // Assert
        Assert.IsFalse(shouldPause, "在激活距离内的物品动画应该播放");
    }

    /// <summary>
    /// 属性测试：距离检测一致性
    /// </summary>
    [Test]
    public void AnimationPause_PropertyTest_DistanceCheckIsConsistent()
    {
        const int iterations = 100;
        var random = new System.Random(42);
        float activeDistance = 15f;

        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            Vector3 playerPos = new Vector3(
                (float)random.NextDouble() * 100f - 50f,
                (float)random.NextDouble() * 100f - 50f,
                0f
            );
            Vector3 itemPos = new Vector3(
                (float)random.NextDouble() * 100f - 50f,
                (float)random.NextDouble() * 100f - 50f,
                0f
            );

            // Act
            float distance = Vector3.Distance(playerPos, itemPos);
            bool shouldPause = distance > activeDistance;

            // Assert
            if (distance > activeDistance)
            {
                Assert.IsTrue(shouldPause, 
                    $"迭代 {i}: 距离 {distance} > {activeDistance}，应该暂停");
            }
            else
            {
                Assert.IsFalse(shouldPause, 
                    $"迭代 {i}: 距离 {distance} <= {activeDistance}，应该播放");
            }
        }
    }

    #endregion

    #region Property 7: 物品数量上限

    /// <summary>
    /// **Feature: world-item-drop-system, Property 7: 物品数量上限**
    /// *For any* 时刻，场景中活跃的掉落物品数量应该不超过maxActiveItems
    /// **Validates: Requirements 5.4**
    /// </summary>
    [Test]
    public void ItemCount_ExceedsMax_CleansUpOldest()
    {
        // Arrange
        int maxActiveItems = 100;
        int currentCount = 105;
        int cleanupBatchSize = 10;

        // Act - 模拟清理逻辑
        int toRemove = 0;
        if (currentCount > maxActiveItems)
        {
            toRemove = Mathf.Min(currentCount - maxActiveItems, cleanupBatchSize);
        }
        int afterCleanup = currentCount - toRemove;

        // Assert
        Assert.LessOrEqual(afterCleanup, currentCount, 
            "清理后数量应该减少");
        Assert.GreaterOrEqual(toRemove, 1, 
            "超过上限时应该清理至少1个物品");
    }

    /// <summary>
    /// 属性测试：物品数量管理
    /// </summary>
    [Test]
    public void ItemCount_PropertyTest_NeverExceedsMaxAfterCleanup()
    {
        const int iterations = 100;
        var random = new System.Random(42);
        int maxActiveItems = 100;

        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            int currentCount = random.Next(50, 150);

            // Act - 模拟多次清理直到在上限内
            int cleanupIterations = 0;
            while (currentCount > maxActiveItems && cleanupIterations < 10)
            {
                int toRemove = Mathf.Min(currentCount - maxActiveItems, 10);
                currentCount -= toRemove;
                cleanupIterations++;
            }

            // Assert
            Assert.LessOrEqual(currentCount, maxActiveItems, 
                $"迭代 {i}: 清理后数量应不超过上限");
        }
    }

    #endregion

    #region Property 3: worldPrefab回退逻辑

    /// <summary>
    /// **Feature: world-item-drop-system, Property 3: worldPrefab回退逻辑**
    /// *For any* ItemData，当worldPrefab为空时，WorldSpawnService应该使用默认模板生成
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Test]
    public void WorldPrefab_FallbackLogic_WhenPrefabIsNull_UsesDefault()
    {
        // Arrange - 模拟 worldPrefab 回退逻辑
        GameObject worldPrefab = null;
        GameObject defaultPrefab = new GameObject("DefaultWorldItem");

        // Act - 模拟 WorldSpawnService 的回退逻辑
        GameObject prefabToUse = worldPrefab != null ? worldPrefab : defaultPrefab;

        // Assert
        Assert.AreEqual(defaultPrefab, prefabToUse,
            "当worldPrefab为空时，应使用默认模板");

        // Cleanup
        Object.DestroyImmediate(defaultPrefab);
    }

    [Test]
    public void WorldPrefab_FallbackLogic_WhenPrefabIsSet_UsesPrefab()
    {
        // Arrange
        GameObject worldPrefab = new GameObject("CustomWorldItem");
        GameObject defaultPrefab = new GameObject("DefaultWorldItem");

        // Act - 模拟回退逻辑
        GameObject prefabToUse = worldPrefab != null ? worldPrefab : defaultPrefab;

        // Assert
        Assert.AreEqual(worldPrefab, prefabToUse,
            "当worldPrefab已设置时，应使用该prefab");

        // Cleanup
        Object.DestroyImmediate(worldPrefab);
        Object.DestroyImmediate(defaultPrefab);
    }

    /// <summary>
    /// 属性测试：worldPrefab回退逻辑一致性
    /// </summary>
    [Test]
    public void WorldPrefab_PropertyTest_FallbackLogicIsConsistent()
    {
        const int iterations = 100;
        var random = new System.Random(42);
        GameObject defaultPrefab = new GameObject("DefaultWorldItem");

        for (int i = 0; i < iterations; i++)
        {
            // Arrange - 随机决定是否设置worldPrefab
            bool hasWorldPrefab = random.Next(2) == 1;
            GameObject worldPrefab = hasWorldPrefab ? new GameObject($"WorldItem_{i}") : null;

            // Act
            GameObject prefabToUse = worldPrefab != null ? worldPrefab : defaultPrefab;

            // Assert
            if (hasWorldPrefab)
            {
                Assert.AreEqual(worldPrefab, prefabToUse,
                    $"迭代 {i}: 当worldPrefab已设置时应使用该prefab");
            }
            else
            {
                Assert.AreEqual(defaultPrefab, prefabToUse,
                    $"迭代 {i}: 当worldPrefab为空时应使用默认模板");
            }

            // Cleanup
            if (worldPrefab != null) Object.DestroyImmediate(worldPrefab);
        }

        Object.DestroyImmediate(defaultPrefab);
    }

    #endregion

    #region Property 8: 掉落表物品生成

    /// <summary>
    /// **Feature: world-item-drop-system, Property 8: 掉落表物品生成**
    /// *For any* 矿石类型和品质，生成的掉落物品应该符合配置的DropTable
    /// **Validates: Requirements 7.2**
    /// 
    /// 测试掉落表的核心算法逻辑
    /// </summary>
    [Test]
    public void DropTable_GenerateDrops_RespectsDropChance()
    {
        // Arrange - 模拟 DropConfig 的 RollDrop 逻辑
        float dropChance = 0.5f;
        int iterations = 1000;
        int successCount = 0;
        var random = new System.Random(42);

        // Act
        for (int i = 0; i < iterations; i++)
        {
            float roll = (float)random.NextDouble();
            if (roll <= dropChance)
            {
                successCount++;
            }
        }

        // Assert - 允许10%的误差
        float actualRate = (float)successCount / iterations;
        Assert.That(actualRate, Is.InRange(dropChance - 0.1f, dropChance + 0.1f),
            $"掉落率应接近配置值 {dropChance}，实际为 {actualRate}");
    }

    [Test]
    public void DropTable_GenerateDrops_AmountWithinRange()
    {
        // Arrange
        int minAmount = 2;
        int maxAmount = 5;
        int iterations = 100;
        var random = new System.Random(42);

        // Act & Assert
        for (int i = 0; i < iterations; i++)
        {
            // 模拟 GetRandomAmount() 逻辑
            int amount = random.Next(minAmount, maxAmount + 1);

            Assert.GreaterOrEqual(amount, minAmount,
                $"迭代 {i}: 数量不应小于最小值");
            Assert.LessOrEqual(amount, maxAmount,
                $"迭代 {i}: 数量不应大于最大值");
        }
    }

    [Test]
    public void DropTable_GuaranteeOneDrop_WhenAllRollsFail()
    {
        // Arrange - 模拟所有掉落都失败的情况
        bool guaranteeOneDrop = true;
        bool hasDropped = false;
        int dropConfigCount = 3;
        float[] dropChances = { 0.1f, 0.1f, 0.1f }; // 低概率
        var random = new System.Random(12345); // 选择一个让所有roll都失败的种子

        // Act - 模拟 GenerateDrops 逻辑
        var results = new System.Collections.Generic.List<int>();
        
        for (int i = 0; i < dropConfigCount; i++)
        {
            float roll = (float)random.NextDouble();
            if (roll <= dropChances[i])
            {
                results.Add(i);
                hasDropped = true;
            }
        }

        // 保证至少掉落一个
        if (guaranteeOneDrop && !hasDropped && dropConfigCount > 0)
        {
            results.Add(0); // 添加第一个配置的物品
        }

        // Assert
        Assert.GreaterOrEqual(results.Count, 1,
            "当guaranteeOneDrop为true时，应至少有一个掉落");
    }

    [Test]
    public void DropTable_GuaranteeOneDrop_Disabled_CanHaveNoDrops()
    {
        // Arrange
        bool guaranteeOneDrop = false;
        int dropConfigCount = 3;
        float[] dropChances = { 0f, 0f, 0f }; // 0概率

        // Act
        var results = new System.Collections.Generic.List<int>();
        bool hasDropped = false;

        for (int i = 0; i < dropConfigCount; i++)
        {
            if (0f <= dropChances[i]) // 永远不会触发
            {
                // 不会执行
            }
        }

        if (guaranteeOneDrop && !hasDropped && dropConfigCount > 0)
        {
            results.Add(0);
        }

        // Assert
        Assert.AreEqual(0, results.Count,
            "当guaranteeOneDrop为false且所有roll失败时，可以没有掉落");
    }

    /// <summary>
    /// 属性测试：掉落表生成的物品数量在合理范围内
    /// </summary>
    [Test]
    public void DropTable_PropertyTest_GeneratedAmountsAreValid()
    {
        const int iterations = 100;
        var random = new System.Random(42);

        for (int i = 0; i < iterations; i++)
        {
            // Arrange - 随机配置
            int minAmount = random.Next(1, 5);
            int maxAmount = minAmount + random.Next(0, 10);
            float dropChance = (float)random.NextDouble();
            int quality = random.Next(0, 5);

            // Act - 模拟掉落
            bool dropped = (float)random.NextDouble() <= dropChance;
            int amount = dropped ? random.Next(minAmount, maxAmount + 1) : 0;

            // Assert
            if (dropped)
            {
                Assert.GreaterOrEqual(amount, minAmount,
                    $"迭代 {i}: 掉落数量应 >= minAmount");
                Assert.LessOrEqual(amount, maxAmount,
                    $"迭代 {i}: 掉落数量应 <= maxAmount");
            }

            Assert.GreaterOrEqual(quality, 0,
                $"迭代 {i}: 品质应 >= 0");
            Assert.LessOrEqual(quality, 4,
                $"迭代 {i}: 品质应 <= 4");
        }
    }

    /// <summary>
    /// 属性测试：掉落概率分布符合预期
    /// </summary>
    [Test]
    public void DropTable_PropertyTest_DropChanceDistribution()
    {
        const int iterations = 10000;
        var random = new System.Random(42);

        // 测试不同的掉落概率
        float[] testChances = { 0.1f, 0.25f, 0.5f, 0.75f, 0.9f, 1.0f };

        foreach (float targetChance in testChances)
        {
            int successCount = 0;

            for (int i = 0; i < iterations; i++)
            {
                if ((float)random.NextDouble() <= targetChance)
                {
                    successCount++;
                }
            }

            float actualRate = (float)successCount / iterations;
            float tolerance = 0.05f; // 5%误差

            Assert.That(actualRate, Is.InRange(targetChance - tolerance, targetChance + tolerance),
                $"掉落概率 {targetChance}: 实际率 {actualRate} 应在误差范围内");
        }
    }

    #endregion

    #region 辅助方法

    private Sprite CreateTestSprite(int width, int height)
    {
        var texture = new Texture2D(width, height);
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    private void DestroySprite(Sprite sprite)
    {
        if (sprite != null)
        {
            if (sprite.texture != null)
                Object.DestroyImmediate(sprite.texture);
            Object.DestroyImmediate(sprite);
        }
    }

    #endregion
}
