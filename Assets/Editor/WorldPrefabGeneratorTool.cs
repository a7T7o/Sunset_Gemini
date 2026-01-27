using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FarmGame.Data;

/// <summary>
/// World Prefab 批量生成工具
/// 从 ItemData 的 icon 生成世界物品预制体
/// </summary>
public class WorldPrefabGeneratorTool : EditorWindow
{
    #region 配置

    private List<ItemData> selectedItems = new List<ItemData>();
    private Vector2 scrollPosition;
    private Vector2 itemListScrollPos;
    
    // 输出路径
    private string prefabsOutputPath = "Assets/Prefabs/WorldItems";
    
    // 阴影配置
    private Sprite shadowSprite;
    private Color shadowColor = new Color(0f, 0f, 0f, 1f); // alpha=1.0，用户图片已有透明度处理
    
    // 世界物品配置
    private float worldItemScale = 0.75f;
    private float spriteRotationZ = 45f;
    private float shadowBottomOffset = 0.02f;

    // 生成选项
    private bool overwriteExisting = false;
    
    // 批量生成选项
    private bool useBatchMode = false;
    private string batchFolderPath = "Assets/111_Data/Items";

    // EditorPrefs Keys
    private const string PREF_OUTPUT_PATH = "WorldPrefab_OutputPath";
    private const string PREF_SCALE = "WorldPrefab_Scale";
    private const string PREF_ROTATION = "WorldPrefab_Rotation";
    private const string PREF_SHADOW_OFFSET = "WorldPrefab_ShadowOffset";
    private const string PREF_OVERWRITE = "WorldPrefab_Overwrite";
    private const string PREF_BATCH_MODE = "WorldPrefab_BatchMode";
    private const string PREF_BATCH_FOLDER = "WorldPrefab_BatchFolder";
    private const string PREF_SHADOW_SPRITE = "WorldPrefab_ShadowSprite";
    private const string PREF_SHADOW_COLOR = "WorldPrefab_ShadowColor";

    #endregion

    [MenuItem("Tools/World Item/批量生成 World Prefab")]
    public static void ShowWindow()
    {
        var window = GetWindow<WorldPrefabGeneratorTool>("World Prefab 生成器");
        window.minSize = new Vector2(450, 550);
    }

    private void OnEnable()
    {
        LoadSettings();
        
        // 加载阴影 Sprite（如果没有保存的路径，使用默认路径）
        string shadowPath = EditorPrefs.GetString(PREF_SHADOW_SPRITE, "Assets/Sprites/Generated/Shadow_Ellipse.png");
        shadowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(shadowPath);
    }

    private void OnDisable()
    {
        SaveSettings();
    }

    private void LoadSettings()
    {
        prefabsOutputPath = EditorPrefs.GetString(PREF_OUTPUT_PATH, "Assets/Prefabs/WorldItems");
        worldItemScale = EditorPrefs.GetFloat(PREF_SCALE, 0.75f);
        spriteRotationZ = EditorPrefs.GetFloat(PREF_ROTATION, 45f);
        shadowBottomOffset = EditorPrefs.GetFloat(PREF_SHADOW_OFFSET, 0.02f);
        overwriteExisting = EditorPrefs.GetBool(PREF_OVERWRITE, false);
        useBatchMode = EditorPrefs.GetBool(PREF_BATCH_MODE, false);
        batchFolderPath = EditorPrefs.GetString(PREF_BATCH_FOLDER, "Assets/111_Data/Items");
        
        // 加载阴影颜色（使用 ColorUtility 序列化）
        string colorHex = EditorPrefs.GetString(PREF_SHADOW_COLOR, "#000000FF");
        if (ColorUtility.TryParseHtmlString(colorHex, out Color loadedColor))
        {
            shadowColor = loadedColor;
        }
    }

    private void SaveSettings()
    {
        EditorPrefs.SetString(PREF_OUTPUT_PATH, prefabsOutputPath);
        EditorPrefs.SetFloat(PREF_SCALE, worldItemScale);
        EditorPrefs.SetFloat(PREF_ROTATION, spriteRotationZ);
        EditorPrefs.SetFloat(PREF_SHADOW_OFFSET, shadowBottomOffset);
        EditorPrefs.SetBool(PREF_OVERWRITE, overwriteExisting);
        EditorPrefs.SetBool(PREF_BATCH_MODE, useBatchMode);
        EditorPrefs.SetString(PREF_BATCH_FOLDER, batchFolderPath);
        
        // 保存阴影 Sprite 路径
        if (shadowSprite != null)
        {
            string shadowPath = AssetDatabase.GetAssetPath(shadowSprite);
            EditorPrefs.SetString(PREF_SHADOW_SPRITE, shadowPath);
        }
        
        // 保存阴影颜色（使用 ColorUtility 序列化）
        string colorHex = "#" + ColorUtility.ToHtmlStringRGBA(shadowColor);
        EditorPrefs.SetString(PREF_SHADOW_COLOR, colorHex);
    }

