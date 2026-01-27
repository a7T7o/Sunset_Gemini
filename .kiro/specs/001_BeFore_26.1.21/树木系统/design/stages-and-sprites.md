# 树木系统 - 阶段与 Sprite 配置规范

## 文档说明
本文档从 memory.md 会话 1 迁移而来，包含 StageConfigs / SpriteConfigs 规范。

---

## 阶段配置（StageConfigs）

### 固定 6 阶段结构

| 阶段 | 工具类型 | 有树桩 | 有碰撞体 | 有遮挡 | 说明 |
|------|---------|-------|---------|-------|------|
| Stage 0 | Hoe（只读） | ❌ | ❌ | ❌ | 树苗，只能用锄头挖出 |
| Stage 1 | Axe | ❌ | ✅ | ✅ | 小树 |
| Stage 2 | Axe | ❌ | ✅ | ✅ | 中小树 |
| Stage 3 | Axe | ✅ | ✅ | ✅ | 中树 |
| Stage 4 | Axe | ✅ | ✅ | ✅ | 大树 |
| Stage 5 | Axe | ✅ | ✅ | ✅ | 成熟树 |

### 默认配置工厂

```csharp
// StageConfigFactory.CreateDefaultConfigs()
new StageConfig
{
    daysToNextStage = 1,
    health = 0,
    hasStump = false,
    stumpHealth = 0,
    enableCollider = false,
    enableOcclusion = false,
    acceptedToolType = ToolType.Hoe  // Stage 0 固定
}
```

---

## Sprite 配置（SpriteConfigs）

### 每阶段 Sprite 结构

```csharp
public class StageSpriteData
{
    // 正常状态 - 5种季节样式
    public SeasonSpriteSet normal;  // spring, summer, earlyFall, lateFall, winter
    
    // 枯萎状态 - 2种样式（Stage 0 隐藏）
    public Sprite witheredSummer;   // 夏季枯萎
    public Sprite witheredFall;     // 秋季枯萎
    
    // 树桩状态 - 3种样式（仅 Stage 3-5）
    public Sprite stumpSpringSummer;  // 春夏树桩
    public Sprite stumpFall;          // 秋季树桩
    public Sprite stumpWinter;        // 冬季树桩
}
```

### Inspector 显示规则

| 阶段 | 正常样式 | 枯萎样式 | 树桩样式 | 冬季样式 |
|------|---------|---------|---------|---------|
| Stage 0 | ✅ 显示 | ❌ 隐藏 | ❌ 隐藏 | ❌ 隐藏（显示"无-冬季直接死亡"） |
| Stage 1-2 | ✅ 显示 | ✅ 显示 | ❌ 隐藏 | ✅ 显示 |
| Stage 3-5 | ✅ 显示 | ✅ 显示 | ✅ 显示 | ✅ 显示 |

---

## 阴影配置（ShadowConfigs）

- 固定 5 个配置（对应 Stage 1-5）
- Stage 0 没有阴影

---

## 经验配置（StageExperience）

- 固定 6 个经验值（对应 Stage 0-5）
- Stage 0 经验值通常为 0

---

## 关键决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 移除 winterMelted 字段 | 冬季阶段0直接死亡，不存在融化状态 | 2025-12-22 |
| 阶段0-2隐藏树桩设置 | 只有阶段3-5有树桩 | 2025-12-22 |
| 阶段0工具类型固定为 Hoe | 树苗只能用锄头挖出 | 2025-12-22 |
| 使用 CustomEditor 固定展示 | 数组大小固定，不需要增减功能 | 2025-12-22 |

## 实现日期
2025-12-22
