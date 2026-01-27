using UnityEngine;
using UnityEngine.UI;
using FarmGame.Data;

/// <summary>
/// 物品图标缩放适配工具
/// 用于统一处理背包/快捷栏/装备栏中的物品图标显示
/// 确保不同大小的Sprite都能等比例适配显示区域
/// 支持可配置的旋转显示（默认 45 度，与世界物品视觉风格一致）
/// ★ 支持从 ItemData 读取自定义旋转和尺寸配置
/// </summary>
public static class UIItemIconScaler
{
    #region 常量配置
    
    // 槽位配置
    private const float SLOT_SIZE = 64f;           // 槽位总大小（像素）
    private const float BORDER_SIZE = 4f;          // 边框大小（像素）
    private const float DISPLAY_AREA = 56f;        // 实际显示区域（56x56）
    private const float PADDING = 2f;              // 内边距（像素）
    private const float PIXELS_PER_UNIT = 16f;     // 所有sprite的PPU统一为16
    
    // 图标旋转配置
    private const float ICON_ROTATION_Z = 45f;     // 图标 Z 轴旋转角度（与世界物品一致）
    
    // 默认可用区域
    private const float DEFAULT_AVAILABLE_AREA = DISPLAY_AREA - PADDING * 2;  // 52 像素
    
    #endregion
    
    /// <summary>
    /// 为Image组件设置sprite并自动缩放适配（使用 ItemData 配置）
    /// ★ 支持从 ItemData 读取旋转和尺寸配置
    /// </summary>
    /// <param name="image">目标Image组件</param>
    /// <param name="sprite">要显示的sprite（可为null）</param>
    /// <param name="itemData">物品数据（可为null，为null时使用默认配置）</param>
    public static void SetIconWithAutoScale(Image image, Sprite sprite, ItemData itemData = null)
    {
        if (image == null) return;
        
        // 设置sprite
        image.sprite = sprite;
        
        if (sprite == null)
        {
            image.enabled = false;
            return;
        }
        
        image.enabled = true;
        
        // 确保颜色不透明
        if (image.color.a < 1f)
        {
            image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
        }
        
        // 重置Image的基本设置
        image.preserveAspect = true;  // 保持宽高比
        image.type = Image.Type.Simple;
        
        // 从 ItemData 读取配置，或使用默认值
        bool shouldRotate = itemData?.rotateBagIcon ?? true;
        int customSize = itemData?.GetBagDisplayPixelSize() ?? -1;
        float availableArea = customSize > 0 ? customSize : DEFAULT_AVAILABLE_AREA;
        float rotationAngle = shouldRotate ? ICON_ROTATION_Z : 0f;
        
        // 计算sprite的像素尺寸
        Rect rect = sprite.rect;
        float spriteWidthInPixels = rect.width;
        float spriteHeightInPixels = rect.height;
        
        // 计算最终尺寸（考虑旋转）
        float finalWidth, finalHeight, scale;
        
        if (shouldRotate)
        {
            // ★ 计算旋转后的边界框尺寸（像素）
            float rotRad = rotationAngle * Mathf.Deg2Rad;
            float cos = Mathf.Abs(Mathf.Cos(rotRad));
            float sin = Mathf.Abs(Mathf.Sin(rotRad));
            float rotatedWidthInPixels = spriteWidthInPixels * cos + spriteHeightInPixels * sin;
            float rotatedHeightInPixels = spriteWidthInPixels * sin + spriteHeightInPixels * cos;
            
            // ★ 使用旋转后边界框计算缩放比例
            float scaleX = availableArea / rotatedWidthInPixels;
            float scaleY = availableArea / rotatedHeightInPixels;
            scale = Mathf.Min(scaleX, scaleY);
            
            // ★ RectTransform 尺寸应该是旋转后的边界尺寸
            finalWidth = rotatedWidthInPixels * scale;
            finalHeight = rotatedHeightInPixels * scale;
        }
        else
        {
            // 不旋转：直接使用原始尺寸计算
            float scaleX = availableArea / spriteWidthInPixels;
            float scaleY = availableArea / spriteHeightInPixels;
            scale = Mathf.Min(scaleX, scaleY);
            
            finalWidth = spriteWidthInPixels * scale;
            finalHeight = spriteHeightInPixels * scale;
        }
        
        // 应用到RectTransform
        RectTransform rt = image.rectTransform;
        
        // 设置为居中锚点
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        
        // 应用旋转
        rt.localRotation = Quaternion.Euler(0f, 0f, rotationAngle);
        
        // 设置RectTransform的sizeDelta（像素单位）
        rt.sizeDelta = new Vector2(finalWidth, finalHeight);
        
        // 应用偏移量（从 ItemData 读取，默认为零）
        Vector2 offset = itemData?.bagDisplayOffset ?? Vector2.zero;
        rt.anchoredPosition = offset;
        
        // 🔥 P1：移除高频调用的日志输出（符合日志规范）
    }
    
    /// <summary>
    /// 批量处理：为多个槽位设置图标
    /// </summary>
    public static void SetIconsWithAutoScale(Image[] images, Sprite[] sprites, ItemData[] itemDatas = null)
    {
        if (images == null || sprites == null) return;
        
        int count = Mathf.Min(images.Length, sprites.Length);
        for (int i = 0; i < count; i++)
        {
            ItemData data = (itemDatas != null && i < itemDatas.Length) ? itemDatas[i] : null;
            SetIconWithAutoScale(images[i], sprites[i], data);
        }
    }
    
    /// <summary>
    /// 获取推荐的槽位尺寸配置（用于调试和文档）
    /// </summary>
    public static string GetSlotConfiguration()
    {
        return $"槽位配置:\n" +
               $"- 槽位总大小: {SLOT_SIZE}x{SLOT_SIZE} 像素\n" +
               $"- 边框大小: {BORDER_SIZE} 像素\n" +
               $"- 实际显示区域: {DISPLAY_AREA}x{DISPLAY_AREA} 像素\n" +
               $"- 内边距: {PADDING} 像素\n" +
               $"- 默认可用区域: {DEFAULT_AVAILABLE_AREA}x{DEFAULT_AVAILABLE_AREA} 像素\n" +
               $"- Sprite PPU: {PIXELS_PER_UNIT}\n" +
               $"- 默认图标旋转角度: {ICON_ROTATION_Z}°";
    }
}
