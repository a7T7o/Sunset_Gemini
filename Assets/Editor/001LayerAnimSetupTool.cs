// using UnityEngine;
// using UnityEditor;
// using UnityEditor.Animations;
// using UnityEditor.U2D.Sprites;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;

// /// <summary>
// /// 同步pivot生成clip工具（全自动版）
// /// 只需提供Slice文件夹，自动扫描所有方向并生成动画
// /// </summary>
// public class LayerAnimSetupTool : EditorWindow
// {
//     [MenuItem("Tools/pivot工具/同步pivot生成clip")]
//     static void ShowWindow()
//     {
//         GetWindow<LayerAnimSetupTool>("同步pivot生成clip");
//     }

//     // ━━━━ 输入 ━━━━
//     DefaultAsset sliceFolder;
//     DefaultAsset sliceBaseFolder;  // Slice_Base文件夹（包含Aseprite源文件）
    
//     // ━━━━ 输出 ━━━━
//     string outputBasePath = "Assets/Animations/Player/Slice";
    
//     // ━━━━ 设置 ━━━━
//     string animationName = "Slice";
//     int totalFrames = 100;
//     int lastFrame = 80;

//     void OnGUI()
//     {
//         GUILayout.Label("━━━━ 工具动画批量创建 ━━━━", EditorStyles.boldLabel);
        
//         EditorGUILayout.HelpBox(
//             "✅ 自动扫描Slice文件夹\n" +
//             "✅ 自动识别所有方向（Down/Side/Up）\n" +
//             "✅ 自动查找Hand和Axe\n" +
//             "✅ 批量生成所有动画剪辑",
//             MessageType.Info);
        
//         EditorGUILayout.Space(10);
        
//         // ━━━━ 输入 ━━━━
//         GUILayout.Label("━━━━ 输入 ━━━━", EditorStyles.boldLabel);
        
//         sliceFolder = EditorGUILayout.ObjectField("Slice文件夹", sliceFolder, typeof(DefaultAsset), false) as DefaultAsset;
        
//         EditorGUILayout.HelpBox(
//             "拖入包含Down、Side、Up子文件夹的Slice文件夹\n" +
//             "示例：Sprites/Slice/",
//             MessageType.Info);
        
//         EditorGUILayout.Space(5);
        
//         sliceBaseFolder = EditorGUILayout.ObjectField("Slice_Base文件夹", sliceBaseFolder, typeof(DefaultAsset), false) as DefaultAsset;
        
//         EditorGUILayout.HelpBox(
//             "拖入包含原始Aseprite文件的文件夹\n" +
//             "需包含：Slice_Down, Slice_Side, Slice_Up\n" +
//             "工具会自动为每个方向匹配对应的Aseprite文件",
//             MessageType.Info);
        
//         EditorGUILayout.Space(10);
        
//         // ━━━━ 输出 ━━━━
//         GUILayout.Label("━━━━ 输出 ━━━━", EditorStyles.boldLabel);
        
//         EditorGUILayout.BeginHorizontal();
//         outputBasePath = EditorGUILayout.TextField("输出基础路径", outputBasePath);
//         if (GUILayout.Button("浏览", GUILayout.Width(60)))
//         {
//             string path = EditorUtility.OpenFolderPanel("选择输出文件夹", outputBasePath, "");
//             if (!string.IsNullOrEmpty(path))
//             {
//                 outputBasePath = ConvertToAssetPath(path);
//             }
//         }
//         EditorGUILayout.EndHorizontal();
        
//         EditorGUILayout.HelpBox(
//             "动画将输出到：\n" +
//             $"{outputBasePath}/Down/\n" +
//             $"{outputBasePath}/Side/\n" +
//             $"{outputBasePath}/Up/",
//             MessageType.Info);
        
//         EditorGUILayout.Space(10);
        
//         // ━━━━ 设置 ━━━━
//         GUILayout.Label("━━━━ 设置 ━━━━", EditorStyles.boldLabel);
        
//         animationName = EditorGUILayout.TextField("动画名称", animationName);
//         totalFrames = EditorGUILayout.IntField("动画总帧数", totalFrames);
//         lastFrame = EditorGUILayout.IntField("最后一帧", lastFrame);
        
//         EditorGUILayout.HelpBox(
//             $"sprite将均匀分布在前{lastFrame}帧\n" +
//             $"最后{totalFrames - lastFrame}帧保持最后一个sprite",
//             MessageType.Info);
        
//         EditorGUILayout.Space(10);
        
