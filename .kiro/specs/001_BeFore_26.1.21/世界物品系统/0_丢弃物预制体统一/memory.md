# 丢弃物预制体统一 - 开发记忆

## 模块概述

统一背包丢弃物与资源节点掉落物的预制体使用，确保所有掉落物都有正确的视觉效果（阴影、动画等）。

## 当前状态

- **完成度**: 100%
- **最后更新**: 2026-01-07
- **状态**: ✅ 已完成所有实现任务

---

## 会话 2 - 2026-01-07（实现）

**用户需求**:
> 继续完成所有任务

**完成任务**:
1. ✅ 修改 WorldItemPool 数据结构
   - 添加 `_poolByItemId` 字典
   - 添加 `maxPoolSizePerItem` 配置字段
   - 保留 `_defaultPool` 作为回退
2. ✅ 实现 GetFromPoolByItemId 方法
   - 优先从对应 itemId 的池中获取
   - 检查 ItemData.worldPrefab 是否存在
   - 回退到默认池
3. ✅ 修改 Spawn 方法
   - 调用 GetFromPoolByItemId 替代 GetFromPool
4. ✅ 修改 Despawn 方法
   - 根据 itemId 放入对应的池
   - 处理池容量限制
5. ✅ 确保 WorldItemPickup.ItemId 属性可访问
   - 添加 `public int ItemId => itemId;`
6. ✅ 验证 TreeController 和 StoneController 的掉落逻辑
7. ✅ 添加调试日志

**修改文件**:
- `Assets/YYY_Scripts/World/WorldItemPool.cs`
  - 添加 `using System.Linq;`
  - 添加 `maxPoolSizePerItem` 配置字段
  - 修改数据结构：`_defaultPool` + `_poolByItemId`
  - 实现 `GetFromPoolByItemId` 方法
  - 修改 `Spawn` 方法使用 `GetFromPoolByItemId`
  - 修改 `Despawn` 方法按 itemId 分类回收
  - 修改 `WarmupPool` 使用 `_defaultPool`
  - 修改 `DEBUG_ShowStatus` 显示分类池状态
- `Assets/YYY_Scripts/World/WorldItemPickup.cs`
  - 添加 `public int ItemId => itemId;` 属性

**解决方案**:

### 1. 按 itemId 分类对象池

```csharp
private Stack<WorldItemPickup> _defaultPool = new Stack<WorldItemPickup>();
private Dictionary<int, Stack<WorldItemPickup>> _poolByItemId = new Dictionary<int, Stack<WorldItemPickup>>();
```

### 2. 优先使用 worldPrefab

```csharp
private WorldItemPickup GetFromPoolByItemId(ItemData data)
{
    int itemId = data.itemID;
    
    // 1. 尝试从对应 itemId 的池中获取
    if (_poolByItemId.TryGetValue(itemId, out var pool) && pool.Count > 0)
        return pool.Pop();
    
    // 2. 检查是否有 worldPrefab
    if (data.worldPrefab != null)
    {
        var obj = Instantiate(data.worldPrefab);
        return obj.GetComponent<WorldItemPickup>();
    }
    
    // 3. 回退到默认池
    return GetFromPool();
}
```

### 3. 按 itemId 回收

```csharp
public void Despawn(WorldItemPickup item)
{
    int itemId = item.ItemId;
    if (itemId >= 0)
    {
        if (!_poolByItemId.ContainsKey(itemId))
            _poolByItemId[itemId] = new Stack<WorldItemPickup>();
        
        if (_poolByItemId[itemId].Count < maxPoolSizePerItem)
        {
            _poolByItemId[itemId].Push(item);
            return;
        }
    }
    
    // 回退到默认池或销毁
}
```

**遗留问题**:
- 无

---

## 会话 1 - 2026-01-07（问题发现与需求分析）

**用户反馈**:
> 从背包丢弃的物品没有阴影，与砍树掉落的物品外观不一致。WorldItemPool 是否应该使用预先生成的 WorldItem Prefab？

**问题分析**:

### 当前实现

1. **WorldItemPool.Spawn** 使用 `defaultWorldPrefab`
2. `defaultWorldPrefab` 是动态创建的简单预制体：
   - 程序生成的椭圆阴影（`CreateEllipseShadowSprite`）
   - 没有使用预先生成的 WorldItem Prefab

3. **预先生成的 WorldItem Prefab**（在 `Assets/Prefabs/WorldItems/` 目录）：
   - 有正确的阴影 Sprite
   - 有正确的动画组件配置
   - 关联了 `ItemData.worldPrefab`

### 问题根源

`WorldItemPool.Spawn` 没有查找 `ItemData.worldPrefab`，而是总是使用动态创建的 `defaultWorldPrefab`。

### 解决方案

修改 `WorldItemPool` 以支持：
1. 优先使用 `ItemData.worldPrefab`
2. 按 itemId 分类管理对象池
3. 回退到 `defaultWorldPrefab`（当 worldPrefab 不存在时）

**完成任务**:
1. ✅ 创建需求文档 `requirements.md`
2. ✅ 创建设计文档 `design.md`
3. ✅ 创建任务列表 `tasks.md`
4. ✅ 创建工作区记忆 `memory.md`

**下一步**:
- [ ] 执行任务列表中的实现任务

---

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 按 itemId 分类对象池 | 不同物品使用不同预制体，需要分类管理 | 2026-01-07 |
| 保留 defaultWorldPrefab | 作为回退方案，处理没有 worldPrefab 的物品 | 2026-01-07 |
| 每种物品独立池容量 | 避免某种物品占用过多内存 | 2026-01-07 |

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/World/WorldItemPool.cs` | 核心修改文件 |
| `Assets/YYY_Scripts/World/WorldItemPickup.cs` | 需要确认 ItemId 属性 |
| `Assets/Prefabs/WorldItems/` | 预先生成的 WorldItem Prefab 目录 |
| `Docx/分类/世界物品/world prefab生成与功能设计.md` | 系统设计文档 |
