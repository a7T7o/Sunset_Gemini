# 实现计划

- [x] 1. 修改 ItemData 基类，添加显示尺寸参数




  - [ ] 1.1 添加 useCustomDisplaySize 和 displayPixelSize 字段
    - 在 ItemData.cs 中添加新的 Header 区域「显示尺寸配置」
    - 添加 bool useCustomDisplaySize = false


    - 添加 int displayPixelSize = 32，范围 8-128
    - _需求: 1.1, 1.2, 7.1_


  - [ ] 1.2 实现 GetWorldDisplayScale() 方法
    - 计算基于 displayPixelSize 的缩放比例




    - 处理 icon 为空、尺寸为 0 等边界情况
    - _需求: 1.3, 1.4_
  - [x] 1.3 更新 OnValidate() 验证逻辑




    - 验证 displayPixelSize 范围
    - 输出警告信息
    - _需求: 7.2, 7.3_





- [ ] 2. 修复 UIItemIconScaler 旋转后边界计算
  - [x] 2.1 修改 SetIconWithAutoScale 方法


    - 添加 PADDING 常量（2 像素）




    - 修复旋转后边界计算，使用旋转后的实际宽高
    - RectTransform.sizeDelta 设置为旋转后边界尺寸


    - _需求: 4.1, 4.2, 4.3, 4.4, 5.4_




- [ ] 3. 修改 WorldPrefabGeneratorTool 支持显示尺寸
  - [ ] 3.1 在 GeneratePrefab 方法中应用显示尺寸
    - 调用 ItemData.GetWorldDisplayScale() 获取缩放比例
    - 应用缩放到 Sprite 子物体
    - 同步缩放阴影子物体
    - _需求: 2.1, 2.2, 2.3, 2.4_

- [ ] 4. 修改 WorldItemPickup 运行时应用显示尺寸
  - [ ] 4.1 添加 ApplyDisplaySize 方法
    - 检查 linkedItemData 的显示尺寸设置
    - 应用缩放到 spriteRenderer
    - 同步缩放阴影子物体
    - _需求: 3.1, 3.2, 3.3_
  - [ ] 4.2 在 Init 方法中调用 ApplyDisplaySize
    - 确保初始化时应用显示尺寸
    - _需求: 3.1_

- [ ] 5. 修改 Tool_BatchItemSOGenerator 支持显示尺寸
  - [ ] 5.1 添加显示尺寸配置 UI
    - 添加「启用自定义显示尺寸」勾选框
    - 添加像素尺寸滑块（8-128）
    - _需求: 6.1, 6.2_
  - [ ] 5.2 在创建 SO 时应用显示尺寸设置
    - 将配置应用到新创建的 ItemData
    - _需求: 6.3, 6.4_

- [ ] 6. 更新相关工作区的 memory.md
  - 更新背包图标旋转、物品掉落拾取、世界物品系统的 memory.md
  - 记录本次修改内容
