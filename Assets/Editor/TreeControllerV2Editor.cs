using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using FarmGame.Data;

/// <summary>
/// TreeControllerV2 自定义编辑器
/// 以固定展示方式显示6阶段配置、Sprite配置、影子配置和经验配置
/// </summary>
[CustomEditor(typeof(TreeControllerV2))]
public class TreeControllerV2Editor : Editor
{
    // 阶段名称
    private static readonly string[] StageNames = new string[]
    {
        "Stage 0 (树苗)",
        "Stage 1 (小树苗)",
        "Stage 2 (中等树)",
        "Stage 3 (大树)",
        "Stage 4 (成熟树)",
        "Stage 5 (完全成熟)"
    };
    
    // 影子阶段名称（阶段1-5）
    private static readonly string[] ShadowStageNames = new string[]
    {
        "Stage 1",
        "Stage 2",
        "Stage 3",
        "Stage 4",
        "Stage 5"
    };
    
    // 折叠状态
    private bool[] stageConfigFoldouts = new bool[6];
    private bool[] spriteConfigFoldouts = new bool[6];
    private bool[] shadowConfigFoldouts = new bool[5];
    
    private bool showStageConfigs = true;
    private bool showSpriteConfig = true;
    private bool showShadowConfigs = true;
    private bool showStageExperience = true;
    private bool showDropConfig = true;
    private bool showCurrentState = true;
    private bool showGrowthSettings = true;
    private bool showHealthState = true;
    private bool showSpriteAlignment = true;
    private bool showFallAnimation = true;
    private bool showSoundSettings = true;
    private bool showDebugSettings = true;
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // 绘制阶段配置
        DrawStageConfigs();
        
        EditorGUILayout.Space(10);
        
        // 绘制 Sprite 配置
        DrawSpriteConfig();
        
        EditorGUILayout.Space(10);
        
        // 绘制当前状态
        DrawCurrentState();
        
        EditorGUILayout.Space(10);
        
        // 绘制成长设置
        DrawGrowthSettings();
        
        EditorGUILayout.Space(10);
        
        // 绘制血量状态
        DrawHealthState();
        
        EditorGUILayout.Space(10);
        
        // 绘制影子配置
        DrawShadowConfigs();
        
        EditorGUILayout.Space(10);
        
        // 绘制 Sprite 对齐
        DrawSpriteAlignment();
        
        EditorGUILayout.Space(10);
        
        // 绘制倒下动画
        DrawFallAnimation();
        
        EditorGUILayout.Space(10);
        
        // 绘制音效设置
        DrawSoundSettings();
        
        EditorGUILayout.Space(10);
        
        // 绘制经验配置
        DrawStageExperience();
        
        EditorGUILayout.Space(10);
        
        // 绘制掉落配置
        DrawDropConfig();
        
        EditorGUILayout.Space(10);
        
        // 绘制调试设置
        DrawDebugSettings();
        