//         // ━━━━ 操作按钮 ━━━━
//         GUI.enabled = sliceFolder != null && sliceBaseFolder != null;
        
//         if (GUILayout.Button("批量创建工具动画", GUILayout.Height(40)))
//         {
//             CreateAllAnimations();
//         }
        
//         GUI.enabled = true;
//     }

//     void CreateAllAnimations()
//     {
//         string slicePath = AssetDatabase.GetAssetPath(sliceFolder);
        
//         if (!Directory.Exists(slicePath))
//         {
//             EditorUtility.DisplayDialog("错误", "Slice文件夹路径无效！", "确定");
//             return;
//         }
        
//         // 扫描所有方向
//         string[] directions = { "Down", "Side", "Up" };
//         List<string> foundDirections = new List<string>();
        
//         foreach (string dir in directions)
//         {
//             string dirPath = Path.Combine(slicePath, dir);
//             if (Directory.Exists(dirPath))
//             {
//                 foundDirections.Add(dir);
//             }
//         }
        
//         if (foundDirections.Count == 0)
//         {
//             EditorUtility.DisplayDialog("错误", 
//                 "未找到任何方向文件夹（Down/Side/Up）！\n" +
//                 "请检查Slice文件夹结构。", 
//                 "确定");
//             return;
//         }
        
//         if (!EditorUtility.DisplayDialog("确认",
//             $"找到 {foundDirections.Count} 个方向：\n" +
//             string.Join("\n", foundDirections.Select(d => $"• {d}")) + "\n\n" +
//             "开始批量生成动画？",
//             "确定", "取消"))
//         {
//             return;
//         }
        
//         // 获取Slice_Base路径
//         string baseAssetPath = AssetDatabase.GetAssetPath(sliceBaseFolder);
        
//         // 处理每个方向
//         int totalCount = 0;
        
//         try
//         {
//             foreach (string direction in foundDirections)
//             {
//                 EditorUtility.DisplayProgressBar("批量生成动画", 
//                     $"处理方向: {direction}...", 
//                     (float)foundDirections.IndexOf(direction) / foundDirections.Count);
                
//                 // 为每个方向查找对应的Aseprite文件
//                 Object pivotSource = FindAsepriteForDirection(baseAssetPath, direction);
                
//                 if (pivotSource == null)
//                 {
//                     Debug.LogWarning($"[{direction}] 未找到对应的Aseprite文件（Slice_{direction}），跳过");
//                     continue;
//                 }
                
//                 // 读取该方向的pivot数据
//                 List<Vector2> pivots = GetPivotsFromAseprite(pivotSource);
                
//                 if (pivots.Count == 0)
//                 {
//                     Debug.LogWarning($"[{direction}] 无法读取Pivot数据，跳过");
//                     continue;
//                 }
                
//                 int count = ProcessDirection(slicePath, direction, pivots);
//                 totalCount += count;
//             }
            
//             EditorUtility.ClearProgressBar();
//             AssetDatabase.Refresh();
            
//             EditorUtility.DisplayDialog("完成",
//                 $"✅ 批量生成完成！\n\n" +
//                 $"处理方向数：{foundDirections.Count}\n" +
//                 $"生成动画数：{totalCount}\n" +
//                 $"输出位置：{outputBasePath}",
//                 "确定");
//         }
//         catch (System.Exception e)
//         {
//             EditorUtility.ClearProgressBar();
//             EditorUtility.DisplayDialog("错误", $"生成失败：{e.Message}", "确定");
//             Debug.LogError($"[批量生成] 失败: {e}\n{e.StackTrace}");
//         }
//     }

//     Object FindAsepriteForDirection(string baseAssetPath, string direction)
//     {
//         // 查找命名为 Slice_{direction} 的Aseprite文件
//         string targetName = $"Slice_{direction}";
        
//         // 在Slice_Base文件夹中查找所有资源
//         string[] guids = AssetDatabase.FindAssets("", new[] { baseAssetPath });
        
//         foreach (string guid in guids)
//         {
//             string path = AssetDatabase.GUIDToAssetPath(guid);
//             string fileName = Path.GetFileNameWithoutExtension(path);
            
//             // 匹配 Slice_Down, Slice_Side, Slice_Up
//             if (fileName == targetName)
//             {
//                 // 尝试加载为Texture2D或Sprite
//                 Object asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
//                 if (asset != null)
//                 {
//                     Debug.Log($"  ✅ 找到 {direction} 的Aseprite源: {fileName}");
//                     return asset;
//                 }
                
