# 遮挡透明与云朵阴影系统设计文档

## 概述

本系统为 Sunset 2D 农场模拟游戏提供遮挡透明和云朵阴影功能。遮挡透明系统确保玩家被物体遮挡时能够看到角色，云朵阴影系统提供动态的天气视觉效果。两个子系统相互独立，可以单独启用或禁用。

## 文件结构

### 核心脚本
```
Assets/Scripts/Service/Rendering/
├── OcclusionManager.cs        # 遮挡管理器（单例）
├── OcclusionTransparency.cs   # 可遮挡物体组件
└── CloudShadowManager.cs      # 云朵阴影管理器
```

### Shader 和材质
```
Assets/Shaders/
├── Shader/
│   ├── VerticalGradientOcclusion.shader  # 树木渐变透明
│   └── CloudShadowMultiply.shader        # 云朵阴影 Multiply
└── Material/
    └── TreeOcclusion.mat                 # 树木渐变材质
```

### 编辑器工具
```
Assets/Editor/
├── OcclusionManagerEditor.cs         # 遮挡管理器编辑器
├── OcclusionTransparencyEditor.cs    # 遮挡组件编辑器
├── CloudShadowManagerEditor.cs       # 云朵阴影编辑器
├── BatchAddOcclusionComponents.cs    # 批量添加遮挡组件
├── BatchApplyTreeMaterial.cs         # 批量应用树木材质
└── CleanInvalidOcclusionComponents.cs # 清理无效组件
```

## 架构

### 系统架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    遮挡透明与云朵阴影系统                      │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────┐    ┌─────────────────────────────┐  │
│  │   遮挡透明子系统      │    │      云朵阴影子系统          │  │
│  │                     │    │                             │  │
│  │  OcclusionManager   │    │   CloudShadowManager        │  │
│  │  ┌───────────────┐  │    │   ┌─────────────────────┐   │  │
│  │  │ 检测逻辑       │  │    │   │ 对象池管理          │   │  │
│  │  │ 标签过滤       │  │    │   │ 天气联动            │   │  │
│  │  │ 树林算法       │  │    │   │ 移动循环            │   │  │
│  │  └───────────────┘  │    │   └─────────────────────┘   │  │
│  │          │          │    │             │               │  │
│  │          ▼          │    │             ▼               │  │
│  │ OcclusionTransparency│    │    Cloud GameObjects       │  │
│  └─────────────────────┘    └─────────────────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│                    外部系统依赖                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ WeatherSystem│  │ Unity Tags  │  │ Sorting Layers      │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### 核心组件关系

- **OcclusionManager**: 单例管理器，负责全局遮挡检测和参数管理
- **OcclusionTransparency**: 组件，挂载在可遮挡物体上，处理透明度变化
- **CloudShadowManager**: 独立管理器，负责云朵阴影的生成和移动
- **WeatherSystem**: 外部依赖，提供天气状态用于云影联动

## 组件和接口

### OcclusionManager

**职责**: 
- 全局遮挡检测逻辑
- 参数集中管理
- 树林连通性算法
- 调试和可视化

**关键接口**:
```csharp
public class OcclusionManager : MonoBehaviour
{
    // 注册管理
    public void RegisterOccluder(OcclusionTransparency occluder);
    public void UnregisterOccluder(OcclusionTransparency occluder);
    
    // 参数获取
    public void GetOcclusionParams(string tag, out float alpha, out float speed);
    
    // 单例访问
    public static OcclusionManager Instance { get; }
}
```

### OcclusionTransparency

**职责**:
- 透明度状态管理
- 渐变动画处理
- 边界信息提供

**关键接口**:
```csharp
public class OcclusionTransparency : MonoBehaviour
{
    // 状态控制
    public void SetOccluding(bool occluding);
    public void SetOccluding(bool occluding, float customAlpha, float customSpeed);
    
    // 信息查询
    public Bounds GetBounds();
    public string GetSortingLayerName();
    public bool CanBeOccluded { get; }
}
```

