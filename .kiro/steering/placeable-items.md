---
inclusion: manual
priority: P1
keywords: [放置, 可放置, 树苗, 箱子, 工作台, 底部对齐, Collider, 预制体]
lastUpdated: 2026-01-21
---

# 可放置物品开发规范

> **权威文档**: `.kiro/specs/物品放置系统/7_可放置物品开发规范/可放置物品开发规范.md`
> 
> 本文件是 steering 规则摘要，完整规范请查阅权威文档。

---

## 一、适用范围

所有可通过放置系统放置到世界中的物品：
- 树苗（TreeControllerV2）
- 箱子（ChestController）
- 工作台（待开发）
- 装饰物（待开发）

---

## 二、🔴 底部对齐规范（最高优先级）

### 核心原则

**所有可放置物品必须在 `Start()` 或 `Initialize()` 时执行底部对齐。**

### 统一实现方式

```csharp
private void AlignSpriteBottom()
{
    if (spriteRenderer == null || spriteRenderer.sprite == null) return;
    
    Bounds spriteBounds = spriteRenderer.sprite.bounds;
    float spriteBottomOffset = spriteBounds.min.y;
    
    Vector3 localPos = spriteRenderer.transform.localPosition;
    localPos.y = -spriteBottomOffset;  // ★ 绝对值设置
    spriteRenderer.transform.localPosition = localPos;
}
```

### 触发时机

| 时机 | 是否执行 |
|------|---------|
| Start() / Initialize() | ✅ 必须 |
| Sprite 切换时 | ✅ 必须 |
| 每帧 Update | ❌ 禁止 |

---

## 三、预制体结构规范

```
{ItemName}_Root (根物体)
├── Transform: position = 放置位置（格子中心）
├── Tag: 对应标签
│
├── {ItemName} (子物体) ← SpriteRenderer + Collider + Controller
│   ├── Transform: localPosition.y = 由底部对齐计算
│   ├── SpriteRenderer
│   ├── PolygonCollider2D / BoxCollider2D
│   └── {ItemName}Controller
│
└── Shadow (子物体，可选)
```

### 关键要求

1. **SpriteRenderer 必须在子物体上**
2. **Collider 必须与 SpriteRenderer 在同一子物体上**
3. **Controller 脚本挂载在子物体上**
4. **根物体的 position 代表放置位置**

---

## 四、Sprite 状态切换链路

当 Sprite 状态变化时，必须按以下顺序执行：

```
1. 更新 Sprite
2. 执行底部对齐
3. 更新 Collider 形状
4. 刷新 NavGrid
```

### 标准实现

```csharp
private void UpdateSpriteState(Sprite newSprite)
{
    spriteRenderer.sprite = newSprite;
    AlignSpriteBottom();
    UpdateColliderShape();
    NavGrid2D.OnRequestGridRefresh?.Invoke();
}
```

---

## 五、新物品开发检查清单

- [ ] SpriteRenderer 在子物体上
- [ ] Collider 与 SpriteRenderer 在同一子物体上
- [ ] 实现了 `AlignSpriteBottom()` 方法
- [ ] 在 `Start()` 或 `Initialize()` 中调用底部对齐
- [ ] Sprite 切换时调用底部对齐
- [ ] Collider 变化后刷新 NavGrid

---

## 六、参考实现

- **TreeControllerV2.cs** - 完全合规的参考实现
- **ChestController.cs** - 需要修改以符合规范

---

**文档维护者**: Kiro  
**创建日期**: 2026-01-21
