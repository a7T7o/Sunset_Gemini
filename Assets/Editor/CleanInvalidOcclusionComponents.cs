using UnityEngine;
using UnityEditor;

/// <summary>
/// 清理无效的OcclusionTransparency组件
/// 用于清理误添加到管理器物体上的OcclusionTransparency
/// </summary>
public class CleanInvalidOcclusionComponents : Editor
{
    [MenuItem("Tools/🧹 清理无效的遮挡组件")]
    static void CleanInvalidComponents()
    {
        // 查找所有OcclusionTransparency组件
        OcclusionTransparency[] allOcclusions = Object.FindObjectsByType<OcclusionTransparency>(FindObjectsSortMode.None);
        
        int removedCount = 0;
        
        foreach (var occlusion in allOcclusions)
        {
            // 检查是否有SpriteRenderer（包括子物体）
            SpriteRenderer[] renderers = occlusion.GetComponentsInChildren<SpriteRenderer>();
            
            if (renderers.Length == 0)
            {
                // 没有SpriteRenderer → 无效组件，删除
                Undo.DestroyObjectImmediate(occlusion);
                removedCount++;
                Debug.Log($"<color=yellow>[清理] 删除 {occlusion.gameObject.name} 上的无效OcclusionTransparency（没有SpriteRenderer）</color>");
            }
        }
        
        if (removedCount > 0)
        {
            EditorUtility.DisplayDialog("清理完成", $"已删除 {removedCount} 个无效的OcclusionTransparency组件", "确定");
            Debug.Log($"<color=green>[清理] 共删除 {removedCount} 个无效组件</color>");
        }
        else
        {
            EditorUtility.DisplayDialog("无需清理", "未发现无效的OcclusionTransparency组件", "确定");
            Debug.Log("<color=green>[清理] 未发现无效组件</color>");
        }
    }
}
