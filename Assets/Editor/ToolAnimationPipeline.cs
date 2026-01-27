using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// 工具动画一键生成流水线（方案A）
/// 
/// 完整流程：
/// 1. 自动切片 - 智能检测或网格切片
/// 2. 位置排序 + 规范重命名 - 按X坐标排序，命名为 {Action}_{Direction}_{FrameIndex}
/// 3. Pivot同步（可选）- 从源文件读取Pivot并应用
/// 4. 生成动画剪辑 - 按规范名称生成 .anim 文件
/// 5. 生成控制器 - 创建 AnimatorController
/// 
/// 命名规范：
/// - Sprite: {ActionType}_{Direction}_{FrameIndex}  例如: Crush_Down_0, Crush_Down_1
/// - 动画剪辑: {ActionType}_{Direction}_Clip_{ItemID}  例如: Crush_Down_Clip_100
/// - 控制器: {ActionType}_Controller_{ItemID}_{ItemName}  例如: Crush_Controller_100_Hoe
/// </summary>
public class ToolAnimationPipeline : EditorWindow
{
    [MenuItem("Tools/手持三向生成流程/🔧 工具动画流水线（推荐）")]
    static void ShowWindow()
    {
        var window = GetWindow<ToolAnimationPipeline>("工具动画流水线");
        window.minSize = new Vector2(600, 800);
        window.Show();
    }

    #region 输入配置
    
    // 输入
    DefaultAsset spriteSheetFolder;      // 包含 Down/Side/Up 子文件夹的根目录
    DefaultAsset pivotSourceFolder;      // Pivot源文件夹（可选）
    
    #endregion

    #region 输出配置
    
    // 输出
    string animClipOutputPath = "Assets/Animations/Tools/Clips";
    string controllerOutputPath = "Assets/Animations/Tools/Controllers";
    
    #endregion

    #region 动画设置
    
    // 动画设置
    int itemId = 100;                    // 起始物品ID
    string itemName = "Tool";            // 物品名称
    int totalFrames = 100;               // 动画总帧数
    int lastFrame = 80;                  // 最后一帧位置
    
    #endregion

    #region 切片设置
    
    // 切片设置
    enum SliceMode { AutoDetect, Grid }
    SliceMode sliceMode = SliceMode.Grid;
    int gridColumns = 8;                 // 网格列数（帧数）
    int gridRows = 1;                    // 网格行数
    int pixelsPerUnit = 16;              // 每单位像素数
    float mergeDistanceThreshold = 5f;   // 相邻区域合并距离阈值（像素）
    
    #endregion

    #region 动作类型
    
    // 动作类型
    string[] actionTypeOptions = { "Slice", "Crush", "Pierce", "Watering", "Fish" };
    int selectedActionTypeIndex = 1;     // 默认 Crush
    bool autoDetectActionType = true;    // 自动检测动作类型
    string detectedActionType = "";
    
    #endregion

    #region 流程控制
    
    // 流程控制
    bool step1_Slice = true;             // 步骤1：切片
    bool step2_Rename = true;            // 步骤2：重命名
    bool step3_Pivot = false;            // 步骤3：Pivot同步（可选）
    bool step4_Animation = true;         // 步骤4：生成动画
    bool step5_Controller = true;        // 步骤5：生成控制器
    bool step6_SyncToSO = false;         // 步骤6：同步到SO（可选）
    
    #endregion

    #region SO同步配置
    
    // SO同步配置
    string toolSOFolder = "Assets/111_Data/Items/Tools";  // ToolData SO 文件夹路径
    
    #endregion

    Vector2 scrollPos;

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        DrawHeader();
        DrawInputSection();
        DrawSliceSettings();
        DrawAnimationSettings();
        DrawOutputSection();
        DrawPipelineControl();
        DrawPreview();
        DrawActionButtons();
        
