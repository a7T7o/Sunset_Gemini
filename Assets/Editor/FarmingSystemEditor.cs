using UnityEngine;
using UnityEditor;
using FarmGame.Farm;

/// <summary>
/// 农田系统编辑器工具
/// 用于快速测试和调试
/// 支持新版 FarmingManagerNew 和旧版 FarmingManager
/// </summary>
public class FarmingSystemEditor : EditorWindow
{
    private FarmingManager farmingManager;
    private FarmingManagerNew farmingManagerNew;
    private Vector2 scrollPos;
    private bool useNewManager = true;

    [MenuItem("Farm/农田系统调试工具")]
    public static void ShowWindow()
    {
        GetWindow<FarmingSystemEditor>("农田系统调试");
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("农田系统调试工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        useNewManager = EditorGUILayout.Toggle("使用新版管理器", useNewManager);
        EditorGUILayout.Space();

        if (useNewManager)
            DrawNewManagerUI();
        else
            DrawOldManagerUI();

        EditorGUILayout.EndScrollView();
    }

    private void DrawNewManagerUI()
    {
        if (farmingManagerNew == null)
            farmingManagerNew = FindFirstObjectByType<FarmingManagerNew>();

        if (farmingManagerNew == null)
        {
            EditorGUILayout.HelpBox("场景中未找到 FarmingManagerNew！", MessageType.Warning);
            if (GUILayout.Button("创建新版 FarmingSystem", GUILayout.Height(30)))
                CreateNewFarmingManager();
        }
        else
        {
            EditorGUILayout.HelpBox("已找到 FarmingManagerNew（新版）", MessageType.Info);
            EditorGUILayout.Space();
            GUILayout.Label("统计信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("状态", "新版管理器已就绪");
        }
    }


    private void CreateNewFarmingManager()
    {
        GameObject go = new GameObject("FarmingSystem");
        go.AddComponent<FarmingManagerNew>();
        go.AddComponent<FarmTileManager>();
        go.AddComponent<CropManager>();
        go.AddComponent<FarmVisualManager>();
        farmingManagerNew = go.GetComponent<FarmingManagerNew>();
        Debug.Log("<color=green>[农田系统] 已创建新版 FarmingSystem！</color>");
    }

    private void DrawOldManagerUI()
    {
        if (farmingManager == null)
            farmingManager = FindFirstObjectByType<FarmingManager>();

        if (farmingManager == null)
        {
            EditorGUILayout.HelpBox("场景中未找到 FarmingManager！建议使用新版管理器", MessageType.Warning);
            if (GUILayout.Button("创建旧版 FarmingManager（不推荐）", GUILayout.Height(30)))
                CreateOldFarmingManager();
        }
        else
        {
            EditorGUILayout.HelpBox("已找到 FarmingManager（旧版）\n建议迁移到新版", MessageType.Warning);
            EditorGUILayout.Space();
            
            var allTiles = farmingManager.GetAllFarmTiles();
            GUILayout.Label("统计信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("总耕地数量", allTiles.Count.ToString());
            
            EditorGUILayout.Space();
            GUILayout.Label("快捷操作", EditorStyles.boldLabel);
            
            if (GUILayout.Button("清除所有耕地", GUILayout.Height(30)))
                ClearAllFarmlands();
            if (GUILayout.Button("浇灌所有耕地", GUILayout.Height(30)))
                WaterAllFarmlands();
            if (GUILayout.Button("手动触发每日生长", GUILayout.Height(30)))
                ManualGrowthUpdate();
        }
    }

    private void CreateOldFarmingManager()
    {
        #pragma warning disable 0618
        GameObject go = new GameObject("FarmingSystem_Legacy");
        go.AddComponent<FarmingManager>();
        go.AddComponent<CropGrowthSystem>();
        #pragma warning restore 0618
        farmingManager = go.GetComponent<FarmingManager>();
    }

    private void ClearAllFarmlands()
    {
        var allTiles = farmingManager.GetAllFarmTiles();
        foreach (var kvp in allTiles.Values)
            kvp.ClearCrop();
        allTiles.Clear();
    }

    private void WaterAllFarmlands()
    {
        var allTiles = farmingManager.GetAllFarmTiles();
        foreach (var kvp in allTiles)
            farmingManager.WaterTileAtCell(kvp.Key);
    }

    private void ManualGrowthUpdate()
    {
        #pragma warning disable 0618
        CropGrowthSystem growthSystem = FindFirstObjectByType<CropGrowthSystem>();
        if (growthSystem != null)
            growthSystem.ManualGrowthUpdate();
        #pragma warning restore 0618
    }
}