//                 // 尝试加载第一个Sprite
//                 Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
//                 foreach (var obj in allAssets)
//                 {
//                     if (obj is Sprite)
//                     {
//                         Debug.Log($"  ✅ 找到 {direction} 的Aseprite源: {fileName}");
//                         return obj;
//                     }
//                 }
//             }
//         }
        
//         return null;
//     }

//     int ProcessDirection(string slicePath, string direction, List<Vector2> pivots)
//     {
//         string dirPath = Path.Combine(slicePath, direction);
        
//         // 查找Hand和Axe文件夹
//         string handPath = Path.Combine(dirPath, "Hand");
//         string axePath = Path.Combine(dirPath, "Axe");
        
//         if (!Directory.Exists(handPath))
//         {
//             Debug.LogWarning($"[{direction}] 未找到Hand文件夹，跳过");
//             return 0;
//         }
        
//         if (!Directory.Exists(axePath))
//         {
//             Debug.LogWarning($"[{direction}] 未找到Axe文件夹，跳过");
//             return 0;
//         }
        
//         // 创建输出文件夹
//         string outputDir = Path.Combine(outputBasePath, direction);
//         if (!Directory.Exists(outputDir))
//         {
//             Directory.CreateDirectory(outputDir);
//         }
        
//         Debug.Log($"━━━━ 处理方向: {direction} ━━━━");
        
//         int animCount = 0;
        
//         // 1. 处理Hand（只同步pivot，不生成动画）
//         Texture2D[] handTextures = FindTexturesInFolder(handPath);
//         foreach (Texture2D handTex in handTextures)
//         {
//             ApplyPivotsToTexture(handTex, pivots);
//         }
//         Debug.Log($"  ✅ Hand Pivot同步: {handTextures.Length}个texture");
        
//         // 2. 处理Axe（同步pivot并生成动画）
//         Texture2D[] axeTextures = FindTexturesInFolder(axePath);
        
//         foreach (Texture2D axeTex in axeTextures)
//         {
//             // 同步pivot
//             ApplyPivotsToTexture(axeTex, pivots);
            
//             // 识别品质
//             int quality = ExtractQualityFromName(axeTex.name);
            
//             // 生成动画名称
//             string animName = $"{animationName}_{direction}_Clip_0_{quality}";
            
//             // 创建动画
//             CreateAnimationClipFromTexture(axeTex, outputDir, animName);
            
//             animCount++;
//         }
        
//         Debug.Log($"  ✅ {direction} 完成: {animCount}个动画");
        
//         return animCount;
//     }

//     Texture2D[] FindTexturesInFolder(string folderPath)
//     {
//         List<Texture2D> textures = new List<Texture2D>();
        
//         string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        
//         foreach (string guid in guids)
//         {
//             string path = AssetDatabase.GUIDToAssetPath(guid);
//             Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
//             if (tex != null)
//             {
//                 textures.Add(tex);
//             }
//         }
        
//         return textures.OrderBy(t => t.name).ToArray();
//     }

//     int ExtractQualityFromName(string name)
//     {
//         // 识别 Axe_0, Axe_1 等
//         if (name.Contains("_"))
//         {
//             string[] parts = name.Split('_');
//             if (parts.Length >= 2 && int.TryParse(parts[parts.Length - 1], out int quality))
//             {
//                 return quality;
//             }
//         }
//         return 0;
//     }

//     List<Vector2> GetPivotsFromAseprite(Object asepriteFile)
//     {
//         List<Vector2> pivots = new List<Vector2>();
        
//         if (asepriteFile == null)
//         {
//             Debug.LogWarning("[Pivot读取] 未指定文件");
//             return pivots;
//         }

//         string path = AssetDatabase.GetAssetPath(asepriteFile);
        
//         if (asepriteFile is Sprite)
//         {
//             path = AssetDatabase.GetAssetPath((asepriteFile as Sprite).texture);
//         }
        
//         Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
//         List<Sprite> sprites = new List<Sprite>();
        
//         foreach (var asset in allAssets)
//         {
//             if (asset is Sprite sprite)
//             {
//                 sprites.Add(sprite);
//             }
//         }
        
//         if (sprites.Count == 0)
//         {
//             Debug.LogError($"[Pivot读取] 未找到任何Sprite！路径: {path}");
//             return pivots;
//         }
        
//         sprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        
//         Debug.Log($"✅ 读取Pivot（归一化坐标）: {asepriteFile.name} ({sprites.Count}帧)");
        
