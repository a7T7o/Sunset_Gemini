# 摄像头边界限制 - 开发记忆

## 模块概述

实现 Cinemachine 摄像头边界限制功能。运行游戏时，无论场景如何切换，系统都能自动获取当前场景的大小和边界，确保摄像头视野不超出场景边界。

## 当前状态

- **完成度**: 95%
- **最后更新**: 2025-12-25
- **状态**: 待验证

## 会话记录

### 会话 1 - 2025-12-25

**用户需求**:
> 我的游戏当前使用的是，然后我想你能用代码实现对摄像头死区的自动同步，也就是我运行游戏，无论场景怎么转换，你都能获取到当前场景的大小和边界，然后自动同步到死区数据

**完成任务**:
1. 创建新工作区 `摄像头死区同步/`
2. 更新全局规则 workspace-memory.md（追加最高优先级规则）
3. 创建 memory.md 初始文档

**修改文件**:
- `.kiro/steering/workspace-memory.md` - 追加最高优先级规则（禁止覆盖/删除文档）
- `.kiro/specs/摄像头死区同步/memory.md` - 新建
- `.kiro/specs/摄像头死区同步/0_初始需求/requirements.md` - 新建需求文档
- `.kiro/specs/摄像头死区同步/0_初始需求/design.md` - 新建设计文档
- `.kiro/specs/摄像头死区同步/0_初始需求/tasks.md` - 新建任务列表
- `.kiro/specs/README.md` - 追加新工作区信息

**解决方案**:
1. 创建 CameraDeadZoneSync 组件，复用 NavGrid2D 的边界检测逻辑
2. 通过 Cinemachine 3.x API 同步 Dead Zone 参数到 PositionComposer
3. 监听场景切换事件自动刷新

**遗留问题**:
- [x] 实现 CameraDeadZoneSync.cs 代码
- [ ] 测试场景边界检测
- [ ] 测试场景切换同步

---

### 会话 2 - 2025-12-25

**用户需求**:
> 摄像头层级如图所示，Camera是世界内的根物体，然后请你开始完成.kiro\specs\摄像头死区同步\0_初始需求内的所有任务

**完成任务**:
1. 创建 `Assets/YYY_Scripts/Service/Camera/CameraDeadZoneSync.cs`
2. 实现边界检测逻辑（复用 NavGrid2D 的 DetectWorldBounds 模式）
3. 实现 Dead Zone 计算与同步
4. 实现场景切换监听
5. 添加调试功能（Gizmos + Inspector 按钮）

**修改文件**:
- `Assets/YYY_Scripts/Service/Camera/CameraDeadZoneSync.cs` - 新建

**解决方案**:
核心实现：
1. 边界检测：扫描 LAYER 1/2/3 下的所有 Tilemap，计算联合边界
2. Dead Zone 计算：
   - 摄像头可视区域 = orthographicSize * 2 * aspect
   - 摄像头可移动范围 = 世界边界 - 可视区域
   - Dead Zone = 可移动范围 / 可视区域（归一化到 0-1）
3. 场景切换：订阅 SceneManager.sceneLoaded 事件，延迟一帧刷新
4. 调试：Scene 视图绘制边界 Gizmos，ContextMenu 手动刷新

**遗留问题**:
- [ ] 用户手动添加组件到 CinemachineCamera
- [ ] 测试验证

---

### 会话 3 - 2025-12-25

**用户需求**:
> 我的意思也不是死区，就是我的视野不能超出场景的边界，死区是我自己定我说错了，我要的是视野不能超出游戏场景，这是最主要的逻辑

**完成任务**:
1. 重构 `CameraDeadZoneSync.cs`，改用 `CinemachineConfiner2D` 方案
2. 自动创建边界碰撞体 `PolygonCollider2D`
3. 自动检测 Tilemap 边界并更新碰撞体形状

**修改文件**:
- `Assets/YYY_Scripts/Service/Camera/CameraDeadZoneSync.cs` - 重构为 Confiner 方案

**解决方案**:
核心实现：
1. 使用 `CinemachineConfiner2D` 组件限制摄像头边界
2. 自动创建 `_CameraBounds` 物体，包含 `PolygonCollider2D`
3. 检测 LAYER 1/2/3 下的 Tilemap，计算联合边界
4. 将边界转换为矩形碰撞体顶点
5. 调用 `InvalidateBoundingShapeCache()` 强制刷新

**遗留问题**:
- [ ] 用户手动添加组件到 CinemachineCamera
- [ ] 测试验证

---

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 使用 Cinemachine 3.x API | 项目使用 Unity 6 + Cinemachine 3.1.4 | 2025-12-25 |
| 改用 CinemachineConfiner2D | 用户需要的是视野边界限制，不是死区 | 2025-12-25 |

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/Service/Camera/CameraDeadZoneSync.cs` | 摄像头边界限制组件（使用 Confiner2D） |

## 技术背景

从用户提供的截图可以看到：
- 使用 CinemachineCamera 组件
- Position Composer 控制摄像头位置
- Dead Zone 参数需要根据场景边界动态调整
- Orthographic Size = 10.5
- Camera Distance = 15
