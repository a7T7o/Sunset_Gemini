using UnityEngine;
using UnityEditor;

/// <summary>
/// 批量修复OcclusionTransparency的标签配置
/// </summary>
public class FixOcclusionTags : Editor
{
    [MenuItem("Tools/🔧 修复遮挡组件标签")]
    static void FixTags()
    {
        // 查找所有OcclusionTransparency组件
        OcclusionTransparency[] allOcclusions = Object.FindObjectsByType<OcclusionTransparency>(FindObjectsSortMode.None);
        
        if (allOcclusions.Length == 0)
        {
            EditorUtility.DisplayDialog("未找到组件", "场景中没有OcclusionTransparency组件", "确定");
            return;
        }
        
        int fixedCount = 0;
        
        // 正确的标签配置（匹配OcclusionManager）
        string[] correctTags = new string[] { "Tree", "Building", "Rock" };
        
        foreach (var occlusion in allOcclusions)
        {
            SerializedObject so = new SerializedObject(occlusion);
            SerializedProperty tagsProp = so.FindProperty("occlusionTags");
            
            if (tagsProp != null && tagsProp.isArray)
            {
                // 清空现有标签
                tagsProp.ClearArray();
                
                // 设置新标签
                tagsProp.arraySize = correctTags.Length;
                for (int i = 0; i < correctTags.Length; i++)
                {
                    tagsProp.GetArrayElementAtIndex(i).stringValue = correctTags[i];
                }
                
                so.ApplyModifiedProperties();
                fixedCount++;
                
                Debug.Log($"<color=green>[FixTags] {occlusion.gameObject.name} 标签已修复: [{string.Join(", ", correctTags)}]</color>");
            }
        }
        
        EditorUtility.DisplayDialog("修复完成", 
            $"已修复 {fixedCount} 个组件的标签配置\n" +
            $"新标签: {string.Join(", ", correctTags)}", "确定");
        
        Debug.Log($"<color=cyan>[FixTags] 批量修复完成，共修复 {fixedCount} 个组件</color>");
    }
}
