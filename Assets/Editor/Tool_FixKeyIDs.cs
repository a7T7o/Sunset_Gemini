using UnityEngine;
using UnityEditor;
using System.Linq;
using FarmGame.Data;

/// <summary>
/// 钥匙 ID 修复工具
/// 自动扫描并修复错误的钥匙 ID（100-105 → 1420-1425）
/// </summary>
public static class Tool_FixKeyIDs
{
    [MenuItem("Tools/Fix Key IDs")]
    public static void FixKeyIDs()
    {
        Debug.Log("[FixKeyIDs] 开始扫描钥匙...");
        
        // 查找所有 KeyLockData
        var guids = AssetDatabase.FindAssets("t:KeyLockData");
        Debug.Log($"[FixKeyIDs] 找到 {guids.Length} 个 KeyLockData 资产");
        
        var keys = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<KeyLockData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(k => k != null && k.itemID < 1420)
            .OrderBy(k => k.itemID)
            .ToList();

        if (keys.Count() == 0)
        {
            Debug.Log("<color=green>[FixKeyIDs] 没有需要修复的钥匙，所有钥匙 ID 都在正确范围内</color>");
            return;
        }

        Debug.Log($"<color=yellow>[FixKeyIDs] 找到 {keys.Count()} 个需要修复的钥匙</color>");

        int newID = 1420;
        foreach (var key in keys)
        {
            int oldID = key.itemID;
            key.itemID = newID;
            EditorUtility.SetDirty(key);
            Debug.Log($"[FixKeyIDs] {key.name}: {oldID} → {newID}");
            newID++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"<color=green>[FixKeyIDs] 完成！修复了 {keys.Count()} 个钥匙</color>");
        
        // 同步到 ItemDatabase
        SyncToDatabase();
    }

    private static void SyncToDatabase()
    {
        // 查找 MasterItemDatabase
        var database = AssetDatabase.FindAssets("t:ItemDatabase")
            .Select(guid => AssetDatabase.LoadAssetAtPath<ItemDatabase>(AssetDatabase.GUIDToAssetPath(guid)))
            .FirstOrDefault(db => db != null && db.name.Contains("Master"));

        if (database != null)
        {
            // 触发数据库更新
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=cyan>[FixKeyIDs] 已同步到 ItemDatabase</color>");
        }
        else
        {
            Debug.LogWarning("[FixKeyIDs] 未找到 MasterItemDatabase，请手动同步");
        }
    }
}
