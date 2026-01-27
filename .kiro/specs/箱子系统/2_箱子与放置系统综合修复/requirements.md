# 需求文档

## 介绍

本需求文档整合箱子系统前两个工作区的未完成任务，并针对新发现的问题进行综合修复。主要包括：

**工作区 1 未完成任务**：
- Sprite 状态管理（4个 Sprite 字段的切换逻辑）
- 抖动和推动效果（受击抖动、多次跳跃推动）
- IResourceNode 接口实现和 OnHit 重构
- UI 状态同步（BoxPanelUI 与 ChestController 的 isOpen 状态）
- 批量生成工具更新（支持 KeyData 和 LockData）
- 初始化逻辑完善（支持设置 origin）

**新发现问题**：
- 箱子碰撞体未从 Sprite 的 Custom Physics Shape 更新
- 放置系统在向左/向下方向时放置失败
- 箱子丢弃时应使用 Box 预制体而非 WorldItem
- 箱子打开功能无法触发
- 钥匙和锁的材质等级需要扩展到 0-5（6种）

## 术语表

- **PolygonCollider2D**：Unity 的多边形碰撞体组件
- **Custom Physics Shape**：Sprite 的自定义物理形状
- **ChestController**：箱子控制器脚本，管理箱子的所有交互逻辑
- **PlacementNavigator**：放置系统的导航组件
- **ChestDropHandler**：箱子掉落处理器
- **BoxPanelUI**：箱子交互 UI 面板
- **MaterialTier**：材质等级（0-5，对应木/石/铁/铜/钢/金）
- **ChestOrigin**：箱子来源（PlayerCrafted/WorldSpawned）
- **ChestOwnership**：箱子归属（Player/World）
- **IResourceNode**：资源节点接口，用于工具交互系统

## 需求

### 需求 1：Sprite 状态管理（工作区 1 未完成）

**用户故事：** 作为开发者，我希望箱子能够根据状态（上锁/未锁、打开/关闭）自动切换显示对应的 Sprite，以便箱子的视觉外观与状态保持一致。

#### 验收标准

1. WHEN 箱子状态为未锁且关闭时 THEN ChestController SHALL 显示 spriteUnlockedClosed
2. WHEN 箱子状态为未锁且打开时 THEN ChestController SHALL 显示 spriteUnlockedOpen
3. WHEN 箱子状态为上锁且关闭时 THEN ChestController SHALL 显示 spriteLockedClosed
4. WHEN 箱子状态为上锁且打开时 THEN ChestController SHALL 显示 spriteLockedOpen
5. WHEN 箱子状态变化时 THEN ChestController SHALL 自动调用 UpdateSprite() 更新显示
6. WHEN 任意 Sprite 字段为空时 THEN 系统 SHALL 在 OnValidate 中输出警告

### 需求 2：箱子 PolygonCollider2D 初始化和更新（新问题）

**用户故事：** 作为开发者，我希望箱子控制器能够从 Sprite 的 Custom Physics Shape 自动初始化 PolygonCollider2D，并在状态变化时实时更新，以便箱子的碰撞体与视觉外观保持一致。

#### 验收标准

1. WHEN 箱子预制体实例化时 THEN ChestController SHALL 从当前 Sprite 的 Custom Physics Shape 自动更新 PolygonCollider2D 的形状
2. WHEN 箱子 Sprite 因状态变化而改变时 THEN ChestController SHALL 自动更新 PolygonCollider2D 以匹配新 Sprite 的 Custom Physics Shape
3. WHEN 更新 PolygonCollider2D 时 THEN 系统 SHALL 参考 TreeControllerV2 或 StoneController 的实现方式
4. WHEN Sprite 没有 Custom Physics Shape 时 THEN 系统 SHALL 保持原有碰撞体不变并输出警告日志

### 需求 3：抖动和推动效果（工作区 1 未完成）

**用户故事：** 作为玩家，我希望箱子被工具命中时能够播放抖动效果，被镐子推动时能够播放多次跳跃动画，以便交互反馈更加生动。

#### 验收标准

1. WHEN 任意工具命中箱子时 THEN ChestController SHALL 播放抖动效果（随机偏移位置）
2. WHEN 抖动效果播放时 THEN 系统 SHALL 在 shakeDuration 时间内随机偏移位置，结束后恢复原位置
3. WHEN 镐子推动有物品的箱子时 THEN ChestController SHALL 播放多次跳跃动画（3次，越跳越短越矮）
4. WHEN 推动动画播放时 THEN 系统 SHALL 执行 3 次跳跃，高度分别为 1.0、0.6、0.3，距离分别为 0.5、0.3、0.2
5. WHEN 非镐子工具命中箱子时 THEN ChestController SHALL 只播放抖动效果，不造成伤害

