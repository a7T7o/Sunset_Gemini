# 箱子系统 - 存储数据与类型配置

## 文档说明
对应需求 1 & 2：StorageData 扩展和箱子类型参数

---

## StorageData 结构

### 已实现字段

| 字段 | 类型 | 说明 | 状态 |
|------|------|------|------|
| `storageRows` | int | 行数 | ✅ 已实现 |
| `storageCols` | int | 列数（1-15） | ✅ 已实现 |
| `storageCapacity` | int | 容量 = rows × cols | ✅ 已实现 |
| `maxHealth` | int | 箱子血量 | ✅ 已实现 |
| `chestMaterial` | ChestMaterial | 箱子材质（木/铁/特殊） | ✅ 已实现 |
| `baseUnlockChance` | float | 被开锁概率 | ✅ 已实现 |
| `storagePrefab` | GameObject | 箱子预制体 | ✅ 已实现 |
| `defaultLocked` | bool | 默认是否上锁 | ✅ 已实现 |

### 待验证

- [ ] 是否有自动校验 `capacity = rows × cols`
- [ ] 是否已创建 4 种箱子 SO（小木箱/大木箱/小铁箱/大铁箱）

---


## 箱子类型规格

| 类型 | 材质 | 行×列 | 容量 | 血量 | ID |
|------|------|-------|------|------|-----|
| 小木箱 | 木质 | 1×12 | 12格 | 2 | 1400 |
| 大木箱 | 木质 | 2×12 | 24格 | 3 | 1401 |
| 小铁箱 | 铁质 | 3×12 | 36格 | 4 | 1402 |
| 大铁箱 | 铁质 | 4×12 | 48格 | 5 | 1403 |

---

## 材质等级定义

| 等级 | 名称 | 说明 |
|------|------|------|
| 0 | 木质 | Wood |
| 1 | 石质 | Stone |
| 2 | 铁质 | Iron |
| 3 | 铜质 | Copper |
| 4 | 钢质 | Steel |
| 5 | 金质 | Gold |

---

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/YYY_Scripts/Data/Items/StorageData.cs` | 存储容器数据 SO |
| `Assets/YYY_Scripts/Data/Items/PlaceableItemData.cs` | 可放置物品基类 |
