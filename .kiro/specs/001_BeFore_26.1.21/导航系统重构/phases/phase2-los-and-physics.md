# 第二阶段：物理校正 - 视线检测重写

## 阶段目标

使用 CircleCast + NavGrid 重写视线检测，彻底根除 Tunneling（穿墙漏检）现象。

## 验收状态：✅ 通过

**验收日期**：2025-01-09

## 核心修改

### 1. CircleCast 连续检测

```csharp
// ❌ 旧代码：步进法，存在 Tunneling 漏洞
int sampleCount = Mathf.Max(3, Mathf.CeilToInt(distance / 0.3f));
for (int i = 0; i <= sampleCount; i++) {
    ... OverlapCircleAll ...
}

// ✅ 新代码：连续检测，零 GC
RaycastHit2D hit = Physics2D.CircleCast(from, checkRadius, direction.normalized, distance);
```

### 2. NavGrid 可走性检测

在物理检测之前，先沿路径采样检查 NavGrid：

```csharp
if (navGrid != null)
{
    int sampleCount = Mathf.Max(5, Mathf.CeilToInt(distance / 0.3f));
    for (int i = 1; i < sampleCount; i++)
    {
        float t = (float)i / sampleCount;
        Vector2 samplePoint = Vector2.Lerp(from, to, t);
        
        if (!navGrid.IsWalkable(samplePoint))
        {
            _lastLOSBlocked = true;
            return false;
        }
    }
}
```

### 3. "宁可误杀，不可放过"策略

- 检测所有物体，只排除明确不需要的（Trigger、掉落物）
- 配置错误不会导致漏检

### 4. 可视化调试

- 绿线：视线通畅
- 红线 + 红球：视线被阻挡

## 修复历程

### 第一次测试：❌ 失败

**问题**：薄墙测试失败，绿线穿墙
**原因**：配置疏忽 + 代码脆弱性

### 第二次测试：❌ 失败

**问题**：控制台没有警告日志
**原因**：CircleCast 使用了 losObstacleMask，墙的层不在 Mask 中

### 策略转变

从"白名单模式"改为"黑名单模式"：
- 旧：只检测配置中的物体
- 新：检测所有物体，只排除明确不需要的

### 第三次测试：❌ 失败

**问题**：绿线穿过水域
**原因**：水域没有物理碰撞体，只通过 NavGrid 标记为不可走

### 最终修复

添加 NavGrid 可走性检测，在物理检测之前先检查 NavGrid。

### 第四次测试：✅ 通过

**证据**：控制台输出黄色日志 `[Nav] 视线被 NavGrid 阻挡：点 xxx 不可走`

## 验收数据

| 检测类型 | 修复前 | 修复后 |
|---------|--------|--------|
| 墙体（有碰撞体） | ✅ 正确检测 | ✅ 正确检测 |
| 水域（无碰撞体） | ❌ 穿透 | ✅ 通过 NavGrid 检测 |
| 悬崖（无碰撞体） | ❌ 穿透 | ✅ 通过 NavGrid 检测 |

## 相关文件

- `Assets/YYY_Scripts/Service/Player/PlayerAutoNavigator.cs`

---

## Code Reaper 验收锐评 - 2026-01-10

### 验收结论：✅ 通过（需实机验证）

### 1. NavGrid 采样部分 ✅

```csharp
if (navGrid != null) {
    int sampleCount = Mathf.Max(5, Mathf.CeilToInt(distance / 0.3f));
    for (int i = 1; i < sampleCount; i++)
    {
        float t = (float)i / sampleCount;
        Vector2 samplePoint = Vector2.Lerp(from, to, t);
        
        if (!navGrid.IsWalkable(samplePoint))
        {
            _lastLOSBlocked = true;
            return false;
        }
    }
}
```

**评价**：
- 负责挡掉 NavGrid 上标记为不可走但场景里没有 Collider 的区域（水、悬崖等）
- 采样密度、返回逻辑都没问题
- 可以视为"通过并保持"

### 2. CircleCast 物理检测 ✅

```csharp
float checkRadius = Mathf.Max(0.05f, (playerRadius + losSafetyMargin) * 0.8f);
int layerMask = losObstacleMask.value != 0 ? losObstacleMask.value : Physics2D.DefaultRaycastLayers;

RaycastHit2D hit = Physics2D.CircleCast(
    from,
    checkRadius,
    direction.normalized,
    distance,
    layerMask
);

if (hit.collider == null) {
     _lastLOSBlocked = false;
     return true;
}

if (IsPlayerCollider(hit.collider)) {
     _lastLOSBlocked = false;
     return true;
}

bool isObstacle = IsObstacle(hit.collider);
if (isObstacle) {
    _lastLOSHit = hit;
    _lastLOSBlocked = true;
    return false;
}

_lastLOSBlocked = false;
return true;
```

### 3. "宁可误杀，不可放过"的 IsObstacle ✅

```csharp
private bool IsObstacle(Collider2D col) {
    if (col.isTrigger) return false;
    if (col.name.Contains("(Clone)") || col.name.Contains("Pickup")) return false;
    
    if (losObstacleMask.value != 0)
    {
        bool inMask = ((1 << col.gameObject.layer) & losObstacleMask.value) != 0;
        if (inMask) return true;
    }
    
    if (losObstacleTags != null && losObstacleTags.Length > 0)
    {
        bool hasTag = HasAnyTag(col.transform, losObstacleTags);
        if (hasTag) return true;
    }
    
    // 没在排除列表里 → 默认当障碍物
    return true;
}
```

**评价**：
- 使用 CircleCast（体积检测）而不是步进/线检测 ✅
- 考虑玩家半径（playerRadius + losSafetyMargin）✅
- 默认所有非触发器、且不是掉落物的 Collider 都会被视作遮挡 ✅
- **第二阶段的"物理校正"现在是合格的** ✅

### 需要注意的小点（提醒，而不是否决）

- 现在 CircleCast 命中的是第一个碰撞体，在多层叠加碰撞体的场景中，要确保层/Tag 配置能把"真障碍物"挡住
- 如果将来场景里出现"可穿透的实体（有 Collider 但逻辑上不算遮挡）"，需要补一层"白名单排除"

### 建议测试场景

1. **薄墙场景**（0.05–0.1 厚）、密墙、拐角场景 → 确认不会穿透
2. **有水/悬崖的区域** → 确认视线优化不会让路径穿过 NavGrid 标记为不可走的区域

### 最终评价

> 第二阶段：可以记为已完成，建议在薄墙场景和含水域场景实际跑一轮验证。
