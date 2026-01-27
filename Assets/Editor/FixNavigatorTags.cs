using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// 修复 PlayerAutoNavigator 中的旧标签（Trees/Rocks/Buildings → Tree/Rock/Building）
/// </summary>
public class FixNavigatorTags : EditorWindow
{
    [MenuItem("Tools/🔧 修复导航器标签")]
    public static void FixTags()
    {
        // 查找场景中所有的 PlayerAutoNavigator
        var navigators = FindObjectsByType<PlayerAutoNavigator>(FindObjectsSortMode.None);
        
        int fixedCount = 0;
        
        foreach (var navigator in navigators)
        {
            SerializedObject so = new SerializedObject(navigator);
            SerializedProperty tagsProperty = so.FindProperty("losObstacleTags");
            
            if (tagsProperty != null && tagsProperty.isArray)
            {
                bool needsFix = false;
                
                // 检查是否有旧标签
                for (int i = 0; i < tagsProperty.arraySize; i++)
                {
                    string tag = tagsProperty.GetArrayElementAtIndex(i).stringValue;
                    if (tag == "Trees" || tag == "Rocks" || tag == "Buildings")
                    {
                        needsFix = true;
                        break;
                    }
                }
                
                if (needsFix)
                {
                    // 修复标签
                    for (int i = 0; i < tagsProperty.arraySize; i++)
                    {
                        string tag = tagsProperty.GetArrayElementAtIndex(i).stringValue;
                        
                        if (tag == "Trees")
                            tagsProperty.GetArrayElementAtIndex(i).stringValue = "Tree";
                        else if (tag == "Rocks")
                            tagsProperty.GetArrayElementAtIndex(i).stringValue = "Rock";
                        else if (tag == "Buildings")
                            tagsProperty.GetArrayElementAtIndex(i).stringValue = "Building";
                    }
                    
                    so.ApplyModifiedProperties();
                    fixedCount++;
                    
                    Debug.Log($"✅ 已修复 {navigator.gameObject.name} 的标签配置");
                }
            }
        }
        
        if (fixedCount > 0)
        {
            Debug.Log($"<color=green>✅ 修复完成！共修复 {fixedCount} 个 PlayerAutoNavigator 组件</color>");
            EditorUtility.DisplayDialog("修复完成", $"已修复 {fixedCount} 个 PlayerAutoNavigator 组件的标签配置", "确定");
        }
        else
        {
            Debug.Log("<color=yellow>⚠️ 未找到需要修复的组件</color>");
            EditorUtility.DisplayDialog("无需修复", "未找到需要修复的 PlayerAutoNavigator 组件", "确定");
        }
    }
}
