---
inclusion: manual
priority: P1
keywords: [动画, 工具, 同步, 帧, Animator, Animation]
lastUpdated: 2025-01-09
---

# 动画系统规范

## 人物与工具动画同步（Mode A++ 帧索引硬锁）

### 核心原理
- 用"帧号"而非"时间"做同步基准
- 从 Player 的 Sprite 名称解析帧号
- 按帧数比例映射到 Tool 帧号
- 使用 `Play(hash, normalizedTime) + Update(0)` 实现当帧同步
- 添加极小正偏置 (1e-6) 避免边界抖动

### 运行时流程
1. 镜像 `State/Direction` 到 Tool Animator，立刻 `Update(0)` 完成过渡评估
2. 从 `playerSpriteRenderer.sprite.name` 末尾解析帧号
3. 按配置的帧数将 Player 帧号映射到 Tool 帧号
4. 计算工具目标状态哈希，`Play(hash, toolNormalized)` 并 `Update(0)`
5. 切换显隐与排序（Order = Player - 1），同步 flipX

### 命名规范
- 状态/剪辑名：`{ActionType}_{Direction}_Clip_{itemId}_{quality}`
- 动作类型：`Slice`/`Crush`/`Pierce`
- 方向：`Down`/`Side`/`Up`

### 文件结构
```
Assets/Animations/
├── Clips/{ActionType}/{id}_{itemName}/{Down|Side|Up}/
│   └── {Action}_{Dir}_Clip_{id}_{quality}.anim
└── Controllers/{ActionType}/{id}_{itemName}/
    └── {Action}_Controller_{id}_{itemName}.controller
```

### Animator 参数
- `State(int)`：动画状态
- `Direction(int)`：方向
  - **🔴 重要：0=Down, 1=Up, 2=Side（不是 0=Down, 1=Side, 2=Up！）**
  - 来源：`PlayerAnimController.ConvertToAnimatorDirection()`
- `ToolItemId(int)`：工具物品ID
- `ToolQuality(int)`：工具品质
- `FlipX(bool)`：水平翻转

### ⚠️ Direction 参数映射（全局统一）

**唯一真相来源**：`PlayerAnimController.ConvertToAnimatorDirection()`

```csharp
// 正确映射
AnimDirection.Down  => 0  // Direction=0 表示朝下
AnimDirection.Up    => 1  // Direction=1 表示朝上（不是 Side！）
AnimDirection.Right => 2  // Direction=2 表示朝左/右（Side）
AnimDirection.Left  => 2  // Direction=2 表示朝左/右（Side）
```

**常见错误**：误以为 `1=Side, 2=Up`，实际上是 `1=Up, 2=Side`！

### 品质回退机制
- 目标状态名：`{ActionType}_{Dir}_Clip_{ToolItemId}_{ToolQuality}`
- 若不存在则回退到 `ToolQuality=0`

## 动画ID映射

### ToolData/WeaponData 字段
- `useQualityIdMapping (bool)`：是否启用品质到默认动画ID的映射
- `animationDefaultId (int)`：启用映射时用于命名/匹配的默认ID
- `GetAnimationKeyId()`：返回用于动画匹配的ID

### 使用示例
- 工具A 不同品质ID：101/102/103
- 若映射启用且默认ID=101，则三者动画均使用 `{...}_Clip_101_{quality}`

## 三向动画生成器（TriDirectionalFusionTool）

### 功能特性
- 中文 UI，拖放文件夹支持
- 单品质计数，可选的 Pivot 应用
- 固定 8 帧
- 时间轴控制：`totalFrames`/`lastFrame` 控制关键帧分布

### 控制器配置
- 仅使用 Any State → state 转换（无状态间转换）
- 参数：`State`, `Direction`, `ToolItemId`, `ToolQuality`

## 动画状态枚举

```csharp
public enum AnimState
{
    Idle = 0,       // 待机站立（无移动）
    Walk = 1,       // 行走
    Run = 2,        // 跑步
    Carry = 3,      // 举着物体（包含站立、走路、跑步）
    Collect = 4,    // 捡起物体（农作物或埋在地里的宝箱之类）
    Hit = 5,        // 受击
    Slice = 6,      // 挥砍（斧头）
    Pierce = 7,     // 刺出（长剑）
    Crush = 8,      // 挖掘（镐子、锄头）
    Fish = 9,       // 钓鱼（鱼竿）
    Watering = 10,  // 浇水（洒水壶）
    Death = 11      // 死亡特效
}
```

## 工具类型到动画状态映射

| 工具类型 | 动画状态 | 说明 |
|---------|---------|------|
| Axe（斧头） | Slice (6) | 挥砍动作 |
| Sword（长剑） | Pierce (7) | 刺出动作 |
| Pickaxe（镐子） | Crush (8) | 挖掘动作 |
| Hoe（锄头） | Crush (8) | 挖掘动作（与镐子共用） |
| FishingRod（鱼竿） | Fish (9) | 钓鱼动作 |
| WateringCan（洒水壶） | Watering (10) | 浇水动作 |
| Sickle（镰刀） | Slice (6) | 挥砍动作（与斧头共用） |

**重要说明**：
- 锄头和镐子都使用 `Crush` 动画（挖掘动作）
- 斧头和镰刀都使用 `Slice` 动画（挥砍动作）
- 长剑使用 `Pierce` 动画（刺出动作）

## 关键脚本职责

### PlayerToolController
- 管理工具切换
- 设置 Animator 参数：ToolType, ToolQuality, ToolItemId
- 使用 `GetAnimationKeyId()` 获取动画ID

### LayerAnimSync
- 负责帧同步逻辑
- 镜像参数、帧锁定、显隐排序
- 处理 flipX 同步

### PlayerAnimController
- 控制玩家动画状态机
- 设置 State, Direction 参数