        EditorGUILayout.EndScrollView();
    }

    void DrawHeader()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("━━━━ 工具动画流水线 ━━━━", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "一键完成工具动画的完整流程：\n\n" +
            "① 自动切片 - 将 Sprite Sheet 切成独立帧\n" +
            "② 规范重命名 - 按位置排序，命名为 {Action}_{Dir}_{Frame}\n" +
            "③ Pivot同步 - 从源文件复制 Pivot（可选）\n" +
            "④ 生成动画 - 创建 .anim 动画剪辑\n" +
            "⑤ 生成控制器 - 创建 AnimatorController\n" +
            "⑥ 同步到SO - 自动赋值控制器到 ToolData/WeaponData\n\n" +
            "规范命名后，后续流程自动按名称排序，确保帧顺序正确",
            MessageType.Info);
        EditorGUILayout.Space(10);
    }

    void DrawInputSection()
    {
        EditorGUILayout.LabelField("━━━━ 输入文件夹 ━━━━", EditorStyles.boldLabel);
        
        spriteSheetFolder = EditorGUILayout.ObjectField(
            "Sprite Sheet 文件夹（必填）",
            spriteSheetFolder,
            typeof(DefaultAsset),
            false) as DefaultAsset;
        
        EditorGUILayout.HelpBox(
            "包含 Down/Side/Up 子文件夹的根目录\n" +
            "每个子文件夹内放置对应方向的 Sprite Sheet\n" +
            "例如：Crush_Hoe/Down/Hoe_0.png, Crush_Hoe/Side/Hoe_0.png",
            MessageType.None);
        
        EditorGUILayout.Space(5);
        
        pivotSourceFolder = EditorGUILayout.ObjectField(
            "Pivot 源文件夹（可选）",
            pivotSourceFolder,
            typeof(DefaultAsset),
            false) as DefaultAsset;
        
        EditorGUILayout.HelpBox(
            "包含原始 Aseprite 文件的文件夹（用于读取 Pivot）\n" +
            "如果不提供，将使用默认 Pivot（中心点）",
            MessageType.None);
        
        EditorGUILayout.Space(10);
    }

    void DrawSliceSettings()
    {
        EditorGUILayout.LabelField("━━━━ 切片设置 ━━━━", EditorStyles.boldLabel);
        
        sliceMode = (SliceMode)EditorGUILayout.EnumPopup("切片模式", sliceMode);
        
        if (sliceMode == SliceMode.Grid)
        {
            gridColumns = EditorGUILayout.IntField("列数（帧数）", gridColumns);
            gridRows = EditorGUILayout.IntField("行数", gridRows);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "自动检测模式将根据透明像素边界自动识别每帧区域\n" +
                "⚠️ 对于紧密排列的 Sprite Sheet 可能不准确",
                MessageType.Warning);
        }
        
        pixelsPerUnit = EditorGUILayout.IntField("Pixels Per Unit", pixelsPerUnit);
        
        EditorGUILayout.Space(5);
        mergeDistanceThreshold = EditorGUILayout.FloatField("相邻区域合并阈值(px)", mergeDistanceThreshold);
        EditorGUILayout.HelpBox(
            "自动检测模式下，水平距离小于此阈值的相邻区域会被合并为一个 Sprite\n" +
            "用于解决细微间隙导致的分离问题（如工具手柄上的小点）",
            MessageType.None);
        
        EditorGUILayout.Space(10);
    }

    void DrawAnimationSettings()
    {
        EditorGUILayout.LabelField("━━━━ 动画设置 ━━━━", EditorStyles.boldLabel);
        
        // 动作类型
        EditorGUILayout.BeginHorizontal();
        autoDetectActionType = EditorGUILayout.Toggle("自动检测动作类型", autoDetectActionType);
        if (!autoDetectActionType)
        {
            selectedActionTypeIndex = EditorGUILayout.Popup(selectedActionTypeIndex, actionTypeOptions);
        }
        EditorGUILayout.EndHorizontal();
        
        // 物品信息
        itemId = EditorGUILayout.IntField("起始物品ID", itemId);
        itemName = EditorGUILayout.TextField("物品名称", itemName);
        
        EditorGUILayout.Space(5);
        
        // 时间轴设置
        totalFrames = EditorGUILayout.IntField("动画总帧数", totalFrames);
        lastFrame = EditorGUILayout.IntField("最后一帧位置", lastFrame);
        
        string actionType = GetFinalActionType();
        EditorGUILayout.HelpBox(
            $"动画命名预览：\n" +
            $"• Sprite: {actionType}_Down_0, {actionType}_Down_1, ...\n" +
            $"• 动画剪辑: {actionType}_Down_Clip_{itemId}.anim\n" +
            $"• 控制器: {actionType}_Controller_{itemId}_{itemName}.controller",
            MessageType.Info);
        
        EditorGUILayout.Space(10);
    }

    void DrawOutputSection()
    {
        EditorGUILayout.LabelField("━━━━ 输出路径 ━━━━", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        animClipOutputPath = EditorGUILayout.TextField("动画剪辑输出", animClipOutputPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择动画剪辑输出文件夹", animClipOutputPath, "");
            if (!string.IsNullOrEmpty(path))
                animClipOutputPath = ConvertToAssetPath(path);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        controllerOutputPath = EditorGUILayout.TextField("控制器输出", controllerOutputPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择控制器输出文件夹", controllerOutputPath, "");
            if (!string.IsNullOrEmpty(path))
                controllerOutputPath = ConvertToAssetPath(path);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
    }

    void DrawPipelineControl()
    {
        EditorGUILayout.LabelField("━━━━ 流程控制 ━━━━", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox("勾选需要执行的步骤（可单独执行某些步骤）", MessageType.None);
        
        step1_Slice = EditorGUILayout.Toggle("① 自动切片", step1_Slice);
        step2_Rename = EditorGUILayout.Toggle("② 规范重命名", step2_Rename);
        step3_Pivot = EditorGUILayout.Toggle("③ Pivot同步（需要源文件）", step3_Pivot);
        step4_Animation = EditorGUILayout.Toggle("④ 生成动画剪辑", step4_Animation);
        step5_Controller = EditorGUILayout.Toggle("⑤ 生成控制器", step5_Controller);
        step6_SyncToSO = EditorGUILayout.Toggle("⑥ 同步到SO（自动赋值控制器）", step6_SyncToSO);
        
        if (step3_Pivot && pivotSourceFolder == null)
        {
            EditorGUILayout.HelpBox("⚠️ Pivot同步需要提供源文件夹", MessageType.Warning);
        }
        
        // SO同步配置
        if (step6_SyncToSO)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            toolSOFolder = EditorGUILayout.TextField("SO文件夹", toolSOFolder);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("选择SO文件夹", toolSOFolder, "");
                if (!string.IsNullOrEmpty(path))
                    toolSOFolder = ConvertToAssetPath(path);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox(
                "支持的SO命名格式:\n" +
                "• Tool_{ID}_{Name}_{Quality}  例如: Tool_12_Hoe_0\n" +
                "• Weapon_{ID}_{Name}_{Quality}  例如: Weapon_200_Sword_0\n" +
                "将自动把生成的控制器赋值到对应SO的 animatorController 字段",
                MessageType.Info);
        }
        
        EditorGUILayout.Space(10);
    }

    void DrawPreview()
    {
        EditorGUILayout.LabelField("━━━━ 预览 ━━━━", EditorStyles.boldLabel);
        
        if (spriteSheetFolder != null)
        {
            // 自动检测动作类型
            if (autoDetectActionType)
            {
                detectedActionType = DetectActionType();
            }
            
            string actionType = GetFinalActionType();
            var directions = DetectDirections();
            
            EditorGUILayout.LabelField($"动作类型: {actionType}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"检测到方向: {string.Join(", ", directions)}");
            
            // 显示每个方向的文件
            foreach (string dir in directions)
            {
                string dirPath = Path.Combine(AssetDatabase.GetAssetPath(spriteSheetFolder), dir);
                if (Directory.Exists(dirPath))
                {
                    var textures = FindTexturesInFolder(dirPath);
                    EditorGUILayout.LabelField($"  {dir}: {textures.Length} 个文件", EditorStyles.miniLabel);
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("请先选择 Sprite Sheet 文件夹", MessageType.Warning);
        }
        
        EditorGUILayout.Space(10);
    }

    void DrawActionButtons()
    {
        EditorGUILayout.LabelField("━━━━ 操作 ━━━━", EditorStyles.boldLabel);
        
        GUI.enabled = spriteSheetFolder != null;
        
        // 一键执行
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("🚀 一键执行全部流程", GUILayout.Height(50)))
        {
            ExecuteFullPipeline();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(5);
        
        // 分步执行
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("1️⃣ 切片", GUILayout.Height(30)))
        {
            ExecuteStep1_Slice();
        }
        
        if (GUILayout.Button("2️⃣ 重命名", GUILayout.Height(30)))
        {
            ExecuteStep2_Rename();
        }
        
        if (GUILayout.Button("3️⃣ Pivot", GUILayout.Height(30)))
        {
            ExecuteStep3_Pivot();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("4️⃣ 动画", GUILayout.Height(30)))
        {
            ExecuteStep4_Animation();
        }
        
        if (GUILayout.Button("5️⃣ 控制器", GUILayout.Height(30)))
        {
            ExecuteStep5_Controller();
        }
        
        if (GUILayout.Button("6️⃣ 同步SO", GUILayout.Height(30)))
        {
            ExecuteStep6_SyncToSO();
        }
        
        EditorGUILayout.EndHorizontal();
        
        GUI.enabled = true;
    }

    #region 流程执行

    void ExecuteFullPipeline()
    {
        string actionType = GetFinalActionType();
        var directions = DetectDirections();
        
        if (!EditorUtility.DisplayDialog("确认执行",
            $"即将执行完整流水线：\n\n" +
            $"动作类型: {actionType}\n" +
            $"方向: {string.Join(", ", directions)}\n" +
            $"物品ID: {itemId}\n" +
            $"物品名称: {itemName}\n\n" +
            $"执行步骤:\n" +
            $"{(step1_Slice ? "✅" : "⬜")} 1. 自动切片\n" +
            $"{(step2_Rename ? "✅" : "⬜")} 2. 规范重命名\n" +
            $"{(step3_Pivot ? "✅" : "⬜")} 3. Pivot同步\n" +
            $"{(step4_Animation ? "✅" : "⬜")} 4. 生成动画\n" +
            $"{(step5_Controller ? "✅" : "⬜")} 5. 生成控制器\n" +
            $"{(step6_SyncToSO ? "✅" : "⬜")} 6. 同步到SO\n\n" +
            "是否继续？",
            "执行", "取消"))
        {
            return;
        }
        
        try
        {
            int totalSteps = (step1_Slice ? 1 : 0) + (step2_Rename ? 1 : 0) + 
                            (step3_Pivot ? 1 : 0) + (step4_Animation ? 1 : 0) + 
                            (step5_Controller ? 1 : 0) + (step6_SyncToSO ? 1 : 0);
            int currentStep = 0;
            
            if (step1_Slice)
            {
                EditorUtility.DisplayProgressBar("流水线执行", "步骤1: 自动切片...", (float)currentStep / totalSteps);
                ExecuteStep1_SliceInternal();
                currentStep++;
            }
            
            if (step2_Rename)
            {
                EditorUtility.DisplayProgressBar("流水线执行", "步骤2: 规范重命名...", (float)currentStep / totalSteps);
                ExecuteStep2_RenameInternal();
                currentStep++;
            }
            
            if (step3_Pivot)
            {
                EditorUtility.DisplayProgressBar("流水线执行", "步骤3: Pivot同步...", (float)currentStep / totalSteps);
                ExecuteStep3_PivotInternal();
                currentStep++;
            }
            
            // 刷新资源数据库，确保前面的修改生效
            AssetDatabase.Refresh();
            
            if (step4_Animation)
            {
                EditorUtility.DisplayProgressBar("流水线执行", "步骤4: 生成动画...", (float)currentStep / totalSteps);
                ExecuteStep4_AnimationInternal();
                currentStep++;
            }
            
            if (step5_Controller)
            {
                EditorUtility.DisplayProgressBar("流水线执行", "步骤5: 生成控制器...", (float)currentStep / totalSteps);
                ExecuteStep5_ControllerInternal();
                currentStep++;
            }
            
            AssetDatabase.Refresh();
            
            if (step6_SyncToSO)
            {
                EditorUtility.DisplayProgressBar("流水线执行", "步骤6: 同步到SO...", (float)currentStep / totalSteps);
                ExecuteStep6_SyncToSOInternal();
                currentStep++;
            }
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("完成",
                $"✅ 流水线执行完成！\n\n" +
                $"动作类型: {actionType}\n" +
                $"物品ID: {itemId}\n" +
                $"物品名称: {itemName}\n\n" +
                $"动画输出: {animClipOutputPath}\n" +
                $"控制器输出: {controllerOutputPath}" +
                (step6_SyncToSO ? $"\nSO同步: {toolSOFolder}" : ""),
                "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("错误", $"流水线执行失败：{e.Message}", "确定");
            Debug.LogError($"[流水线] 失败: {e}\n{e.StackTrace}");
        }
    }

    void ExecuteStep1_Slice()
    {
        if (!EditorUtility.DisplayDialog("确认", "执行步骤1: 自动切片？", "确定", "取消"))
            return;
        
        try
        {
            ExecuteStep1_SliceInternal();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", "✅ 步骤1: 自动切片完成！", "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"切片失败：{e.Message}", "确定");
            Debug.LogError($"[切片] 失败: {e}\n{e.StackTrace}");
        }
    }

    void ExecuteStep2_Rename()
    {
        if (!EditorUtility.DisplayDialog("确认", "执行步骤2: 规范重命名？", "确定", "取消"))
            return;
        
        try
        {
            ExecuteStep2_RenameInternal();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", "✅ 步骤2: 规范重命名完成！", "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"重命名失败：{e.Message}", "确定");
            Debug.LogError($"[重命名] 失败: {e}\n{e.StackTrace}");
        }
    }

    void ExecuteStep3_Pivot()
    {
        if (pivotSourceFolder == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择 Pivot 源文件夹", "确定");
            return;
        }
        
        if (!EditorUtility.DisplayDialog("确认", "执行步骤3: Pivot同步？", "确定", "取消"))
            return;
        
        try
        {
            ExecuteStep3_PivotInternal();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", "✅ 步骤3: Pivot同步完成！", "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"Pivot同步失败：{e.Message}", "确定");
            Debug.LogError($"[Pivot] 失败: {e}\n{e.StackTrace}");
        }
    }

    void ExecuteStep4_Animation()
    {
        if (!EditorUtility.DisplayDialog("确认", "执行步骤4: 生成动画剪辑？", "确定", "取消"))
            return;
        
        try
        {
            ExecuteStep4_AnimationInternal();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", "✅ 步骤4: 动画剪辑生成完成！", "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"动画生成失败：{e.Message}", "确定");
            Debug.LogError($"[动画] 失败: {e}\n{e.StackTrace}");
        }
    }

    void ExecuteStep5_Controller()
    {
        if (!EditorUtility.DisplayDialog("确认", "执行步骤5: 生成控制器？", "确定", "取消"))
            return;
        
        try
        {
            ExecuteStep5_ControllerInternal();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", "✅ 步骤5: 控制器生成完成！", "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"控制器生成失败：{e.Message}", "确定");
            Debug.LogError($"[控制器] 失败: {e}\n{e.StackTrace}");
        }
    }

    #endregion


    #region 步骤1: 自动切片

    void ExecuteStep1_SliceInternal()
    {
        string folderPath = AssetDatabase.GetAssetPath(spriteSheetFolder);
        var directions = DetectDirections();
        
        foreach (string direction in directions)
        {
            string dirPath = Path.Combine(folderPath, direction);
            if (!Directory.Exists(dirPath)) continue;
            
            var textures = FindTexturesInFolder(dirPath);
            
            foreach (Texture2D texture in textures)
            {
                SliceTexture(texture);
            }
        }
        
        Debug.Log($"✅ [步骤1] 切片完成");
    }

    void SliceTexture(Texture2D texture)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        
        if (importer == null)
        {
            Debug.LogError($"[切片] 无法获取 TextureImporter: {texture.name}");
            return;
        }
        
        // 设置为 Sprite 模式
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        
        // 必须设置为可读才能读取像素
        importer.isReadable = true;
        importer.SaveAndReimport();
        
        // 重新加载纹理以获取最新数据
        texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        
        int texWidth = texture.width;
        int texHeight = texture.height;
        
        // 使用 ISpriteEditorDataProvider 进行切片
        var dataProviderFactories = new SpriteDataProviderFactories();
        dataProviderFactories.Init();
        var dataProvider = dataProviderFactories.GetSpriteEditorDataProviderFromObject(importer);
        
        if (dataProvider == null)
        {
            Debug.LogError($"[切片] 无法获取 ISpriteEditorDataProvider: {texture.name}");
            return;
        }
        
        dataProvider.InitSpriteEditorDataProvider();
        
        // 自动检测切片 - 检测非透明像素区域
        List<SpriteRect> spriteRects = AutoDetectSprites(texture);
        
        if (spriteRects.Count == 0)
        {
            Debug.LogWarning($"[切片] {texture.name}: 未检测到任何 sprite 区域");
            return;
        }
        
        dataProvider.SetSpriteRects(spriteRects.ToArray());
        dataProvider.Apply();
        importer.SaveAndReimport();
        Debug.Log($"  ✅ 切片: {texture.name} (检测到 {spriteRects.Count} 个 sprite)");
    }
    
    /// <summary>
    /// 自动检测 sprite 区域 - 找到所有非透明像素的连通区域
    /// 使用洪水填充算法检测独立的 sprite
    /// 增加相邻区域合并功能，解决细微间隙导致的分离问题
    /// </summary>
    List<SpriteRect> AutoDetectSprites(Texture2D texture)
    {
        List<SpriteRect> results = new List<SpriteRect>();
        
        int width = texture.width;
        int height = texture.height;
        
        // 获取所有像素
        Color32[] pixels = texture.GetPixels32();
        
        // 标记已访问的像素
        bool[,] visited = new bool[width, height];
        
        // Alpha 阈值（低于此值视为透明）
        byte alphaThreshold = 1;
        
        // 扫描所有像素，找到非透明区域
        List<Rect> boundingBoxes = new List<Rect>();
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (visited[x, y]) continue;
                
                int pixelIndex = y * width + x;
                if (pixels[pixelIndex].a < alphaThreshold)
                {
                    visited[x, y] = true;
                    continue;
                }
                
                // 找到一个非透明像素，使用洪水填充找到整个连通区域
                Rect bounds = FloodFillAndGetBounds(pixels, visited, width, height, x, y, alphaThreshold);
                
                if (bounds.width > 0 && bounds.height > 0)
                {
                    boundingBoxes.Add(bounds);
                }
            }
        }
        
        // 按 X 坐标排序（从左到右）
        boundingBoxes.Sort((a, b) => a.x.CompareTo(b.x));
        
        // 合并相邻的区域（距离小于阈值的区域合并为一个）
        boundingBoxes = MergeNearbyRects(boundingBoxes, mergeDistanceThreshold);
        
        // 创建 SpriteRect
        for (int i = 0; i < boundingBoxes.Count; i++)
        {
            Rect bounds = boundingBoxes[i];
            
            SpriteRect rect = new SpriteRect();
            rect.name = $"{texture.name}_{i}";
            rect.rect = bounds;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.alignment = SpriteAlignment.Center;
            rect.spriteID = GUID.Generate();
            
            results.Add(rect);
            
            Debug.Log($"    检测到 sprite {i}: x={bounds.x}, y={bounds.y}, w={bounds.width}, h={bounds.height}");
        }
        
        return results;
    }
    
    /// <summary>
    /// 合并相邻的矩形区域
    /// 如果两个矩形的水平距离小于阈值，则合并为一个
    /// </summary>
    List<Rect> MergeNearbyRects(List<Rect> rects, float threshold)
    {
        if (rects.Count <= 1) return rects;
        
        List<Rect> merged = new List<Rect>();
        Rect current = rects[0];
        
        for (int i = 1; i < rects.Count; i++)
        {
            Rect next = rects[i];
            
            // 计算两个矩形之间的水平距离
            float gap = next.x - (current.x + current.width);
            
            // 如果距离小于阈值，合并两个矩形
            if (gap <= threshold)
            {
                // 合并：取两个矩形的并集
                float minX = Mathf.Min(current.x, next.x);
                float minY = Mathf.Min(current.y, next.y);
                float maxX = Mathf.Max(current.x + current.width, next.x + next.width);
                float maxY = Mathf.Max(current.y + current.height, next.y + next.height);
                
                current = new Rect(minX, minY, maxX - minX, maxY - minY);
                
                Debug.Log($"    合并区域: gap={gap:F1}px, 新区域 x={minX}, w={maxX - minX}");
            }
            else
            {
                // 距离超过阈值，保存当前矩形，开始新的
                merged.Add(current);
                current = next;
            }
        }
        
        // 添加最后一个
        merged.Add(current);
        
        Debug.Log($"    合并结果: {rects.Count} → {merged.Count} 个区域");
        
        return merged;
    }
    
    /// <summary>
    /// 洪水填充算法 - 找到连通区域并返回边界框
    /// </summary>
    Rect FloodFillAndGetBounds(Color32[] pixels, bool[,] visited, int width, int height, int startX, int startY, byte alphaThreshold)
    {
        int minX = startX, maxX = startX;
        int minY = startY, maxY = startY;
        
        // 使用栈进行非递归洪水填充
        Stack<(int x, int y)> stack = new Stack<(int, int)>();
        stack.Push((startX, startY));
        
        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();
            
            if (x < 0 || x >= width || y < 0 || y >= height) continue;
            if (visited[x, y]) continue;
            
            int pixelIndex = y * width + x;
            if (pixels[pixelIndex].a < alphaThreshold)
            {
                visited[x, y] = true;
                continue;
            }
            
            visited[x, y] = true;
            
            // 更新边界
            minX = Mathf.Min(minX, x);
            maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);
            
            // 添加相邻像素（4方向）
            stack.Push((x + 1, y));
            stack.Push((x - 1, y));
            stack.Push((x, y + 1));
            stack.Push((x, y - 1));
        }
        
        // 返回边界框（Unity 的 Rect 使用左下角为原点）
        return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    #endregion

    #region 步骤2: 规范重命名

    void ExecuteStep2_RenameInternal()
    {
        string folderPath = AssetDatabase.GetAssetPath(spriteSheetFolder);
        string actionType = GetFinalActionType();
        var directions = DetectDirections();
        
        foreach (string direction in directions)
        {
            string dirPath = Path.Combine(folderPath, direction);
            if (!Directory.Exists(dirPath)) continue;
            
            var textures = FindTexturesInFolder(dirPath);
            
            foreach (Texture2D texture in textures)
            {
                RenameSpritesInTexture(texture, actionType, direction);
            }
        }
        
        Debug.Log($"✅ [步骤2] 重命名完成");
    }

    void RenameSpritesInTexture(Texture2D texture, string actionType, string direction)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        
        if (importer == null)
        {
            Debug.LogError($"[重命名] 无法获取 TextureImporter: {texture.name}");
            return;
        }
        
        // 使用 ISpriteEditorDataProvider 进行重命名
        var dataProviderFactories = new SpriteDataProviderFactories();
        dataProviderFactories.Init();
        var dataProvider = dataProviderFactories.GetSpriteEditorDataProviderFromObject(importer);
        
        if (dataProvider == null)
        {
            Debug.LogError($"[重命名] 无法获取 ISpriteEditorDataProvider: {texture.name}");
            return;
        }
        
        dataProvider.InitSpriteEditorDataProvider();
        var spriteRects = dataProvider.GetSpriteRects();
        
        if (spriteRects == null || spriteRects.Length == 0)
        {
            Debug.LogWarning($"[重命名] {texture.name}: 没有 sprite 数据");
            return;
        }
        
        // 按 X 坐标排序
        var sortedRects = spriteRects.OrderBy(r => r.rect.x).ThenByDescending(r => r.rect.y).ToArray();
        
        // 重命名为规范格式: {ActionType}_{Direction}_{FrameIndex}
        for (int i = 0; i < sortedRects.Length; i++)
        {
            string oldName = sortedRects[i].name;
            sortedRects[i].name = $"{actionType}_{direction}_{i}";
            Debug.Log($"    {oldName} → {sortedRects[i].name}");
        }
        
        dataProvider.SetSpriteRects(sortedRects);
        dataProvider.Apply();
        importer.SaveAndReimport();
        
        Debug.Log($"  ✅ 重命名: {texture.name} ({sortedRects.Length} sprites)");
    }

    #endregion

    #region 步骤3: Pivot同步

    void ExecuteStep3_PivotInternal()
    {
        if (pivotSourceFolder == null)
        {
            Debug.LogWarning("[Pivot] 未提供源文件夹，跳过");
            return;
        }
        
        string folderPath = AssetDatabase.GetAssetPath(spriteSheetFolder);
        string sourcePath = AssetDatabase.GetAssetPath(pivotSourceFolder);
        string actionType = GetFinalActionType();
        var directions = DetectDirections();
        
        foreach (string direction in directions)
        {
            // 查找源文件
            Object pivotSource = FindPivotSourceForDirection(sourcePath, direction, actionType);
            
            if (pivotSource == null)
            {
                Debug.LogWarning($"[Pivot] 未找到 {direction} 的源文件");
                continue;
            }
            
            // 读取 Pivot
            List<Vector2> pivots = GetPivotsFromSource(pivotSource);
            
            if (pivots.Count == 0)
            {
                Debug.LogWarning($"[Pivot] {direction}: 无法读取 Pivot 数据");
                continue;
            }
            
            // 应用到目标文件
            string dirPath = Path.Combine(folderPath, direction);
            if (!Directory.Exists(dirPath)) continue;
            
            var textures = FindTexturesInFolder(dirPath);
            
            foreach (Texture2D texture in textures)
            {
                ApplyPivotsToTexture(texture, pivots);
            }
        }
        
        Debug.Log($"✅ [步骤3] Pivot同步完成");
    }

    Object FindPivotSourceForDirection(string sourcePath, string direction, string actionType)
    {
        // 查找 {ActionType}_{Direction} 格式的文件
        string targetName = $"{actionType}_{direction}";
        
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { sourcePath });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            
            if (fileName.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
            {
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }
        
        return null;
    }

    List<Vector2> GetPivotsFromSource(Object source)
    {
        List<Vector2> pivots = new List<Vector2>();
        
        if (source == null) return pivots;
        
        string path = AssetDatabase.GetAssetPath(source);
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        
        List<Sprite> sprites = allAssets.OfType<Sprite>().ToList();
        
        if (sprites.Count == 0) return pivots;
        
        // 按名称排序
        sprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        
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
        
        if (importer == null) return;
        
        var dataProviderFactories = new SpriteDataProviderFactories();
        dataProviderFactories.Init();
        var dataProvider = dataProviderFactories.GetSpriteEditorDataProviderFromObject(importer);
        
        if (dataProvider == null) return;
        
        dataProvider.InitSpriteEditorDataProvider();
        var spriteRects = dataProvider.GetSpriteRects();
        
        if (spriteRects == null || spriteRects.Length == 0) return;
        
        // 按名称排序（因为已经规范化命名）
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
        
        Debug.Log($"  ✅ Pivot应用: {texture.name} ({count} sprites)");
    }

    #endregion


    #region 步骤4: 生成动画剪辑

    void ExecuteStep4_AnimationInternal()
    {
        string folderPath = AssetDatabase.GetAssetPath(spriteSheetFolder);
        string actionType = GetFinalActionType();
        var directions = DetectDirections();
        
        // 确保输出目录存在
        if (!Directory.Exists(animClipOutputPath))
        {
            Directory.CreateDirectory(animClipOutputPath);
        }
        
        int totalClips = 0;
        
        foreach (string direction in directions)
        {
            string dirPath = Path.Combine(folderPath, direction);
            if (!Directory.Exists(dirPath)) continue;
            
            // 创建方向子文件夹
            string outputDir = Path.Combine(animClipOutputPath, direction);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            
            var textures = FindTexturesInFolder(dirPath);
            
            // 每个 Texture 对应一个品质/ItemID
            for (int i = 0; i < textures.Length; i++)
            {
                Texture2D texture = textures[i];
                int currentItemId = itemId + i;  // 递增 ItemID
                
                string clipName = $"{actionType}_{direction}_Clip_{currentItemId}";
                CreateAnimationClip(texture, outputDir, clipName);
                totalClips++;
            }
        }
        
        Debug.Log($"✅ [步骤4] 动画剪辑生成完成: {totalClips} 个");
    }

    void CreateAnimationClip(Texture2D texture, string outputDir, string clipName)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        
        List<Sprite> sprites = allAssets.OfType<Sprite>().ToList();
        
        if (sprites.Count == 0)
        {
            Debug.LogWarning($"[动画] {texture.name}: 没有 sprite");
            return;
        }
        
        // 按名称排序（因为已经规范化命名为 {Action}_{Dir}_{Index}）
        sprites.Sort((a, b) => 
        {
            // 提取末尾数字进行比较
            int indexA = ExtractTrailingNumber(a.name);
            int indexB = ExtractTrailingNumber(b.name);
            return indexA.CompareTo(indexB);
        });
        
        Debug.Log($"  [动画] {clipName}: {sprites.Count} 帧");
        for (int i = 0; i < sprites.Count; i++)
        {
            Debug.Log($"    帧{i}: {sprites[i].name}");
        }
        
        string clipPath = $"{outputDir}/{clipName}.anim";
        
        // 检查是否已存在
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
        
        // 创建 Sprite 绑定
        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };
        
        // 创建关键帧
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
        
        for (int i = 0; i < sprites.Count; i++)
        {
            float time;
            if (sprites.Count == 1)
            {
                time = 0f;
            }
            else
            {
                // 在前 lastFrame 帧均匀分布
                time = (i * (float)lastFrame / (sprites.Count - 1)) / 60f;
            }
            
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = time,
                value = sprites[i]
            };
        }
        
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
        
        // 设置为非循环
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
        
        Debug.Log($"  ✅ 创建动画: {clipName}");
    }

    int ExtractTrailingNumber(string name)
    {
        // 从字符串末尾提取数字
        int end = name.Length - 1;
        int start = end;
        
        while (start >= 0 && char.IsDigit(name[start]))
        {
            start--;
        }
        
        if (start < end)
        {
            string numStr = name.Substring(start + 1);
            if (int.TryParse(numStr, out int num))
            {
                return num;
            }
        }
        
        return 0;
    }

    #endregion

    #region 步骤5: 生成控制器

    void ExecuteStep5_ControllerInternal()
    {
        string actionType = GetFinalActionType();
        
        // 确保输出目录存在
        if (!Directory.Exists(controllerOutputPath))
        {
            Directory.CreateDirectory(controllerOutputPath);
        }
        
        // 收集所有动画剪辑
        var allClips = CollectAnimationClips();
        
        if (allClips.Count == 0)
        {
            Debug.LogWarning("[控制器] 未找到任何动画剪辑");
            return;
        }
        
        // 生成控制器
        string controllerPath = $"{controllerOutputPath}/{actionType}_Controller_{itemId}_{itemName}.controller";
        
        // 删除旧的
        if (File.Exists(controllerPath))
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }
        
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        
        // 添加参数
        controller.AddParameter("State", AnimatorControllerParameterType.Int);
        controller.AddParameter("Direction", AnimatorControllerParameterType.Int);
        controller.AddParameter("ToolItemId", AnimatorControllerParameterType.Int);
        
        // 获取 Base Layer
        AnimatorControllerLayer baseLayer = controller.layers[0];
        AnimatorStateMachine stateMachine = baseLayer.stateMachine;
        
        // 创建 Idle 状态
        AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(300, 0, 0));
        stateMachine.defaultState = idleState;
        
        // 创建所有动画状态
        CreateAnimatorStates(stateMachine, allClips, actionType);
        
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"✅ [步骤5] 控制器生成完成: {actionType}_Controller_{itemId}_{itemName}");
    }

    Dictionary<string, AnimationClip> CollectAnimationClips()
    {
        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
        
        if (!Directory.Exists(animClipOutputPath))
        {
            return clips;
        }
        
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animClipOutputPath });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            
            if (fileName.Contains("_Clip_"))
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                {
                    clips[fileName] = clip;
                }
            }
        }
        
        return clips;
    }

    void CreateAnimatorStates(AnimatorStateMachine stateMachine, Dictionary<string, AnimationClip> clips, string actionType)
    {
        int stateValue = GetStateValueForAction(actionType);
        Vector3 basePos = new Vector3(400, 0, 0);
        
        var stateInfoList = new List<(AnimatorState state, int direction, int itemId)>();
        
        // 按 ItemID 和方向排序
        var sortedClips = clips.OrderBy(c => ExtractItemIdFromClipName(c.Key)).ThenBy(c => c.Key).ToList();
        
        int currentRow = 0;
        int lastItemId = -1;
        
        foreach (var kvp in sortedClips)
        {
            string fileName = kvp.Key;
            AnimationClip clip = kvp.Value;
            
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
                Debug.LogWarning($"[控制器] 无法识别方向: {fileName}");
                continue;
            }
            
            // 计算位置
            Vector3 pos = basePos + new Vector3(directionValue * 180, currentRow * 60, 0);
            
            AnimatorState state = stateMachine.AddState(clip.name, pos);
            state.motion = clip;
            
            stateInfoList.Add((state, directionValue, clipItemId));
            
            Debug.Log($"  创建状态: {clip.name} (State={stateValue}, Dir={directionValue}, ItemID={clipItemId})");
        }
        
        // 添加 Any State 转换
        foreach (var info in stateInfoList)
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(info.state);
            transition.hasExitTime = false;
            transition.duration = 0;
            transition.canTransitionToSelf = false;
            
            transition.AddCondition(AnimatorConditionMode.Equals, stateValue, "State");
            transition.AddCondition(AnimatorConditionMode.Equals, info.direction, "Direction");
            transition.AddCondition(AnimatorConditionMode.Equals, info.itemId, "ToolItemId");
        }
        
        Debug.Log($"  创建了 {stateInfoList.Count} 个状态");
    }

    int ExtractItemIdFromClipName(string clipName)
    {
        // 格式: {Action}_{Direction}_Clip_{ItemID}
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
        // 根据动作类型返回对应的 State 值
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
            default: return 6;          // 默认使用 Slice
        }
    }

    #endregion

    #region 辅助方法

    string GetFinalActionType()
    {
        if (autoDetectActionType && !string.IsNullOrEmpty(detectedActionType))
        {
            return detectedActionType;
        }
        return actionTypeOptions[selectedActionTypeIndex];
    }

    string DetectActionType()
    {
        if (spriteSheetFolder == null) return "";
        
        string folderName = spriteSheetFolder.name;
        
        // 从文件夹名称检测
        string[] knownActions = { "Slice", "Crush", "Pierce", "Watering", "Fish" };
        foreach (string action in knownActions)
        {
            if (folderName.Contains(action))
            {
                return action;
            }
        }
        
        // 从工具名称推断
        if (folderName.Contains("Axe") && !folderName.Contains("Pick")) return "Slice";
        if (folderName.Contains("Pickaxe") || folderName.Contains("Pick")) return "Crush";
        if (folderName.Contains("Hoe") || folderName.Contains("Shovel")) return "Crush";  // 锄头用 Crush！
        if (folderName.Contains("Sword")) return "Pierce";
        if (folderName.Contains("Water")) return "Watering";
        if (folderName.Contains("Fish") || folderName.Contains("Rod")) return "Fish";
        if (folderName.Contains("Sickle") || folderName.Contains("Scythe")) return "Slice";
        
        return "";
    }

    List<string> DetectDirections()
    {
        List<string> directions = new List<string>();
        
        if (spriteSheetFolder == null) return directions;
        
        string folderPath = AssetDatabase.GetAssetPath(spriteSheetFolder);
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

    Texture2D[] FindTexturesInFolder(string folderPath)
    {
        List<Texture2D> textures = new List<Texture2D>();
        
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // 只包含直接在 folderPath 下的文件
            string parentDir = Path.GetDirectoryName(path).Replace("\\", "/");
            string normalizedFolderPath = folderPath.Replace("\\", "/");
            
            if (!parentDir.Equals(normalizedFolderPath, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                textures.Add(tex);
            }
        }
        
        return textures.OrderBy(t => t.name).ToArray();
    }

    string ConvertToAssetPath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath)) return "";
        
        string dataPath = Application.dataPath;
        
        if (absolutePath.StartsWith(dataPath))
        {
            return "Assets" + absolutePath.Substring(dataPath.Length);
        }
        
        return absolutePath;
    }

    #endregion

    #region 步骤6: 同步到SO

    void ExecuteStep6_SyncToSO()
    {
        if (!EditorUtility.DisplayDialog("确认", "执行步骤6: 同步控制器到ToolData SO？", "确定", "取消"))
            return;
        
        try
        {
            ExecuteStep6_SyncToSOInternal();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", "✅ 步骤6: SO同步完成！", "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"SO同步失败：{e.Message}", "确定");
            Debug.LogError($"[SO同步] 失败: {e}\n{e.StackTrace}");
        }
    }

    void ExecuteStep6_SyncToSOInternal()
    {
        string actionType = GetFinalActionType();
        
        // 查找生成的控制器
        string controllerName = $"{actionType}_Controller_{itemId}_{itemName}";
        string controllerPath = $"{controllerOutputPath}/{controllerName}.controller";
        
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        
        if (controller == null)
        {
            Debug.LogWarning($"[SO同步] 未找到控制器: {controllerPath}");
            return;
        }
        
        // 查找所有 SO
        if (!Directory.Exists(toolSOFolder))
        {
            Debug.LogWarning($"[SO同步] SO文件夹不存在: {toolSOFolder}");
            return;
        }
        
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { toolSOFolder });
        
        int syncCount = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            
            // 解析 SO 命名格式: Tool_{ID}_{Name}_{Quality} 或 Weapon_{ID}_{Name}_{Quality}
            var parsed = ParseItemSOName(fileName);
            if (parsed == null) continue;
            
            int soItemId = parsed.Value.id;
            string soName = parsed.Value.name;
            int soQuality = parsed.Value.quality;
            string soType = parsed.Value.type;
            
            // 检查是否匹配当前生成的控制器
            // 控制器是按 itemId 范围生成的，需要检查 SO 的 itemId 是否在范围内
            // 假设同一工具的不同品质 ID 是连续的
            if (soName.Equals(itemName, System.StringComparison.OrdinalIgnoreCase))
            {
                // 加载 SO
                var itemData = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                
                if (itemData != null)
                {
                    // 使用反射设置 animatorController 字段
                    var field = itemData.GetType().GetField("animatorController", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    if (field != null)
                    {
                        field.SetValue(itemData, controller);
                        EditorUtility.SetDirty(itemData);
                        syncCount++;
                        Debug.Log($"  ✅ 同步: {fileName} ({soType}) → {controllerName}");
                    }
                    else
                    {
                        Debug.LogWarning($"  ⚠️ {fileName}: 未找到 animatorController 字段");
                    }
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ [步骤6] SO同步完成: {syncCount} 个 SO 已更新");
    }

    /// <summary>
    /// 解析物品 SO 命名格式: {Type}_{ID}_{Name}_{Quality}
    /// 支持 Tool 和 Weapon 前缀
    /// 例如: Tool_12_Hoe_0 → (type="Tool", id=12, name="Hoe", quality=0)
    /// 例如: Weapon_200_Sword_0 → (type="Weapon", id=200, name="Sword", quality=0)
    /// </summary>
    (string type, int id, string name, int quality)? ParseItemSOName(string fileName)
    {
        // 支持的前缀
        string[] supportedPrefixes = { "Tool_", "Weapon_" };
        string matchedPrefix = null;
        
        foreach (string prefix in supportedPrefixes)
        {
            if (fileName.StartsWith(prefix))
            {
                matchedPrefix = prefix;
                break;
            }
        }
        
        if (matchedPrefix == null) return null;
        
        string type = matchedPrefix.TrimEnd('_');
        string[] parts = fileName.Split('_');
        
        // 至少需要 4 部分: Type, ID, Name, Quality
        if (parts.Length < 4) return null;
        
        // parts[0] = "Tool" 或 "Weapon"
        // parts[1] = ID
        // parts[2..n-1] = Name (可能包含下划线)
        // parts[n] = Quality
        
        if (!int.TryParse(parts[1], out int id)) return null;
        if (!int.TryParse(parts[parts.Length - 1], out int quality)) return null;
        
        // Name 是中间部分（可能包含多个下划线）
        string name = string.Join("_", parts.Skip(2).Take(parts.Length - 3));
        
        return (type, id, name, quality);
    }

    #endregion
}
