# 预览与放置位置对齐修复 - 任务列表

**创建日期**: 2026-01-21  
**状态**: 进行中（V2 修复）

---

## 任务列表

- [x] 1. 修改 PlacementGridCalculator - 添加从 PolygonCollider2D 计算格子大小和中心的方法
  - [x] 1.1 添加 GetPolygonColliderLocalBounds 私有方法
  - [x] 1.2 添加 GetGridSizeFromPolygonCollider 私有方法
  - [x] 1.3 添加 GetRequiredGridSizeFromPrefab 公共方法
  - [x] 1.4 添加 GetColliderLocalCenter 公共方法
  - [x] 1.5 修改现有 GetRequiredGridSize(GameObject) 调用新方法

- [x] 2. V2 修复 - 考虑底部对齐的影响
  - [x] 2.1 添加 GetColliderCenterAfterBottomAlign 方法
  - [x] 2.2 添加 GetPreviewSpriteLocalPosition 方法
  - [x] 2.3 修改 PlacementPreviewV3.Show() 使用新方法
  - [x] 2.4 修改 PlacementManagerV3.InstantiatePlacementPrefab() 使用新方法

- [x] 3. 验证和测试
  - [x] 3.1 Unity 编译通过
  - [ ] 3.2 箱子放置测试
  - [ ] 3.3 树苗放置测试

- [x] 4. 文档更新
  - [x] 4.1 更新 memory.md
  - [x] 4.2 创建验收指南.md

---

**文档维护者**: Kiro  
**最后更新**: 2026-01-21