### CloudShadowManager

**职责**:
- 云朵对象池管理
- 移动和循环逻辑
- 天气系统联动
- 编辑器预览支持

**关键接口**:
```csharp
public class CloudShadowManager : MonoBehaviour
{
    // 控制接口
    public void SetWeatherState(WeatherState state);
    public void RandomizeSeed();
    
    // 编辑器支持
    public void EditorRebuildNow();
    public void EditorDespawnAll();
}
```

## 数据模型

### 遮挡检测数据流

```
玩家位置 → 距离过滤 → 标签过滤 → 层级过滤 → 边界检测 → 状态更新
    ↓
HashSet<OcclusionTransparency> currentlyOccluding
    ↓
树林算法 (Flood Fill) → HashSet<OcclusionTransparency> currentForest
```

### 云朵阴影数据结构

```csharp
private struct Cloud
{
    public Transform transform;
    public SpriteRenderer sr;
    public Vector2 velocity;
    public float halfWidth;
    public float halfHeight;
}
```

### 配置参数模型

```csharp
[System.Serializable]
public class TagOcclusionParams
{
    public string tag;
    [Range(0f, 1f)] public float occludedAlpha;
    [Range(1f, 20f)] public float fadeSpeed;
}
```

## 正确性属性

*属性是一个特征或行为，应该在系统的所有有效执行中保持为真——本质上，是关于系统应该做什么的正式声明。属性作为人类可读规范和机器可验证正确性保证之间的桥梁。*

基于预分析，以下是经过筛选和优化的核心正确性属性：

### 属性 1: 遮挡检测双向一致性
*对于任何*玩家位置和遮挡物配置，当玩家中心点进入遮挡物边界时该遮挡物应变透明，当玩家中心点离开边界时应恢复不透明
**验证需求: 1.1, 1.2**

### 属性 2: 标签过滤准确性
*对于任何*启用标签过滤的配置，只有指定标签的物体应该参与遮挡检测，其他标签的物体应该被忽略
**验证需求: 1.5**

### 属性 3: 透明度渐变连续性
*对于任何*遮挡物状态变化，透明度应该在指定时间内平滑过渡，不应该出现突变或跳跃
**验证需求: 1.4**

### 属性 4: 树木渐变材质正确性
*对于任何*使用渐变材质的树木，遮挡时应该应用垂直渐变效果（根部0.8，树干0.5，树叶0.3），阴影子物体应该保持完全不透明
**验证需求: 2.1, 2.2**

### 属性 5: 树林连通性算法正确性
*对于任何*树木配置，两棵树应该被判定为连通当且仅当它们的树根距离小于2.5米或树冠重叠面积超过15%
**验证需求: 3.2**

### 属性 6: 树林搜索边界限制
*对于任何*树林搜索操作，搜索深度不应超过50棵树，搜索半径不应超过15米
**验证需求: 3.3**

### 属性 7: 云朵天气联动一致性
*对于任何*天气状态，云朵阴影的显示状态应该与配置一致：晴天和多云时显示，阴天、雨天和雪天时隐藏
**验证需求: 4.1, 4.2**

### 属性 8: 云朵循环移动不变性
*对于任何*云朵对象，当它移出活动区域边界时应该在对侧边界重新出现，活动云朵的总数量应该保持不变
**验证需求: 4.3**

### 属性 9: 对象池资源管理正确性
*对于任何*云朵对象的创建和销毁，应该使用对象池进行管理，不应该频繁创建和销毁GameObject
**验证需求: 4.4**

### 属性 10: 性能限制遵守性
*对于任何*系统运行状态，遮挡检测应该按0.1秒间隔执行，云朵数量不应超过32个，距离过滤应该只检测指定半径内的物体
**验证需求: 6.1, 6.2, 6.3**

