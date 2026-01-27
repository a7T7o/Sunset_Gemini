# Phase 1 - Editor & Config UI 重构

## 阶段概述
重构 TreeControllerV2 的 Inspector UI，实现固定展示的配置界面。

## 完成日期
2025-12-22

## 用户需求
> 树木还需要重新规划一下...你的UI交互做的很烂让我很不方便

## 完成任务

1. 从 StageSpriteData 类中移除 winterMelted 字段
2. 更新 TreeControllerV2 中 GetWinterSprite 方法
3. 创建 TreeControllerV2Editor 自定义编辑器
4. 实现固定展示的 StageConfigs UI（Stage 0-5）
5. 实现固定展示的 SpriteConfig UI
6. 实现固定展示的 ShadowConfigs UI（Stage 1-5）
7. 实现固定展示的 StageExperience UI（Stage 0-5）
8. 阶段0工具类型固定为 Hoe 且只读

## 修改文件

| 文件 | 修改内容 |
|------|---------|
| `Assets/YYY_Scripts/Data/TreeSpriteData.cs` | 移除 winterMelted 字段 |
| `Assets/YYY_Scripts/Controller/TreeControllerV2.cs` | 更新 GetWinterSprite 方法，添加掉落配置字段 |
| `Assets/Editor/TreeControllerV2Editor.cs` | 新建自定义编辑器 |

## 解决方案
创建 TreeControllerV2Editor 自定义编辑器，完全接管 Inspector 绘制：
- StageConfigs: 固定6个阶段，阶段0-2隐藏树桩设置，阶段0工具类型只读
- SpriteConfig: 固定6个阶段，阶段0隐藏枯萎和树桩
- ShadowConfigs: 固定5个配置（对应阶段1-5）
- StageExperience: 固定6个经验值
- DropConfig: 新增掉落配置
