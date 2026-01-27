# 需求文档 - 箱子样式与交互完善

## 简介

本需求文档描述箱子系统的样式管理、交互完善、来源归属系统和钥匙开锁概率系统，包括：
- 4种视觉状态的 Sprite 切换（未锁关闭、未锁打开、上锁关闭、上锁打开）
- 箱子打开/关闭的动画效果（可选，基于2个 Sprite 实现）
- 箱子推动的跳跃动画（多次跳跃，越跳越短越矮）
- 与玩家工具系统的集成（镐子攻击箱子）
- **箱子来源与归属系统**（玩家制作 vs 野外生成）
- **钥匙开锁概率系统**（6种材质钥匙，概率开锁玩法）
- 箱子不需要影子

## 术语表

- **ChestController**: 箱子控制器组件，管理箱子的所有交互逻辑
- **ChestVisualState**: 箱子视觉状态（未锁关闭、未锁打开、上锁关闭、上锁打开）
- **ChestOrigin**: 箱子来源（PlayerCrafted=玩家制作, WorldSpawned=野外生成）
- **ChestOwnership**: 箱子归属（Player=玩家所有, World=世界所有）
- **IResourceNode**: 资源节点接口，用于与工具攻击系统集成
- **StorageData**: 箱子数据 ScriptableObject
- **KeyLockData**: 钥匙/锁数据 ScriptableObject，包含材质和开锁概率
- **LockData**: 锁数据（KeyLockData 的锁类型），用于上锁箱子
- **开锁概率**: 最终开锁成功率 = 钥匙开锁概率 + 箱子被开锁概率

## 箱子类型

| 类型 | 材质 | 行×列 | 容量 | 血量 | 被开锁概率 | ID |
|------|------|-------|------|------|-----------|-----|
| 小木箱 | 木质 | 1×12 | 12格 | 2 | 0.60 | 1400 |
| 大木箱 | 木质 | 2×12 | 24格 | 3 | 0.60 | 1401 |
| 小铁箱 | 铁质 | 3×12 | 36格 | 4 | 0.50 | 1402 |
| 大铁箱 | 铁质 | 4×12 | 48格 | 5 | 0.50 | 1403 |

## 钥匙类型

| 材质 | 开锁概率 | ID范围 |
|------|----------|--------|
| 木钥匙 | 0.10 | 01XX |
| 石钥匙 | 0.15 | 01XX |
| 铁钥匙 | 0.20 | 01XX |
| 铜钥匙 | 0.25 | 01XX |
| 钢钥匙 | 0.30 | 01XX |
| 金钥匙 | 0.40 | 01XX |

## 箱子来源与归属规则

### 来源类型

| 来源 | 说明 | 初始归属 |
|------|------|----------|
| PlayerCrafted | 玩家通过制作台制作 | Player |
| WorldSpawned | 野外生成（地图预置/怪物掉落） | World |

### 归属与交互规则矩阵

| 来源 | 初始状态 | 上锁规则 | 挖取/移动规则 | 开锁规则 |
|------|----------|----------|---------------|----------|
| 玩家制作 | 未锁 | 可上锁（消耗锁，不需要钥匙打开） | ✅ 可挖取、可移动 | 不需要钥匙 |
| 玩家制作 | 已锁 | ❌ 不能再次上锁 | ✅ 可挖取、可移动 | 不需要钥匙 |
| 野外生成 | 未锁 | 可上锁（变为Player归属） | ✅ 可挖取、可移动 | 不需要钥匙 |
| 野外生成 | 已锁 | ❌ 不能上锁 | ❌ 不能挖取、不能移动 | 需要钥匙（概率开锁） |
| 野外生成 | 已锁→开锁后 | ❌ 不能再次上锁 | ❌ 不能挖取、不能移动 | 已开锁，可直接打开 |

### 核心规则

