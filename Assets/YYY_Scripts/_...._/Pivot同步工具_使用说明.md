# 📐 Sprite Pivot 同步工具 - 使用说明

## 🎯 工具用途

将原动画（Aseprite）的Pivot点复制到Mask动画的Sprite上，解决对齐问题。

---

## 🔧 使用方法（4步）

### Step 1: 打开工具
Unity顶部菜单：`Tools → 同步Sprite Pivot ⚡`

### Step 2: 拖入Sprite Sheet
1. **原动画 (Aseprite)**：
   - 在Project窗口找到原slice动画的图片
   - 拖入"原动画"字段
   - 工具会自动加载所有Sprite

2. **Mask动画 (需要同步)**：
   - 找到你画的黑白Mask图片
   - 拖入"Mask动画"字段
   - 工具会自动加载所有Sprite

### Step 3: 检查匹配
工具会显示：
- ✅ 两边的Sprite数量
- ✅ 每一帧的Pivot对比
- ⚠️ 如果数量不匹配，会警告

### Step 4: 同步
1. 点击 `⚡ 同步Pivot` 按钮
2. 确认对话框
3. 等待完成提示
4. 查看Console的详细报告

---

## 📋 重要说明

### Sprite Sheet要求
1. **必须已经切割**：
   - 在Texture的Import Settings中
   - Sprite Mode = Multiple
   - 已经用Sprite Editor切割好

2. **帧数必须相同**：
   - 原动画有8帧，Mask也必须有8帧
   - Sprite的顺序要一致（按名称排序）

3. **命名建议**：
   ```
   原动画: slice_down_0, slice_down_1, slice_down_2...
   Mask: mask_slice_down_0, mask_slice_down_1, mask_slice_down_2...
   ```

### Pivot的含义
- **Pivot**: Sprite的"锚点"，决定Sprite的中心点
- **归一化Pivot**: 范围0-1，表示Pivot在Sprite中的相对位置
  - (0.5, 0.5) = 中心
  - (0.5, 0) = 底部中心
  - (0.5, 1) = 顶部中心

### 工具的工作原理
1. 读取原动画Sprite的Pivot（像素坐标）
2. 转换为归一化坐标（0-1）
3. 写入到Mask Sprite的TextureImporter设置
4. 重新导入Texture

---

## 🎨 完整工作流程

### 1. 准备原动画
```
你的Aseprite动画：
• 文件：slice_down.png
• 已导入Unity
• Sprite Mode = Multiple
• 已切割成8帧
• Pivot正确（Aseprite自动设置的）
```

### 2. 准备Mask动画
```
你画的黑白Mask：
• 文件：mask_slice_down.png
• 已导入Unity
• Sprite Mode = Multiple
• 已切割成8帧（和原动画对应）
• Pivot可能不对（默认是中心点）
```

### 3. 使用工具同步
```
1. 打开 Tools → 同步Sprite Pivot
2. 拖入 slice_down.png → "原动画"
3. 拖入 mask_slice_down.png → "Mask动画"
4. 检查预览（应该显示8帧，Pivot不同）
5. 点击"同步Pivot"
6. 等待完成
```

### 4. 验证结果
```
1. 在Project中选中 mask_slice_down.png
2. 点击 Sprite Editor 按钮
3. 查看每个Sprite的Pivot（蓝色圆点）
4. 应该和原动画的Pivot位置一致
```

---

## 🔍 常见问题

### Q1: 提示"Sprite数量不匹配"
**原因**：原动画和Mask的帧数不同

**解决**：
1. 检查两个Sprite Sheet的切割
2. 确保帧数完全相同
3. 如果原动画是8帧，Mask也必须切成8帧

---

### Q2: 同步后Pivot还是不对
**原因**：Sprite Sheet没有重新加载

**解决**：
1. 选中Mask Sprite Sheet
2. 右键 → Reimport
3. 或者重启Unity

---

### Q3: 工具提示"找不到对应的SpriteMetaData"
**原因**：Sprite的名称不匹配

**解决**：
1. 在Sprite Editor中检查Sprite名称
2. 确保两边的Sprite按名称排序后是对应的
3. 可以手动重命名Sprite

---

### Q4: 原动画的Sprite Sheet加载不出来
**原因**：Texture设置不正确

**解决**：
1. 选中原动画图片
2. Inspector → Texture Type = Sprite (2D and UI)
3. Sprite Mode = Multiple
4. Apply

---

## 📊 Pivot预览说明

工具窗口中的预览显示：

```
帧 0   slice_down_0                    →  mask_slice_down_0
       Pivot: (8.0, 16.0)                  ❌ Pivot: (12.0, 12.0)
       归一化: (0.500, 0.667)               归一化: (0.500, 0.500)
```

解释：
- **左边**：原动画的Pivot信息
- **右边**：Mask的Pivot信息
- **✅/❌**：Pivot是否已经一致
- **归一化**：用于实际复制的值（0-1范围）

---

## ⚠️ 注意事项

### 1. 备份
同步前建议备份Mask Sprite Sheet（复制一份）

### 2. 不可撤销
修改Pivot后无法通过Ctrl+Z撤销，只能：
- 重新导入备份
- 或手动在Sprite Editor中调整

### 3. 批量操作
如果有多个方向（Down, Up, Side）：
- 需要分别同步
- 每个方向拖入对应的Sprite Sheet

### 4. Pivot和Alignment
工具会自动设置：
- Alignment = Custom
- Pivot = 从原动画复制的值

---

## 🎬 下一步

同步Pivot后：
1. ✅ Mask Sprite的Pivot已经和原动画一致
2. ✅ 在HandMaskController中使用这些Mask Sprite
3. ✅ 测试动画，Axe应该能正确对齐

---

## 🐛 如果还是不对齐

如果同步Pivot后Axe还是对不齐：

1. **检查Axe的父子关系**：
   - Axe必须跟随HandMask（作为子物体或用脚本）

2. **检查HandMask的Transform**：
   - Position应该在Player附近
   - Scale应该是(1, 1, 1)

3. **检查Axe的Transform**：
   - Local Position需要微调
   - Local Rotation可能需要调整

4. **检查Sprite大小**：
   - 原动画和Mask的Pixels Per Unit应该相同
   - 在Texture Import Settings中查看

---

## 📞 还有问题？

提供以下信息：
1. 原动画Sprite Sheet的截图（在Sprite Editor中）
2. Mask Sprite Sheet的截图（在Sprite Editor中）
3. 工具的预览截图
4. Console的同步报告

---

Good luck! 🍀


