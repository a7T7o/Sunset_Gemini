using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 静态物体Order自动校准器
/// 运行游戏前自动校准所有静态物体（树木、房屋等）的Sorting Order
/// 完全复制Tool_002_BatchHierarchy的计算逻辑，确保统一标准
/// </summary>
[InitializeOnLoad]
public class StaticObjectOrderAutoCalibrator
{
    // 🔥 统一标准参数（与Tool_002完全一致）
    private const int MULTIPLIER = 100;
    private const int ORDER_OFFSET = 0;
    private const float BOTTOM_OFFSET = 0f;
    private const int SHADOW_OFFSET = -1;
    private const int GLOW_OFFSET = 0;
    
    static StaticObjectOrderAutoCalibrator()
    {
        // 注册Play模式切换事件
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // 在进入Play模式前校准
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            CalibrateAllStaticObjects();
        }
    }
    
    /// <summary>
    /// 手动触发校准（菜单）
    /// </summary>
    [MenuItem("Tools/🔧 校准所有静态物体Order")]
    public static void ManualCalibrate()
    {
        CalibrateAllStaticObjects();
    }
    
    /// <summary>
    /// 校准所有静态物体的Order
    /// </summary>
    private static void CalibrateAllStaticObjects()
    {
        Debug.Log("<color=cyan>========== 开始自动校准静态物体Order ==========</color>");
        
        // 🔥 步骤1：清理所有有OcclusionTransparency的父物体的空SpriteRenderer
        CleanEmptySpriteRenderers();
        
        // 获取所有静态物体（不包含DynamicSortingOrder的物体）
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<SpriteRenderer> staticRenderers = new List<SpriteRenderer>();
        
        foreach (GameObject obj in allObjects)
        {
            // 跳过有DynamicSortingOrder的物体（它们会自己动态计算）
            if (obj.GetComponent<DynamicSortingOrder>() != null)
                continue;
            
            // 收集所有SpriteRenderer（包括子物体）
            SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                // 确保没有DynamicSortingOrder在同一物体或父物体上
                if (!HasDynamicSortingOrderInHierarchy(sr.gameObject))
                {
                    staticRenderers.Add(sr);
                }
            }
        }
        
        if (staticRenderers.Count == 0)
        {
            Debug.Log("<color=yellow>[AutoCalibrator] 没有找到需要校准的静态物体</color>");
            return;
        }
        
        // 记录Undo
        Undo.RecordObjects(staticRenderers.ToArray(), "Auto Calibrate Static Objects Order");
        
        int calibratedCount = 0;
        int skippedCount = 0;
        
        // 为每个SpriteRenderer计算Order（完全复制Tool_002逻辑）
        foreach (SpriteRenderer sr in staticRenderers)
        {
            // ✅ 跳过特殊标记的物体（Order < -9990）
            if (sr.sortingOrder < -9990)
            {
                skippedCount++;
                continue;
            }
            
            float sortingY = CalculateSortingY(sr);
            int calculatedOrder = -Mathf.RoundToInt(sortingY * MULTIPLIER) + ORDER_OFFSET;
            
            // 🔥 特殊处理：Shadow子物体（完全复制Tool_002逻辑）
            if (sr.gameObject.name.ToLower().Contains("shadow"))
            {
                Transform parent = sr.transform.parent;
                if (parent != null)
                {
                    SpriteRenderer parentSr = parent.GetComponent<SpriteRenderer>();
                    if (parentSr != null)
                    {
                        float parentSortY = CalculateSortingY(parentSr);
                        int parentOrder = -Mathf.RoundToInt(parentSortY * MULTIPLIER) + ORDER_OFFSET;
                        calculatedOrder = parentOrder + SHADOW_OFFSET;
                    }
                }
            }
            // 🔥 特殊处理：Glow/Light/Effect子物体（完全复制Tool_002逻辑）
            else if (sr.gameObject.name.ToLower().Contains("glow") || 
                     sr.gameObject.name.ToLower().Contains("light") || 
                     sr.gameObject.name.ToLower().Contains("effect"))
            {
                Transform parent = sr.transform.parent;
                if (parent != null)
                {
                    SpriteRenderer parentSr = parent.GetComponent<SpriteRenderer>();
                    if (parentSr != null)
                    {
                        float parentSortY = CalculateSortingY(parentSr);
                        int parentOrder = -Mathf.RoundToInt(parentSortY * MULTIPLIER) + ORDER_OFFSET;
                        calculatedOrder = parentOrder + GLOW_OFFSET;
                    }
                }
            }
            
            // 只在Order变化时才更新（避免不必要的Dirty标记）
            if (sr.sortingOrder != calculatedOrder)
            {
                sr.sortingOrder = calculatedOrder;
                EditorUtility.SetDirty(sr);
                calibratedCount++;
            }
        }
        
        // 标记场景为修改（确保保存）
        if (calibratedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        
        string summary = $"<color=green>[AutoCalibrator] 校准完成！</color>\n" +
                        $"  • 校准: {calibratedCount} 个物体\n" +
                        $"  • 跳过: {skippedCount} 个特殊标记物体\n" +
                        $"  • 总计: {staticRenderers.Count} 个静态SpriteRenderer";
        
        Debug.Log(summary);
        Debug.Log("<color=cyan>========================================</color>");
    }
    
    /// <summary>
    /// 计算排序用的Y坐标
    /// 🔥 完全复制Tool_002_BatchHierarchy的CalculateSortingY逻辑
    /// 🔥 关键修正：双层结构（父物体无SR）时用父物体的Y坐标
    /// </summary>
    private static float CalculateSortingY(SpriteRenderer sr)
    {
        float sortingY;
        
        // 🔥 关键：如果是子物体且父物体没有SpriteRenderer（双层结构）
        // 使用父物体的Transform.position.y（种植点/树根位置）
        Transform parent = sr.transform.parent;
        if (parent != null)
        {
            SpriteRenderer parentSr = parent.GetComponent<SpriteRenderer>();
            if (parentSr == null)
            {
                // 父物体没有SR → 双层结构（Tree_M1_XX / Tree）
                // 用父物体的Y坐标（种植点）
                sortingY = parent.position.y + BOTTOM_OFFSET;
                return sortingY;
            }
        }
        
        // 常规计算：优先Collider，回退Sprite，最后Transform
        Collider2D collider = sr.GetComponent<Collider2D>();
        
        if (collider != null)
        {
            // 使用Collider底部 = 物理边界的最低点
            sortingY = collider.bounds.min.y + BOTTOM_OFFSET;
        }
        else if (sr.sprite != null)
        {
            // 回退：使用Sprite底部
            sortingY = sr.bounds.min.y + BOTTOM_OFFSET;
        }
        else
        {
            // Fallback：使用Transform位置
            sortingY = sr.transform.position.y + BOTTOM_OFFSET;
        }
        
        return sortingY;
    }
    
    /// <summary>
    /// 清理所有OcclusionTransparency父物体的空SpriteRenderer
    /// ⭐关键：避免父物体的空SpriteRenderer导致Order计算错误
    /// </summary>
    private static void CleanEmptySpriteRenderers()
    {
        // 找到所有有OcclusionTransparency的物体
        OcclusionTransparency[] occlusionObjects = Object.FindObjectsByType<OcclusionTransparency>(FindObjectsSortMode.None);
        
        int cleanedCount = 0;
        
        foreach (var occlusion in occlusionObjects)
        {
            // 检查该物体是否有SpriteRenderer
            SpriteRenderer sr = occlusion.GetComponent<SpriteRenderer>();
            
            if (sr != null)
            {
                // 如果SpriteRenderer没有Sprite（空的），删除它
                if (sr.sprite == null)
                {
                    Undo.DestroyObjectImmediate(sr);
                    cleanedCount++;
                    Debug.Log($"<color=yellow>[AutoCalibrator] 清理 {occlusion.gameObject.name} 的空SpriteRenderer</color>");
                }
            }
        }
        
        if (cleanedCount > 0)
        {
            Debug.Log($"<color=green>[AutoCalibrator] 清理了 {cleanedCount} 个空SpriteRenderer</color>");
        }
    }
    
    /// <summary>
    /// 检查物体或其父级是否有DynamicSortingOrder组件
    /// </summary>
    private static bool HasDynamicSortingOrderInHierarchy(GameObject obj)
    {
        Transform current = obj.transform;
        while (current != null)
        {
            if (current.GetComponent<DynamicSortingOrder>() != null)
                return true;
            current = current.parent;
        }
        return false;
    }
}