1. **玩家箱子上锁不需要钥匙**：玩家对自己放置/制作的箱子上锁后，打开时不需要钥匙
2. **野外上锁箱子是"一次性宝箱"**：只能开锁取物，不能挖取带走
3. **所有上过锁的箱子不能再次上锁**：防止滥用锁机制
4. **野外未锁箱子可被玩家占有**：上锁后变为玩家归属，可挖取移动

## 玩法设计说明

### 钥匙刷箱玩法

- **设计理念**：钥匙开锁是概率玩法，玩家可以用低级钥匙反复尝试开锁
- **平衡机制**：
  - 开锁失败时钥匙消失
  - 开锁成功时钥匙保留
  - 钥匙制作时间较长，材料稀缺
- **策略选择**：
  - 用低级钥匙多次尝试（成本低但概率低）
  - 用高级钥匙一次成功（成本高但概率高）

### 野外宝箱探索

- **设计理念**：野外上锁箱子是探索奖励，不能被玩家带走
- **玩法体验**：
  - 发现野外上锁箱子 → 准备钥匙 → 概率开锁 → 获取内部物品
  - 箱子本身不能获取，增加探索价值

### 后续扩展预留

- **6种材质箱子**：木、石、铁、铜、钢、金（当前只实现木和铁）
- **材质对应被开锁概率**：材质越高级，被开锁概率越低

## 需求

### 需求 1 - Sprite 状态显示

**用户故事:** 作为玩家，我希望箱子能根据其状态（上锁/未锁、打开/关闭）显示不同的外观，以便我能直观地了解箱子的当前状态。

#### 验收标准

1. WHEN 箱子被放置且未上锁且关闭 THEN ChestController SHALL 显示"未锁关闭"样式的 Sprite
2. WHEN 箱子被打开且未上锁 THEN ChestController SHALL 显示"未锁打开"样式的 Sprite
3. WHEN 箱子被上锁且关闭 THEN ChestController SHALL 显示"上锁关闭"样式的 Sprite
4. WHEN 箱子被打开且已上锁（玩家归属） THEN ChestController SHALL 显示"上锁打开"样式的 Sprite
5. WHEN 箱子状态变化 THEN ChestController SHALL 尝试播放过渡动画效果（可选，不影响基础功能）

### 需求 2 - 空箱子挖取

**用户故事:** 作为玩家，我希望用镐子攻击空箱子时能看到抖动效果并最终变成掉落物，以便我知道攻击是否有效。

#### 验收标准

1. WHEN 玩家用镐子攻击空箱子 THEN ChestController SHALL 播放抖动效果并扣除血量
2. WHEN 空箱子血量归零 THEN ChestController SHALL 销毁箱子并生成可拾取的箱子掉落物
3. WHEN 箱子掉落物生成 THEN 掉落物 SHALL 先弹跳1秒后才能被玩家拾取
4. WHEN 玩家用非镐子工具攻击箱子 THEN ChestController SHALL 播放抖动效果但不造成伤害

### 需求 3 - 有物品箱子推动

**用户故事:** 作为玩家，我希望用镐子攻击有物品的箱子时能看到箱子被推动的跳跃动画，以便我能移动箱子位置。

#### 验收标准

1. WHEN 玩家用镐子攻击有物品的箱子 THEN ChestController SHALL 向玩家朝向方向推动箱子
2. WHEN 箱子被推动 THEN ChestController SHALL 播放多次跳跃动画（越跳越短、越跳越矮）
3. WHEN 推动目标位置有碰撞体 THEN ChestController SHALL 取消推动
4. WHEN 箱子推动过程中 THEN Collider SHALL 跟随箱子 Sprite 一起移动（与玩家移动时 Collider 跟随一致）

### 需求 4 - 工具系统集成

**用户故事:** 作为玩家，我希望箱子能与现有的工具攻击系统集成，以便我能用镐子挖取箱子。

#### 验收标准

