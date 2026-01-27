# 实现计划

- [x] 1. 创建 CameraDeadZoneSync 组件


  - 创建 `Assets/YYY_Scripts/Service/Camera/CameraDeadZoneSync.cs`
  - 实现边界检测逻辑（复用 NavGrid2D 的 DetectWorldBounds 模式）
  - 添加 Inspector 配置项
  - _需求: 2.1, 2.2_



- [ ] 2. 实现 Dead Zone 计算与同步
  - [ ] 2.1 实现边界到 Dead Zone 的转换算法
    - 根据 orthographicSize 和 aspect 计算可视区域

    - 计算摄像头可移动范围
    - _需求: 1.1_
  - [x] 2.2 实现与 CinemachinePositionComposer 的同步

    - 获取 PositionComposer 组件引用
    - 设置 Dead Zone Width/Height
    - _需求: 2.3_


- [x] 3. 实现场景切换监听


  - 订阅 SceneManager.sceneLoaded 事件


  - 场景加载后自动刷新边界
  - _需求: 1.2_

- [ ] 4. 添加调试功能
  - [ ] 4.1 在 Scene 视图绘制边界 Gizmos
  - [ ] 4.2 添加 Inspector 按钮手动刷新
  - _需求: 3.2_

- [ ] 5. 测试与验证
  - 在主场景测试边界检测
  - 测试场景切换同步
  - 测试边缘情况（小场景、无 Tilemap）
