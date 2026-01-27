# 遮挡透明与云朵阴影系统需求文档

## 简介

本文档定义了 Sunset 2D 农场模拟游戏中遮挡透明系统和云朵阴影系统的功能需求。遮挡透明系统确保玩家被物体遮挡时能够看到角色，云朵阴影系统提供动态的天气视觉效果。

## 当前实现状态

**遮挡透明系统**: ✅ 已完成核心功能
- OcclusionManager: 遮挡检测、标签过滤、树林连通算法
- OcclusionTransparency: 透明度渐变、状态管理
- VerticalGradientOcclusion Shader: 树木渐变透明效果

**云朵阴影系统**: ✅ 已完成核心功能
- CloudShadowManager: 对象池、移动循环、天气联动
- CloudShadowMultiply Shader: Multiply 混合效果
- 编辑器预览和调试工具

## 术语表

- **OcclusionManager**: 遮挡管理器，负责检测和管理所有遮挡透明逻辑
- **OcclusionTransparency**: 可遮挡物体组件，挂载在需要透明化的物体上
- **CloudShadowManager**: 云朵阴影管理器，负责生成和管理移动的云影效果
- **WeatherSystem**: 天气系统，控制游戏中的天气状态
- **Sorting Layer**: Unity 渲染层级系统，用于控制精灵渲染顺序
- **Bounds**: 物体的边界框，用于碰撞和遮挡检测
- **Flood Fill**: 洪水填充算法，用于查找连通的树林区域

## 需求

### 需求 1

**用户故事:** 作为玩家，我希望当被物体遮挡时能够看到我的角色，这样我就不会在复杂场景中迷失方向。

#### 验收标准

1. WHEN 玩家角色的中心点进入遮挡物的边界范围内 THEN 系统 SHALL 将该遮挡物设置为透明状态
2. WHEN 玩家角色离开遮挡物的边界范围 THEN 系统 SHALL 将该遮挡物恢复为不透明状态
3. WHEN 系统检测遮挡状态 THEN 系统 SHALL 使用玩家 Collider 的中心点作为检测基准而非脚底位置
4. WHEN 遮挡物状态发生变化 THEN 系统 SHALL 在指定的渐变时间内平滑过渡透明度
5. WHEN 系统启用标签过滤 THEN 系统 SHALL 仅对指定标签的物体进行遮挡检测

### 需求 2

**用户故事:** 作为玩家，我希望树木在透明时呈现渐变效果，这样看起来更自然美观。

#### 验收标准

1. WHEN 树木被设置为遮挡状态 THEN 系统 SHALL 应用垂直渐变透明效果，根部透明度为 0.8，树干为 0.5，树叶为 0.3
2. WHEN 树木的阴影子物体存在 THEN 系统 SHALL 保持阴影完全不透明
3. WHEN 树木使用渐变材质 THEN 系统 SHALL 确保未遮挡时外观与原始精灵完全一致
4. WHEN 系统应用渐变透明 THEN 系统 SHALL 使用预乘输出避免出现矩形边框效果

### 需求 3

**用户故事:** 作为玩家，我希望进入树林时整片连通的树木都变透明，这样我能更好地导航。

#### 验收标准

1. WHEN 玩家被树木遮挡 THEN 系统 SHALL 使用 Flood Fill 算法查找所有连通的树木
2. WHEN 判断树木连通性 THEN 系统 SHALL 基于树根距离小于 2.5 米或树冠重叠面积超过 15% 的条件
3. WHEN 搜索连通树林 THEN 系统 SHALL 限制最大搜索深度为 50 棵树和最大搜索半径为 15 米
4. WHEN 玩家离开树林边界 THEN 系统 SHALL 清除树林缓存并重新计算连通区域
5. WHEN 系统计算树林边界 THEN 系统 SHALL 扩展边界 2 米以避免频繁重新计算

### 需求 4

**用户故事:** 作为玩家，我希望看到动态的云朵阴影效果，这样游戏世界更有生机。

#### 验收标准

1. WHEN 天气为晴天或多云 THEN 系统 SHALL 显示移动的云朵阴影
2. WHEN 天气为阴天、雨天或雪天 THEN 系统 SHALL 隐藏云朵阴影效果
3. WHEN 云朵阴影移动到区域边界 THEN 系统 SHALL 将其传送到对侧边界实现循环
4. WHEN 系统生成云朵阴影 THEN 系统 SHALL 使用对象池管理以优化性能
5. WHEN 云朵阴影渲染 THEN 系统 SHALL 使用 CloudShadow 排序层级，位于世界物体之上、UI 之下

### 需求 5

**用户故事:** 作为开发者，我希望有完善的编辑器工具，这样我能高效地配置和调试系统。

#### 验收标准

1. WHEN 开发者使用 OcclusionManager Inspector THEN 系统 SHALL 提供可视化的参数配置界面
2. WHEN 开发者启用调试模式 THEN 系统 SHALL 显示检测范围、遮挡物边界和玩家中心点的 Gizmos
3. WHEN 开发者使用批量工具 THEN 系统 SHALL 能够一键为选中的树木应用渐变材质
4. WHEN 开发者配置云朵阴影 THEN 系统 SHALL 提供编辑器预览功能
5. WHEN 系统输出调试信息 THEN 系统 SHALL 限制日志频率为每秒最多一次以避免性能问题

### 需求 6

**用户故事:** 作为系统架构师，我希望系统具有良好的性能和可扩展性，这样能支持大型场景。

#### 验收标准

1. WHEN 系统进行遮挡检测 THEN 系统 SHALL 按 0.1 秒间隔执行以避免每帧计算
2. WHEN 系统管理云朵阴影 THEN 系统 SHALL 限制最大云朵数量为 32 个
3. WHEN 系统处理大量遮挡物 THEN 系统 SHALL 使用距离过滤仅检测玩家周围指定半径内的物体
4. WHEN 系统缓存树林数据 THEN 系统 SHALL 仅在玩家离开缓存边界时重新计算
5. WHEN 系统组件初始化 THEN 系统 SHALL 在 OnEnable 时一次性读取管理器参数而非运行时热更新

### 需求 7

**用户故事:** 作为质量保证工程师，我希望系统能正确处理各种边界情况，这样游戏运行稳定。

#### 验收标准

1. WHEN 遮挡物被销毁 THEN 系统 SHALL 自动从注册列表中移除该物体
2. WHEN 玩家物体不存在 THEN 系统 SHALL 自动查找带有 Player 标签的物体
3. WHEN 云朵精灵数组为空 THEN 系统 SHALL 保持空闲状态不产生错误
4. WHEN Shader 资源缺失 THEN 系统 SHALL 回退到默认材质并记录警告
5. WHEN 系统参数超出有效范围 THEN 系统 SHALL 自动钳制到合理数值