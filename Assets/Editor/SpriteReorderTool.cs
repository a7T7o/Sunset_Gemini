using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// Sprite重排序工具
/// 按X坐标（从左到右）重新排序并重命名sprite
/// 支持批量处理整个文件夹
/// </summary>
public class SpriteReorderTool : EditorWindow
{
    [MenuItem("Tools/Sprite重排序工具")]
    static void Open()
    {
        var window = GetWindow<SpriteReorderTool>("Sprite重排序");
        window.minSize = new Vector2(500, 400);
        window.Show();
    }

    // 单个文件模式
    Object targetTexture;
    string baseName = "Axe";
    int startIndex = 0;
    
    // 批量模式
    DefaultAsset targetFolder;
    bool keepOriginalName = true;  // 保持原文件名（只改编号）

    void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("━━━━ Sprite重排序工具 ━━━━", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "按X坐标（从左到右）重新排序并重命名sprite\n" +
            "✅ 支持批量处理整个文件夹",
            MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // ========== 批量模式 ==========
        EditorGUILayout.LabelField("━━━━ 批量模式（推荐）━━━━", EditorStyles.boldLabel);
        
        targetFolder = EditorGUILayout.ObjectField(
            "目标文件夹",
            targetFolder,
            typeof(DefaultAsset),
            false) as DefaultAsset;
        
        keepOriginalName = EditorGUILayout.Toggle("保持原文件名", keepOriginalName);
        
        EditorGUILayout.HelpBox(
            keepOriginalName ? 
            "保持原sprite sheet名称，只修改编号\n例如：Axe_0 → Axe_0, Axe_1 → Axe_1（按X坐标重新分配）" :
            "使用统一名称+编号\n例如：所有sprite改为 Frame_0, Frame_1...",
            MessageType.None);
        
        if (!keepOriginalName)
        {
            baseName = EditorGUILayout.TextField("统一名称", baseName);
        }
        
        // 预览
        if (targetFolder != null)
        {
            string folderPath = AssetDatabase.GetAssetPath(targetFolder);
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"检测到 {guids.Length} 个Texture文件", EditorStyles.boldLabel);
            
            if (guids.Length > 0)
            {
                EditorGUILayout.LabelField("文件列表（前5个）：", EditorStyles.miniLabel);
                for (int i = 0; i < Mathf.Min(5, guids.Length); i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    string fileName = Path.GetFileNameWithoutExtension(path);
                    EditorGUILayout.LabelField($"  • {fileName}", EditorStyles.miniLabel);
                }
                if (guids.Length > 5)
                {
                    EditorGUILayout.LabelField($"  ... 还有 {guids.Length - 5} 个", EditorStyles.miniLabel);
                }
            }
        }
        
        EditorGUILayout.Space(5);
        
        GUI.enabled = targetFolder != null;
        if (GUILayout.Button("批量处理文件夹", GUILayout.Height(50)))
        {
            BatchReorderFolder();
        }
        GUI.enabled = true;
        
        EditorGUILayout.Space(20);
        
        // ========== 单个文件模式 ==========
        EditorGUILayout.LabelField("━━━━ 单个文件模式 ━━━━", EditorStyles.boldLabel);
        
        targetTexture = EditorGUILayout.ObjectField(
            "目标Texture",
            targetTexture,
            typeof(Texture2D),
            false);
        
        baseName = EditorGUILayout.TextField("基础名称", baseName);
        startIndex = EditorGUILayout.IntField("起始编号", startIndex);
        
