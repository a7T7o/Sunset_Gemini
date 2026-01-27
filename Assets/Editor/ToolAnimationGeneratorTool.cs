using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// 工具动画一键生成器（合并版）
/// 功能：从Aseprite源文件读取Pivot → 应用到新动画Sprite → 生成动画剪辑 → 生成动画控制器
/// 支持任意动作类型（Slice/Crush/Pierce等），自动识别文件夹名称
/// </summary>
public class ToolAnimationGeneratorTool : EditorWindow
{
    [MenuItem("Tools/手持三向生成流程/工具动画一键生成")]
    static void ShowWindow()
    {
        var window = GetWindow<ToolAnimationGeneratorTool>("工具动画一键生成");
        window.minSize = new Vector2(550, 700);
        window.Show();
    }

    // ━━━━ 输入 ━━━━
    DefaultAsset asepriteSourceFolder;  // 包含原始Aseprite文件的文件夹（含Down/Side/Up子文件夹）
    DefaultAsset newSpriteSheetFolder;  // 包含新动画Sprite的文件夹（含Down/Side/Up子文件夹）
    
    // ━━━━ 输出 ━━━━
    string animClipOutputPath = "Assets/Animations/Tools/Clips";
    string controllerOutputPath = "Assets/Animations/Tools/Controllers";
    
    // ━━━━ 设置 ━━━━
    int totalFrames = 100;
    int lastFrame = 80;
    int itemId = 0;  // 物品ID（用于动画状态名：{Action}_{Dir}_Clip_{ItemID}）
    string itemName = "";  // 物品名称（用于控制器命名：{Action}_Controller_{ItemID}_{ItemName}）
    
    // ━━━━ 动作类型 ━━━━
    string[] actionTypeOptions = { "Slice", "Crush", "Pierce", "Watering", "Fish" };
    int selectedActionTypeIndex = 0;  // 手动选择的动作类型索引
    bool useManualActionType = false;  // 是否使用手动选择的动作类型
    
    // ━━━━ 运行时状态 ━━━━
    string detectedActionType = "";  // 自动检测的动作类型
    Vector2 scrollPos;

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        DrawHeader();
        DrawInputSection();
        DrawOutputSection();
        DrawSettingsSection();
        DrawDetectionSection();
        DrawActionButtons();
        
