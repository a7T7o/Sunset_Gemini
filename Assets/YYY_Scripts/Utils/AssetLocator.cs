using UnityEngine;
using FarmGame.Data;

public static class AssetLocator
{
    // 通过 Resources 加载 ItemDatabase（默认路径可传入），Editor 下提供 AssetDatabase 回退
    public static ItemDatabase LoadItemDatabase(string resourcesPath = "Data/Database/MasterItemDatabase")
    {
        ItemDatabase db = null;
        if (!string.IsNullOrEmpty(resourcesPath))
        {
            db = Resources.Load<ItemDatabase>(resourcesPath);
            if (db != null) return db;
        }
#if UNITY_EDITOR
        // Editor 回退：尝试通过名称寻找
        var guids = UnityEditor.AssetDatabase.FindAssets("t:ItemDatabase MasterItemDatabase");
        if (guids != null && guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            db = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);
            if (db != null) return db;
        }
        // 兜底：找任意一个 ItemDatabase
        var any = UnityEditor.AssetDatabase.FindAssets("t:ItemDatabase");
        if (any != null && any.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(any[0]);
            db = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);
        }
#endif
        return db;
    }
}