### 需求 4：IResourceNode 接口实现（工作区 1 未完成）

**用户故事：** 作为开发者，我希望箱子实现 IResourceNode 接口，以便箱子能够被工具系统正确识别和交互。

#### 验收标准

1. WHEN ChestController 实例化时 THEN 系统 SHALL 将箱子注册到 ResourceNodeRegistry
2. WHEN ChestController 销毁时 THEN 系统 SHALL 从 ResourceNodeRegistry 注销箱子
3. WHEN 工具系统查询 ResourceTag 时 THEN ChestController SHALL 返回 "Chest"
4. WHEN 工具系统查询 IsDepleted 时 THEN ChestController SHALL 返回 false（箱子不会耗尽）
5. WHEN 工具系统查询 CanAccept 时 THEN ChestController SHALL 返回 true（所有工具都能触发抖动）
6. WHEN 工具系统查询 GetBounds 时 THEN ChestController SHALL 返回 SpriteRenderer 或 Collider 的边界

### 需求 5：OnHit 方法重构（工作区 1 未完成）

**用户故事：** 作为开发者，我希望 OnHit 方法能够集成来源归属系统，以便正确处理野外上锁箱子的不可挖取逻辑。

#### 验收标准

1. WHEN 任意工具命中箱子时 THEN ChestController SHALL 始终播放抖动效果
2. WHEN 镐子命中箱子时 THEN ChestController SHALL 检查 CanBeMinedOrMoved()，不可挖取则直接返回
3. WHEN 镐子命中有物品的箱子且可移动时 THEN ChestController SHALL 触发推动逻辑
4. WHEN 镐子命中空箱子且可挖取时 THEN ChestController SHALL 造成伤害
5. WHEN 非镐子工具命中箱子时 THEN ChestController SHALL 不造成任何伤害

### 需求 6：UI 状态同步（工作区 1 未完成）

**用户故事：** 作为开发者，我希望 BoxPanelUI 与 ChestController 的 isOpen 状态能够同步，以便箱子的视觉状态与 UI 状态保持一致。

#### 验收标准

1. WHEN BoxPanelUI.Open() 被调用时 THEN 系统 SHALL 调用 ChestController.SetOpen(true)
2. WHEN BoxPanelUI.Close() 被调用时 THEN 系统 SHALL 调用 ChestController.SetOpen(false)
3. WHEN ChestController.SetOpen() 被调用时 THEN 系统 SHALL 更新 _isOpen 状态并调用 UpdateSprite()
4. WHEN 箱子打开时 THEN 系统 SHALL 显示对应的打开状态 Sprite

### 需求 7：批量生成工具更新（工作区 1 未完成）

**用户故事：** 作为开发者，我希望批量生成工具能够支持创建 KeyData 和 LockData，以便快速创建钥匙和锁的 SO 资产。

#### 验收标准

1. WHEN 批量生成工具打开时 THEN 系统 SHALL 在 ItemSOType 枚举中包含 KeyData 和 LockData 类型
2. WHEN 选择 KeyData 类型时 THEN 系统 SHALL 显示 keyMaterial 下拉框和 unlockChance 滑动条
3. WHEN 选择 LockData 类型时 THEN 系统 SHALL 显示 lockMaterial 下拉框
4. WHEN 创建 KeyData SO 时 THEN 系统 SHALL 根据材质自动设置默认开锁概率
5. WHEN 创建 LockData SO 时 THEN 系统 SHALL 将 unlockChance 固定设置为 0
6. WHEN 批量生成时 THEN 系统 SHALL 将 KeyData 输出到 Assets/111_Data/Items/Keys 文件夹
7. WHEN 批量生成时 THEN 系统 SHALL 将 LockData 输出到 Assets/111_Data/Items/Locks 文件夹

### 需求 8：初始化逻辑完善（工作区 1 未完成）

**用户故事：** 作为开发者，我希望 ChestController 的初始化逻辑能够支持设置 origin，以便正确区分玩家制作和野外生成的箱子。

#### 验收标准

1. WHEN ChestController.Awake() 被调用时 THEN 系统 SHALL 缓存 SpriteRenderer 和 PolygonCollider2D 引用
2. WHEN ChestController.Start() 被调用时 THEN 系统 SHALL 调用 UpdateSprite() 初始化显示
3. WHEN ChestController.Initialize() 被调用时 THEN 系统 SHALL 支持传入 ChestOrigin 参数
4. WHEN 箱子通过放置系统放置时 THEN 系统 SHALL 调用 Initialize(StorageData, ChestOrigin.PlayerCrafted, ChestOwnership.Player)
5. WHEN 箱子通过场景预置时 THEN 系统 SHALL 调用 Initialize(StorageData, ChestOrigin.WorldSpawned, ChestOwnership.World)

