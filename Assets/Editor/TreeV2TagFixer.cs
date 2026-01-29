using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 修复 Tree_V2 预制体的标签问题
/// V2 树的父物体需要设置 Tree 标签，以便导航系统识别
/// </summary>
public class TreeV2TagFixer : Editor
{
    [MenuItem("Tools/🌳 修复 V2 树标签")]
    public static void FixTreeV2Tags()
    {
        string folderPath = "Assets/222_Prefabs/Tree_V2";
        
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"[TreeV2TagFixer] 文件夹不存在: {folderPath}");
            return;
        }
        
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        int fixedCount = 0;
        
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab == null) continue;
            
            // 检查是否有 TreeController 组件
            var treeController = prefab.GetComponentInChildren<TreeController>();
            if (treeController == null) continue;
            
            // 检查父物体的标签
            if (prefab.tag != "Tree")
            {
                // 使用 PrefabUtility 修改预制体
                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                
                if (prefabRoot != null)
                {
                    prefabRoot.tag = "Tree";
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                    
                    Debug.Log($"<color=green>[TreeV2TagFixer] 修复标签: {path}</color>");
                    fixedCount++;
                }
            }
        }
        
        if (fixedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=cyan>[TreeV2TagFixer] 完成！修复了 {fixedCount} 个预制体的标签</color>");
        }
        else
        {
            Debug.Log("<color=yellow>[TreeV2TagFixer] 所有 V2 树预制体的标签已经正确</color>");
        }
    }
    
    [MenuItem("Tools/🌳 检查 V2 树标签")]
    public static void CheckTreeV2Tags()
    {
        string folderPath = "Assets/222_Prefabs/Tree_V2";
        
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"[TreeV2TagFixer] 文件夹不存在: {folderPath}");
            return;
        }
        
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        int correctCount = 0;
        int incorrectCount = 0;
        
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab == null) continue;
            
            // 检查是否有 TreeController 组件
            var treeController = prefab.GetComponentInChildren<TreeController>();
            if (treeController == null) continue;
            
            if (prefab.tag == "Tree")
            {
                Debug.Log($"<color=green>✓ {path} - 标签正确 (Tree)</color>");
                correctCount++;
            }
            else
            {
                Debug.Log($"<color=red>✗ {path} - 标签错误 ({prefab.tag})</color>");
                incorrectCount++;
            }
        }
        
        Debug.Log($"<color=cyan>[TreeV2TagFixer] 检查完成：{correctCount} 个正确，{incorrectCount} 个需要修复</color>");
    }
}
