# 需求文档

## 简介

实现 Cinemachine 摄像头死区与场景边界的自动同步功能。

## 术语表

- **摄像头死区（Dead Zone）**：Cinemachine Position Composer 中的参数，定义目标在屏幕中可以移动而不触发摄像头跟随的区域
- **场景边界（Scene Bounds）**：当前场景的可玩区域边界，通常由 Tilemap 或 Confiner 定义
- **Position Composer**：Cinemachine 3.x 中控制摄像头位置的组件

## 需求

### 需求 1

**用户故事**：作为玩家，我希望摄像头能够智能地限制在场景边界内，这样我就不会看到场景外的空白区域。

#### 验收标准

1. WHEN 场景加载完成 THEN 系统 SHALL 自动检测场景边界并计算死区参数
2. WHEN 场景切换时 THEN 系统 SHALL 重新计算并应用新场景的死区参数
3. WHEN 玩家移动到场景边缘 THEN 摄像头 SHALL 停止跟随以避免显示场景外区域

### 需求 2

**用户故事**：作为开发者，我希望系统能自动获取场景大小，这样我不需要手动配置每个场景的摄像头参数。

#### 验收标准

1. WHEN 系统初始化时 THEN 系统 SHALL 扫描场景中的 Tilemap 计算联合边界
2. WHEN 场景中存在 Confiner 边界 THEN 系统 SHALL 优先使用 Confiner 定义的边界
3. WHEN 边界计算完成 THEN 系统 SHALL 将边界数据同步到 Cinemachine Position Composer

### 需求 3

**用户故事**：作为开发者，我希望能够在运行时动态调整死区参数，以便调试和优化摄像头行为。

#### 验收标准

1. WHEN 场景边界发生变化 THEN 系统 SHALL 自动更新死区参数
2. WHEN 开发者在 Inspector 中修改参数 THEN 系统 SHALL 实时应用更改
3. IF 自动计算的参数不合适 THEN 系统 SHALL 允许手动覆盖