        EditorGUILayout.EndScrollView();
    }

    void DrawHeader()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("━━━━ 工具动画一键生成器 ━━━━", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "✅ 可选：从Aseprite源文件读取Pivot信息并应用\n" +
            "✅ 自动生成动画剪辑（.anim）\n" +
            "✅ 自动生成动画控制器（.controller）\n" +
            "✅ 支持任意动作类型（Slice/Crush/Pierce等）\n" +
            "💡 如果不上传源文件，将直接使用新Sprite文件夹生成动画",
            MessageType.Info);
        EditorGUILayout.Space(10);
    }

    void DrawInputSection()
    {
        EditorGUILayout.LabelField("━━━━ 输入文件夹 ━━━━", EditorStyles.boldLabel);
        
        // Aseprite源文件夹（可选）
        asepriteSourceFolder = EditorGUILayout.ObjectField(
            "Aseprite源文件夹（可选）", 
            asepriteSourceFolder, 
            typeof(DefaultAsset), 
            false) as DefaultAsset;
        
        EditorGUILayout.HelpBox(
            "【可选】包含原始Aseprite文件的文件夹（如 Slice_Base、Crush_Base）\n" +
            "用于读取Pivot信息并应用到新Sprite\n" +
            "如果不上传，将直接使用新Sprite文件夹生成动画（不同步Pivot）",
            MessageType.Info);
        
        EditorGUILayout.Space(5);
        
        // 新动画Sprite文件夹（必填）
        newSpriteSheetFolder = EditorGUILayout.ObjectField(
            "新动画Sprite文件夹（必填）", 
            newSpriteSheetFolder, 
            typeof(DefaultAsset), 
            false) as DefaultAsset;
        
        EditorGUILayout.HelpBox(
            "【必填】包含新动画Sprite的文件夹\n" +
            "需包含 Down/Side/Up 子文件夹\n" +
            "每个子文件夹内有 Hand 和 工具（如Axe）子文件夹",
            MessageType.Info);
        
        EditorGUILayout.Space(10);
    }

    void DrawOutputSection()
    {
        EditorGUILayout.LabelField("━━━━ 输出路径 ━━━━", EditorStyles.boldLabel);
        
        // 动画剪辑输出路径
        EditorGUILayout.BeginHorizontal();
        animClipOutputPath = EditorGUILayout.TextField("动画剪辑输出", animClipOutputPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择动画剪辑输出文件夹", animClipOutputPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                animClipOutputPath = ConvertToAssetPath(path);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // 控制器输出路径
        EditorGUILayout.BeginHorizontal();
        controllerOutputPath = EditorGUILayout.TextField("控制器输出", controllerOutputPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择控制器输出文件夹", controllerOutputPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                controllerOutputPath = ConvertToAssetPath(path);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
    }

    void DrawSettingsSection()
    {
        EditorGUILayout.LabelField("━━━━ 动画设置 ━━━━", EditorStyles.boldLabel);
        
        itemId = EditorGUILayout.IntField("物品ID (itemID)", itemId);
        itemName = EditorGUILayout.TextField("物品名称 (itemName)", itemName);
        
        EditorGUILayout.Space(5);
        
        // 动作类型选择（手动/自动）
        EditorGUILayout.BeginHorizontal();
        useManualActionType = EditorGUILayout.Toggle("手动指定动作类型", useManualActionType);
        if (useManualActionType)
        {
            selectedActionTypeIndex = EditorGUILayout.Popup(selectedActionTypeIndex, actionTypeOptions);
        }
        EditorGUILayout.EndHorizontal();
        
        // 获取当前使用的动作类型
        string currentActionType = useManualActionType ? actionTypeOptions[selectedActionTypeIndex] : detectedActionType;
        
        EditorGUILayout.HelpBox(
            "物品ID用于动画状态名命名（简化版，不含品质）\n" +
            $"动画格式：{currentActionType}_{{Dir}}_Clip_{itemId}\n" +
            $"控制器格式：{currentActionType}_Controller_{itemId}_{itemName}\n" +
            $"例如：{currentActionType}_Down_Clip_{itemId}（ItemID={itemId}的工具）\n" +
            $"控制器：{currentActionType}_Controller_{itemId}_{itemName}\n" +
            "注意：每个品质的工具都是独立 ItemID",
            MessageType.Info);
        
        EditorGUILayout.Space(5);
        
        totalFrames = EditorGUILayout.IntField("动画总帧数", totalFrames);
        lastFrame = EditorGUILayout.IntField("最后一帧位置", lastFrame);
        
        EditorGUILayout.HelpBox(
            $"Sprite将均匀分布在前 {lastFrame} 帧\n" +
            $"最后 {totalFrames - lastFrame} 帧保持最后一个Sprite",
            MessageType.Info);
        
        EditorGUILayout.Space(10);
    }

    void DrawDetectionSection()
    {
        EditorGUILayout.LabelField("━━━━ 自动检测结果 ━━━━", EditorStyles.boldLabel);
        
        if (newSpriteSheetFolder != null)
        {
            // 自动检测动作类型
            detectedActionType = DetectActionType();
            
            // 获取最终使用的动作类型
            string finalActionType = GetFinalActionType();
            
            if (!string.IsNullOrEmpty(finalActionType))
            {
                string pivotInfo = asepriteSourceFolder != null ? "（将同步Pivot）" : "（不同步Pivot）";
                string sourceInfo = useManualActionType ? "（手动指定）" : "（自动检测）";
                EditorGUILayout.HelpBox(
                    $"✅ 动作类型：{finalActionType} {sourceInfo} {pivotInfo}\n" +
                    $"将生成：{finalActionType}_Down_Clip_{itemId}.anim 等文件",
                    MessageType.Info);
                
                // 显示检测到的方向
                var directions = DetectDirections();
                if (directions.Count > 0)
                {
                    EditorGUILayout.LabelField($"检测到方向：{string.Join(", ", directions)}");
                }
                
                // 显示检测到的工具类型
                var toolTypes = DetectToolTypes();
                if (toolTypes.Count > 0)
                {
                    EditorGUILayout.LabelField($"检测到工具：{string.Join(", ", toolTypes)}");
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "⚠️ 无法自动检测动作类型\n" +
                    "请勾选「手动指定动作类型」并选择动作类型\n" +
                    "或确保文件夹命名包含动作类型（如 Slice_Base、Crush_Base）",
                    MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("请先选择新动画Sprite文件夹（必填）", MessageType.Warning);
        }
        
        EditorGUILayout.Space(10);
    }
    
    /// <summary>
    /// 获取最终使用的动作类型（手动优先，否则自动检测）
    /// </summary>
    string GetFinalActionType()
    {
        if (useManualActionType)
        {
            return actionTypeOptions[selectedActionTypeIndex];
        }
        return detectedActionType;
    }

    void DrawActionButtons()
    {
        // 获取最终使用的动作类型
        string finalActionType = GetFinalActionType();
        
        // 只需要新Sprite文件夹和有效的动作类型即可启用按钮
        GUI.enabled = newSpriteSheetFolder != null && !string.IsNullOrEmpty(finalActionType);
        
        EditorGUILayout.LabelField("━━━━ 操作 ━━━━", EditorStyles.boldLabel);
        
        if (GUILayout.Button("一键生成（动画 + 控制器）", GUILayout.Height(50)))
        {
            ExecuteFullGeneration();
        }
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("仅生成动画剪辑", GUILayout.Height(30)))
        {
            ExecuteAnimationGeneration();
        }
        
        if (GUILayout.Button("仅生成控制器", GUILayout.Height(30)))
        {
            ExecuteControllerGeneration();
        }
        
        EditorGUILayout.EndHorizontal();
        
        GUI.enabled = true;
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 检测方法
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    string DetectActionType()
    {
        // 优先从 Aseprite 源文件夹检测
        if (asepriteSourceFolder != null)
        {
            string folderPath = AssetDatabase.GetAssetPath(asepriteSourceFolder);
            
            // 直接在文件夹内查找 {ActionType}_{Direction} 格式的文件（不带-Sheet后缀）
            string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                
                // 跳过Sheet文件和子文件夹
                if (fileName.Contains("-Sheet") || fileName.Contains("_Sheet"))
                    continue;
                
                // 尝试解析 {ActionType}_{Direction} 格式
                // 例如：Crush_Down, Slice_Side, Pierce_Up
                if (fileName.Contains("_"))
                {
                    string[] parts = fileName.Split('_');
                    if (parts.Length >= 2)
                    {
                        string possibleAction = parts[0];
                        string possibleDir = parts[1];
                        
                        // 验证方向部分
                        if (possibleDir.Equals("Down", System.StringComparison.OrdinalIgnoreCase) ||
                            possibleDir.Equals("Side", System.StringComparison.OrdinalIgnoreCase) ||
                            possibleDir.Equals("Up", System.StringComparison.OrdinalIgnoreCase))
                        {
                            return possibleAction;
                        }
                    }
                }
            }
        }
        
        // 如果没有源文件夹，尝试从新Sprite文件夹名称检测
        if (newSpriteSheetFolder != null)
        {
            string folderName = newSpriteSheetFolder.name;
            
            // 尝试从文件夹名称提取动作类型
            // 例如：Slice_Axe_0 -> Slice, Crush_Pickaxe -> Crush
            if (folderName.Contains("_"))
            {
                string[] parts = folderName.Split('_');
                string possibleAction = parts[0];
                
                // 验证是否是已知的动作类型
                string[] knownActions = { "Slice", "Crush", "Pierce", "Watering", "Fish" };
                foreach (string action in knownActions)
                {
                    if (possibleAction.Equals(action, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return action;
                    }
                }
            }
            
            // 如果文件夹名称不包含动作类型，检查子文件夹内的Texture名称
            string folderPath = AssetDatabase.GetAssetPath(newSpriteSheetFolder);
            var directions = new[] { "Down", "Side", "Up" };
            
            foreach (string dir in directions)
            {
                string dirPath = Path.Combine(folderPath, dir);
                if (!Directory.Exists(dirPath)) continue;
                
                // 检查子文件夹（排除Hand）
                string[] subDirs = Directory.GetDirectories(dirPath);
                foreach (string subDir in subDirs)
                {
                    string subDirName = Path.GetFileName(subDir);
                    if (subDirName.Equals("Hand", System.StringComparison.OrdinalIgnoreCase))
                        continue;
                    
                    // 从工具文件夹名称推断动作类型
                    // 重要：Hoe（锄头）使用 Crush，不是 Pierce！
                    // Axe/Sickle -> Slice (挥砍)
                    // Pickaxe/Hoe -> Crush (挖掘)
                    // Sword -> Pierce (刺出)
                    if (subDirName.Contains("Axe") && !subDirName.Contains("Pick"))
                        return "Slice";
                    if (subDirName.Contains("Pickaxe") || subDirName.Contains("Pick"))
                        return "Crush";
                    if (subDirName.Contains("Hoe") || subDirName.Contains("Shovel"))
                        return "Crush";  // 锄头使用 Crush（挖掘动作），不是 Pierce！
                    if (subDirName.Contains("Sword"))
                        return "Pierce"; // 只有长剑使用 Pierce（刺出动作）
                    if (subDirName.Contains("Water"))
                        return "Watering";
                    if (subDirName.Contains("Fish") || subDirName.Contains("Rod"))
                        return "Fish";
                    if (subDirName.Contains("Sickle") || subDirName.Contains("Scythe"))
                        return "Slice";
                }
            }
        }
        
        return "";
    }

    List<string> DetectDirections()
    {
        List<string> directions = new List<string>();
        
        if (newSpriteSheetFolder == null) return directions;
        
        string folderPath = AssetDatabase.GetAssetPath(newSpriteSheetFolder);
        string[] possibleDirs = { "Down", "Side", "Up" };
        
        foreach (string dir in possibleDirs)
        {
            string dirPath = Path.Combine(folderPath, dir);
            if (Directory.Exists(dirPath))
            {
                directions.Add(dir);
            }
        }
        
        return directions;
    }

    List<string> DetectToolTypes()
    {
        List<string> toolTypes = new List<string>();
        
        if (newSpriteSheetFolder == null) return toolTypes;
        
        string folderPath = AssetDatabase.GetAssetPath(newSpriteSheetFolder);
        var directions = DetectDirections();
        
        if (directions.Count == 0) return toolTypes;
        
        // 检查第一个方向下的子文件夹（排除Hand）
        string firstDirPath = Path.Combine(folderPath, directions[0]);
        
        if (Directory.Exists(firstDirPath))
        {
            string[] subDirs = Directory.GetDirectories(firstDirPath);
            
            foreach (string subDir in subDirs)
            {
                string subDirName = Path.GetFileName(subDir);
                if (!subDirName.Equals("Hand", System.StringComparison.OrdinalIgnoreCase))
                {
                    toolTypes.Add(subDirName);
                }
            }
        }
        
        return toolTypes;
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 执行方法
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    void ExecuteFullGeneration()
    {
        var directions = DetectDirections();
        var toolTypes = DetectToolTypes();
        string finalActionType = GetFinalActionType();
        
        string pivotInfo = asepriteSourceFolder != null ? "1. 同步Pivot并生成动画剪辑" : "1. 直接生成动画剪辑（不同步Pivot）";
        
        if (!EditorUtility.DisplayDialog("确认",
            $"动作类型：{finalActionType}\n" +
            $"检测到方向：{string.Join(", ", directions)}\n" +
            $"检测到工具：{string.Join(", ", toolTypes)}\n" +
            $"Pivot同步：{(asepriteSourceFolder != null ? "是" : "否")}\n\n" +
            "开始一键生成？\n" +
            $"{pivotInfo}\n" +
            "2. 生成动画控制器",
            "确定", "取消"))
        {
            return;
        }
        
        try
        {
            // 第一步：生成动画剪辑
            int animCount = ExecuteAnimationGenerationInternal();
            
            // 第二步：生成控制器
            int controllerCount = ExecuteControllerGenerationInternal();
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("完成",
                $"✅ 一键生成完成！\n\n" +
                $"动作类型：{finalActionType}\n" +
                $"生成动画：{animCount} 个\n" +
                $"生成控制器：{controllerCount} 个\n\n" +
                $"动画输出：{animClipOutputPath}\n" +
                $"控制器输出：{controllerOutputPath}",
                "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("错误", $"生成失败：{e.Message}", "确定");
            Debug.LogError($"[一键生成] 失败: {e}\n{e.StackTrace}");
        }
    }

    void ExecuteAnimationGeneration()
    {
        string finalActionType = GetFinalActionType();
        
        if (!EditorUtility.DisplayDialog("确认",
            $"生成动画剪辑？\n\n" +
            $"动作类型：{finalActionType}\n" +
            $"输出路径：{animClipOutputPath}",
            "确定", "取消"))
        {
            return;
        }
        
        try
        {
            int count = ExecuteAnimationGenerationInternal();
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("完成",
                $"✅ 动画剪辑生成完成！\n\n" +
                $"生成数量：{count} 个\n" +
                $"输出位置：{animClipOutputPath}",
                "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("错误", $"生成失败：{e.Message}", "确定");
            Debug.LogError($"[动画生成] 失败: {e}\n{e.StackTrace}");
        }
    }

    void ExecuteControllerGeneration()
    {
        string finalActionType = GetFinalActionType();
        
        if (!EditorUtility.DisplayDialog("确认",
            $"生成动画控制器？\n\n" +
            $"动作类型：{finalActionType}\n" +
            $"输出路径：{controllerOutputPath}",
            "确定", "取消"))
        {
            return;
        }
        
        try
        {
            int count = ExecuteControllerGenerationInternal();
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("完成",
                $"✅ 控制器生成完成！\n\n" +
                $"生成数量：{count} 个\n" +
                $"输出位置：{controllerOutputPath}",
                "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("错误", $"生成失败：{e.Message}", "确定");
            Debug.LogError($"[控制器生成] 失败: {e}\n{e.StackTrace}");
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 动画生成核心逻辑（来自 LayerAnimSetupTool）
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    int ExecuteAnimationGenerationInternal()
    {
        string spriteFolderPath = AssetDatabase.GetAssetPath(newSpriteSheetFolder);
        string asepriteFolderPath = asepriteSourceFolder != null ? AssetDatabase.GetAssetPath(asepriteSourceFolder) : null;
        string finalActionType = GetFinalActionType();
        
        var directions = DetectDirections();
        int totalCount = 0;
        
        bool hasPivotSource = asepriteSourceFolder != null;
        
        foreach (string direction in directions)
        {
            EditorUtility.DisplayProgressBar("生成动画", 
                $"处理方向: {direction}...", 
                (float)directions.IndexOf(direction) / directions.Count);
            
            List<Vector2> pivots = null;
            
            // 如果有源文件夹，尝试读取 Pivot
            if (hasPivotSource)
            {
                Object pivotSource = FindAsepriteForDirection(asepriteFolderPath, direction, finalActionType);
                
                if (pivotSource != null)
                {
                    pivots = GetPivotsFromAseprite(pivotSource);
                    if (pivots.Count == 0)
                    {
                        Debug.LogWarning($"[{direction}] 无法读取Pivot数据，将不同步Pivot");
                        pivots = null;
                    }
                }
                else
                {
                    Debug.LogWarning($"[{direction}] 未找到对应的Aseprite文件（{finalActionType}_{direction}），将不同步Pivot");
                }
            }
            
            // 处理该方向（pivots 可以为 null，表示不同步 Pivot）
            int count = ProcessDirectionForAnimation(spriteFolderPath, direction, pivots, finalActionType);
            totalCount += count;
        }
        
        return totalCount;
    }

    Object FindAsepriteForDirection(string baseAssetPath, string direction, string actionType)
    {
        // 直接在文件夹内查找 {ActionType}_{Direction} 格式的文件（不带-Sheet后缀）
        // 例如：Crush_Down, Slice_Side, Pierce_Up
        string targetName = $"{actionType}_{direction}";
        
        string[] guids = AssetDatabase.FindAssets("", new[] { baseAssetPath });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            
            // 精确匹配 {ActionType}_{Direction}，排除 -Sheet 后缀的文件
            if (fileName.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
            {
                // 尝试加载为Texture2D
                Object asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (asset != null)
                {
                    Debug.Log($"  ✅ 找到 {direction} 的Aseprite源: {fileName}");
                    return asset;
                }
                
                // 尝试加载第一个Sprite
                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var obj in allAssets)
                {
                    if (obj is Sprite)
                    {
                        Debug.Log($"  ✅ 找到 {direction} 的Aseprite源: {fileName}");
                        return obj;
                    }
                }
            }
        }
        
        Debug.LogWarning($"[查找Aseprite] 未找到 {targetName}，请确保文件夹内有该文件");
        return null;
    }

    int ProcessDirectionForAnimation(string spriteFolderPath, string direction, List<Vector2> pivots, string actionType)
    {
        string dirPath = Path.Combine(spriteFolderPath, direction);
        
        Debug.Log($"━━━━ 处理方向: {direction} ━━━━");
        Debug.Log($"  📁 方向文件夹路径: {dirPath}");
        
        if (!Directory.Exists(dirPath))
        {
            Debug.LogWarning($"  ❌ 方向文件夹不存在: {dirPath}");
            return 0;
        }
        
        // 创建输出文件夹
        string outputDir = Path.Combine(animClipOutputPath, direction);
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }
        
        int totalAnimCount = 0;
        
        // 检查是否有子文件夹
        string[] subDirs = Directory.GetDirectories(dirPath);
        Debug.Log($"  📁 子文件夹数量: {subDirs.Length}");
        
        bool hasPivots = pivots != null && pivots.Count > 0;
        
        if (subDirs.Length > 0)
        {
            // 模式A：有子文件夹结构（Hand + 工具文件夹）
            string handPath = Path.Combine(dirPath, "Hand");
            
            // 处理Hand（只同步pivot，不生成动画）
            if (Directory.Exists(handPath) && hasPivots)
            {
                Texture2D[] handTextures = FindTexturesInFolder(handPath);
                foreach (Texture2D handTex in handTextures)
                {
                    ApplyPivotsToTexture(handTex, pivots);
                }
                Debug.Log($"  ✅ Hand Pivot同步: {handTextures.Length}个texture");
            }
            
            // 收集所有工具文件夹（非Hand），按名称排序
            var toolFolders = subDirs
                .Where(d => !Path.GetFileName(d).Equals("Hand", System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => ExtractIndexFromFolderName(Path.GetFileName(d)))
                .ToList();
            
            Debug.Log($"  📁 工具文件夹数量: {toolFolders.Count}");
            
            // 处理每个工具文件夹，每个文件夹对应一个品质/ItemID
            for (int i = 0; i < toolFolders.Count; i++)
            {
                string subDir = toolFolders[i];
                string subDirName = Path.GetFileName(subDir);
                
                // 从文件夹名称提取品质索引（如 Axe_0 → 0, Axe_5 → 5）
                int qualityIndex = ExtractIndexFromFolderName(subDirName);
                
                // 计算当前品质的 ItemID = 起始ID + 品质索引
                int currentItemId = itemId + qualityIndex;
                
                Texture2D[] toolTextures = FindTexturesInFolder(subDir);
                Debug.Log($"  📁 工具文件夹 [{subDirName}] (品质索引={qualityIndex}) → ItemID={currentItemId}: {toolTextures.Length}个Texture");
                
                // 每个工具文件夹应该只有一个 Texture（sprite sheet）
                // 如果有多个，只使用第一个
                if (toolTextures.Length == 0)
                {
                    Debug.LogWarning($"    ⚠️ 工具文件夹 [{subDirName}] 内没有找到 Texture！");
                    continue;
                }
                
                Texture2D toolTex = toolTextures[0];
                
                // 只有在有 Pivot 数据时才同步
                if (hasPivots)
                {
                    ApplyPivotsToTexture(toolTex, pivots);
                }
                
                // 格式：{ActionType}_{Direction}_Clip_{ItemID}
                // ItemID = 起始ID + 品质索引
                string animName = $"{actionType}_{direction}_Clip_{currentItemId}";
                Debug.Log($"    创建动画: {animName} (来自 {toolTex.name})");
                CreateAnimationClipFromTexture(toolTex, outputDir, animName);
                totalAnimCount++;
            }
        }
        else
        {
            // 模式B：扁平结构 - Texture直接在方向文件夹内（如 Axe_0.png, Axe_1.png, ...）
            Debug.Log($"  📁 扁平结构模式：直接在方向文件夹内查找Texture");
            
            Texture2D[] textures = FindTexturesInFolder(dirPath);
            Debug.Log($"  📁 找到 {textures.Length} 个Texture");
            
            // 按文件名中的索引排序
            var sortedTextures = textures.OrderBy(t => ExtractIndexFromFolderName(t.name)).ToList();
            
            foreach (Texture2D tex in sortedTextures)
            {
                // 只有在有 Pivot 数据时才同步
                if (hasPivots)
                {
                    ApplyPivotsToTexture(tex, pivots);
                }
                
                // 从 Texture 文件名提取品质索引（如 Axe_0 → 0, Axe_5 → 5）
                int qualityIndex = ExtractIndexFromFolderName(tex.name);
                
                // 计算当前品质的 ItemID = 起始ID + 品质索引
                int currentItemId = itemId + qualityIndex;
                
                // 格式：{ActionType}_{Direction}_Clip_{ItemID}
                string animName = $"{actionType}_{direction}_Clip_{currentItemId}";
                
                Debug.Log($"    创建动画: {animName} (来自 {tex.name}, 品质索引={qualityIndex})");
                
                // 创建动画
                CreateAnimationClipFromTexture(tex, outputDir, animName);
                
                totalAnimCount++;
            }
        }
        
        Debug.Log($"  ✅ {direction} 完成: {totalAnimCount}个动画");
        
        return totalAnimCount;
    }
    
    /// <summary>
    /// 从文件夹名称提取索引（如 Axe_0 → 0, Axe_5 → 5）
    /// </summary>
    int ExtractIndexFromFolderName(string folderName)
    {
        // 尝试从末尾提取数字
        if (folderName.Contains("_"))
        {
            string[] parts = folderName.Split('_');
            if (parts.Length >= 2 && int.TryParse(parts[parts.Length - 1], out int index))
            {
                return index;
            }
        }
        return 0;
    }

    /// <summary>
    /// 查找文件夹内的 Texture2D（不递归搜索子目录）
    /// </summary>
    Texture2D[] FindTexturesInFolder(string folderPath)
    {
        List<Texture2D> textures = new List<Texture2D>();
        
        // 使用 AssetDatabase.FindAssets 会递归搜索，所以我们需要过滤结果
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // 只包含直接在 folderPath 下的文件（不包含子目录中的文件）
            string parentDir = Path.GetDirectoryName(path).Replace("\\", "/");
            string normalizedFolderPath = folderPath.Replace("\\", "/");
            
            if (!parentDir.Equals(normalizedFolderPath, System.StringComparison.OrdinalIgnoreCase))
            {
                continue; // 跳过子目录中的文件
            }
            
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                textures.Add(tex);
            }
        }
        
        return textures.OrderBy(t => t.name).ToArray();
    }

    int ExtractQualityFromName(string name)
    {
        // 识别 Axe_0, Axe_1, Tool_0 等
        if (name.Contains("_"))
        {
            string[] parts = name.Split('_');
            if (parts.Length >= 2 && int.TryParse(parts[parts.Length - 1], out int quality))
            {
                return quality;
            }
        }
        return 0;
    }

    List<Vector2> GetPivotsFromAseprite(Object asepriteFile)
    {
        List<Vector2> pivots = new List<Vector2>();
        
        if (asepriteFile == null)
        {
            Debug.LogWarning("[Pivot读取] 未指定文件");
            return pivots;
        }

        string path = AssetDatabase.GetAssetPath(asepriteFile);
        
        if (asepriteFile is Sprite)
        {
            path = AssetDatabase.GetAssetPath((asepriteFile as Sprite).texture);
        }
        
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        List<Sprite> sprites = new List<Sprite>();
        
        foreach (var asset in allAssets)
        {
            if (asset is Sprite sprite)
            {
                sprites.Add(sprite);
            }
        }
        
        if (sprites.Count == 0)
        {
            Debug.LogError($"[Pivot读取] 未找到任何Sprite！路径: {path}");
            return pivots;
        }
        
        sprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        
        Debug.Log($"✅ 读取Pivot（归一化坐标）: {asepriteFile.name} ({sprites.Count}帧)");
        
        foreach (var sprite in sprites)
        {
            Vector2 pivotPixels = sprite.pivot;
            Vector2 spriteSize = sprite.rect.size;
            Vector2 pivotNormalized = new Vector2(
                pivotPixels.x / spriteSize.x,
                pivotPixels.y / spriteSize.y
            );
            
            pivots.Add(pivotNormalized);
        }
        
        return pivots;
    }

    void ApplyPivotsToTexture(Texture2D texture, List<Vector2> pivots)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        
        if (importer == null)
        {
            Debug.LogError($"[Pivot应用] 无法获取TextureImporter: {texture.name}");
            return;
        }

        if (!importer.isReadable)
        {
            importer.isReadable = true;
        }

        var dataProviderFactories = new SpriteDataProviderFactories();
        dataProviderFactories.Init();
        var dataProvider = dataProviderFactories.GetSpriteEditorDataProviderFromObject(importer);
        
        if (dataProvider == null)
        {
            Debug.LogError($"[Pivot应用] 无法获取ISpriteEditorDataProvider: {texture.name}");
            return;
        }
        
        dataProvider.InitSpriteEditorDataProvider();
        var spriteRects = dataProvider.GetSpriteRects();
        
        if (spriteRects == null || spriteRects.Length == 0)
        {
            Debug.LogWarning($"[Pivot应用] {texture.name} 没有sprite数据！");
            return;
        }

        var sortedRects = spriteRects.OrderBy(r => r.name).ToArray();
        
        int count = Mathf.Min(sortedRects.Length, pivots.Count);

        for (int i = 0; i < count; i++)
        {
            sortedRects[i].pivot = pivots[i];
            sortedRects[i].alignment = SpriteAlignment.Custom;
        }

        dataProvider.SetSpriteRects(sortedRects);
        dataProvider.Apply();
        
        importer.SaveAndReimport();
    }

    void CreateAnimationClipFromTexture(Texture2D texture, string outputPath, string animName)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        List<Sprite> sprites = new List<Sprite>();
        
        foreach (var asset in allAssets)
        {
            if (asset is Sprite sprite)
            {
                sprites.Add(sprite);
            }
        }
        
        if (sprites.Count == 0)
        {
            Debug.LogWarning($"[创建动画] {texture.name} 没有sprite！");
            return;
        }

        // 按 sprite 在 texture 中的位置排序（从左到右）
        // 使用 rect.x 作为主要排序依据，这样可以正确处理水平排列的 sprite sheet
        sprites.Sort((a, b) => 
        {
            // 首先按 X 坐标排序（从左到右）
            int xCompare = a.rect.x.CompareTo(b.rect.x);
            if (xCompare != 0) return xCompare;
            
            // 如果 X 相同，按 Y 坐标排序（从上到下，注意 Unity 的 Y 是从下往上的，所以要反过来）
            return b.rect.y.CompareTo(a.rect.y);
        });
        
        Debug.Log($"[创建动画] {animName}: {sprites.Count}帧");
        for (int i = 0; i < sprites.Count; i++)
        {
            Debug.Log($"  帧{i}: {sprites[i].name} (x={sprites[i].rect.x}, y={sprites[i].rect.y})");
        }

        string clipPath = $"{outputPath}/{animName}.anim";
        
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        bool isNew = clip == null;
        
        if (isNew)
        {
            clip = new AnimationClip();
        }
        else
        {
            clip.ClearCurves();
        }

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        // 创建关键帧：sprites在前 lastFrame 帧均匀分布
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];

        for (int i = 0; i < sprites.Count; i++)
        {
            // 修复：确保最后一帧在正确的时间位置
            float time;
            if (sprites.Count == 1)
            {
                time = 0f;
            }
            else
            {
                time = (i * (float)lastFrame / (sprites.Count - 1)) / 60f;
            }

            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = time,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (isNew)
        {
            AssetDatabase.CreateAsset(clip, clipPath);
        }
        else
        {
            EditorUtility.SetDirty(clip);
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 控制器生成核心逻辑（来自 SliceAnimControllerTool）
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    class ScanResult
    {
        public int totalClips = 0;
        public HashSet<string> directions = new HashSet<string>();
        public HashSet<int> toolTypes = new HashSet<int>();
    }

    int ExecuteControllerGenerationInternal()
    {
        var scanResult = ScanAnimationFolder();
        
        if (scanResult.totalClips == 0)
        {
            Debug.LogWarning("未检测到有效的动画文件！");
            return 0;
        }
        
        return CreateToolControllers(scanResult);
    }

    ScanResult ScanAnimationFolder()
    {
        ScanResult result = new ScanResult();
        
        // 扫描动画剪辑输出文件夹
        if (!Directory.Exists(animClipOutputPath))
        {
            return result;
        }
        
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animClipOutputPath });
        
        result.totalClips = guids.Length;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            
            // 命名格式：{动作}_{方向}_Clip_{工具类型}_{品质}
            // 例如：Slice_Down_Clip_0_0, Crush_Up_Clip_0_1
            
            // 识别方向
            if (fileName.ToLower().Contains("_down"))
                result.directions.Add("Down");
            else if (fileName.ToLower().Contains("_up"))
                result.directions.Add("Up");
            else if (fileName.ToLower().Contains("_side"))
                result.directions.Add("Side");
            
            // 识别工具类型（简化版：_Clip_{ItemID}）
            if (fileName.Contains("_Clip_"))
            {
                string[] parts = fileName.Split(new string[] { "_Clip_" }, System.StringSplitOptions.None);
                if (parts.Length >= 2)
                {
                    // 简化格式：直接解析 ItemID
                    if (int.TryParse(parts[1], out int toolType))
                    {
                        result.toolTypes.Add(toolType);
                    }
                }
            }
        }
        
        return result;
    }

    int CreateToolControllers(ScanResult scanResult)
    {
        if (!Directory.Exists(controllerOutputPath))
        {
            Directory.CreateDirectory(controllerOutputPath);
        }
        
        string finalActionType = GetFinalActionType();
        
        // 获取所有动画剪辑（所有 ItemID）
        var allClips = GetAllAnimationClipsInFolder();
        
        if (allClips.Count == 0)
        {
            Debug.LogWarning("未找到任何动画剪辑！");
            return 0;
        }
        
        // 生成控制器路径
        // 格式：{ActionType}_Controller_{起始ItemID}_{ItemName}
        string actualItemName = string.IsNullOrEmpty(itemName) ? GetToolName(itemId) : itemName;
        string controllerPath = $"{controllerOutputPath}/{finalActionType}_Controller_{itemId}_{actualItemName}.controller";
        
        // 删除旧的Controller（如果存在）
        if (File.Exists(controllerPath))
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }
        
        // 创建Controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        
        // 添加参数
        controller.AddParameter("State", AnimatorControllerParameterType.Int);
        controller.AddParameter("Direction", AnimatorControllerParameterType.Int);
        controller.AddParameter("ToolItemId", AnimatorControllerParameterType.Int);
        
        // 获取Base Layer
        AnimatorControllerLayer baseLayer = controller.layers[0];
        AnimatorStateMachine stateMachine = baseLayer.stateMachine;
        
        // 创建Idle状态
        AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(300, 0, 0));
        stateMachine.defaultState = idleState;
        
        // 创建所有状态（所有 ItemID × 所有方向）
        CreateAllStatesForAllItems(stateMachine, allClips, scanResult.toolTypes);
        
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"✅ 工具控制器创建: {finalActionType}_Controller_{itemId}_{actualItemName}.controller ({allClips.Count}个动画状态)");
        
        return 1;
    }

    string GetToolName(int toolType)
    {
        switch (toolType)
        {
            case 0: return "Axe";
            case 1: return "Pickaxe";
            case 2: return "Shovel";
            case 3: return "Hoe";
            default: return "Tool" + toolType;
        }
    }

    /// <summary>
    /// 获取输出文件夹中的所有动画剪辑
    /// </summary>
    Dictionary<string, AnimationClip> GetAllAnimationClipsInFolder()
    {
        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
        
        Debug.Log($"[扫描动画] 扫描路径: {animClipOutputPath}");
        
        if (!Directory.Exists(animClipOutputPath))
        {
            Debug.LogWarning($"[扫描动画] 输出文件夹不存在: {animClipOutputPath}");
            return clips;
        }
        
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animClipOutputPath });
        Debug.Log($"[扫描动画] 找到 {guids.Length} 个 AnimationClip 资源");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            
            // 格式：{动作}_{方向}_Clip_{ItemID}
            if (fileName.Contains("_Clip_"))
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                {
                    clips[fileName] = clip;
                    Debug.Log($"[扫描动画] ✅ 加载: {fileName} (路径: {path})");
                }
                else
                {
                    Debug.LogWarning($"[扫描动画] ⚠️ 无法加载: {fileName}");
                }
            }
            else
            {
                Debug.Log($"[扫描动画] ⏭️ 跳过（不符合命名格式）: {fileName}");
            }
        }
        
        Debug.Log($"[扫描动画] 共找到 {clips.Count} 个有效动画剪辑");
        return clips;
    }

    // 辅助类：存储状态信息（简化版）
    class StateInfo
    {
        public AnimatorState state;
        public string stateName;
        public int directionValue;
        public int toolItemId;
    }

    /// <summary>
    /// 为所有 ItemID 创建状态（支持多品质）
    /// </summary>
    void CreateAllStatesForAllItems(AnimatorStateMachine stateMachine, Dictionary<string, AnimationClip> allClips, HashSet<int> itemIds)
    {
        string finalActionType = GetFinalActionType();
        int stateValue = GetStateValueForAction(finalActionType);
        
        Vector3 basePos = new Vector3(400, 0, 0);
        
        var stateInfoList = new List<StateInfo>();
        
        // 按 ItemID 排序，然后按方向排序
        var sortedClips = allClips.OrderBy(c => ExtractItemIdFromClipName(c.Key)).ThenBy(c => c.Key).ToList();
        
        int currentRow = 0;
        int lastItemId = -1;
        
        foreach (var kvp in sortedClips)
        {
            string fileName = kvp.Key;
            AnimationClip clip = kvp.Value;
            
            // 提取 ItemID
            int clipItemId = ExtractItemIdFromClipName(fileName);
            
            // 新的 ItemID 换行
            if (clipItemId != lastItemId)
            {
                currentRow++;
                lastItemId = clipItemId;
            }
            
            // 识别方向
            int directionValue = -1;
            if (fileName.ToLower().Contains("_down")) directionValue = 0;
            else if (fileName.ToLower().Contains("_up")) directionValue = 1;
            else if (fileName.ToLower().Contains("_side")) directionValue = 2;
            
            if (directionValue == -1)
            {
                Debug.LogWarning($"无法识别动画方向: {fileName}");
                continue;
            }
            
            // 计算位置（按 ItemID 行，按方向列）
            Vector3 pos = basePos + new Vector3(directionValue * 180, currentRow * 60, 0);
            
            AnimatorState state = stateMachine.AddState(clip.name, pos);
            state.motion = clip;
            
            stateInfoList.Add(new StateInfo
            {
                state = state,
                stateName = clip.name,
                directionValue = directionValue,
                toolItemId = clipItemId
            });
            
            Debug.Log($"  创建状态: {clip.name} (State={stateValue}, Dir={directionValue}, ItemID={clipItemId})");
        }
        
        // 添加 Any State 转换
        foreach (var stateInfo in stateInfoList)
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(stateInfo.state);
            transition.hasExitTime = false;
            transition.duration = 0;
            transition.canTransitionToSelf = false;
            
            // 条件：State + Direction + ToolItemId
            transition.AddCondition(AnimatorConditionMode.Equals, stateValue, "State");
            transition.AddCondition(AnimatorConditionMode.Equals, stateInfo.directionValue, "Direction");
            transition.AddCondition(AnimatorConditionMode.Equals, stateInfo.toolItemId, "ToolItemId");
        }
        
        Debug.Log($"✅ 创建了 {stateInfoList.Count} 个状态");
    }
    
    /// <summary>
    /// 从动画剪辑名称提取 ItemID
    /// 格式：{Action}_{Direction}_Clip_{ItemID}
    /// </summary>
    int ExtractItemIdFromClipName(string clipName)
    {
        if (clipName.Contains("_Clip_"))
        {
            string[] parts = clipName.Split(new string[] { "_Clip_" }, System.StringSplitOptions.None);
            if (parts.Length >= 2 && int.TryParse(parts[1], out int id))
            {
                return id;
            }
        }
        return 0;
    }

    int GetStateValueForAction(string actionType)
    {
        // 根据动作类型返回对应的State值
        // 参考 AnimState 枚举
        // 重要：Hoe（锄头）使用 Crush (8)，不是 Pierce！
        switch (actionType.ToLower())
        {
            case "slice": return 6;     // Slice = 6 (斧头/镰刀 - 挥砍)
            case "crush": return 8;     // Crush = 8 (镐子/锄头 - 挖掘)
            case "pierce": return 7;    // Pierce = 7 (长剑 - 刺出)
            case "watering": return 10; // Watering = 10 (水壶 - 浇水)
            case "fish": return 9;      // Fish = 9 (鱼竿 - 钓鱼)
            case "hit": return 5;       // Hit = 5 (受击)
            case "collect": return 4;   // Collect = 4 (捡起)
            default: return 6;          // 默认使用Slice
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 工具方法
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    string ConvertToAssetPath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
            return "";
        
        string dataPath = Application.dataPath;
        
        if (absolutePath.StartsWith(dataPath))
        {
            return "Assets" + absolutePath.Substring(dataPath.Length);
        }
        
        return absolutePath;
    }
}
