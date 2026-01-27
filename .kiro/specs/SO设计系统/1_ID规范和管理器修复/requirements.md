# SO ID 规范完善和管理器修复 - 需求文档

## 简介

本需求旨在解决两个紧急问题：
1. 完善 SO ID 分配规范，明确钥匙/锁的 ID 范围
2. 修复 TimeManager、SeasonManager、WeatherSystem 的 DontDestroyOnLoad 警告

## 术语表

- **KeyLockData**：钥匙和锁的 SO 数据类
- **DontDestroyOnLoad**：Unity API，用于在场景切换时保持物体不被销毁
- **根物体**：场景层级中没有父物体的 GameObject

---

## 需求 1：完善 ID 分配规范

**用户故事**：作为开发者，我希望 SO 参数设计文档中明确定义所有物品类型的 ID 范围，以便正确分配物品 ID 并避免警告。

### 验收标准

1. WHEN 开发者查看 SO 参数设计文档 THEN 应看到钥匙/锁的 ID 范围定义（1420-1499）
2. WHEN 开发者查看 SO 参数设计文档 THEN 应看到树苗的 ID 范围定义（12XX）
3. WHEN 开发者查看 SO 参数设计文档 THEN 应看到建筑材料的 ID 范围定义（13XX）
4. WHEN 开发者查看 SO 设计概要文档 THEN 应看到相同的 ID 范围定义
5. WHEN 开发者查看 so-design.md steering 规则 THEN 应看到更新的 ID 范围

---

## 需求 2：修复钥匙 ID 警告

**用户故事**：作为开发者，我希望现有的钥匙 SO 使用正确的 ID 范围，以便消除控制台警告。

### 验收标准

1. WHEN 游戏运行时 THEN 控制台不应显示"钥匙ID建议在1420-1499范围内"的警告
2. WHEN 开发者检查现有钥匙 SO THEN 所有钥匙的 ID 应在 1420-1499 范围内
3. WHEN 开发者使用批量生成工具创建钥匙 THEN 默认 ID 应从 1420 开始
4. WHEN 开发者使用批量修改工具修改钥匙 ID THEN 工具应验证 ID 在正确范围内
5. WHEN ItemDatabase 加载钥匙 SO THEN 不应有 ID 冲突

---

## 需求 3：修复 DontDestroyOnLoad 警告

**用户故事**：作为开发者，我希望 TimeManager、SeasonManager、WeatherSystem 能够正确使用 DontDestroyOnLoad，以便在场景切换时保持状态。

### 验收标准

1. WHEN 游戏运行时 THEN 控制台不应显示"DontDestroyOnLoad only works for root GameObjects"的警告
2. WHEN TimeManager 初始化时 THEN 应正确调用 DontDestroyOnLoad
3. WHEN SeasonManager 初始化时 THEN 应正确调用 DontDestroyOnLoad
4. WHEN WeatherSystem 初始化时 THEN 应正确调用 DontDestroyOnLoad
5. WHEN 场景切换时 THEN 这三个管理器应保持存在且状态不丢失

---

## 需求 4：更新批量生成工具

**用户故事**：作为开发者，我希望批量生成工具能够使用正确的 ID 范围，以便快速创建符合规范的 SO。

### 验收标准

1. WHEN 使用批量生成工具创建钥匙 THEN 默认起始 ID 应为 1420
2. WHEN 使用批量生成工具创建树苗 THEN 默认起始 ID 应为 1200
3. WHEN 使用批量生成工具创建建筑材料 THEN 默认起始 ID 应为 1300
4. WHEN 批量生成工具显示 ID 范围提示 THEN 应显示正确的范围
5. WHEN 批量生成完成 THEN 应自动同步到 ItemDatabase

---

## 需求 5：更新文档

**用户故事**：作为开发者，我希望所有相关文档都反映最新的 ID 分配规范，以便正确理解和使用 SO 系统。

### 验收标准

1. WHEN 开发者查看 SO 参数设计文档 THEN 应看到完整的 ID 分配规范（包含 12XX、13XX、14XX）
2. WHEN 开发者查看 so-design.md THEN 应看到更新的 ID 分配规范
3. WHEN 开发者查看工作区 memory.md THEN 应看到本次修复的完整记录
4. WHEN 开发者查看 SO 设计概要文档 THEN 应看到 ID 范围的更新说明
5. WHEN 开发者查看文档 THEN 应看到 DontDestroyOnLoad 修复的说明

---

## 非功能需求

### 兼容性要求

1. 现有的钥匙 SO 需要手动或批量修改 ID
2. 修复后的管理器应与现有代码兼容
3. 场景中的管理器物体应保持原有配置

### 性能要求

1. DontDestroyOnLoad 修复不应影响游戏性能
2. ID 范围验证不应影响 SO 加载速度

---

## 约束条件

1. 不能破坏现有的 SO 资产
2. 不能导致 ID 冲突
3. 必须保持向后兼容性

---

## 依赖关系

| 依赖项 | 说明 |
|--------|------|
| KeyLockData.cs | 已有 ID 范围验证逻辑 |
| TimeManager.cs | 需要修复 DontDestroyOnLoad |
| SeasonManager.cs | 需要修复 DontDestroyOnLoad |
| WeatherSystem.cs | 需要修复 DontDestroyOnLoad |
| Tool_BatchItemSOGenerator.cs | 需要更新默认 ID |

---

## 验收测试场景

### 场景 1：创建新的钥匙 SO

1. 打开批量生成工具
2. 选择 KeyLockData 类型
3. 验证：默认起始 ID 为 1420
4. 生成钥匙 SO
5. 验证：控制台无警告

### 场景 2：修复现有钥匙 ID

1. 打开批量修改工具
2. 选中所有钥匙 SO（ID 100-105）
3. 批量修改 ID 为 1420-1425
4. 保存修改
5. 验证：控制台无警告

### 场景 3：测试管理器持久化

1. 启动游戏
2. 验证：控制台无 DontDestroyOnLoad 警告
3. 切换场景
4. 验证：TimeManager、SeasonManager、WeatherSystem 仍然存在
5. 验证：时间和季节状态保持不变

---

*需求文档结束*