        GUI.enabled = targetTexture != null;
        if (GUILayout.Button("处理单个文件", GUILayout.Height(30)))
        {
            ReorderSprites();
        }
        GUI.enabled = true;
    }

    void BatchReorderFolder()
    {
        string folderPath = AssetDatabase.GetAssetPath(targetFolder);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("错误", "文件夹中未找到任何Texture2D文件！", "确定");
            return;
        }
        
        if (!EditorUtility.DisplayDialog(
            "确认批量处理",
            $"将处理 {guids.Length} 个Texture文件\n\n" +
            $"每个文件的sprite将按X坐标（从左到右）重新编号\n" +
            $"保持原名称：{(keepOriginalName ? "是" : "否")}\n\n" +
            $"是否继续？",
            "确定", "取消"))
        {
            return;
        }
        
        int successCount = 0;
        int failCount = 0;
        
        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string fileName = Path.GetFileNameWithoutExtension(path);
                
                EditorUtility.DisplayProgressBar(
                    "批量处理Sprite",
                    $"处理: {fileName} ({i + 1}/{guids.Length})",
                    (float)i / guids.Length);
                
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                
                if (texture != null)
                {
                    bool success = ReorderSingleTexture(texture, keepOriginalName ? null : baseName);
                    if (success)
                        successCount++;
                    else
                        failCount++;
                }
                else
                {
                    Debug.LogWarning($"[批量处理] 无法加载: {path}");
                    failCount++;
                }
            }
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("完成",
                $"✅ 批量处理完成！\n\n" +
                $"成功：{successCount} 个\n" +
                $"失败：{failCount} 个\n" +
                $"总计：{guids.Length} 个",
                "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("错误", $"批量处理失败：{e.Message}", "确定");
            Debug.LogError($"[批量处理] 失败: {e}\n{e.StackTrace}");
        }
    }

    void ReorderSprites()
    {
        bool success = ReorderSingleTexture(targetTexture as Texture2D, baseName, startIndex);
        
        if (success)
        {
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", "✅ 重排序完成！", "确定");
        }
    }

    bool ReorderSingleTexture(Texture2D texture, string customBaseName = null, int startIdx = 0)
    {
        if (texture == null)
            return false;
        
        string path = AssetDatabase.GetAssetPath(texture);
        
        // 加载所有sprite
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        List<Sprite> sprites = allAssets.OfType<Sprite>().ToList();
        
        if (sprites.Count == 0)
        {
            Debug.LogWarning($"[Sprite重排序] {texture.name}: 未找到sprite");
            return false;
        }
        
        // 按X坐标排序
        sprites.Sort((a, b) => a.rect.x.CompareTo(b.rect.x));
        
        try
        {
            // 获取TextureImporter
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer == null)
            {
                Debug.LogError($"[Sprite重排序] {texture.name}: 无法获取TextureImporter");
                return false;
            }
            
            // 使用ISpriteEditorDataProvider
            var dataProviderFactories = new SpriteDataProviderFactories();
            dataProviderFactories.Init();
            var dataProvider = dataProviderFactories.GetSpriteEditorDataProviderFromObject(importer);
            
            if (dataProvider == null)
            {
                Debug.LogError($"[Sprite重排序] {texture.name}: 无法获取ISpriteEditorDataProvider");
                return false;
            }
            
            dataProvider.InitSpriteEditorDataProvider();
            var spriteRects = dataProvider.GetSpriteRects();
            
            if (spriteRects == null || spriteRects.Length == 0)
            {
                Debug.LogError($"[Sprite重排序] {texture.name}: 无法读取sprite数据");
                return false;
            }
            
            // 确定基础名称
            string finalBaseName = customBaseName;
            if (string.IsNullOrEmpty(finalBaseName))
            {
                // 保持原名：从第一个sprite的名称提取（去掉后缀编号）
                string firstName = sprites[0].name;
                int lastUnderscoreIndex = firstName.LastIndexOf('_');
                if (lastUnderscoreIndex >= 0)
                {
                    finalBaseName = firstName.Substring(0, lastUnderscoreIndex);
                }
                else
                {
                    finalBaseName = Path.GetFileNameWithoutExtension(texture.name);
                }
            }
            
            // 创建映射：原sprite名 -> 新编号
            Dictionary<string, int> spriteToIndex = new Dictionary<string, int>();
            for (int i = 0; i < sprites.Count; i++)
            {
                spriteToIndex[sprites[i].name] = i;
            }
            
            // 重命名所有spriteRect
            foreach (var rect in spriteRects)
            {
                if (spriteToIndex.ContainsKey(rect.name))
                {
                    int newIndex = spriteToIndex[rect.name];
                    string oldName = rect.name;
                    rect.name = $"{finalBaseName}_{startIdx + newIndex}";
                    Debug.Log($"[{texture.name}] {oldName} → {rect.name} (X={rect.rect.x:F0})");
                }
            }
            
            // 保存修改
            dataProvider.SetSpriteRects(spriteRects);
            dataProvider.Apply();
            importer.SaveAndReimport();
            
            Debug.Log($"✅ [{texture.name}] 重排序完成: {sprites.Count}个sprite");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Sprite重排序] {texture.name} 失败: {e.Message}");
            return false;
        }
    }
}
