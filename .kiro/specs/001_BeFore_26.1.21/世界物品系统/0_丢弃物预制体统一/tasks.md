# 丢弃物预制体统一 - 任务列表

## 实现计划

- [x] 1. 修改 WorldItemPool 数据结构


  - 添加 `_poolByItemId` 字典（按 itemId 分类的对象池）
  - 添加 `maxPoolSizePerItem` 配置字段
  - 保留 `_defaultPool` 作为回退
  - _需求: 3.1, 3.2, 3.3_



- [ ] 2. 实现 GetFromPoolByItemId 方法
  - 优先从对应 itemId 的池中获取
  - 检查 ItemData.worldPrefab 是否存在
  - 如果存在，从 worldPrefab 实例化


  - 如果不存在，回退到默认池
  - _需求: 1.1, 1.2_



- [ ] 3. 修改 Spawn 方法
  - 调用 GetFromPoolByItemId 替代 GetFromPool
  - 确保正确初始化物品数据
  - _需求: 1.1, 1.3, 1.4_



- [x] 4. 修改 Despawn 方法

  - 根据 itemId 放入对应的池
  - 处理池容量限制
  - _需求: 3.2_


- [ ] 5. 确保 WorldItemPickup.ItemId 属性可访问
  - 检查是否有公开的 ItemId 属性
  - 如果没有，添加 `public int ItemId => itemId;`

  - _需求: 3.2_

- [x] 6. 验证 TreeController 和 StoneController 的掉落逻辑


  - 确认它们通过 WorldItemPool 生成掉落物
  - 确认使用正确的 ItemData
  - _需求: 2.1, 2.2_

- [ ] 7. 添加调试日志
  - 记录使用 worldPrefab 还是 defaultPrefab
  - 记录池的使用情况

- [ ] 8. 更新工作区记忆
  - 更新 `.kiro/specs/世界物品系统/memory.md`
  - 记录本次修改

- [ ] 9. Checkpoint - 验证所有功能
  - 测试丢弃物有阴影
  - 测试砍树掉落物有阴影
  - 确保对象池正常工作