### 需求 9：放置系统向左/向下方向放置失败修复（新问题）

**用户故事：** 作为玩家，我希望在任何方向（包括向左和向下）都能正常放置物品，以便放置功能在所有情况下都能可靠工作。

#### 验收标准

1. WHEN 玩家向左方向放置物品时 THEN 系统 SHALL 正确计算导航目标点并成功放置
2. WHEN 玩家向下方向放置物品时 THEN 系统 SHALL 正确计算导航目标点并成功放置
3. WHEN 导航到达目标位置后 THEN 系统 SHALL 以放置成功为终止条件，而非以到达目的地为终止条件
4. WHEN 放置过程中玩家停止移动但预放置方框仍存在时 THEN 系统 SHALL 检测此异常状态并尝试重新放置或提供反馈
5. IF 导航目标点计算导致玩家无法到达 THEN 系统 SHALL 将导航目标设置为放置点中心，并在玩家足够接近时触发放置

### 需求 10：箱子丢弃特殊机制（新问题）

**用户故事：** 作为玩家，我希望箱子从背包丢弃时能够直接落地成为世界物体，而非变为可拾取物品，以便箱子的行为符合游戏设计。

#### 验收标准

1. WHEN 箱子从背包被丢弃时 THEN 系统 SHALL 使用 Box 预制体而非 WorldItem 预制体
2. WHEN 箱子掉落动画播放时 THEN 系统 SHALL 为 Box 预制体添加临时阴影以模拟掉落物的弹跳视觉效果
3. WHEN 箱子掉落动画完成时 THEN 系统 SHALL 将箱子直接转换为已放置的世界物体
4. WHILE 箱子处于掉落动画状态 THEN 系统 SHALL 阻止箱子变为可拾取状态
5. WHEN 箱子落地成为世界物体后 THEN 系统 SHALL 移除临时阴影并使用箱子自身的阴影配置

### 需求 11：箱子打开功能修复（新问题）

**用户故事：** 作为玩家，我希望能够正常打开箱子并查看其内容，以便使用箱子的存储功能。

#### 验收标准

1. WHEN 玩家右键点击箱子且在交互距离内时 THEN 系统 SHALL 打开箱子 UI 面板
2. WHEN 玩家右键点击箱子且超出交互距离时 THEN 系统 SHALL 触发自动导航并在到达后打开箱子 UI
3. WHEN 箱子 UI 打开时 THEN ChestController SHALL 正确引用 BoxPanelUI 预制体
4. WHEN GameInputManager 检测到箱子交互时 THEN 系统 SHALL 正确调用 ChestController.TryOpen() 方法
5. IF ChestController 的 UI 预制体引用为空 THEN 系统 SHALL 输出错误日志并提示配置问题

### 需求 12：钥匙和锁材质等级扩展（新问题）

**用户故事：** 作为开发者，我希望钥匙和锁的 SO 设计能够支持 0-5 共 6 种材质等级，以便与游戏中的材质系统保持一致。

#### 验收标准

1. WHEN KeyLockData SO 创建时 THEN 系统 SHALL 支持 0-5 共 6 种材质等级（而非仅 Wood/Iron/Special）
2. WHEN 钥匙 SO 配置时 THEN 系统 SHALL 使用 materialTier 字段（int 类型，0-5）表示材质等级
3. WHEN 锁 SO 配置时 THEN 系统 SHALL 使用 materialTier 字段（int 类型，0-5）表示材质等级
4. WHEN 钥匙尝试开锁时 THEN 系统 SHALL 基于材质等级计算开锁概率
5. WHEN 锁尝试上锁时 THEN 系统 SHALL 检查锁的材质等级与箱子材质是否兼容
6. WHEN 批量生成工具创建钥匙/锁时 THEN 系统 SHALL 支持选择 0-5 的材质等级
7. WHEN StorageData SO 配置时 THEN 系统 SHALL 使用 materialTier 字段（int 类型，0-5）表示箱子材质等级

### 需求 13：箱子系统与放置系统集成

**用户故事：** 作为开发者，我希望箱子系统能够与放置系统正确集成，以便箱子的放置、交互、导航功能协调工作。

#### 验收标准

1. WHEN 箱子通过放置系统放置时 THEN 系统 SHALL 正确初始化 ChestController 的所有状态
2. WHEN 玩家导航到箱子附近进行交互时 THEN 系统 SHALL 使用与放置系统相同的交互距离判断
3. WHEN 箱子被放置后 THEN 系统 SHALL 将箱子注册到 ResourceNodeRegistry 以支持工具交互
4. WHEN 箱子被销毁时 THEN 系统 SHALL 从 ResourceNodeRegistry 注销