//         foreach (var sprite in sprites)
//         {
//             Vector2 pivotPixels = sprite.pivot;
//             Vector2 spriteSize = sprite.rect.size;
//             Vector2 pivotNormalized = new Vector2(
//                 pivotPixels.x / spriteSize.x,
//                 pivotPixels.y / spriteSize.y
//             );
            
//             pivots.Add(pivotNormalized);
//             Debug.Log($"  {sprite.name}: 像素({pivotPixels.x:F2}, {pivotPixels.y:F2}) → 归一化({pivotNormalized.x:F10}, {pivotNormalized.y:F10}), Size=({sprite.rect.width}x{sprite.rect.height})");
//         }
        
//         return pivots;
//     }

//     void ApplyPivotsToTexture(Texture2D texture, List<Vector2> pivots)
//     {
//         string path = AssetDatabase.GetAssetPath(texture);
        
//         TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        
//         if (importer == null)
//         {
//             Debug.LogError($"[Pivot应用] 无法获取TextureImporter: {texture.name}");
//             return;
//         }

//         if (!importer.isReadable)
//         {
//             importer.isReadable = true;
//         }

//         var dataProviderFactories = new SpriteDataProviderFactories();
//         dataProviderFactories.Init();
//         var dataProvider = dataProviderFactories.GetSpriteEditorDataProviderFromObject(importer);
        
//         if (dataProvider == null)
//         {
//             Debug.LogError($"[Pivot应用] 无法获取ISpriteEditorDataProvider: {texture.name}");
//             return;
//         }
        
//         dataProvider.InitSpriteEditorDataProvider();
//         var spriteRects = dataProvider.GetSpriteRects();
        
//         if (spriteRects == null || spriteRects.Length == 0)
//         {
//             Debug.LogWarning($"[Pivot应用] {texture.name} 没有sprite数据！");
//             return;
//         }

//         var sortedRects = spriteRects.OrderBy(r => r.name).ToArray();
        
//         int count = Mathf.Min(sortedRects.Length, pivots.Count);

//         for (int i = 0; i < count; i++)
//         {
//             sortedRects[i].pivot = pivots[i];
//             sortedRects[i].alignment = SpriteAlignment.Custom;
//         }

//         dataProvider.SetSpriteRects(sortedRects);
//         dataProvider.Apply();
        
//         importer.SaveAndReimport();
//     }

//     void CreateAnimationClipFromTexture(Texture2D texture, string outputPath, string animName)
//     {
//         string path = AssetDatabase.GetAssetPath(texture);
//         Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
//         List<Sprite> sprites = new List<Sprite>();
        
//         foreach (var asset in allAssets)
//         {
//             if (asset is Sprite sprite)
//             {
//                 sprites.Add(sprite);
//             }
//         }
        
//         if (sprites.Count == 0)
//         {
//             Debug.LogWarning($"[创建动画] {texture.name} 没有sprite！");
//             return;
//         }

//         sprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

//         string clipPath = $"{outputPath}/{animName}.anim";
        
//         AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
//         bool isNew = clip == null;
        
//         if (isNew)
//         {
//             clip = new AnimationClip();
//         }
//         else
//         {
//             clip.ClearCurves();
//         }

//         EditorCurveBinding spriteBinding = new EditorCurveBinding
//         {
//             type = typeof(SpriteRenderer),
//             path = "",
//             propertyName = "m_Sprite"
//         };

//         ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
        
//         for (int i = 0; i < sprites.Count; i++)
//         {
//             float time = (i * lastFrame / (float)(sprites.Count - 1)) / 100f;
            
//             keyframes[i] = new ObjectReferenceKeyframe
//             {
//                 time = time,
//                 value = sprites[i]
//             };
//         }

//         AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

//         AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
//         settings.loopTime = false;
//         AnimationUtility.SetAnimationClipSettings(clip, settings);

//         if (isNew)
//         {
//             AssetDatabase.CreateAsset(clip, clipPath);
//         }
//         else
//         {
//             EditorUtility.SetDirty(clip);
//         }
//     }

//     string ConvertToAssetPath(string absolutePath)
//     {
//         if (string.IsNullOrEmpty(absolutePath))
//             return "";
        
//         string dataPath = Application.dataPath;
        
//         if (absolutePath.StartsWith(dataPath))
//         {
//             return "Assets" + absolutePath.Substring(dataPath.Length);
//         }
        
//         return absolutePath;
//     }
// }
