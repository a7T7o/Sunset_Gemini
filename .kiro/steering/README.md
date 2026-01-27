# Steering 规则索引

> **本文档是所有 Steering 规则文件的索引，用于快速查找和了解各规则的用途。**

---

## 规则加载机制

| 加载类型 | 说明 | 配置方式 |
|---------|------|---------|
| `always` | 每次对话自动加载 | 默认行为，或 `inclusion: always` |
| `manual` | 需要手动引用 `#规则名` | `inclusion: manual` |
| `fileMatch` | 读取匹配文件时自动加载 | `inclusion: fileMatch` + `fileMatchPattern` |

---

## P0 - 核心架构规则（默认加载）

**每次对话自动加载，包含最容易犯的严重错误和基础工作流程。**

| 文件 | 用途 | 关键内容 |
|------|------|---------|
| `rules.md` | 开发规则与禁止事项 | 玩家位置、碰撞体用途、遮挡系统、全局编辑器规范 |
| `layers.md` | 项目层级设计规范 | 楼层结构、Sorting Layers、树木层级、命中检测 |
| `workspace-memory.md` | 工作区记忆规范 | specs 目录结构、memory.md 规范 |
| `communication.md` | 沟通规范 | 一条龙模式、语言要求、方案讨论流程 |

**Token 消耗**: ~4,000 tokens

---

## P1 - 系统规范（手动加载）

**开发相关功能时通过 `#规则名` 加载。**

| 文件 | 用途 | 触发关键词 | 使用场景 |
|------|------|-----------|---------|
| `animation.md` | 动画系统规范 | 动画、工具、同步、帧 | 开发动画相关功能 |
| `ui.md` | UI 系统规范 | UI、面板、快捷键、Toggle | 开发 UI 功能 |
| `so-design.md` | ScriptableObject 设计规范 | SO、物品ID、品质 | 创建/修改物品数据 |
| `items.md` | 物品系统规范 | 物品、背包、堆叠 | 开发物品相关功能 |
| `systems.md` | 核心系统设计规范 | 时间、季节、导航、遮挡 | 开发核心系统功能 |
| `trees.md` | 树木系统规范 | 树木、树林、砍树 | 开发树木相关功能 |
| `placeable-items.md` | 可放置物品开发规范 | 放置、树苗、箱子、底部对齐 | 开发可放置物品 |

**Token 消耗**: ~19,000 tokens（全部加载时）

---

## P2 - 开发规范（手动加载）

**编写代码或文档时参考。**

| 文件 | 用途 | 触发关键词 | 使用场景 |
|------|------|-----------|---------|
| `coding-standards.md` | 编码规范 | 编码、命名、Region | 编写新代码 |
| `documentation.md` | 技术文档编写规范 | 文档、日志、记录 | 编写技术文档 |
| `scene-modification-rule.md` | 场景/组件修改规则 | 场景、组件、修改 | 修改场景配置 |
| `scene-hierarchy-sync.md` | 场景层级同步规则 | 层级、同步 | 修改场景层级 |

**Token 消耗**: ~10,500 tokens（全部加载时）

---

## P3 - 项目信息（归档）

**已移至 `archive/` 目录，需要时手动加载。**

> ⚠️ **注意**：以下文档原文为英文，但每个文件开头都有"中文概述"章节。
> 若你需要了解这些信息，可以直接阅读中文概述部分。
> 若需要完整英文原文，请明确说明，我会转述/总结为中文。

| 文件 | 用途 | 使用场景 |
|------|------|---------|
| `archive/product.md` | 产品概述 | 新成员了解项目 |
| `archive/tech.md` | 技术栈 | 查看技术配置 |
| `archive/structure.md` | 项目结构 | 了解文件夹组织 |
| `archive/progress.md` | 项目进度与状态 | 查看整体进度 |

**Token 消耗**: ~11,000 tokens（全部加载时）

---

## P4 - 特定系统（归档）

**已移至 `archive/` 目录，只在开发特定系统时加载。**

| 文件 | 用途 | 使用场景 |
|------|------|---------|
| `archive/farming.md` | 农田系统规范 | 开发农田系统 |

**Token 消耗**: ~2,500 tokens

---

## 维护文档

| 文件 | 用途 |
|------|------|
| `maintenance-guidelines.md` | 规则维护安全规范（删除/合并检查清单、审查流程） |
| `README.md` | 本索引文件 |

---

## 快速引用指南

### 如何手动加载规则

在对话中使用 `#规则名`（不含 .md 后缀）：

```
#animation    → 加载动画系统规范
#ui           → 加载 UI 系统规范
#trees        → 加载树木系统规范
#coding-standards → 加载编码规范
```

### 常见开发场景对应规则

| 开发场景 | 需要加载的规则 |
|---------|---------------|
| 开发动画功能 | `#animation` |
| 开发 UI 功能 | `#ui` |
| 创建物品 SO | `#so-design` `#items` |
| 开发树木功能 | `#trees` |
| 开发导航功能 | `#systems` |
| 开发农田功能 | `#archive/farming` |
| 开发可放置物品 | `#placeable-items` |

---

## 目录结构

```
.kiro/steering/
├── README.md                    # 本索引文件
├── maintenance-guidelines.md    # 维护安全规范
│
├── [P0 - 默认加载]
│   ├── rules.md
│   ├── layers.md
│   ├── workspace-memory.md
│   └── communication.md
│
├── [P1 - 手动加载]
│   ├── animation.md
│   ├── ui.md
│   ├── so-design.md
│   ├── items.md
│   ├── systems.md
│   ├── trees.md
│   └── placeable-items.md
│
├── [P2 - 手动加载]
│   ├── coding-standards.md
│   ├── documentation.md
│   ├── scene-modification-rule.md
│   └── scene-hierarchy-sync.md
│
└── archive/                     # P3/P4 归档目录
    ├── product.md
    ├── tech.md
    ├── structure.md
    ├── progress.md
    └── farming.md
```

---

## 更新日志

| 日期 | 更新内容 |
|------|---------|
| 2025-01-09 | 创建索引文件，实施方案 A |
| 2025-01-09 | 删除 `context-handoff.md`、`tree-system.md` |
| 2025-01-09 | 合并游戏数值到 `trees.md`，迁移全局规则到 `rules.md` |
| 2026-01-21 | 新增 `placeable-items.md` 可放置物品开发规范 |