### 属性 11: 资源清理完整性
*对于任何*组件销毁操作，系统应该自动清理相关引用，不应该存在悬空指针或内存泄漏
**验证需求: 7.1**

### 属性 12: 边界情况处理安全性
*对于任何*异常输入（空数组、缺失资源、超范围参数），系统应该安全处理不产生错误，并在必要时记录警告
**验证需求: 7.3, 7.4, 7.5**

## 错误处理

### 遮挡透明系统错误处理

1. **空引用处理**: 自动查找玩家对象，组件缺失时记录警告
2. **边界情况**: 遮挡物销毁时自动清理引用
3. **参数验证**: 透明度和速度参数自动钳制到有效范围
4. **性能保护**: 限制树林搜索深度和半径

### 云朵阴影系统错误处理

1. **资源缺失**: 精灵数组为空时保持空闲状态
2. **材质回退**: 材质缺失时使用默认材质
3. **天气系统**: 天气系统不存在时使用手动模式
4. **排序层级**: 排序层不存在时回退到默认层

## 测试策略

### 单元测试策略

**遮挡检测逻辑测试**:
- 测试边界检测算法的准确性
- 验证距离过滤和标签过滤的正确性
- 测试树林连通性算法的各种边界情况

**云朵管理测试**:
- 测试对象池的创建和回收逻辑
- 验证循环移动的边界处理
- 测试天气联动的状态切换

**参数管理测试**:
- 测试参数的读取和应用
- 验证边界值的钳制逻辑
- 测试标签自定义参数的优先级

### 属性测试策略

使用 Unity Test Framework 和 NUnit 进行属性测试：

**遮挡一致性测试**:
- 生成随机玩家位置和遮挡物配置
- 验证遮挡状态的一致性和连续性
- 测试大量随机场景下的系统稳定性

**树林算法测试**:
- 生成随机树木布局
- 验证连通性算法的正确性
- 测试性能边界和搜索限制

**云朵循环测试**:
- 生成随机云朵配置和移动参数
- 验证循环逻辑的完整性
- 测试对象池的内存管理

### 集成测试策略

**场景集成测试**:
- 在完整游戏场景中测试系统交互
- 验证与其他系统（天气、导航等）的兼容性
- 测试性能在复杂场景下的表现

**编辑器工具测试**:
- 测试批量工具的正确性
- 验证调试可视化的准确性
- 测试参数配置界面的响应性

## 性能考虑

### 遮挡透明系统优化

1. **检测频率控制**: 使用 0.1 秒间隔避免每帧检测
2. **距离过滤**: 仅检测玩家周围指定半径内的物体
3. **缓存机制**: 树林区域缓存避免频繁重新计算
4. **搜索限制**: 限制 Flood Fill 的最大深度和半径

### 云朵阴影系统优化

1. **对象池管理**: 复用 GameObject 避免频繁创建销毁
2. **数量限制**: 限制最大云朵数量控制渲染负载
3. **更新优化**: 批量更新云朵位置减少 Transform 操作
4. **编辑器模式**: 仅在预览模式下运行编辑器更新

### 内存管理

1. **集合复用**: 使用对象池模式管理集合对象
2. **引用清理**: 及时清理无效引用避免内存泄漏
3. **事件订阅**: 正确管理事件订阅和取消订阅
4. **调试输出**: 限制调试日志频率避免 GC 压力

## 扩展性设计

### 材质系统扩展

- 支持更多渐变模式（水平、径向、自定义）
- 支持动态材质参数调整
- 支持材质动画和特效

### 天气联动扩展

- 支持更多天气类型
- 支持渐变过渡效果
- 支持季节性云朵变化

### 性能扩展

- 支持 LOD 系统根据距离调整检测精度
- 支持多线程处理大规模场景
- 支持空间分割算法优化检测性能

### 编辑器工具扩展

- 支持可视化编辑器配置复杂场景
- 支持批量操作和模板系统
- 支持性能分析和优化建议