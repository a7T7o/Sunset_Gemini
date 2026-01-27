# 实现计划

## 状态：✅ 已完成

所有任务已集成到 TreeControllerV2 中完成。

---

- [x] 1. 扩展 TreeControllerV2 影子控制功能
  - [x] 1.1 添加影子 Sprite 配置字段
    - ✅ 在 TreeControllerV2 中添加 `shadowSprites` 数组字段
    - ✅ 添加 `_shadowTransform`, `_shadowRenderer`, `_originalShadowSprite` 私有字段
    - _需求: 1.1, 1.2_

  - [x] 1.2 重构 UpdateShadowScale 方法
    - ✅ 使用缓存的引用避免每次 Find
    - ✅ 添加 `ShouldShowShadow()` 辅助方法
    - ✅ 添加 `GetShadowSpriteForCurrentStage()` 辅助方法
    - ✅ 添加 `GetShadowScaleForCurrentStage()` 辅助方法
    - ✅ 添加 `AlignShadowCenter()` 辅助方法
    - _需求: 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4_

  - [x] 1.3 实现影子 Sprite 切换逻辑
    - ✅ 在 `UpdateShadowScale()` 中实现 Sprite 切换
    - ✅ 实现未配置时回退到原始 Sprite 的逻辑
    - ✅ 确保切换时同步更新缩放和位置
    - _需求: 1.3, 1.4, 3.1, 3.2_

- [x] 2. 更新调用点和缓存优化
  - [x] 2.1 添加影子引用缓存
    - ✅ 实现 `InitializeShadowCache()` 方法
    - ✅ 在 `Start()` 中调用初始化
    - ✅ 缓存原始影子 Sprite
    - _需求: 1.3_

- [x] 3. 检查点 - 确保编译通过
  - ✅ 编译通过（0 错误）

- [x] 4. 更新 memory 文档
  - ✅ 记录模块概述和当前状态
  - ✅ 记录本次会话的需求和完成任务
  - ✅ 记录关键决策和相关文件

---

## 实现总结

### 修改的文件

| 文件 | 修改内容 |
|------|----------|
| `Assets/Scripts/Controller/TreeControllerV2.cs` | 扩展影子系统 |

### 新增字段

```csharp
// 序列化字段
[SerializeField] private Sprite[] shadowSprites = new Sprite[STAGE_COUNT];

// 私有字段（缓存）
private Transform _shadowTransform;
private SpriteRenderer _shadowRenderer;
private Sprite _originalShadowSprite;
```

### 新增方法

| 方法 | 说明 |
|------|------|
| `InitializeShadowCache()` | 初始化影子缓存 |
| `ShouldShowShadow()` | 判断是否显示影子 |
| `GetShadowScaleForCurrentStage()` | 获取当前阶段影子缩放 |
| `GetShadowSpriteForCurrentStage()` | 获取当前阶段影子 Sprite |
| `AlignShadowCenter()` | 对齐影子中心 |

### 功能说明

- 阶段0-1：无影子（shadowScales[0]=0, shadowScales[1]=0）
- 阶段2-5：有影子，缩放逐渐增大
- 树桩状态：无影子
- 支持为每个阶段配置不同的影子 Sprite
- 未配置时自动回退到原始 Sprite
