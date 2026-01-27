using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// 工具动画控制器生成
/// 为工具（Axe, Pickaxe等）创建Animator Controller
/// 参数：State, Direction, ToolType（工具类型）, ToolQuality（工具品质）
/// </summary>
public class SliceAnimControllerTool : EditorWindow
{
    [MenuItem("Tools/手持三向生成流程/工具动画控制器生成")]
    static void Open()
    {
        var window = GetWindow<SliceAnimControllerTool>("工具控制器生成");
        window.minSize = new Vector2(500, 600);
        window.Show();
    }

    // ========== 数据结构 ==========
    
    class ScanResult
    {
        public int totalClips = 0;
        public HashSet<string> directions = new HashSet<string>();
        public HashSet<int> toolTypes = new HashSet<int>();
        public HashSet<int> toolQualities = new HashSet<int>();
    }
    
    // ========== UI状态 ==========
    
    DefaultAsset animFolder;
    
    string controllerOutputPath = "Assets/Animations/Tools";
    
    Vector2 scrollPos;
    
    // ========== 绘制界面 ==========
    
    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        DrawHeader();
        DrawInputSection();
        DrawOutputSection();
        DrawScanResultSection();
        DrawActionButtons();
        
        EditorGUILayout.EndScrollView();
    }
    
    void DrawHeader()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("━━━━ 工具动画控制器生成 ━━━━", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "自动扫描并识别：方向、工具类型、工具品质\n" +
            "生成参数：State, Direction, ToolType, ToolQuality\n" +
            "命名格式：{动作}_{方向}_Clip_{类型}_{品质}.anim",
            MessageType.Info);
        EditorGUILayout.Space(10);
    }
    
    void DrawInputSection()
    {
        EditorGUILayout.LabelField("━━━━ 输入 ━━━━", EditorStyles.boldLabel);
        
        animFolder = EditorGUILayout.ObjectField(
            "工具动画文件夹",
            animFolder,
            typeof(DefaultAsset),
            false) as DefaultAsset;
        
        EditorGUILayout.HelpBox(
            "命名格式：{动作}_{方向}_Clip_{工具类型}_{品质}.anim\n" +
            "示例：Slice_Down_Clip_0_0.anim (类型0=斧头, 品质0=木质)",
            MessageType.Info);
        
        EditorGUILayout.Space(10);
    }
    
    void DrawOutputSection()
    {
        EditorGUILayout.LabelField("━━━━ 输出 ━━━━", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        controllerOutputPath = EditorGUILayout.TextField("输出文件夹", controllerOutputPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择输出文件夹", controllerOutputPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                controllerOutputPath = ConvertToAssetPath(path);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.HelpBox(
            "✅ 为每个工具类型生成一个统一的Controller\n" +
            "包含所有方向和品质的状态\n" +
            "命名：Tool_{类型}.controller（如 Tool_Axe.controller）",
            MessageType.Info);
        
        EditorGUILayout.Space(10);
    }
    
    void DrawScanResultSection()
    {
        EditorGUILayout.LabelField("━━━━ 扫描结果 ━━━━", EditorStyles.boldLabel);
        
        if (animFolder != null)
        {
            var scanResult = ScanAnimationFolder();
            
            if (scanResult.totalClips > 0)
            {
                EditorGUILayout.HelpBox(
                    $"总动画文件：{scanResult.totalClips} 个\n" +
                    $"方向：{scanResult.directions.Count} 个\n" +
                    $"工具类型：{scanResult.toolTypes.Count} 种\n" +
                    $"工具品质：{scanResult.toolQualities.Count} 种",
                    MessageType.Info);
                
                if (scanResult.directions.Count > 0)
                {
                    EditorGUILayout.LabelField("方向：" + string.Join(", ", scanResult.directions));
                }
                
                if (scanResult.toolTypes.Count > 0)
                {
                    EditorGUILayout.LabelField("工具类型：" + string.Join(", ", scanResult.toolTypes.OrderBy(t => t).Select(t => $"类型{t}")));
                }
                
                if (scanResult.toolQualities.Count > 0)
                {
                    EditorGUILayout.LabelField("工具品质：" + string.Join(", ", scanResult.toolQualities.OrderBy(q => q).Select(q => $"品质{q}")));
                }
            }
            else
            {
                EditorGUILayout.HelpBox("未检测到有效动画文件", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("请先选择动画文件夹", MessageType.Warning);
        }
        
        EditorGUILayout.Space(10);
    }
    
    void DrawActionButtons()
    {
        GUI.enabled = animFolder != null;
        
        if (GUILayout.Button("生成控制器", GUILayout.Height(40)))
        {
            GenerateControllers();
        }
        
        GUI.enabled = true;
    }
    
    // ========== 核心逻辑 ==========
    
    ScanResult ScanAnimationFolder()
    {
        ScanResult result = new ScanResult();
        
        if (animFolder == null)
            return result;
        
        string folderPath = AssetDatabase.GetAssetPath(animFolder);
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });
        
        result.totalClips = guids.Length;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            
            // 命名格式：{动作}_{方向}_Clip_{工具类型}_{品质}
            // 例如：Slice_Down_Clip_0_0
            
            // 识别方向
            if (fileName.ToLower().Contains("down"))
                result.directions.Add("Down");
            else if (fileName.ToLower().Contains("up"))
                result.directions.Add("Up");
            else if (fileName.ToLower().Contains("side"))
                result.directions.Add("Side");
            
            // 识别工具类型和品质 (_Clip_{类型}_{品质})
            if (fileName.Contains("_Clip_"))
            {
                string[] parts = fileName.Split(new string[] { "_Clip_" }, System.StringSplitOptions.None);
                if (parts.Length >= 2)
                {
                    string[] typeParts = parts[1].Split('_');
                    if (typeParts.Length >= 2)
                    {
                        if (int.TryParse(typeParts[0], out int toolType))
                        {
                            result.toolTypes.Add(toolType);
                        }
                        if (int.TryParse(typeParts[1], out int toolQuality))
                        {
                            result.toolQualities.Add(toolQuality);
                        }
                    }
                }
            }
        }
        
        return result;
    }
    
    void GenerateControllers()
    {
        var scanResult = ScanAnimationFolder();
        
        if (scanResult.totalClips == 0)
        {
            EditorUtility.DisplayDialog("错误", 
                "未检测到有效的动画文件！\n\n" +
                "命名格式：{动作}_{方向}_Clip_{类型}_{品质}.anim", 
                "确定");
            return;
        }
        
        if (!EditorUtility.DisplayDialog(
            "确认",
            $"生成工具动画控制器？\n\n" +
            $"检测到：\n" +
            $"• 方向：{scanResult.directions.Count} 个\n" +
            $"• 工具类型：{scanResult.toolTypes.Count} 种\n" +
            $"• 工具品质：{scanResult.toolQualities.Count} 种\n\n" +
            $"将生成：{scanResult.toolTypes.Count} 个Controller\n" +
            $"每个Controller包含 {scanResult.directions.Count}方向 × {scanResult.toolQualities.Count}品质 = {scanResult.directions.Count * scanResult.toolQualities.Count} 个状态",
            "确定", "取消"))
        {
            return;
        }
        
        try
        {
            CreateToolControllers(scanResult);
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("完成",
                "✅ 控制器生成完成！\n\n" +
                $"生成数量：{scanResult.toolTypes.Count} 个Controller\n" +
                $"每个包含：{scanResult.directions.Count * scanResult.toolQualities.Count} 个状态\n" +
                $"输出位置：{controllerOutputPath}",
                "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("错误", $"生成失败：{e.Message}", "确定");
            Debug.LogError($"[生成控制器] 失败: {e}\n{e.StackTrace}");
        }
    }
    
    void CreateToolControllers(ScanResult scanResult)
    {
        if (!Directory.Exists(controllerOutputPath))
        {
            Directory.CreateDirectory(controllerOutputPath);
        }
        
        string folderPath = AssetDatabase.GetAssetPath(animFolder);
        int count = 0;
        
        // 为每个工具类型创建一个统一的Controller
        foreach (int toolType in scanResult.toolTypes.OrderBy(t => t))
        {
            EditorUtility.DisplayProgressBar("生成控制器", 
                $"创建控制器 (类型{toolType})...", 
                (float)count / scanResult.toolTypes.Count);
            
            // 获取该类型的所有动画（所有方向和品质）
            var allClips = GetAllToolAnimationClips(folderPath, toolType, scanResult.toolQualities);
            
            if (allClips.Count == 0)
            {
                Debug.LogWarning($"未找到类型{toolType}的任何动画！");
                continue;
            }
            
            // 生成控制器路径（只按类型命名）
            string toolName = GetToolName(toolType);
            string controllerPath = $"{controllerOutputPath}/Tool_{toolName}.controller";
            
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
            controller.AddParameter("ToolType", AnimatorControllerParameterType.Int);
            controller.AddParameter("ToolQuality", AnimatorControllerParameterType.Int);
            
            // 获取Base Layer
            AnimatorControllerLayer baseLayer = controller.layers[0];
            AnimatorStateMachine stateMachine = baseLayer.stateMachine;
            
            // 创建Idle状态
            AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(300, 0, 0));
            stateMachine.defaultState = idleState;
            
            // 创建所有Slice状态（所有方向×所有品质）
            CreateAllToolStates(stateMachine, allClips, toolType, scanResult.toolQualities);
            
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"✅ 工具控制器创建: Tool_{toolName}.controller ({allClips.Count}个动画状态)");
            count++;
        }
    }
    
    string GetToolName(int toolType)
    {
        switch (toolType)
        {
            case 0: return "Axe";
            case 1: return "Pickaxe";
            case 2: return "Shovel";
            case 3: return "Hoe";
            default: return toolType.ToString();
        }
    }
    
    void CreateAllToolStates(AnimatorStateMachine stateMachine, Dictionary<string, AnimationClip> allClips, int toolType, HashSet<int> toolQualities)
    {
        // State=6 for Slice
        int sliceState = 6;
        
        Vector3 basePos = new Vector3(300, 100, 0);
        
        // 第一阶段：创建所有状态
        var stateInfoList = new List<StateInfo>();
        
        foreach (var kvp in allClips.OrderBy(c => c.Key))
        {
            string fileName = kvp.Key;
            AnimationClip clip = kvp.Value;
            
            // 识别方向
            int directionValue = -1;
            
            if (fileName.ToLower().Contains("down"))
            {
                directionValue = 0;
            }
            else if (fileName.ToLower().Contains("up"))
            {
                directionValue = 1;
            }
            else if (fileName.ToLower().Contains("side"))
            {
                directionValue = 2;
            }
            
            if (directionValue == -1)
            {
                Debug.LogWarning($"无法识别动画方向: {fileName}");
                continue;
            }
            
            // 识别品质
            int qualityValue = -1;
            string[] parts = fileName.Split(new string[] { "_Clip_" }, System.StringSplitOptions.None);
            if (parts.Length >= 2)
            {
                string[] typeParts = parts[1].Split('_');
                if (typeParts.Length >= 2 && int.TryParse(typeParts[1], out int quality))
                {
                    qualityValue = quality;
                }
            }
            
            if (qualityValue == -1)
            {
                Debug.LogWarning($"无法识别动画品质: {fileName}");
                continue;
            }
            
            // 使用AnimationClip的名称作为状态名称（确保一致）
            string stateName = clip.name;
            
            // 计算位置（按品质和方向排列）
            int row = qualityValue;
            int col = directionValue;
            Vector3 pos = basePos + new Vector3(col * 200, row * 70, 0);
            
            AnimatorState state = stateMachine.AddState(stateName, pos);
            state.motion = clip;
            
            // 记录状态信息
            stateInfoList.Add(new StateInfo
            {
                state = state,
                stateName = stateName,
                directionValue = directionValue,
                qualityValue = qualityValue,
                toolType = toolType
            });
            
            Debug.Log($"  创建状态: {stateName} (State=6, Dir={directionValue}, Type={toolType}, Quality={qualityValue})");
        }
        
        // 第二阶段：为每个状态添加转换（包括从Idle和从其他状态）
        foreach (var sourceInfo in stateInfoList)
        {
            // 为每个目标状态添加转换
            foreach (var targetInfo in stateInfoList)
            {
                // 跳过自己（不需要转换到自己）
                if (sourceInfo.state == targetInfo.state)
                    continue;
                
                // 添加从当前状态到目标状态的转换
                AnimatorStateTransition transition = sourceInfo.state.AddTransition(targetInfo.state);
                transition.hasExitTime = false;
                transition.duration = 0.1f; // 短暂的过渡
                
                // 添加条件：State=6 AND Direction={targetDirection} AND ToolQuality={targetQuality}
                transition.AddCondition(AnimatorConditionMode.Equals, sliceState, "State");
                transition.AddCondition(AnimatorConditionMode.Equals, targetInfo.directionValue, "Direction");
                transition.AddCondition(AnimatorConditionMode.Equals, targetInfo.toolType, "ToolType");
                transition.AddCondition(AnimatorConditionMode.Equals, targetInfo.qualityValue, "ToolQuality");
            }
            
            // 添加从Idle到该状态的转换
            AnimatorState idleState = stateMachine.defaultState;
            if (idleState != null && idleState != sourceInfo.state)
            {
                AnimatorStateTransition idleTransition = idleState.AddTransition(sourceInfo.state);
                idleTransition.hasExitTime = false;
                idleTransition.duration = 0;
                
                idleTransition.AddCondition(AnimatorConditionMode.Equals, sliceState, "State");
                idleTransition.AddCondition(AnimatorConditionMode.Equals, sourceInfo.directionValue, "Direction");
                idleTransition.AddCondition(AnimatorConditionMode.Equals, sourceInfo.toolType, "ToolType");
                idleTransition.AddCondition(AnimatorConditionMode.Equals, sourceInfo.qualityValue, "ToolQuality");
            }
            
            // 添加返回Idle的转换（当State不是6时）
            AnimatorStateTransition backToIdle = sourceInfo.state.AddTransition(idleState);
            backToIdle.hasExitTime = false;
            backToIdle.duration = 0;
            backToIdle.AddCondition(AnimatorConditionMode.NotEqual, sliceState, "State");
        }
        
        Debug.Log($"✅ 创建了 {stateInfoList.Count} 个状态，每个状态有 {stateInfoList.Count} 个转换");
    }
    
    // 辅助类：存储状态信息
    class StateInfo
    {
        public AnimatorState state;
        public string stateName;
        public int directionValue;
        public int qualityValue;
        public int toolType;
    }
    
    string GetQualityName(int quality)
    {
        switch (quality)
        {
            case 0: return "Wood";
            case 1: return "Stone";
            case 2: return "Grindstone";
            case 3: return "Copper";
            case 4: return "Iron";
            case 5: return "Gold";
            default: return "Q" + quality;
        }
    }
    
    Dictionary<string, AnimationClip> GetAllToolAnimationClips(string folderPath, int toolType, HashSet<int> toolQualities)
    {
        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
        
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            
            // 格式：{动作}_{方向}_Clip_{类型}_{品质}
            // 只获取匹配该类型的所有动画（所有品质）
            if (fileName.Contains($"_Clip_{toolType}_"))
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
    
    string ConvertToAssetPath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
            return "";

        string assetsPath = Application.dataPath;
        
        if (absolutePath.StartsWith(assetsPath))
        {
            string relativePath = "Assets" + absolutePath.Substring(assetsPath.Length);
            return relativePath.Replace("\\", "/");
        }
        
        return absolutePath;
    }
}

