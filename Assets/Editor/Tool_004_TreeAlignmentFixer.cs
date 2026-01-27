using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 树木对齐修正工具
/// 功能：批量修正已有预制体/场景中树木的Tree和Shadow位置
/// </summary>
public class Tool_004_TreeAlignmentFixer : EditorWindow
{
    #region ========== 窗口管理 ==========
    
    [MenuItem("Tools/005_树木对齐修正工具")]
    public static void ShowWindow()
    {
        var window = GetWindow<Tool_004_TreeAlignmentFixer>("树木对齐修正");
        window.minSize = new Vector2(400, 500);
    }
    
    #endregion
    
    #region ========== 界面变量 ==========
    
    private enum FixMode
    {
        SelectedPrefabs,    // 选中的预制体
        AllPrefabs,         // 文件夹中所有预制体
        SceneObjects        // 场景中的树木
    }
    
    private FixMode fixMode = FixMode.SelectedPrefabs;
    private string prefabFolderPath = "Assets/Z_02_Prefabs";
    private bool showPreview = true;
    private Vector2 scrollPosition;
    
    // 统计
    private int totalProcessed = 0;
    private int treeFixed = 0;
    private int shadowFixed = 0;
    private List<string> processLog = new List<string>();
    
    #endregion
    
    #region ========== GUI绘制 ==========
    
    private void OnGUI()
    {
        DrawHeader();
        DrawModeSelection();
        
        EditorGUILayout.Space(10);
        
        if (fixMode == FixMode.AllPrefabs)
        {
            DrawFolderSettings();
        }
        
        EditorGUILayout.Space(10);
        
        DrawOptions();
        DrawExecuteButton();
        
        EditorGUILayout.Space(10);
        
        DrawResults();
    }
    
