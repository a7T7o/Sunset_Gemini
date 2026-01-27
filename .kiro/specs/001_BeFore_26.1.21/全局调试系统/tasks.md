# 实现计划

- [ ] 1. 修改 TimeManager 添加事件发布开关
  - [ ] 1.1 添加事件发布开关字段（enableDayEvent, enableSeasonEvent, enableHourEvent, enableMinuteEvent）
    - 在 TimeManager.cs 的调试区域添加新的 Header 和字段
    - 每个字段添加详细的 Tooltip 说明影响范围
    - _需求: 1.1, 1.2, 1.3, 1.4, 3.1, 3.2_
  - [ ] 1.2 修改事件发布逻辑，添加开关判断
    - 修改 AdvanceMinute() 中的 OnMinuteChanged 发布
    - 修改 AdvanceHour() 中的 OnHourChanged 发布
    - 修改 AdvanceDay() 中的 OnDayChanged 发布
    - 修改 AdvanceSeason() 中的 OnSeasonChanged 发布
    - 修改 SetTime() 中的事件发布
    - _需求: 1.1, 1.2, 1.3, 1.4, 1.5_
  - [ ] 1.3 添加开关状态变化的调试日志
    - 在开关状态改变时输出日志（通过 OnValidate 或属性 setter）
    - _需求: 3.3_

- [ ] 2. 简化 TreeControllerV2 移除本地开关
  - [ ] 2.1 移除 enableSeasonEvents 字段及相关逻辑
    - 删除 enableSeasonEvents 字段定义
    - 删除 Inspector 中的"季节事件开关"区域
    - _需求: 2.3_
  - [ ] 2.2 修改 Start() 方法，始终订阅所有事件
    - 移除 if (enableSeasonEvents) 条件判断
    - 保持所有事件订阅代码
    - _需求: 2.1, 2.2_
  - [ ] 2.3 修改 OnDestroy() 方法，始终取消订阅
    - 移除 if (enableSeasonEvents) 条件判断
    - 保持所有事件取消订阅代码
    - _需求: 2.1_

- [ ] 3. 更新 TreeControllerV2Editor 移除相关 UI
  - [ ] 3.1 移除"季节事件开关"区域的绘制代码
    - 删除 DrawSeasonEventToggle() 或相关代码
    - _需求: 2.3_

- [ ] 4. 验证和测试
  - [ ] 4.1 验证默认状态下功能正常（所有开关为 true）
    - 运行游戏，按右箭头跳到下一天
    - 确认树木正常成长
    - _需求: 4.1_
  - [ ] 4.2 验证关闭 enableDayEvent 后树木不成长
    - 在 TimeManager Inspector 中关闭 enableDayEvent
    - 按右箭头跳到下一天
    - 确认树木不成长
    - _需求: 1.1, 1.5_
  - [ ] 4.3 验证重新开启开关后功能恢复
    - 重新开启 enableDayEvent
    - 按右箭头跳到下一天
    - 确认树木恢复成长
    - _需求: 4.2, 4.3_

- [ ] 5. 更新相关文档
  - [ ] 5.1 更新树木系统 memory.md
    - 记录 enableSeasonEvents 已移除
    - 记录调试开关已移至 TimeManager
    - _需求: 无（文档维护）_
