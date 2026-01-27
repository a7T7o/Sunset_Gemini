# Design Document - TreeControllerV2 Inspector UI 重构

## Overview

本设计文档描述 TreeControllerV2 Inspector UI 的重构方案。核心目标是将当前使用可变长度数组的 UI 交互改为固定展示的方式，并修正数据结构中的错误设计。

## Architecture

### 组件结构

```
TreeControllerV2 (MonoBehaviour)
├── StageConfig[6]          # 固定6个阶段配置
├── TreeSpriteConfig        # Sprite 配置容器
│   ├── StageSpriteData stage0-5  # 6个阶段的 Sprite 数据
├── ShadowConfig[5]         # 固定5个影子配置（阶段1-5）
├── int[6] stageExperience  # 固定6个经验值

TreeControllerV2Editor (CustomEditor)
├── DrawStageConfigs()      # 绘制阶段配置
├── DrawSpriteConfig()      # 绘制 Sprite 配置
├── DrawShadowConfigs()     # 绘制影子配置
├── DrawStageExperience()   # 绘制经验配置
```

## Components and Interfaces

### 1. TreeControllerV2Editor

新建自定义编辑器脚本，负责绘制 TreeControllerV2 的 Inspector UI。

```csharp
[CustomEditor(typeof(TreeControllerV2))]
public class TreeControllerV2Editor : Editor
{
    // 绘制固定的阶段配置
    private void DrawStageConfigs();
    
    // 绘制固定的 Sprite 配置
    private void DrawSpriteConfig();
    
    // 绘制固定的影子配置
    private void DrawShadowConfigs();
    
    // 绘制固定的经验配置
    private void DrawStageExperience();
}
```

### 2. StageSpriteData 修改

移除 winterMelted 字段：

```csharp
[System.Serializable]
public class StageSpriteData
{
    // 正常状态
    public SeasonSpriteSet normal;
    
    // 枯萎状态（阶段1-5有）
    public bool hasWitheredState = false;
    public Sprite witheredSummer;
    public Sprite witheredFall;
    
    // 树桩状态（阶段3-5有）
    public bool hasStump = false;
    public Sprite stumpSpringSummer;
    public Sprite stumpFall;
    public Sprite stumpWinter;
    
    // ❌ 移除: public Sprite winterMelted;
}
```

## Data Models

### 阶段配置显示规则

| 阶段 | 成长天数 | 血量 | 树桩设置 | 碰撞遮挡 | 掉落设置 | 工具类型 |
|------|----------|------|----------|----------|----------|----------|
| 0    | ✓        | ✓    | ✗ 隐藏   | ✓        | ✓        | 只读 Hoe |
| 1    | ✓        | ✓    | ✗ 隐藏   | ✓        | ✓        | ✓        |
| 2    | ✓        | ✓    | ✗ 隐藏   | ✓        | ✓        | ✓        |
| 3    | ✓        | ✓    | ✓        | ✓        | ✓        | ✓        |
| 4    | ✓        | ✓    | ✓        | ✓        | ✓        | ✓        |
| 5    | ✓        | ✓    | ✓        | ✓        | ✓        | ✓        |

### Sprite 配置显示规则

| 阶段 | 正常状态 | 枯萎状态 | 树桩状态 |
|------|----------|----------|----------|
| 0    | ✓        | ✗ 隐藏   | ✗ 隐藏   |
| 1    | ✓        | ✓        | ✗ 隐藏   |
| 2    | ✓        | ✓        | ✗ 隐藏   |
| 3    | ✓        | ✓        | ✓        |
| 4    | ✓        | ✓        | ✓        |
| 5    | ✓        | ✓        | ✓        |

### 影子配置显示规则

| 索引 | 对应阶段 | 显示标签 |
|------|----------|----------|
| 0    | 阶段 1   | Stage 1  |
| 1    | 阶段 2   | Stage 2  |
| 2    | 阶段 3   | Stage 3  |
| 3    | 阶段 4   | Stage 4  |
| 4    | 阶段 5   | Stage 5  |

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

本需求主要涉及 UI 显示逻辑，不涉及可自动化测试的正确性属性。所有验收标准需要通过手动 UI 测试验证。

## Error Handling

1. **数组大小不匹配**：如果序列化数据中的数组大小与预期不符，Editor 应自动调整到正确大小
2. **空引用**：绘制 Sprite 字段时应处理 null 值，显示 "无 (Sprite)" 占位符

## Testing Strategy

### 手动测试

由于本需求主要涉及 Unity Editor UI，需要通过手动测试验证：

1. 创建带有 TreeControllerV2 组件的 GameObject
2. 验证 Inspector 显示固定的阶段配置（Stage 0-5）
3. 验证阶段 0-2 不显示树桩设置
4. 验证阶段 3-5 显示树桩设置
5. 验证影子配置显示固定的 Stage 1-5
6. 验证经验配置显示固定的 Stage 0-5
7. 验证阶段 0 的工具类型为只读 Hoe
