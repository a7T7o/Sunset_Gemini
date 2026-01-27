# 世界物品掉落系统 - 需求文档

**版本**: v1.0  
**日期**: 2025-12-17  
**状态**: ✅ 已完成

---

## Introduction

本系统为 Sunset 农场模拟游戏提供完整的世界物品掉落与拾取功能。当玩家砍树、挖矿、收获作物时，物品会以弹性动画的形式掉落到地面，等待玩家拾取。系统需要支持从图标自动生成世界Sprite和Prefab，减少美术资源制作工作量。

## Glossary

- **World Prefab**: 物品在世界中的预制体，包含Sprite、阴影和动画组件（无碰撞体）
- **Icon Sprite**: 物品在背包/UI中显示的图标（通常为正面视角）
- **World Sprite**: 物品在世界中显示的精灵（45度旋转斜放效果）
- **Bounce Animation**: 物品掉落时的弹性跳动动画
- **Idle Animation**: 物品落地后的浮动待拾取动画
- **Shadow Sprite**: 物品下方的阴影精灵

---

## Requirements

### Requirement 1: 视觉资源自动生成

**User Story:** As a 开发者, I want 从图标自动生成世界Sprite, so that 减少美术资源制作工作量

#### Acceptance Criteria

1. WHEN 编辑器工具执行时 THE World Sprite Generator SHALL 读取物品的 icon Sprite 并生成旋转45度的世界Sprite
2. WHEN 生成世界Sprite时 THE World Sprite Generator SHALL 确保旋转后的Sprite不超出原始尺寸边界
3. WHEN 生成World Prefab时 THE World Prefab Generator SHALL 自动添加阴影子物体、SpriteRenderer和动画组件（无碰撞体，因为所有SO物品都是可拾取物体）
4. WHEN 批量生成时 THE Generator Tool SHALL 支持选择多个ItemData SO并一次性生成所有World Prefab

### Requirement 2: ItemData SO结构更新

**User Story:** As a 开发者, I want ItemData包含完整的视觉资源引用, so that 系统能正确显示物品在不同场景下的外观

#### Acceptance Criteria

1. THE ItemData SO SHALL 包含三种视觉资源字段：
   - icon：最基础的Sprite，用于生成其他形式
   - bagSprite：背包和工具栏显示的图标
   - worldPrefab：落在地上的预制体（含动画和阴影，无碰撞体）
2. WHEN bagSprite为空时 THE System SHALL 使用icon作为背包显示
3. WHEN worldPrefab为空时 THE System SHALL 使用默认的通用World Prefab模板动态生成

### Requirement 3: 掉落弹性动画

**User Story:** As a 玩家, I want 看到物品掉落时有弹性跳动效果, so that 游戏体验更加生动有趣

#### Acceptance Criteria

1. WHEN 物品从资源点掉落时 THE WorldItemDrop Component SHALL 播放弹出动画（向上抛出+重力下落）
2. WHEN 物品落地时 THE WorldItemDrop Component SHALL 播放弹性回弹动画（2-3次递减弹跳）
3. WHEN 弹跳结束后 THE WorldItemDrop Component SHALL 切换到浮动待拾取动画（上下轻微浮动）
4. WHEN 多个物品同时掉落时 THE System SHALL 为每个物品添加随机偏移，避免完全重叠

### Requirement 4: 阴影系统

**User Story:** As a 玩家, I want 掉落物品有阴影效果, so that 物品看起来更有立体感

#### Acceptance Criteria

1. THE World Prefab SHALL 包含一个阴影子物体，使用椭圆形阴影Sprite
2. WHEN 物品弹跳时 THE Shadow SHALL 保持在地面位置，大小随物品高度变化
3. WHEN 物品浮动时 THE Shadow SHALL 同步进行轻微的缩放动画

### Requirement 5: 性能优化

**User Story:** As a 开发者, I want 掉落物品系统有良好的性能, so that 大量物品掉落时游戏不会卡顿

#### Acceptance Criteria

1. WHEN 物品距离玩家超过指定范围时 THE WorldItemDrop Component SHALL 暂停动画播放
2. WHEN 物品进入玩家视野范围时 THE WorldItemDrop Component SHALL 恢复动画播放
3. THE System SHALL 使用对象池管理World Prefab实例，避免频繁创建销毁
4. WHEN 场景中掉落物品数量超过上限时 THE System SHALL 自动清理最远或最旧的物品

### Requirement 6: 砍树掉落集成

**User Story:** As a 玩家, I want 砍倒树木后掉落木材和树枝, so that 能够收集资源

#### Acceptance Criteria

1. WHEN 树木被砍倒时 THE TreeController SHALL 调用WorldSpawnService生成掉落物品
2. WHEN 生成掉落物时 THE System SHALL 在树木位置周围随机分布多个掉落点
3. THE 掉落物品 SHALL 使用弹性动画从树木中心向外弹出

### Requirement 7: 挖矿掉落集成

**User Story:** As a 玩家, I want 挖掘矿石后掉落矿物, so that 能够收集矿石资源

#### Acceptance Criteria

1. WHEN 矿石被挖掘完毕时 THE RockController SHALL 调用WorldSpawnService生成掉落物品
2. WHEN 生成掉落物时 THE System SHALL 根据矿石类型和品质决定掉落物品种类和数量
3. THE 掉落物品 SHALL 使用弹性动画从矿石位置弹出

### Requirement 8: 调试工具更新

**User Story:** As a 开发者, I want 调试工具支持新的World Prefab系统, so that 能够方便地测试掉落效果

#### Acceptance Criteria

1. THE WorldSpawnDebug SHALL 支持生成带弹性动画的World Prefab
2. WHEN 使用调试工具生成物品时 THE System SHALL 播放完整的掉落动画
3. THE 调试工具 SHALL 提供选项控制是否播放动画或直接放置

---

## Technical Notes

### 图标旋转计算

```
原始图标尺寸: W x H
旋转45度后的边界框: sqrt(2) * max(W, H)
缩放因子: min(W, H) / (sqrt(2) * max(W, H))
最终尺寸: 保持在原始边界内
```

### 弹性动画参数

```
弹出高度: 0.5 - 1.0 单位
弹出角度: 随机 -45° 到 45°
重力加速度: 9.8 单位/秒²
弹跳衰减: 0.5 (每次弹跳高度减半)
弹跳次数: 2-3 次
浮动幅度: 0.05 单位
浮动周期: 1.5 秒
```

### 性能参数

```
动画激活距离: 15 单位
最大掉落物数量: 100 个
对象池初始大小: 20 个
对象池最大大小: 50 个
```