    /// <summary>
    /// 手动获取选中的 ItemData
    /// </summary>
    private void GetSelectedItems()
    {
        selectedItems.Clear();
        
        foreach (var obj in Selection.objects)
        {
            if (obj is ItemData itemData)
            {
                if (!selectedItems.Contains(itemData))
                    selectedItems.Add(itemData);
            }
            else if (obj is DefaultAsset)
            {
                string folderPath = AssetDatabase.GetAssetPath(obj);
                if (AssetDatabase.IsValidFolder(folderPath))
                {
                    var itemsInFolder = GetAllItemDataInFolder(folderPath);
                    foreach (var item in itemsInFolder)
                    {
                        if (!selectedItems.Contains(item))
                            selectedItems.Add(item);
                    }
                }
            }
        }

        selectedItems = selectedItems.OrderBy(i => i.itemID).ToList();
        Repaint();
    }

    private List<ItemData> GetAllItemDataInFolder(string folderPath)
    {
        var result = new List<ItemData>();
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { folderPath });
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
            if (itemData != null)
                result.Add(itemData);
        }
        
        return result;
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        DrawHeader();
        DrawItemSelection();
        DrawLine();
        DrawOutputSettings();
        DrawLine();
        DrawWorldItemSettings();
        DrawLine();
        DrawShadowSettings();
        DrawLine();
        DrawGenerateOptions();
        DrawLine();
        DrawGenerateButton();
        DrawLine();
        DrawUtilityButtons();
        
        EditorGUILayout.EndScrollView();
    }

    #region UI 绘制

    private void DrawHeader()
    {
        EditorGUILayout.Space(10);
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("🌍 World Prefab 批量生成工具", style, GUILayout.Height(30));
        EditorGUILayout.HelpBox(
            "生成世界物品预制体：\n" +
            "• Sprite 子物体 Z 轴旋转（保持像素完整）\n" +
            "• 整体缩放可调节\n" +
            "• 阴影自动计算位置和大小", 
            MessageType.Info);
        EditorGUILayout.Space(5);
    }

    private void DrawItemSelection()
    {
        EditorGUILayout.LabelField("📦 物品来源", EditorStyles.boldLabel);
        
        // 批量模式切换
        EditorGUILayout.BeginHorizontal();
        useBatchMode = EditorGUILayout.Toggle("📂 从文件夹批量生成", useBatchMode);
        EditorGUILayout.EndHorizontal();
        
        if (useBatchMode)
        {
            // 批量模式：输入文件夹路径
            DrawBatchModeUI();
        }
        else
        {
            // 手动选择模式
            DrawManualSelectionUI();
        }
    }
    
    private void DrawBatchModeUI()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.HelpBox("输入包含 ItemData SO 文件的文件夹路径，将递归搜索所有子文件夹", MessageType.Info);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("文件夹路径", GUILayout.Width(70));
        batchFolderPath = EditorGUILayout.TextField(batchFolderPath);
        if (GUILayout.Button("选择", GUILayout.Width(45)))
        {
            string path = EditorUtility.OpenFolderPanel("选择 ItemData 文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
            {
                batchFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // 预览按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔍 预览文件夹内容", GUILayout.Height(28)))
        {
            LoadItemsFromFolder(batchFolderPath);
        }
        EditorGUILayout.EndHorizontal();
        
        // 显示预览结果
        if (selectedItems.Count > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"✓ 找到 {selectedItems.Count} 个 ItemData", EditorStyles.boldLabel);
            
            itemListScrollPos = EditorGUILayout.BeginScrollView(itemListScrollPos, 
                GUILayout.Height(Mathf.Min(selectedItems.Count * 24 + 5, 120)));
            
            int showCount = Mathf.Min(selectedItems.Count, 10);
            for (int i = 0; i < showCount; i++)
            {
                DrawItemPreviewRow(selectedItems[i]);
            }
            
            if (selectedItems.Count > 10)
            {
                EditorGUILayout.LabelField($"... 还有 {selectedItems.Count - 10} 项", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("点击「预览文件夹内容」查看将要处理的 ItemData", MessageType.None);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawManualSelectionUI()
    {
        // 获取选中项按钮
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.HelpBox("在 Project 窗口选择 ItemData 或文件夹", MessageType.None);
        if (GUILayout.Button("🔍 获取选中项", GUILayout.Width(100), GUILayout.Height(38)))
        {
            GetSelectedItems();
        }
        EditorGUILayout.EndHorizontal();
        
        // 显示选中的 ItemData
        if (selectedItems.Count == 0)
        {
            EditorGUILayout.HelpBox("⚠️ 未选择任何 ItemData", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"✓ 已选择 {selectedItems.Count} 个 ItemData", EditorStyles.boldLabel);
            
            itemListScrollPos = EditorGUILayout.BeginScrollView(itemListScrollPos, 
                GUILayout.Height(Mathf.Min(selectedItems.Count * 24 + 5, 120)));
            
            int showCount = Mathf.Min(selectedItems.Count, 10);
            for (int i = 0; i < showCount; i++)
            {
                DrawItemPreviewRow(selectedItems[i]);
            }
            
            if (selectedItems.Count > 10)
            {
                EditorGUILayout.LabelField($"... 还有 {selectedItems.Count - 10} 项", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }
    
    private void DrawItemPreviewRow(ItemData item)
    {
        if (item == null) return;
        
        EditorGUILayout.BeginHorizontal();
        
        // 预览图
        if (item.icon != null && item.icon.texture != null)
        {
            var rect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
            GUI.DrawTextureWithTexCoords(rect, item.icon.texture, 
                new Rect(
                    item.icon.rect.x / item.icon.texture.width,
                    item.icon.rect.y / item.icon.texture.height,
                    item.icon.rect.width / item.icon.texture.width,
                    item.icon.rect.height / item.icon.texture.height
                ));
        }
        else
        {
            GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
        }
        
        EditorGUILayout.LabelField($"[{item.itemID:D4}] {item.itemName}", EditorStyles.miniLabel);
        
        if (item.icon == null)
        {
            GUI.color = Color.red;
            EditorGUILayout.LabelField("⚠️无图标", EditorStyles.miniLabel, GUILayout.Width(50));
            GUI.color = Color.white;
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    /// <summary>
    /// 从指定文件夹加载所有 ItemData
    /// </summary>
    private void LoadItemsFromFolder(string folderPath)
    {
        selectedItems.Clear();
        
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("错误", $"文件夹不存在: {folderPath}", "确定");
            return;
        }
        
        var items = GetAllItemDataInFolder(folderPath);
        selectedItems = items.OrderBy(i => i.itemID).ToList();
        
        Debug.Log($"[WorldPrefabGenerator] 从 {folderPath} 加载了 {selectedItems.Count} 个 ItemData");
        Repaint();
    }

    private void DrawOutputSettings()
    {
        EditorGUILayout.LabelField("📁 输出路径", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Prefabs", GUILayout.Width(60));
        prefabsOutputPath = EditorGUILayout.TextField(prefabsOutputPath);
        if (GUILayout.Button("选择", GUILayout.Width(45)))
        {
            string path = EditorUtility.OpenFolderPanel("选择 Prefabs 输出文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
            {
                prefabsOutputPath = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawWorldItemSettings()
    {
        EditorGUILayout.LabelField("🎮 世界物品配置", EditorStyles.boldLabel);
        
        worldItemScale = EditorGUILayout.Slider("整体缩放", worldItemScale, 0.3f, 1.5f);
        spriteRotationZ = EditorGUILayout.Slider("Sprite Z 轴旋转", spriteRotationZ, 0f, 90f);
    }

    private void DrawShadowSettings()
    {
        EditorGUILayout.LabelField("🌑 阴影配置", EditorStyles.boldLabel);
        
        shadowSprite = (Sprite)EditorGUILayout.ObjectField("阴影 Sprite", shadowSprite, typeof(Sprite), false);
        shadowColor = EditorGUILayout.ColorField("阴影颜色", shadowColor);
        shadowBottomOffset = EditorGUILayout.Slider("底部偏移", shadowBottomOffset, 0f, 0.15f);
    }

    private void DrawGenerateOptions()
    {
        EditorGUILayout.LabelField("⚙️ 生成选项", EditorStyles.boldLabel);
        overwriteExisting = EditorGUILayout.Toggle("覆盖已存在文件", overwriteExisting);
    }

    private void DrawGenerateButton()
    {
        EditorGUILayout.Space(10);
        
        // 批量模式下，如果还没预览，先提示预览
        if (useBatchMode && selectedItems.Count == 0)
        {
            EditorGUILayout.HelpBox("请先点击「预览文件夹内容」查看将要处理的 ItemData", MessageType.Warning);
            
            GUI.enabled = false;
            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            GUILayout.Button("🚀 请先预览文件夹内容", GUILayout.Height(40));
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
            return;
        }
        
        int validCount = selectedItems.Count(i => i != null && i.icon != null);
        int invalidCount = selectedItems.Count - validCount;
        
        GUI.enabled = validCount > 0;
        GUI.backgroundColor = new Color(0.3f, 0.9f, 0.3f);
        
        string buttonText = invalidCount > 0 
            ? $"🚀 生成 {validCount} 个 World Prefab（跳过 {invalidCount} 个无图标）"
            : $"🚀 生成 {validCount} 个 World Prefab";
        
        if (GUILayout.Button(buttonText, GUILayout.Height(40)))
        {
            GenerateWorldPrefabs();
        }
        
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
    }

    private void DrawUtilityButtons()
    {
        EditorGUILayout.LabelField("🔧 工具", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("生成默认阴影 Sprite"))
        {
            GenerateDefaultShadowSprite();
        }
        if (GUILayout.Button("打开 Prefabs 文件夹"))
        {
            EnsureDirectoryExists(prefabsOutputPath);
            EditorUtility.RevealInFinder(prefabsOutputPath);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLine()
    {
        EditorGUILayout.Space(5);
        Rect rect = EditorGUILayout.GetControlRect(false, 2);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        EditorGUILayout.Space(5);
    }

    #endregion

    #region 生成逻辑

    private void GenerateWorldPrefabs()
    {
        EnsureDirectoryExists(prefabsOutputPath);

        if (shadowSprite == null)
        {
            GenerateDefaultShadowSprite();
        }

        int successCount = 0;
        int skipCount = 0;

        foreach (var itemData in selectedItems)
        {
            if (itemData == null || itemData.icon == null)
            {
                Debug.LogWarning($"[WorldPrefabGenerator] 跳过 {itemData?.name}: icon 为空");
                skipCount++;
                continue;
            }

            try
            {
                GeneratePrefab(itemData);
                successCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[WorldPrefabGenerator] 生成 {itemData.name} 失败: {e.Message}\n{e.StackTrace}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("生成完成", 
            $"成功: {successCount}\n跳过: {skipCount}", "确定");
        
        Debug.Log($"<color=green>[WorldPrefabGenerator] ✅ 完成！成功 {successCount}，跳过 {skipCount}</color>");
    }

    private void GeneratePrefab(ItemData itemData)
    {
        // 从 SO 文件名提取名称（格式：Tool_12_Hoe_0 -> Hoe_0）
        string assetName = ExtractNameFromAsset(itemData);
        string prefabPath = $"{prefabsOutputPath}/WorldItem_{itemData.itemID}_{assetName}.prefab";

        if (!overwriteExisting && File.Exists(prefabPath))
        {
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                itemData.worldPrefab = existingPrefab;
                EditorUtility.SetDirty(itemData);
            }
            return;
        }

        Sprite itemSprite = itemData.icon;
        
        // ★ 获取显示尺寸缩放比例
        float displayScale = itemData.GetWorldDisplayScale();
        
        // 计算 Sprite 在世界单位中的尺寸（应用显示尺寸缩放）
        float spriteWidth = (itemSprite.rect.width / itemSprite.pixelsPerUnit) * displayScale;
        float spriteHeight = (itemSprite.rect.height / itemSprite.pixelsPerUnit) * displayScale;
        
        // 计算旋转后的边界框
        float rotRad = spriteRotationZ * Mathf.Deg2Rad;
        float cos = Mathf.Abs(Mathf.Cos(rotRad));
        float sin = Mathf.Abs(Mathf.Sin(rotRad));
        float rotatedWidth = spriteWidth * cos + spriteHeight * sin;
        float rotatedHeight = spriteWidth * sin + spriteHeight * cos;
        
        // 计算旋转后物体底部到中心的距离
        float bottomY = -rotatedHeight * 0.5f;
        
        // 创建根物体
        string assetNameForObject = ExtractNameFromAsset(itemData);
        GameObject root = new GameObject($"WorldItem_{itemData.itemID}_{assetNameForObject}");
        root.tag = "Pickup";
        root.transform.localScale = Vector3.one * worldItemScale;

        // 添加组件
        var pickup = root.AddComponent<WorldItemPickup>();
        var dropAnim = root.AddComponent<WorldItemDrop>();
        
        // ★ 设置 linkedItemData，确保预制体拖入场景后能正确初始化
        // 使用 SerializedObject 设置私有字段
        var so = new UnityEditor.SerializedObject(pickup);
        var linkedItemDataProp = so.FindProperty("linkedItemData");
        if (linkedItemDataProp != null)
        {
            linkedItemDataProp.objectReferenceValue = itemData;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        
        // 同时设置公开的 itemId 字段作为备份
        pickup.itemId = itemData.itemID;
        
        // 添加 Collider
        var collider = root.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = Mathf.Max(rotatedWidth, rotatedHeight) * 0.4f;

        // 创建 Sprite 子物体
        GameObject spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(root.transform);
        
        // Sprite 位置：底部略高于阴影中心
        float spriteY = -bottomY + shadowBottomOffset;
        spriteObj.transform.localPosition = new Vector3(0f, spriteY, 0f);
        spriteObj.transform.localRotation = Quaternion.Euler(0f, 0f, spriteRotationZ);
        // ★ 应用显示尺寸缩放到 Sprite
        spriteObj.transform.localScale = Vector3.one * displayScale;
        
        SpriteRenderer sr = spriteObj.AddComponent<SpriteRenderer>();
        sr.sprite = itemSprite;
        sr.sortingLayerName = "Layer 1";
        sr.sortingOrder = 0;

        // 创建阴影子物体
        GameObject shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(root.transform);
        shadowObj.transform.localPosition = Vector3.zero;
        shadowObj.transform.localRotation = Quaternion.identity;
        
        // 阴影大小（已经包含了 displayScale 的影响）
        float shadowWidth = rotatedWidth * 0.8f;
        float shadowHeight = shadowWidth * 0.5f;
        
        if (shadowSprite != null)
        {
            float shadowSpriteWidth = shadowSprite.rect.width / shadowSprite.pixelsPerUnit;
            float shadowSpriteHeight = shadowSprite.rect.height / shadowSprite.pixelsPerUnit;
            
            float scaleX = shadowWidth / shadowSpriteWidth;
            float scaleY = shadowHeight / shadowSpriteHeight;
            shadowObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
        else
        {
            shadowObj.transform.localScale = new Vector3(shadowWidth, shadowHeight, 1f);
        }
        
        SpriteRenderer shadowSr = shadowObj.AddComponent<SpriteRenderer>();
        shadowSr.sprite = shadowSprite;
        shadowSr.color = shadowColor;
        shadowSr.sortingLayerName = "Layer 1";
        shadowSr.sortingOrder = -1;

        // 保存预制体
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);

        // 关联到 ItemData
        itemData.worldPrefab = prefab;
        EditorUtility.SetDirty(itemData);

        Debug.Log($"[WorldPrefabGenerator] 生成: {prefabPath}" + 
                  (itemData.useCustomDisplaySize ? $" (displaySize={itemData.displayPixelSize}px, scale={displayScale:F2})" : ""));
    }

    private void GenerateDefaultShadowSprite()
    {
        string spritesPath = "Assets/Sprites/Generated";
        EnsureDirectoryExists(spritesPath);
        string shadowPath = $"{spritesPath}/Shadow_Ellipse.png";

        int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radiusX = size / 2f;
        float radiusY = size / 3f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center.x) / radiusX;
                float dy = (y - center.y) / radiusY;
                float dist = dx * dx + dy * dy;

                if (dist <= 1f)
                {
                    float alpha = (1f - dist) * 0.6f;
                    pixels[y * size + x] = new Color(0f, 0f, 0f, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(shadowPath, pngData);
        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(shadowPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        shadowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(shadowPath);
        Debug.Log($"[WorldPrefabGenerator] 生成阴影Sprite: {shadowPath}");
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// 从 SO 资产文件名中提取名称
    /// 例如：Tool_12_Hoe_0 -> Hoe_0, Weapon_200_Sword_0 -> Sword_0
    /// </summary>
    private string ExtractNameFromAsset(ItemData itemData)
    {
        string assetPath = AssetDatabase.GetAssetPath(itemData);
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        
        // 格式：{Type}_{ID}_{Name}_{Quality} 或 {Type}_{ID}_{Name}
        // 例如：Tool_12_Hoe_0, Weapon_200_Sword_0
        string[] parts = fileName.Split('_');
        
        if (parts.Length >= 3)
        {
            // 跳过前两部分（Type 和 ID），取剩余部分
            // Tool_12_Hoe_0 -> Hoe_0
            // Weapon_200_Sword_0 -> Sword_0
            return string.Join("_", parts.Skip(2));
        }
        
        // 回退：使用文件名
        return fileName;
    }

    #endregion
}