        serializedObject.ApplyModifiedProperties();
    }

    
    #region 阶段配置绘制
    /// <summary>
    /// 绘制阶段配置（固定6个阶段）
    /// </summary>
    private void DrawStageConfigs()
    {
        showStageConfigs = EditorGUILayout.BeginFoldoutHeaderGroup(showStageConfigs, "━━━━ 6阶段配置 ━━━━");
        
        if (showStageConfigs)
        {
            var stageConfigsProp = serializedObject.FindProperty("stageConfigs");
            
            // 确保数组大小为6
            if (stageConfigsProp.arraySize != 6)
            {
                stageConfigsProp.arraySize = 6;
            }
            
            EditorGUI.indentLevel++;
            
            for (int i = 0; i < 6; i++)
            {
                var stageProp = stageConfigsProp.GetArrayElementAtIndex(i);
                stageConfigFoldouts[i] = EditorGUILayout.Foldout(stageConfigFoldouts[i], StageNames[i], true);
                
                if (stageConfigFoldouts[i])
                {
                    EditorGUI.indentLevel++;
                    DrawSingleStageConfig(stageProp, i);
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    
    /// <summary>
    /// 绘制单个阶段配置
    /// </summary>
    private void DrawSingleStageConfig(SerializedProperty stageProp, int stageIndex)
    {
        // 成长设置
        EditorGUILayout.LabelField("成长设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(stageProp.FindPropertyRelative("daysToNextStage"), new GUIContent("成长天数"));
        
        EditorGUILayout.Space(5);
        
        // 成长边距设置
        EditorGUILayout.LabelField("成长边距", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(stageProp.FindPropertyRelative("verticalMargin"), new GUIContent("上下边距"));
        EditorGUILayout.PropertyField(stageProp.FindPropertyRelative("horizontalMargin"), new GUIContent("左右边距"));
        
        EditorGUILayout.Space(5);
        
        // 血量设置
        EditorGUILayout.LabelField("血量设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(stageProp.FindPropertyRelative("health"), new GUIContent("血量"));
        
        // 树桩设置（只有阶段3-5显示）
        if (stageIndex >= 3)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("树桩设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(stageProp.FindPropertyRelative("hasStump"), new GUIContent("有树桩"));
            EditorGUILayout.PropertyField(stageProp.FindPropertyRelative("stumpHealth"), new GUIContent("树桩血量"));
        }
        
        EditorGUILayout.Space(5);
        
        // 碰撞与遮挡
        EditorGUILayout.LabelField("碰撞与遮挡", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(stageProp.FindPropertyRelative("enableCollider"), new GUIContent("启用碰撞"));
        EditorGUILayout.PropertyField(stageProp.FindPropertyRelative("enableOcclusion"), new GUIContent("启用遮挡"));
        
        EditorGUILayout.Space(5);
        
        // 工具类型
        EditorGUILayout.LabelField("工具类型", EditorStyles.boldLabel);
        var toolTypeProp = stageProp.FindPropertyRelative("acceptedToolType");
        
        if (stageIndex == 0)
        {
            // 阶段0固定为锄头，只读显示
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.EnumPopup("接受工具", ToolType.Hoe);
            EditorGUI.EndDisabledGroup();
            // 确保值正确
            toolTypeProp.enumValueIndex = (int)ToolType.Hoe;
        }
        else
        {
            EditorGUILayout.PropertyField(toolTypeProp, new GUIContent("接受工具"));
        }
        
        EditorGUILayout.Space(10);
    }
    #endregion

    
    #region Sprite配置绘制
    /// <summary>
    /// 绘制 Sprite 配置（固定6个阶段）
    /// </summary>
    private void DrawSpriteConfig()
    {
        showSpriteConfig = EditorGUILayout.BeginFoldoutHeaderGroup(showSpriteConfig, "━━━━ 6阶段Sprite数据 ━━━━");
        
        if (showSpriteConfig)
        {
            var spriteConfigProp = serializedObject.FindProperty("spriteConfig");
            
            EditorGUI.indentLevel++;
            
            // 绘制6个阶段的 Sprite 数据
            for (int i = 0; i < 6; i++)
            {
                var stageProp = spriteConfigProp.FindPropertyRelative($"stage{i}");
                spriteConfigFoldouts[i] = EditorGUILayout.Foldout(spriteConfigFoldouts[i], StageNames[i], true);
                
                if (spriteConfigFoldouts[i])
                {
                    EditorGUI.indentLevel++;
                    DrawSingleStageSpriteData(stageProp, i);
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    
    /// <summary>
    /// 绘制单个阶段的 Sprite 数据
    /// </summary>
    private void DrawSingleStageSpriteData(SerializedProperty stageProp, int stageIndex)
    {
        // 正常状态（所有阶段都有）
        EditorGUILayout.LabelField("━━━━ 正常状态 ━━━━", EditorStyles.boldLabel);
        var normalProp = stageProp.FindPropertyRelative("normal");
        
        // Stage 0 不需要冬季 Sprite（冬季直接死亡）
        bool hideWinter = (stageIndex == 0);
        DrawSeasonSpriteSet(normalProp, "标准", hideWinter);
        
        // 枯萎状态（阶段1-5有，阶段0没有）
        if (stageIndex >= 1)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("━━━━ 枯萎状态 ━━━━", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(stageProp.FindPropertyRelative("hasWitheredState"), new GUIContent("有枯萎状态"));
            EditorGUILayout.PropertyField(stageProp.FindPropertyRelative("witheredSummer"), new GUIContent("夏季枯萎"));
            EditorGUILayout.PropertyField(stageProp.FindPropertyRelative("witheredFall"), new GUIContent("秋季枯萎"));
        }
        
        // 树桩状态（阶段3-5有，阶段0-2没有）
        if (stageIndex >= 3)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("━━━━ 树桩状态 ━━━━", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(stageProp.FindPropertyRelative("hasStump"), new GUIContent("有树桩"));
            EditorGUILayout.PropertyField(stageProp.FindPropertyRelative("stumpSpringSummer"), new GUIContent("春夏树桩"));
            EditorGUILayout.PropertyField(stageProp.FindPropertyRelative("stumpFall"), new GUIContent("秋季树桩"));
            EditorGUILayout.PropertyField(stageProp.FindPropertyRelative("stumpWinter"), new GUIContent("冬季树桩"));
        }
        
        EditorGUILayout.Space(10);
    }
    
    /// <summary>
    /// 绘制季节 Sprite 集合
    /// </summary>
    /// <param name="prop">SeasonSpriteSet 属性</param>
    /// <param name="label">标签</param>
    /// <param name="hideWinter">是否隐藏冬季字段（Stage 0 不需要冬季 Sprite）</param>
    private void DrawSeasonSpriteSet(SerializedProperty prop, string label, bool hideWinter = false)
    {
        EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(prop.FindPropertyRelative("spring"), new GUIContent("春季"));
        EditorGUILayout.PropertyField(prop.FindPropertyRelative("summer"), new GUIContent("夏季"));
        EditorGUILayout.PropertyField(prop.FindPropertyRelative("earlyFall"), new GUIContent("早秋"));
        EditorGUILayout.PropertyField(prop.FindPropertyRelative("lateFall"), new GUIContent("晚秋"));
        
        // Stage 0 不需要冬季 Sprite（冬季直接死亡）
        if (!hideWinter)
        {
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("winter"), new GUIContent("冬季"));
        }
        else
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField("冬季", "无 (冬季直接死亡)");
            EditorGUI.EndDisabledGroup();
        }
        EditorGUI.indentLevel--;
    }
    #endregion

    
    #region 影子配置绘制
    /// <summary>
    /// 绘制影子配置（固定5个，对应阶段1-5）
    /// </summary>
    private void DrawShadowConfigs()
    {
        showShadowConfigs = EditorGUILayout.BeginFoldoutHeaderGroup(showShadowConfigs, "━━━━ 影子设置 ━━━━");
        
        if (showShadowConfigs)
        {
            var shadowConfigsProp = serializedObject.FindProperty("shadowConfigs");
            
            // 确保数组大小为5
            if (shadowConfigsProp.arraySize != 5)
            {
                shadowConfigsProp.arraySize = 5;
            }
            
            EditorGUI.indentLevel++;
            
            EditorGUILayout.HelpBox("阶段0无影子，以下配置对应阶段1-5", MessageType.Info);
            
            for (int i = 0; i < 5; i++)
            {
                var shadowProp = shadowConfigsProp.GetArrayElementAtIndex(i);
                shadowConfigFoldouts[i] = EditorGUILayout.Foldout(shadowConfigFoldouts[i], ShadowStageNames[i], true);
                
                if (shadowConfigFoldouts[i])
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(shadowProp.FindPropertyRelative("sprite"), new GUIContent("影子 Sprite"));
                    EditorGUILayout.PropertyField(shadowProp.FindPropertyRelative("scale"), new GUIContent("缩放"));
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    #endregion

    
    #region 经验配置绘制
    /// <summary>
    /// 绘制砍树经验配置（固定6个阶段）
    /// </summary>
    private void DrawStageExperience()
    {
        showStageExperience = EditorGUILayout.BeginFoldoutHeaderGroup(showStageExperience, "━━━━ 砍树经验 ━━━━");
        
        if (showStageExperience)
        {
            var experienceProp = serializedObject.FindProperty("stageExperience");
            
            // 确保数组大小为6
            if (experienceProp.arraySize != 6)
            {
                experienceProp.arraySize = 6;
            }
            
            EditorGUI.indentLevel++;
            
            for (int i = 0; i < 6; i++)
            {
                var expProp = experienceProp.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(expProp, new GUIContent(StageNames[i]));
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    #endregion
    
    #region 掉落配置绘制
    /// <summary>
    /// 绘制掉落配置
    /// </summary>
    private void DrawDropConfig()
    {
        showDropConfig = EditorGUILayout.BeginFoldoutHeaderGroup(showDropConfig, "━━━━ 掉落配置 ━━━━");
        
        if (showDropConfig)
        {
            EditorGUI.indentLevel++;
            
            // 掉落物品 SO
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dropItemData"), new GUIContent("掉落物品 SO"));
            
            // 分散半径
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dropSpreadRadius"), new GUIContent("分散半径"));
            
            EditorGUILayout.Space(5);
            
            // 各阶段掉落数量
            EditorGUILayout.LabelField("各阶段掉落数量", EditorStyles.boldLabel);
            var dropAmountsProp = serializedObject.FindProperty("stageDropAmounts");
            if (dropAmountsProp.arraySize != 6)
            {
                dropAmountsProp.arraySize = 6;
            }
            
            EditorGUI.indentLevel++;
            for (int i = 0; i < 6; i++)
            {
                var amountProp = dropAmountsProp.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(amountProp, new GUIContent(StageNames[i]));
            }
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space(5);
            
            // 树桩掉落数量
            EditorGUILayout.LabelField("树桩掉落数量（阶段3-5）", EditorStyles.boldLabel);
            var stumpAmountsProp = serializedObject.FindProperty("stumpDropAmounts");
            if (stumpAmountsProp.arraySize != 6)
            {
                stumpAmountsProp.arraySize = 6;
            }
            
            EditorGUI.indentLevel++;
            for (int i = 3; i < 6; i++)
            {
                var amountProp = stumpAmountsProp.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(amountProp, new GUIContent(StageNames[i]));
            }
            EditorGUI.indentLevel--;
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    #endregion
    
    #region 其他设置绘制
    /// <summary>
    /// 绘制当前状态
    /// </summary>
    private void DrawCurrentState()
    {
        showCurrentState = EditorGUILayout.BeginFoldoutHeaderGroup(showCurrentState, "━━━━ 当前状态 ━━━━");
        
        if (showCurrentState)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("treeID"), new GUIContent("树木ID"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentStageIndex"), new GUIContent("当前阶段"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentState"), new GUIContent("当前状态"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentSeason"), new GUIContent("当前季节"));
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    
    /// <summary>
    /// 绘制成长设置
    /// </summary>
    private void DrawGrowthSettings()
    {
        showGrowthSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showGrowthSettings, "━━━━ 成长设置 ━━━━");
        
        if (showGrowthSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoGrow"), new GUIContent("自动成长"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("plantedDay"), new GUIContent("种植日期"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("daysInCurrentStage"), new GUIContent("当前阶段天数"));
            
            EditorGUILayout.Space(5);
            
            // 成长空间检测设置
            EditorGUILayout.LabelField("成长空间检测", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableGrowthSpaceCheck"), new GUIContent("启用空间检测"));
            
            // 使用 MaskField 绘制 growthObstacleTags
            var tagsProp = serializedObject.FindProperty("growthObstacleTags");
            if (tagsProp != null)
            {
                DrawTagMask(tagsProp, "障碍物标签");
            }
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showGrowthBlockedInfo"), new GUIContent("显示阻挡信息"));
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    
    /// <summary>
    /// 绘制 Tag 多选下拉框（MaskField 风格）
    /// </summary>
    private void DrawTagMask(SerializedProperty arrayProp, string label)
    {
        var builtin = InternalEditorUtility.tags;
        
        // 计算当前选中的 mask
        int mask = 0;
        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            string tag = arrayProp.GetArrayElementAtIndex(i).stringValue;
            int idx = System.Array.IndexOf(builtin, tag);
            if (idx >= 0) mask |= (1 << idx);
        }
        
        // 绘制 MaskField
        int newMask = EditorGUILayout.MaskField(label, mask, builtin);
        
        // 如果 mask 变化，更新数组
        if (newMask != mask)
        {
            arrayProp.ClearArray();
            for (int i = 0; i < builtin.Length; i++)
            {
                if ((newMask & (1 << i)) != 0)
                {
                    arrayProp.InsertArrayElementAtIndex(arrayProp.arraySize);
                    arrayProp.GetArrayElementAtIndex(arrayProp.arraySize - 1).stringValue = builtin[i];
                }
            }
        }
    }
    
    /// <summary>
    /// 绘制血量状态
    /// </summary>
    private void DrawHealthState()
    {
        showHealthState = EditorGUILayout.BeginFoldoutHeaderGroup(showHealthState, "━━━━ 血量状态 ━━━━");
        
        if (showHealthState)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentHealth"), new GUIContent("当前血量"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentStumpHealth"), new GUIContent("树桩血量"));
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    
    /// <summary>
    /// 绘制 Sprite 对齐设置
    /// </summary>
    private void DrawSpriteAlignment()
    {
        showSpriteAlignment = EditorGUILayout.BeginFoldoutHeaderGroup(showSpriteAlignment, "━━━━ Sprite底部对齐 ━━━━");
        
        if (showSpriteAlignment)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("alignSpriteBottom"), new GUIContent("自动对齐"));
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    
    /// <summary>
    /// 绘制倒下动画设置
    /// </summary>
    private void DrawFallAnimation()
    {
        showFallAnimation = EditorGUILayout.BeginFoldoutHeaderGroup(showFallAnimation, "━━━━ 倒下动画 ━━━━");
        
        if (showFallAnimation)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableFallAnimation"), new GUIContent("启用倒下动画"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fallDuration"), new GUIContent("动画时长"));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("向上倒参数", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fallUpMaxStretch"), new GUIContent("最大拉伸"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fallUpMinScale"), new GUIContent("最小缩放"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fallUpStretchPhase"), new GUIContent("拉伸阶段"));
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    
    /// <summary>
    /// 绘制音效设置
    /// </summary>
    private void DrawSoundSettings()
    {
        showSoundSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showSoundSettings, "━━━━ 音效设置 ━━━━");
        
        if (showSoundSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("chopHitSound"), new GUIContent("砍击音效"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("chopFellSound"), new GUIContent("砍倒音效"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("digOutSound"), new GUIContent("挖出音效"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tierInsufficientSound"), new GUIContent("等级不足音效"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("soundVolume"), new GUIContent("音量"));
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    
    /// <summary>
    /// 绘制调试设置
    /// </summary>
    private void DrawDebugSettings()
    {
        showDebugSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showDebugSettings, "━━━━ 调试 ━━━━");
        
        if (showDebugSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showDebugInfo"), new GUIContent("显示调试信息"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("editorPreview"), new GUIContent("编辑器预览"));
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    #endregion
}