1. WHEN ChestController 初始化 THEN ChestController SHALL 注册到 ResourceNodeRegistry
2. WHEN ChestController 销毁 THEN ChestController SHALL 从 ResourceNodeRegistry 注销
3. WHEN 玩家工具命中箱子 THEN PlayerInteraction SHALL 调用 ChestController.OnHit 方法
4. WHEN ChestController.CanAccept 被调用 THEN ChestController SHALL 返回工具类型是否为镐子

### 需求 5 - UI 状态同步

**用户故事:** 作为玩家，我希望箱子UI打开和关闭时箱子外观能同步变化，以便我能看到箱子的打开/关闭状态。

#### 验收标准

1. WHEN BoxPanelUI.Open 被调用 THEN BoxPanelUI SHALL 通知 ChestController 更新为打开状态
2. WHEN BoxPanelUI.Close 被调用 THEN BoxPanelUI SHALL 通知 ChestController 更新为关闭状态
3. WHEN ChestController 收到打开通知 THEN ChestController SHALL 切换到打开状态的 Sprite
4. WHEN ChestController 收到关闭通知 THEN ChestController SHALL 切换到关闭状态的 Sprite

### 需求 6 - Sprite 配置

**用户故事:** 作为开发者，我希望箱子预制体能方便地配置4种样式的 Sprite，以便我能快速创建不同类型的箱子。

#### 验收标准

1. WHEN 在 Inspector 中配置 ChestController THEN ChestController SHALL 提供4个 Sprite 字段（未锁关闭、未锁打开、上锁关闭、上锁打开）
2. WHEN 任意 Sprite 字段为空 THEN ChestController SHALL 在 OnValidate 中输出警告
3. WHEN 箱子类型为木质 THEN ChestController SHALL 使用木质箱子的 Sprite 集
4. WHEN 箱子类型为铁质 THEN ChestController SHALL 使用铁质箱子的 Sprite 集

### 需求 7 - 箱子来源与归属系统

**用户故事:** 作为玩家，我希望玩家制作的箱子和野外生成的箱子有不同的交互规则，以便野外宝箱成为探索奖励而非可带走的资源。

#### 验收标准

1. WHEN 箱子被玩家制作并放置 THEN ChestController SHALL 设置 origin 为 PlayerCrafted 且 ownership 为 Player
2. WHEN 箱子在野外生成（地图预置或怪物掉落） THEN ChestController SHALL 设置 origin 为 WorldSpawned 且 ownership 为 World
3. WHEN 玩家对野外未锁箱子上锁 THEN ChestController SHALL 将 ownership 变更为 Player
4. WHEN 玩家对野外上锁箱子成功开锁 THEN ChestController SHALL 保持 ownership 为 World 但标记为已解锁
5. WHEN 箱子曾经被上过锁（无论来源） THEN ChestController SHALL 禁止再次上锁

### 需求 8 - 野外上锁箱子限制

**用户故事:** 作为玩家，我希望野外上锁的箱子不能被挖取或移动，以便它们成为"一次性宝箱"只能开锁取物。

#### 验收标准

1. WHEN 玩家用镐子攻击野外上锁箱子 THEN ChestController SHALL 播放抖动效果但不造成伤害且不推动
2. WHEN 野外上锁箱子被成功开锁后 THEN ChestController SHALL 仍然禁止挖取和移动
3. WHEN 野外上锁箱子被成功开锁后 THEN ChestController SHALL 允许玩家打开并获取内部物品
4. IF 箱子为野外上锁状态 THEN ChestController SHALL 在 CanAccept 中返回 false 阻止伤害

### 需求 9 - 玩家箱子上锁系统

**用户故事:** 作为玩家，我希望对自己制作的箱子上锁后不需要钥匙就能打开，以便锁只是防止他人（未来多人模式）或意外操作。

#### 验收标准

