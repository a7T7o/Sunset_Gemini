using UnityEngine;
using UnityEditor;
using FarmGame.Data;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(StoneController))]
public class StoneControllerEditor : Editor
{
    private static readonly string[] StageNames = { "M1 (最大)", "M2 (中等)", "M3 (最小)", "M4 (装饰)" };
    private static readonly int[] DefaultHealth = { 36, 17, 9, 4 };
    private static readonly int[] DefaultStoneTotalCount = { 12, 6, 2, 2 };
    private static readonly bool[] DefaultIsFinalStage = { false, false, true, true };
    private static readonly StoneStage[] DefaultNextStage = { StoneStage.M2, StoneStage.M3, StoneStage.M3, StoneStage.M4 };
    private static readonly bool[] DefaultDecreaseOreIndex = { false, true, false, false };

    private bool showStageConfigs = true, showCurrentState = true, showSpriteConfig = true;
    private bool showStatusPreview = true, showDropConfig = true, showSoundSettings = true;
    private bool showDebugSettings = true, showSpriteDebugInfo = false;
    private bool[] stageConfigFoldouts = new bool[4];

    private Dictionary<string, Sprite> cachedSprites = new Dictionary<string, Sprite>();
    private string lastFolderPath = "";
    private bool spriteCacheValid = false;
    private int totalSpriteCount = 0;
    private Dictionary<string, List<string>> spritesByStage = new Dictionary<string, List<string>>();
    private Dictionary<string, List<string>> spritesByOreType = new Dictionary<string, List<string>>();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawStageConfigs();
        EditorGUILayout.Space(10);
        DrawCurrentState();
        EditorGUILayout.Space(10);
        DrawStatusPreview();
        EditorGUILayout.Space(10);
        DrawSpriteConfig();
        EditorGUILayout.Space(10);
        DrawDropConfig();
        EditorGUILayout.Space(10);
        DrawSoundSettings();
        EditorGUILayout.Space(10);
        DrawDebugSettings();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawStageConfigs()
    {
        showStageConfigs = EditorGUILayout.BeginFoldoutHeaderGroup(showStageConfigs, "━━━━ 阶段配置 ━━━━");
        if (showStageConfigs)
        {
            var prop = serializedObject.FindProperty("stageConfigs");
            if (prop.arraySize != 4) prop.arraySize = 4;
            
            if (prop.GetArrayElementAtIndex(0).FindPropertyRelative("health").intValue == 0)
            {
                EditorGUILayout.HelpBox("检测到阶段配置为空，点击下方按钮填充默认值", MessageType.Warning);
                if (GUILayout.Button("填充默认配置值", GUILayout.Height(25)))
                    FillDefaultStageConfigs(prop);
            }
            
            EditorGUI.indentLevel++;
            for (int i = 0; i < 4; i++)
            {
                var s = prop.GetArrayElementAtIndex(i);
                stageConfigFoldouts[i] = EditorGUILayout.Foldout(stageConfigFoldouts[i], StageNames[i], true);
                if (stageConfigFoldouts[i])
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(s.FindPropertyRelative("health"), new GUIContent("血量"));
                    EditorGUILayout.PropertyField(s.FindPropertyRelative("stoneTotalCount"), new GUIContent("石料总量"));
                    EditorGUILayout.PropertyField(s.FindPropertyRelative("isFinalStage"), new GUIContent("是否最终阶段"));
                    if (!s.FindPropertyRelative("isFinalStage").boolValue)
                    {
                        EditorGUILayout.PropertyField(s.FindPropertyRelative("nextStage"), new GUIContent("下一阶段"));
                        EditorGUILayout.PropertyField(s.FindPropertyRelative("decreaseOreIndexOnTransition"), new GUIContent("转换时含量减1"));
                    }
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.indentLevel--;
            if (GUILayout.Button("重置为默认配置"))
                if (EditorUtility.DisplayDialog("确认重置", "确定要重置吗？", "确定", "取消"))
                    FillDefaultStageConfigs(prop);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void FillDefaultStageConfigs(SerializedProperty prop)
    {
        for (int i = 0; i < 4; i++)
        {
            var s = prop.GetArrayElementAtIndex(i);
            s.FindPropertyRelative("health").intValue = DefaultHealth[i];
            s.FindPropertyRelative("stoneTotalCount").intValue = DefaultStoneTotalCount[i];
            s.FindPropertyRelative("isFinalStage").boolValue = DefaultIsFinalStage[i];
            s.FindPropertyRelative("nextStage").enumValueIndex = (int)DefaultNextStage[i];
            s.FindPropertyRelative("decreaseOreIndexOnTransition").boolValue = DefaultDecreaseOreIndex[i];
        }
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCurrentState()
    {
        showCurrentState = EditorGUILayout.BeginFoldoutHeaderGroup(showCurrentState, "━━━━ 当前状态 ━━━━");
        if (showCurrentState)
        {
            EditorGUI.indentLevel++;
            var stageProp = serializedObject.FindProperty("currentStage");
            var oreTypeProp = serializedObject.FindProperty("oreType");
            var oreIndexProp = serializedObject.FindProperty("oreIndex");
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(stageProp, new GUIContent("当前阶段"));
            EditorGUILayout.PropertyField(oreTypeProp, new GUIContent("矿物类型"));
            
            int maxIndex = GetMaxOreIndex((StoneStage)stageProp.enumValueIndex);
            EditorGUILayout.IntSlider(oreIndexProp, 0, maxIndex, new GUIContent("含量指数"));
            if (oreIndexProp.intValue > maxIndex) oreIndexProp.intValue = maxIndex;
            
            // 检测是否有变化，自动同步 Sprite
            bool changed = EditorGUI.EndChangeCheck();
            if (changed)
            {
                serializedObject.ApplyModifiedProperties();
                
                // 如果缓存无效，先刷新缓存
                if (!spriteCacheValid || cachedSprites.Count == 0)
                {
                    var folderProp = serializedObject.FindProperty("spriteFolder");
                    if (folderProp.objectReferenceValue != null)
                    {
                        RefreshSpriteCache();
                    }
                }
                
                // 同步 Sprite
                if (spriteCacheValid && cachedSprites.Count > 0)
                {
                    SyncCurrentSprite();
                }
                else
                {
                    Debug.LogWarning("[StoneControllerEditor] 无法同步 Sprite：请先设置 Sprite 文件夹并刷新缓存");
                }
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("━━━━ 血量状态 ━━━━", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentHealth"), new GUIContent("当前血量"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private int GetMaxOreIndex(StoneStage stage) => stage switch
    {
        StoneStage.M1 => 4, StoneStage.M2 => 4, StoneStage.M3 => 3, StoneStage.M4 => 7, _ => 4
    };

    private void DrawStatusPreview()
    {
        showStatusPreview = EditorGUILayout.BeginFoldoutHeaderGroup(showStatusPreview, "━━━━ 状态预览 ━━━━");
        if (showStatusPreview)
        {
            EditorGUI.indentLevel++;
            var stageProp = serializedObject.FindProperty("currentStage");
            var oreTypeProp = serializedObject.FindProperty("oreType");
            var oreIndexProp = serializedObject.FindProperty("oreIndex");
            var healthProp = serializedObject.FindProperty("currentHealth");
            
            StoneStage stage = (StoneStage)stageProp.enumValueIndex;
            OreType oreType = (OreType)oreTypeProp.enumValueIndex;
            int oreIndex = oreIndexProp.intValue;
            
            string spriteName = GetSpriteName(oreType, stage, oreIndex);
            int expectedHealth = GetExpectedHealth(stage, oreIndex);
            
            EditorGUILayout.LabelField("Sprite 名称", spriteName);
            EditorGUILayout.LabelField("血量", $"{healthProp.intValue} / {expectedHealth}");
            EditorGUILayout.LabelField("预计矿物掉落", GetOreTotal(stage, oreIndex).ToString());
            EditorGUILayout.LabelField("预计石料掉落", GetStoneTotal(stage).ToString());
            EditorGUILayout.LabelField("所需镐子", GetRequiredPickaxe(oreType));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private string GetSpriteName(OreType oreType, StoneStage stage, int oreIndex)
    {
        string oreTypeStr = oreType == OreType.None ? "C0" : oreType.ToString();
        return $"Stone_{oreTypeStr}_{stage}_{oreIndex}";
    }

    private int GetStageHealth(StoneStage stage) => stage switch 
    { 
        StoneStage.M1 => 36, StoneStage.M2 => 17, StoneStage.M3 => 9, StoneStage.M4 => 4, _ => 36 
    };
    
    /// <summary>
    /// 获取期望血量（考虑 M3 + oreIndex=0 的特殊情况）
    /// </summary>
    private int GetExpectedHealth(StoneStage stage, int oreIndex)
    {
        // M3 阶段且无矿物（oreIndex=0）时，血量与 M4 一致（4）
        if (stage == StoneStage.M3 && oreIndex == 0)
        {
            return 4;
        }
        return GetStageHealth(stage);
    }
    
    private int GetOreTotal(StoneStage stage, int idx)
    {
        int[] arr = stage switch
        {
            StoneStage.M1 => new[] { 0, 3, 5, 7, 9 },
            StoneStage.M2 => new[] { 0, 1, 3, 5, 7 },
            StoneStage.M3 => new[] { 0, 1, 2, 3 },
            _ => new int[0]
        };
        return idx < arr.Length ? arr[idx] : 0;
    }
    
    private int GetStoneTotal(StoneStage stage) => stage switch 
    { 
        StoneStage.M1 => 12, StoneStage.M2 => 6, StoneStage.M3 => 2, StoneStage.M4 => 2, _ => 0 
    };
    
    private string GetRequiredPickaxe(OreType t) => t switch 
    { 
        OreType.None => "任意镐子", OreType.C1 => "生铁镐(2) 或更高", 
        OreType.C2 => "石镐(1) 或更高", OreType.C3 => "钢镐(4) 或更高", _ => "任意镐子" 
    };

    private void DrawSpriteConfig()
    {
        showSpriteConfig = EditorGUILayout.BeginFoldoutHeaderGroup(showSpriteConfig, "━━━━ Sprite配置 ━━━━");
        if (showSpriteConfig)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spriteRenderer"), new GUIContent("Sprite 渲染器"));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("━━━ Sprite配置 ━━━", EditorStyles.boldLabel);
            
            var folderProp = serializedObject.FindProperty("spriteFolder");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(folderProp, new GUIContent("Sprite 文件夹"));
            if (EditorGUI.EndChangeCheck() && folderProp.objectReferenceValue != null)
            {
                OnSpriteFolderChanged(folderProp.objectReferenceValue);
            }
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spritePathPrefix"), new GUIContent("Sprite 路径前缀"));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 刷新 Sprite 缓存", GUILayout.Height(25)))
                RefreshSpriteCache();
            if (GUILayout.Button("📋 同步当前 Sprite", GUILayout.Height(25)))
            {
                SyncCurrentSprite();
            }
            EditorGUILayout.EndHorizontal();
            
            showSpriteDebugInfo = EditorGUILayout.Foldout(showSpriteDebugInfo, "📊 Sprite 调试信息", true);
            if (showSpriteDebugInfo)
                DrawSpriteDebugInfo();
            
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void OnSpriteFolderChanged(Object folder)
    {
        string folderPath = AssetDatabase.GetAssetPath(folder);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"[StoneControllerEditor] 无效的文件夹: {folderPath}");
            return;
        }
        
        Debug.Log($"<color=cyan>[StoneControllerEditor] 文件夹已更改: {folderPath}</color>");
        
        var pathPrefixProp = serializedObject.FindProperty("spritePathPrefix");
        pathPrefixProp.stringValue = folderPath.Replace("Assets/", "") + "/";
        serializedObject.ApplyModifiedProperties();
        
        // 刷新缓存
        RefreshSpriteCache();
        
        // 如果缓存有效，同步当前 Sprite
        if (spriteCacheValid && cachedSprites.Count > 0)
        {
            SyncCurrentSprite();
        }
    }

    private void SyncCurrentSprite()
    {
        var controller = (StoneController)target;
        var stageProp = serializedObject.FindProperty("currentStage");
        var oreTypeProp = serializedObject.FindProperty("oreType");
        var oreIndexProp = serializedObject.FindProperty("oreIndex");
        
        StoneStage stage = (StoneStage)stageProp.enumValueIndex;
        OreType oreType = (OreType)oreTypeProp.enumValueIndex;
        int oreIndex = oreIndexProp.intValue;
        
        string spriteName = GetSpriteName(oreType, stage, oreIndex);
        
        Debug.Log($"<color=cyan>[StoneControllerEditor] 开始同步 Sprite: {spriteName}</color>");
        
        // 先检查缓存是否有效
        if (!spriteCacheValid || cachedSprites.Count == 0)
        {
            Debug.LogWarning($"[StoneControllerEditor] Sprite 缓存无效（有效={spriteCacheValid}, 数量={cachedSprites.Count}），请先刷新缓存");
            return;
        }
        
        // 从缓存中查找
        if (!cachedSprites.TryGetValue(spriteName, out Sprite sprite))
        {
            Debug.LogWarning($"[StoneControllerEditor] 缓存中找不到 Sprite: {spriteName}");
            Debug.Log($"[StoneControllerEditor] 缓存中有 {cachedSprites.Count} 个 Sprite，前 5 个:");
            int count = 0;
            foreach (var key in cachedSprites.Keys)
            {
                Debug.Log($"  - {key}");
                if (++count >= 5) break;
            }
            return;
        }
        
        Debug.Log($"<color=green>[StoneControllerEditor] 找到 Sprite: {sprite.name}</color>");
        
        // 获取 SpriteRenderer - 优先使用已配置的，否则自动查找
        var srProp = serializedObject.FindProperty("spriteRenderer");
        SpriteRenderer sr = srProp.objectReferenceValue as SpriteRenderer;
        
        // 如果没有配置，尝试自动查找
        if (sr == null)
        {
            // 先在当前物体上找
            sr = controller.GetComponent<SpriteRenderer>();
            
            // 再在子物体上找
            if (sr == null)
            {
                sr = controller.GetComponentInChildren<SpriteRenderer>();
            }
            
            // 找到了就自动赋值
            if (sr != null)
            {
                srProp.objectReferenceValue = sr;
                serializedObject.ApplyModifiedProperties();
                Debug.Log($"<color=cyan>[StoneControllerEditor] 自动找到并设置 SpriteRenderer: {sr.gameObject.name}</color>");
            }
        }
        
        if (sr == null)
        {
            Debug.LogError($"[StoneControllerEditor] 找不到 SpriteRenderer！请手动指定或确保物体上有 SpriteRenderer 组件");
            return;
        }
        
        // 记录旧 Sprite 名称
        string oldSpriteName = sr.sprite != null ? sr.sprite.name : "null";
        
        // 应用 Sprite
        Undo.RecordObject(sr, "Sync Stone Sprite");
        sr.sprite = sprite;
        EditorUtility.SetDirty(sr);
        
        Debug.Log($"<color=green>[StoneControllerEditor] SpriteRenderer 已更新: {oldSpriteName} → {sprite.name}</color>");
        
        // 对齐 Sprite 底部中心到父物体位置
        AlignSpriteBottomCenter(sr, sprite);
        
        // 同步 Collider
        SyncColliderFromSprite(controller, sr, sprite);
        
        EditorUtility.SetDirty(controller);
        
        // 标记场景为已修改
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }
        
        // 强制重绘 Scene 视图
        SceneView.RepaintAll();
        
        Debug.Log($"<color=green>[StoneControllerEditor] ✅ 同步完成: {spriteName}</color>");
    }
    
    /// <summary>
    /// 对齐 Sprite 底部中心到父物体位置
    /// </summary>
    private void AlignSpriteBottomCenter(SpriteRenderer sr, Sprite sprite)
    {
        if (sr == null || sprite == null) return;
        
        // 获取 Sprite 的 bounds（本地坐标）
        Bounds spriteBounds = sprite.bounds;
        
        // 计算底部中心的偏移量
        float bottomY = spriteBounds.min.y;
        float centerX = spriteBounds.center.x;
        
        // 设置本地位置，使底部中心对齐到 (0, 0)
        sr.transform.localPosition = new Vector3(-centerX, -bottomY, 0);
        
        EditorUtility.SetDirty(sr.transform);
        Debug.Log($"<color=cyan>[StoneControllerEditor] Sprite 底部对齐: localPos = {sr.transform.localPosition}</color>");
    }
    
    /// <summary>
    /// 从 Sprite 的 Custom Physics Shape 同步 PolygonCollider2D
    /// 注意：PolygonCollider2D 和 SpriteRenderer 在同一个物体上
    /// 当我们移动 transform 来对齐 Sprite 底部时，Collider 会自动跟着移动
    /// 所以路径点不需要额外偏移
    /// </summary>
    private void SyncColliderFromSprite(StoneController controller, SpriteRenderer sr, Sprite sprite)
    {
        if (sr == null || sprite == null) return;
        
        // 查找 PolygonCollider2D - 应该在 SpriteRenderer 同一个物体上
        PolygonCollider2D polygonCollider = sr.GetComponent<PolygonCollider2D>();
        
        // 如果 SpriteRenderer 物体上没有，尝试从 serializedObject 获取
        if (polygonCollider == null)
        {
            var colliderProp = serializedObject.FindProperty("polygonCollider");
            polygonCollider = colliderProp.objectReferenceValue as PolygonCollider2D;
        }
        
        // 还是没有，尝试在 controller 上找
        if (polygonCollider == null)
        {
            polygonCollider = controller.GetComponent<PolygonCollider2D>();
        }
        
        if (polygonCollider == null)
        {
            Debug.Log($"[StoneControllerEditor] 没有找到 PolygonCollider2D，跳过 Collider 同步");
            return;
        }
        
        // 更新 serializedObject 中的引用
        var colliderPropUpdate = serializedObject.FindProperty("polygonCollider");
        if (colliderPropUpdate.objectReferenceValue != polygonCollider)
        {
            colliderPropUpdate.objectReferenceValue = polygonCollider;
            serializedObject.ApplyModifiedProperties();
        }
        
        int shapeCount = sprite.GetPhysicsShapeCount();
        
        if (shapeCount == 0)
        {
            Debug.LogWarning($"[StoneControllerEditor] Sprite {sprite.name} 没有 Custom Physics Shape");
            return;
        }
        
        Undo.RecordObject(polygonCollider, "Sync Stone Collider");
        
        // 设置路径数量
        polygonCollider.pathCount = shapeCount;
        
        // 复制每个路径（不需要偏移，因为 Collider 和 SpriteRenderer 在同一个物体上）
        List<Vector2> path = new List<Vector2>();
        
        for (int i = 0; i < shapeCount; i++)
        {
            path.Clear();
            sprite.GetPhysicsShape(i, path);
            polygonCollider.SetPath(i, path);
        }
        
        // 重置 offset
        polygonCollider.offset = Vector2.zero;
        
        EditorUtility.SetDirty(polygonCollider);
        Debug.Log($"<color=cyan>[StoneControllerEditor] Collider 已同步: {shapeCount} 个路径（Collider 在 {polygonCollider.gameObject.name} 上）</color>");
        
        // 如果有 CompositeCollider2D，触发重新生成
        if (controller.transform.parent != null)
        {
            var composite = controller.transform.parent.GetComponent<CompositeCollider2D>();
            if (composite != null)
            {
                composite.GenerateGeometry();
                EditorUtility.SetDirty(composite);
            }
        }
    }

    private void RefreshSpriteCache()
    {
        cachedSprites.Clear();
        spritesByStage.Clear();
        spritesByOreType.Clear();
        totalSpriteCount = 0;
        
        var folderProp = serializedObject.FindProperty("spriteFolder");
        var folder = folderProp.objectReferenceValue;
        
        if (folder == null)
        {
            Debug.LogWarning("[StoneControllerEditor] 未设置 Sprite 文件夹");
            spriteCacheValid = false;
            return;
        }
        
        string folderPath = AssetDatabase.GetAssetPath(folder);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"[StoneControllerEditor] 无效的文件夹路径: {folderPath}");
            spriteCacheValid = false;
            return;
        }
        
        lastFolderPath = folderPath;
        
        // 初始化分类字典
        foreach (var s in new[] { "M1", "M2", "M3", "M4" })
            spritesByStage[s] = new List<string>();
        foreach (var o in new[] { "C0", "C1", "C2", "C3" })
            spritesByOreType[o] = new List<string>();
        
        // 查找所有 Texture2D（png 文件）- FindAssets 会递归搜索子文件夹
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        Debug.Log($"<color=cyan>[StoneControllerEditor] 扫描文件夹: {folderPath}</color>");
        Debug.Log($"<color=cyan>[StoneControllerEditor] 找到 {guids.Length} 个纹理文件</color>");
        
        int processedTextures = 0;
        int totalSpritesFound = 0;
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            processedTextures++;
            
            // 加载所有子资源（包括切片后的 Sprite）
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            
            foreach (var asset in allAssets)
            {
                if (asset is Sprite sprite)
                {
                    totalSpritesFound++;
                    string originalName = sprite.name;
                    
                    // 规范化名称：去掉 Unity 切片后缀 _0
                    string normalizedName = NormalizeSpriteName(originalName);
                    
                    // 调试输出每个 Sprite
                    // Debug.Log($"  原始: {originalName} → 规范化: {normalizedName}");
                    
                    if (TryParseSpriteName(normalizedName, out string oreType, out string stage, out int oreIndex))
                    {
                        // 用规范化名称作为 key
                        if (!cachedSprites.ContainsKey(normalizedName))
                        {
                            cachedSprites[normalizedName] = sprite;
                            
                            if (spritesByStage.ContainsKey(stage))
                                spritesByStage[stage].Add(normalizedName);
                            if (spritesByOreType.ContainsKey(oreType))
                                spritesByOreType[oreType].Add(normalizedName);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[StoneControllerEditor] 无法解析 Sprite 名称: {originalName} (规范化: {normalizedName})");
                    }
                }
            }
        }
        
        totalSpriteCount = cachedSprites.Count;
        spriteCacheValid = totalSpriteCount > 0;
        
        Debug.Log($"<color=cyan>[StoneControllerEditor] 处理了 {processedTextures} 个纹理，找到 {totalSpritesFound} 个 Sprite</color>");
        Debug.Log($"<color=green>[StoneControllerEditor] 缓存完成: {totalSpriteCount} 个有效 Sprite</color>");
        
        // 输出分类统计
        foreach (var kvp in spritesByStage)
        {
            if (kvp.Value.Count > 0)
                Debug.Log($"<color=yellow>  {kvp.Key}: {kvp.Value.Count} 个</color>");
        }
        foreach (var kvp in spritesByOreType)
        {
            if (kvp.Value.Count > 0)
                Debug.Log($"<color=yellow>  {kvp.Key}: {kvp.Value.Count} 个</color>");
        }
        
        // 如果没有找到任何 Sprite，输出更详细的调试信息
        if (totalSpriteCount == 0 && guids.Length > 0)
        {
            Debug.LogWarning("[StoneControllerEditor] 找到纹理但没有有效 Sprite，检查前几个文件：");
            for (int i = 0; i < Mathf.Min(3, guids.Length); i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                Debug.Log($"  文件: {assetPath}");
                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (var asset in allAssets)
                {
                    if (asset is Sprite sprite)
                    {
                        Debug.Log($"    Sprite: {sprite.name}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 规范化 Sprite 名称（去掉 Unity 切片后缀 _0，并修正 Store -> Stone 拼写错误）
    /// Store_C1_M1_0_0 -> Stone_C1_M1_0
    /// Stone_C1_M1_0_0 -> Stone_C1_M1_0
    /// </summary>
    private string NormalizeSpriteName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        
        string[] parts = name.Split('_');
        
        // 修正 Store -> Stone 拼写错误
        if (parts.Length > 0 && parts[0] == "Store")
        {
            parts[0] = "Stone";
        }
        
        // 5个部分且第一个是Stone/Store，说明有切片后缀，去掉最后一个
        if (parts.Length == 5 && (parts[0] == "Stone" || parts[0] == "Store"))
        {
            return $"Stone_{parts[1]}_{parts[2]}_{parts[3]}";
        }
        
        // 4个部分，直接返回（确保前缀是 Stone）
        if (parts.Length == 4)
        {
            return $"Stone_{parts[1]}_{parts[2]}_{parts[3]}";
        }
        
        return name;
    }

    /// <summary>
    /// 解析 Sprite 名称
    /// 格式：Stone_{OreType}_{Stage}_{OreIndex}
    /// </summary>
    private bool TryParseSpriteName(string name, out string oreType, out string stage, out int oreIndex)
    {
        oreType = ""; stage = ""; oreIndex = 0;
        
        if (string.IsNullOrEmpty(name)) return false;
        
        string[] parts = name.Split('_');
        
        // 支持 Stone 和 Store 前缀
        if (parts.Length < 4 || (parts[0] != "Stone" && parts[0] != "Store")) return false;
        
        oreType = parts[1];  // C0, C1, C2, C3
        stage = parts[2];    // M1, M2, M3, M4
        
        return int.TryParse(parts[3], out oreIndex);
    }

    private void DrawSpriteDebugInfo()
    {
        EditorGUI.indentLevel++;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("基本统计", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"缓存状态: {(spriteCacheValid ? "✅ 有效" : "❌ 无效")}");
        EditorGUILayout.LabelField($"总 Sprite 数量: {totalSpriteCount}");
        EditorGUILayout.LabelField($"文件夹路径: {lastFolderPath}");
        EditorGUILayout.EndVertical();
        
        if (!spriteCacheValid || totalSpriteCount == 0)
        {
            EditorGUILayout.HelpBox("请先设置 Sprite 文件夹并点击「刷新 Sprite 缓存」", MessageType.Info);
            EditorGUI.indentLevel--;
            return;
        }
        
        EditorGUILayout.Space(5);
        
        // 按阶段统计
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("按阶段分类", EditorStyles.boldLabel);
        foreach (var kvp in spritesByStage)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{kvp.Key}:", GUILayout.Width(50));
            EditorGUILayout.LabelField($"{kvp.Value.Count} 个");
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // 按矿物类型统计
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("按矿物类型分类", EditorStyles.boldLabel);
        foreach (var kvp in spritesByOreType)
        {
            if (kvp.Value.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{kvp.Key}:", GUILayout.Width(50));
                EditorGUILayout.LabelField($"{kvp.Value.Count} 个");
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // 当前状态检查
        var stageProp = serializedObject.FindProperty("currentStage");
        var oreTypeProp = serializedObject.FindProperty("oreType");
        var oreIndexProp = serializedObject.FindProperty("oreIndex");
        
        StoneStage stage = (StoneStage)stageProp.enumValueIndex;
        OreType oreType = (OreType)oreTypeProp.enumValueIndex;
        int oreIndex = oreIndexProp.intValue;
        
        string expectedName = GetSpriteName(oreType, stage, oreIndex);
        bool exists = cachedSprites.ContainsKey(expectedName);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("当前状态检查", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"期望 Sprite: {expectedName}");
        EditorGUILayout.LabelField($"Sprite 存在: {(exists ? "✅ 是" : "❌ 否")}");
        if (!exists)
            EditorGUILayout.HelpBox($"找不到 Sprite: {expectedName}", MessageType.Warning);
        EditorGUILayout.EndVertical();
        
        EditorGUI.indentLevel--;
    }

    private void DrawDropConfig()
    {
        showDropConfig = EditorGUILayout.BeginFoldoutHeaderGroup(showDropConfig, "━━━━ 掉落配置 ━━━━");
        if (showDropConfig)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("copperOreItem"), new GUIContent("铜矿掉落物品"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ironOreItem"), new GUIContent("铁矿掉落物品"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("goldOreItem"), new GUIContent("金矿掉落物品"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stoneItem"), new GUIContent("石料掉落物品"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dropSpreadRadius"), new GUIContent("掉落散布半径"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawSoundSettings()
    {
        showSoundSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showSoundSettings, "━━━━ 音效设置 ━━━━");
        if (showSoundSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mineHitSound"), new GUIContent("挖掘音效"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("breakSound"), new GUIContent("破碎音效"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tierInsufficientSound"), new GUIContent("等级不足音效"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("soundVolume"), new GUIContent("音量"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawDebugSettings()
    {
        showDebugSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showDebugSettings, "━━━━ 调试 ━━━━");
        if (showDebugSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showDebugInfo"), new GUIContent("显示调试信息"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
}