    private void DrawHeader()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "🛠️ 树木对齐修正工具\n\n" +
            "批量修正已有树木预制体的位置：\n" +
            "• Tree子物体的sprite底部对齐父物体中心（树根）\n" +
            "• Shadow子物体的中心对齐父物体中心\n\n" +
            "⚠️ 操作会直接修改预制体文件，建议先备份！",
            MessageType.Info);
    }
    
    private void DrawModeSelection()
    {
        EditorGUILayout.LabelField("━━━━ 修正模式 ━━━━", EditorStyles.boldLabel);
        
        string[] modeNames = { "选中的预制体", "文件夹中所有预制体", "场景中的树木" };
        int modeIndex = (int)fixMode;
        modeIndex = GUILayout.SelectionGrid(modeIndex, modeNames, 1);
        fixMode = (FixMode)modeIndex;
        
        EditorGUILayout.Space(5);
        
        switch (fixMode)
        {
            case FixMode.SelectedPrefabs:
                EditorGUILayout.HelpBox("在Project窗口中选中要修正的预制体，然后点击\"执行修正\"", MessageType.None);
                
                // 显示当前选中的数量
                int selectedCount = Selection.objects.Where(obj => 
                    PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab).Count();
                EditorGUILayout.LabelField($"当前选中预制体数量: {selectedCount}");
                break;
                
            case FixMode.AllPrefabs:
                EditorGUILayout.HelpBox("修正指定文件夹中的所有预制体", MessageType.None);
                break;
                
            case FixMode.SceneObjects:
                EditorGUILayout.HelpBox("修正当前场景中所有树木（需要有TreeController组件）", MessageType.Warning);
                break;
        }
    }
    
    private void DrawFolderSettings()
    {
        EditorGUILayout.LabelField("━━━━ 文件夹设置 ━━━━", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        prefabFolderPath = EditorGUILayout.TextField("预制体文件夹", prefabFolderPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择预制体文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    prefabFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // 统计文件夹中的预制体数量
        if (!string.IsNullOrEmpty(prefabFolderPath) && System.IO.Directory.Exists(prefabFolderPath))
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolderPath });
            EditorGUILayout.LabelField($"文件夹中预制体数量: {guids.Length}");
        }
    }
    
    private void DrawOptions()
    {
        EditorGUILayout.LabelField("━━━━ 选项 ━━━━", EditorStyles.boldLabel);
        showPreview = EditorGUILayout.Toggle("显示修正详情", showPreview);
    }
    
    private void DrawExecuteButton()
    {
        EditorGUILayout.Space(10);
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🔧 执行修正", GUILayout.Height(40)))
        {
            ExecuteFix();
        }
        GUI.backgroundColor = Color.white;
    }
    
    private void DrawResults()
    {
        if (totalProcessed == 0) return;
        
        EditorGUILayout.LabelField("━━━━ 修正结果 ━━━━", EditorStyles.boldLabel);
        
        EditorGUILayout.LabelField($"处理对象: {totalProcessed}");
        EditorGUILayout.LabelField($"Tree修正: {treeFixed}");
        EditorGUILayout.LabelField($"Shadow修正: {shadowFixed}");
        
        if (showPreview && processLog.Count > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("详细日志:", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            foreach (string log in processLog)
            {
                EditorGUILayout.LabelField(log, EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndScrollView();
        }
        
        EditorGUILayout.Space(5);
        if (GUILayout.Button("清除结果"))
        {
            ClearResults();
        }
    }
    
    #endregion
    
    #region ========== 修正逻辑 ==========
    
    private void ExecuteFix()
    {
        ClearResults();
        
        List<GameObject> targetObjects = new List<GameObject>();
        
        // 根据模式收集目标对象
        switch (fixMode)
        {
            case FixMode.SelectedPrefabs:
                targetObjects = GetSelectedPrefabs();
                break;
                
            case FixMode.AllPrefabs:
                targetObjects = GetAllPrefabsInFolder(prefabFolderPath);
                break;
                
            case FixMode.SceneObjects:
                targetObjects = GetSceneTreeObjects();
                break;
        }
        
        if (targetObjects.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有找到需要修正的对象！", "确定");
            return;
        }
        
        // 确认
        if (!EditorUtility.DisplayDialog(
            "确认修正", 
            $"将要修正 {targetObjects.Count} 个对象\n\n此操作会直接修改文件，确定继续？", 
            "确定", 
            "取消"))
        {
            return;
        }
        
        // 开始修正
        EditorUtility.DisplayProgressBar("修正中", "正在处理...", 0);
        
        for (int i = 0; i < targetObjects.Count; i++)
        {
            float progress = (float)i / targetObjects.Count;
            EditorUtility.DisplayProgressBar("修正中", $"处理 {i + 1}/{targetObjects.Count}", progress);
            
            FixTreeObject(targetObjects[i]);
        }
        
        EditorUtility.ClearProgressBar();
        
        // 保存
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("完成", $"修正完成！\n\n处理对象: {totalProcessed}\nTree修正: {treeFixed}\nShadow修正: {shadowFixed}\n\n⚠️ Collider会在运行时自动调整offset", "确定");
    }
    
    private List<GameObject> GetSelectedPrefabs()
    {
        List<GameObject> result = new List<GameObject>();
        
        foreach (Object obj in Selection.objects)
        {
            if (PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab)
            {
                GameObject prefab = obj as GameObject;
                if (prefab != null)
                {
                    result.Add(prefab);
                }
            }
        }
        
        return result;
    }
    
    private List<GameObject> GetAllPrefabsInFolder(string folderPath)
    {
        List<GameObject> result = new List<GameObject>();
        
        if (string.IsNullOrEmpty(folderPath) || !System.IO.Directory.Exists(folderPath))
        {
            return result;
        }
        
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                result.Add(prefab);
            }
        }
        
        return result;
    }
    
    private List<GameObject> GetSceneTreeObjects()
    {
        List<GameObject> result = new List<GameObject>();
        
        TreeController[] trees = FindObjectsByType<TreeController>(FindObjectsSortMode.None);
        foreach (TreeController tree in trees)
        {
            if (tree.transform.parent != null)
            {
                result.Add(tree.transform.parent.gameObject);
            }
        }
        
        return result;
    }
    
    private void FixTreeObject(GameObject rootObject)
    {
        totalProcessed++;
        
        string objName = rootObject.name;
        bool isModified = false;
        
        // 查找Tree子物体
        Transform treeTransform = rootObject.transform.Find("Tree");
        if (treeTransform != null)
        {
            SpriteRenderer treeSr = treeTransform.GetComponent<SpriteRenderer>();
            if (treeSr != null && treeSr.sprite != null)
            {
                // 计算正确的localY
                Bounds spriteBounds = treeSr.sprite.bounds;
                float spriteBottomOffset = spriteBounds.min.y;
                float correctY = -spriteBottomOffset;
                
                Vector3 oldPos = treeTransform.localPosition;
                float delta = Mathf.Abs(oldPos.y - correctY);
                
                if (delta > 0.001f)
                {
                    treeTransform.localPosition = new Vector3(oldPos.x, correctY, oldPos.z);
                    treeFixed++;
                    isModified = true;
                    
                    if (showPreview)
                    {
                        processLog.Add($"[Tree] {objName}: {oldPos.y:F3} → {correctY:F3}");
                    }
                }
            }
        }
        
        // 查找Shadow子物体
        Transform shadowTransform = rootObject.transform.Find("Shadow");
        if (shadowTransform != null)
        {
            SpriteRenderer shadowSr = shadowTransform.GetComponent<SpriteRenderer>();
            if (shadowSr != null && shadowSr.sprite != null)
            {
                // 计算正确的localY
                Bounds shadowBounds = shadowSr.sprite.bounds;
                float centerOffset = shadowBounds.center.y;
                float correctY = -centerOffset;
                
                Vector3 oldPos = shadowTransform.localPosition;
                float delta = Mathf.Abs(oldPos.y - correctY);
                
                if (delta > 0.001f)
                {
                    shadowTransform.localPosition = new Vector3(oldPos.x, correctY, oldPos.z);
                    shadowFixed++;
                    isModified = true;
                    
                    if (showPreview)
                    {
                        processLog.Add($"[Shadow] {objName}: {oldPos.y:F3} → {correctY:F3}");
                    }
                }
            }
        }
        
        // ⚠️ Collider保持在Tree子物体上
        // TreeController会在运行时通过调整offset来固定碰撞体位置
        
        // 如果是预制体，标记为已修改
        if (isModified && fixMode != FixMode.SceneObjects)
        {
            EditorUtility.SetDirty(rootObject);
            
            // 对于预制体资源，需要保存
            string path = AssetDatabase.GetAssetPath(rootObject);
            if (!string.IsNullOrEmpty(path))
            {
                PrefabUtility.SavePrefabAsset(rootObject);
            }
        }
        
        // 场景对象直接标记dirty
        if (fixMode == FixMode.SceneObjects && isModified)
        {
            EditorUtility.SetDirty(rootObject);
        }
    }
    
    private void ClearResults()
    {
        totalProcessed = 0;
        treeFixed = 0;
        shadowFixed = 0;
        processLog.Clear();
    }
    
    #endregion
}