1. WHEN 玩家手持锁道具右键点击自己的未锁箱子 THEN ChestController SHALL 消耗锁道具并将箱子标记为已上锁
2. WHEN 玩家右键点击自己的已上锁箱子 THEN ChestController SHALL 直接打开箱子无需钥匙
3. WHEN 玩家对自己的已上锁箱子使用镐子 THEN ChestController SHALL 允许挖取和移动
4. WHEN 箱子已经上过锁 THEN ChestController SHALL 拒绝再次上锁并显示提示"箱子已上过锁"

### 需求 10 - 钥匙开锁概率系统

**用户故事:** 作为玩家，我希望用不同材质的钥匙开锁野外上锁箱子时有不同的成功概率，以便钥匙材质成为策略选择。

#### 验收标准

1. WHEN 玩家手持钥匙右键点击野外上锁箱子 THEN ChestController SHALL 计算开锁概率（钥匙概率 + 箱子被开锁概率）
2. WHEN 开锁成功 THEN ChestController SHALL 保留钥匙并将箱子标记为已解锁
3. WHEN 开锁失败 THEN ChestController SHALL 消耗钥匙并显示"开锁失败"提示
4. WHEN 任意材质钥匙尝试开锁 THEN ChestController SHALL 允许尝试（无材质匹配限制）
5. WHEN 开锁概率计算时 THEN ChestController SHALL 使用公式：最终概率 = 钥匙开锁概率 + 箱子被开锁概率

### 需求 11 - 钥匙数据配置

**用户故事:** 作为开发者，我希望能配置不同材质钥匙的开锁概率，以便平衡游戏难度。

#### 验收标准

1. WHEN 创建 KeyLockData SO 时 THEN KeyLockData SHALL 包含 keyLockType 字段（Key 或 Lock）
2. WHEN 创建 KeyLockData SO 时 THEN KeyLockData SHALL 包含 unlockChance 字段（0-1 浮点数）
3. WHEN 木钥匙配置时 THEN unlockChance SHALL 设置为 0.10
4. WHEN 石钥匙配置时 THEN unlockChance SHALL 设置为 0.15
5. WHEN 铁钥匙配置时 THEN unlockChance SHALL 设置为 0.20
6. WHEN 铜钥匙配置时 THEN unlockChance SHALL 设置为 0.25
7. WHEN 钢钥匙配置时 THEN unlockChance SHALL 设置为 0.30
8. WHEN 金钥匙配置时 THEN unlockChance SHALL 设置为 0.40

### 需求 12 - 箱子被开锁概率配置

**用户故事:** 作为开发者，我希望能配置不同材质箱子的被开锁概率，以便区分箱子难度等级。

#### 验收标准

1. WHEN 创建 StorageData SO 时 THEN StorageData SHALL 包含 baseUnlockChance 字段（0-1 浮点数）
2. WHEN 木质箱子配置时 THEN baseUnlockChance SHALL 设置为 0.60
3. WHEN 铁质箱子配置时 THEN baseUnlockChance SHALL 设置为 0.50
4. WHEN 后续扩展更多材质箱子时 THEN baseUnlockChance SHALL 按材质等级递减（预留设计）


### 需求 13 - 批量生成物品 SO 工具更新

**用户故事:** 作为开发者，我希望批量生成物品 SO 的工具能支持创建 KeyData（钥匙数据），以便快速创建6种材质的钥匙。

#### 验收标准

1. WHEN 在批量生成工具中选择物品类型 THEN 工具 SHALL 提供 KeyData 选项
2. WHEN 创建 KeyData 时 THEN 工具 SHALL 显示 keyMaterial 下拉框（复用 ToolMaterial 枚举）
3. WHEN 创建 KeyData 时 THEN 工具 SHALL 显示 unlockChance 滑动条（0-1 范围）
4. WHEN 批量创建钥匙时 THEN 工具 SHALL 根据材质自动设置默认开锁概率
5. WHEN 保存 KeyData 时 THEN 工具 SHALL 使用命名规范 `Key_{id}_{materialName}.asset`
