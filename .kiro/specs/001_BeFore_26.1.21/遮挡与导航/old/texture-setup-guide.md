# 纹理设置指南 - 像素采样遮挡检测

## 概述

像素采样遮挡检测功能需要将 Sprite 的纹理设置为可读（Read/Write Enabled）。这样系统才能在运行时读取像素数据，实现精确的遮挡检测。

## 需要设置的资源

以下类型的 Sprite 资源需要启用 Read/Write：

1. **树木 Sprite** - 所有树木的各个生长阶段和季节变化 Sprite
2. **建筑 Sprite** - 房屋、仓库等建筑物
3. **其他需要精确遮挡检测的 Sprite** - 如大型装饰物

## 设置步骤

### 单个纹理设置

1. 在 Project 窗口中选中 Sprite 资源（或其所在的纹理文件）
2. 在 Inspector 窗口中找到 **Import Settings**
3. 展开 **Advanced** 部分
4. 勾选 **Read/Write Enabled**
5. 点击 **Apply** 按钮

### 批量设置（推荐）

1. 在 Project 窗口中选中多个纹理文件（按住 Ctrl/Cmd 多选）
2. 在 Inspector 窗口中会显示多选编辑模式
3. 勾选 **Read/Write Enabled**
4. 点击 **Apply** 按钮

## 性能影响

启用 Read/Write Enabled 会有以下影响：

- **内存占用增加**：纹理数据会在 CPU 内存中保留一份副本
- **加载时间略增**：首次加载时需要额外处理

### 优化建议

1. **只对需要的纹理启用** - 不要对所有纹理都启用
2. **使用合理的纹理尺寸** - 避免使用过大的纹理
3. **系统会自动回退** - 如果纹理不可读，系统会自动回退到 Bounds 检测

## 回退机制

如果纹理未设置为可读，系统会：

1. 在控制台输出警告信息
2. 自动回退到 Bounds 检测（矩形包围盒）
3. 功能仍然可用，只是精确度降低

## 验证设置

运行游戏后，可以通过以下方式验证设置是否正确：

1. 打开 Console 窗口
2. 如果看到 `纹理不可读，已回退到 Bounds 检测` 的警告，说明该纹理需要设置
3. 如果没有警告，说明像素采样正常工作

## 相关组件

- `OcclusionTransparency` - 遮挡透明组件，包含像素采样设置
- `OcclusionManager` - 遮挡管理器，协调所有遮挡检测

## 参数说明

在 `OcclusionTransparency` 组件中：

| 参数 | 默认值 | 说明 |
|------|--------|------|
| Use Pixel Sampling | true | 是否启用像素采样 |
| Alpha Threshold | 0.1 | 像素 alpha 阈值（低于此值视为透明） |
| Sample Point Count | 5 | 采样点数量（1/5/9） |
