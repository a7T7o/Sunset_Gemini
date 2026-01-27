using UnityEngine;
using UnityEditor;
using System.Reflection;
using FarmGame.Data;

/// <summary>
/// 数据库同步辅助类
/// 封装 ItemDatabase 的自动收集功能，供批量工具调用
/// 不修改 ItemDatabase.cs 源代码，通过反射调用 ContextMenu 方法
/// </summary>
public static class DatabaseSyncHelper
{
    #region 常量

    /// <summary>
    /// 默认数据库资产路径（可通过 SetDatabasePath 修改）
    /// </summary>
    public const string DefaultDatabasePath = "Assets/111_Data/Database/MasterItemDatabase.asset";
    
    /// <summary>
    /// EditorPrefs 键名
    /// </summary>
    private const string DatabasePathPrefKey = "BatchItemSO_DatabasePath";

    #endregion

    #region 属性

    /// <summary>
    /// 当前数据库路径（从 EditorPrefs 读取，支持持久化）
    /// </summary>
    public static string DatabasePath
    {
        get => EditorPrefs.GetString(DatabasePathPrefKey, DefaultDatabasePath);
        set => EditorPrefs.SetString(DatabasePathPrefKey, value);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置数据库路径
    /// </summary>
    /// <param name="path">数据库资产路径</param>
    public static void SetDatabasePath(string path)
    {
        DatabasePath = path;
    }

    /// <summary>
    /// 获取主数据库资产
    /// </summary>
    /// <returns>ItemDatabase 实例，不存在则返回 null</returns>
    public static ItemDatabase GetMasterDatabase()
    {
        var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);
        if (database == null)
        {
            Debug.LogWarning($"<color=yellow>[DatabaseSyncHelper] 未找到数据库资产: {DatabasePath}</color>");
        }
        return database;
    }

    /// <summary>
    /// 检查数据库是否存在
    /// </summary>
    public static bool DatabaseExists()
    {
        return AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath) != null;
    }

    /// <summary>
    /// 自动收集所有物品数据
    /// 通过反射调用 ItemDatabase 的 ContextMenu 方法
    /// </summary>
    /// <returns>收集到的物品数量，失败返回 -1</returns>
    public static int AutoCollectAllItems()
    {
        var database = GetMasterDatabase();
        if (database == null)
        {
            return -1;
        }

        int beforeCount = database.allItems?.Count ?? 0;

        // 通过反射调用私有的 ContextMenu 方法
        var method = typeof(ItemDatabase).GetMethod("AutoCollectAllItems", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (method != null)
        {
            method.Invoke(database, null);
            int afterCount = database.allItems?.Count ?? 0;
            
            Debug.Log($"<color=green>[DatabaseSyncHelper] 物品同步完成！共 {afterCount} 个物品（新增 {afterCount - beforeCount} 个）</color>");
            return afterCount;
        }
        else
        {
            Debug.LogError("[DatabaseSyncHelper] 无法找到 AutoCollectAllItems 方法！");
            return -1;
        }
    }

    /// <summary>
    /// 自动收集所有配方数据
    /// 通过反射调用 ItemDatabase 的 ContextMenu 方法
    /// </summary>
    /// <returns>收集到的配方数量，失败返回 -1</returns>
    public static int AutoCollectAllRecipes()
    {
        var database = GetMasterDatabase();
        if (database == null)
        {
            return -1;
        }

        int beforeCount = database.allRecipes?.Count ?? 0;

        // 通过反射调用私有的 ContextMenu 方法
        var method = typeof(ItemDatabase).GetMethod("AutoCollectAllRecipes", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (method != null)
        {
            method.Invoke(database, null);
            int afterCount = database.allRecipes?.Count ?? 0;
            
            Debug.Log($"<color=green>[DatabaseSyncHelper] 配方同步完成！共 {afterCount} 个配方（新增 {afterCount - beforeCount} 个）</color>");
            return afterCount;
        }
        else
        {
            Debug.LogError("[DatabaseSyncHelper] 无法找到 AutoCollectAllRecipes 方法！");
            return -1;
        }
    }

    /// <summary>
    /// 同步数据库（物品和配方）
    /// </summary>
    /// <returns>同步结果</returns>
    public static SyncResult SyncDatabase()
    {
        var result = new SyncResult();
        
        if (!DatabaseExists())
        {
            result.success = false;
            result.message = $"数据库不存在: {DatabasePath}";
            return result;
        }

        var database = GetMasterDatabase();
        int itemsBefore = database.allItems?.Count ?? 0;
        int recipesBefore = database.allRecipes?.Count ?? 0;

        result.itemCount = AutoCollectAllItems();
        result.recipeCount = AutoCollectAllRecipes();

        if (result.itemCount >= 0 && result.recipeCount >= 0)
        {
            result.success = true;
            result.newItemCount = result.itemCount - itemsBefore;
            result.newRecipeCount = result.recipeCount - recipesBefore;
            result.message = $"同步成功！物品: {result.itemCount}（+{result.newItemCount}），配方: {result.recipeCount}（+{result.newRecipeCount}）";
        }
        else
        {
            result.success = false;
            result.message = "同步失败，请检查控制台日志";
        }

        return result;
    }

    /// <summary>
    /// 显示数据库不存在的警告对话框
    /// </summary>
    /// <returns>用户是否选择创建数据库</returns>
    public static bool ShowDatabaseNotFoundWarning()
    {
        return EditorUtility.DisplayDialog(
            "数据库不存在",
            $"未找到主数据库资产:\n{DatabasePath}\n\n请先创建数据库资产。",
            "前往创建",
            "取消"
        );
    }

    #endregion
}

/// <summary>
/// 数据库同步结果
/// </summary>
public struct SyncResult
{
    /// <summary>物品总数</summary>
    public int itemCount;
    
    /// <summary>配方总数</summary>
    public int recipeCount;
    
    /// <summary>新增物品数</summary>
    public int newItemCount;
    
    /// <summary>新增配方数</summary>
    public int newRecipeCount;
    
    /// <summary>是否成功</summary>
    public bool success;
    
    /// <summary>结果消息</summary>
    public string message;
}
