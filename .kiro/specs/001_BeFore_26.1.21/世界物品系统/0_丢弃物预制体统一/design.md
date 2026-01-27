# 丢弃物预制体统一 - 设计文档

## 概述

本文档描述如何修改 WorldItemPool 以支持使用 ItemData.worldPrefab 生成掉落物，确保丢弃物与资源掉落物外观一致。

## 架构

### 当前架构

```
WorldItemPool
├── defaultWorldPrefab (动态创建的简单预制体)
├── _pool (Stack<WorldItemPickup>) - 单一对象池
└── Spawn() - 总是使用 defaultWorldPrefab
```

### 目标架构

```
WorldItemPool
├── defaultWorldPrefab (回退用)
├── _poolByItemId (Dictionary<int, Stack<WorldItemPickup>>) - 按 itemId 分类的对象池
├── _defaultPool (Stack<WorldItemPickup>) - 默认对象池
└── Spawn() - 优先使用 ItemData.worldPrefab
```

## 组件与接口

### WorldItemPool.Spawn 方法（修改）

**修改前**：
```csharp
public WorldItemPickup Spawn(ItemData data, int quality, int amount, Vector3 position, bool playAnimation = true, bool setSpawnCooldown = true)
{
    WorldItemPickup item = GetFromPool();  // 总是从单一池获取
    // ...
}
```

**修改后**：
```csharp
public WorldItemPickup Spawn(ItemData data, int quality, int amount, Vector3 position, bool playAnimation = true, bool setSpawnCooldown = true)
{
    // ★ 优先使用 ItemData.worldPrefab
    WorldItemPickup item = GetFromPoolByItemId(data);
    // ...
}
```

### GetFromPoolByItemId 方法（新增）

```csharp
private WorldItemPickup GetFromPoolByItemId(ItemData data)
{
    int itemId = data.itemID;
    
    // 1. 尝试从对应 itemId 的池中获取
    if (_poolByItemId.TryGetValue(itemId, out var pool) && pool.Count > 0)
    {
        return pool.Pop();
    }
    
    // 2. 检查是否有 worldPrefab
    if (data.worldPrefab != null)
    {
        // 从 worldPrefab 实例化
        var obj = Instantiate(data.worldPrefab);
        return obj.GetComponent<WorldItemPickup>();
    }
    
    // 3. 回退到默认池
    return GetFromDefaultPool();
}
```

### Despawn 方法（修改）

```csharp
public void Despawn(WorldItemPickup item)
{
    if (item == null) return;
    
    _activeItems.Remove(item);
    
    // 停止动画
    var dropAnim = item.GetComponent<WorldItemDrop>();
    if (dropAnim != null)
    {
        dropAnim.StopAnimation();
    }
    
    item.Reset();
    
    // ★ 根据 itemId 放入对应的池
    int itemId = item.ItemId;
    if (itemId >= 0)
    {
        if (!_poolByItemId.ContainsKey(itemId))
        {
            _poolByItemId[itemId] = new Stack<WorldItemPickup>();
        }
        
        if (_poolByItemId[itemId].Count < maxPoolSizePerItem)
        {
            item.gameObject.SetActive(false);
            item.transform.SetParent(_poolContainer);
            _poolByItemId[itemId].Push(item);
            return;
        }
    }
    
    // 池已满或无效 itemId，销毁
    Destroy(item.gameObject);
}
```

## 数据模型

### 新增字段

```csharp
[Header("对象池配置")]
[Tooltip("每种物品类型的最大池大小")]
[SerializeField] private int maxPoolSizePerItem = 10;

private Dictionary<int, Stack<WorldItemPickup>> _poolByItemId = new Dictionary<int, Stack<WorldItemPickup>>();
private Stack<WorldItemPickup> _defaultPool = new Stack<WorldItemPickup>();
```

### WorldItemPickup.ItemId 属性

确保 WorldItemPickup 有公开的 ItemId 属性：

```csharp
public int ItemId => itemId;
```

## 错误处理

### worldPrefab 为空

- 如果 `ItemData.worldPrefab` 为空，回退到 `defaultWorldPrefab`
- 输出警告日志，提示开发者生成对应的 worldPrefab

### 池容量管理

- 每种 itemId 的池有独立的容量限制（`maxPoolSizePerItem`）
- 超出容量时直接销毁，避免内存泄漏

## 测试策略

### 验收测试

| 测试场景 | 操作 | 预期结果 |
|----------|------|----------|
| 丢弃有 worldPrefab 的物品 | 从背包丢弃树苗 | 掉落物有阴影和浮动动画 |
| 丢弃无 worldPrefab 的物品 | 从背包丢弃新物品 | 使用默认预制体，输出警告 |
| 砍树掉落 | 砍倒树木 | 掉落物有阴影和浮动动画 |
| 拾取后再丢弃 | 拾取物品后丢弃 | 物品正确回收到对应池 |

### 性能测试

- 大量丢弃/拾取操作不应导致内存泄漏
- 对象池复用率应该较高
