using UnityEngine;
using UnityEditor;

/// <summary>
/// CloudShadowManager 自定义编辑器
/// 提供可视化参数调整和实时预览功能
/// </summary>
[CustomEditor(typeof(CloudShadowManager))]
public class CloudShadowManagerEditor : Editor
{
    private SerializedProperty enableCloudShadows;
    private SerializedProperty intensity;
    private SerializedProperty density;
    private SerializedProperty scaleRange;
    private SerializedProperty cloudSprites;
    private SerializedProperty cloudMaterial;
    private SerializedProperty direction;
    private SerializedProperty speed;
    private SerializedProperty areaSizeMode;
    private SerializedProperty areaSize;
    private SerializedProperty worldLayerNames;
    private SerializedProperty boundsPadding;
    private SerializedProperty sortingLayerName;
    private SerializedProperty sortingOrder;
    private SerializedProperty useWeatherGate;
    private SerializedProperty currentWeather;
    private SerializedProperty enableInSunny;
    private SerializedProperty enableInPartlyCloudy;
    private SerializedProperty enableInOvercast;
    private SerializedProperty enableInRain;
    private SerializedProperty enableInSnow;
    private SerializedProperty seed;
    private SerializedProperty randomizeOnStart;
    private SerializedProperty previewInEditor;
    private SerializedProperty maxClouds;

    private void OnEnable()
    {
        enableCloudShadows = serializedObject.FindProperty("enableCloudShadows");
        intensity = serializedObject.FindProperty("intensity");
        density = serializedObject.FindProperty("density");
        scaleRange = serializedObject.FindProperty("scaleRange");
        cloudSprites = serializedObject.FindProperty("cloudSprites");
        cloudMaterial = serializedObject.FindProperty("cloudMaterial");
        direction = serializedObject.FindProperty("direction");
        speed = serializedObject.FindProperty("speed");
        areaSizeMode = serializedObject.FindProperty("areaSizeMode");
        areaSize = serializedObject.FindProperty("areaSize");
        worldLayerNames = serializedObject.FindProperty("worldLayerNames");
        boundsPadding = serializedObject.FindProperty("boundsPadding");
        sortingLayerName = serializedObject.FindProperty("sortingLayerName");
        sortingOrder = serializedObject.FindProperty("sortingOrder");
        useWeatherGate = serializedObject.FindProperty("useWeatherGate");
        currentWeather = serializedObject.FindProperty("currentWeather");
        enableInSunny = serializedObject.FindProperty("enableInSunny");
        enableInPartlyCloudy = serializedObject.FindProperty("enableInPartlyCloudy");
        enableInOvercast = serializedObject.FindProperty("enableInOvercast");
        enableInRain = serializedObject.FindProperty("enableInRain");
        enableInSnow = serializedObject.FindProperty("enableInSnow");
        seed = serializedObject.FindProperty("seed");
        randomizeOnStart = serializedObject.FindProperty("randomizeOnStart");
        previewInEditor = serializedObject.FindProperty("previewInEditor");
        maxClouds = serializedObject.FindProperty("maxClouds");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        CloudShadowManager manager = (CloudShadowManager)target;

        // 标题
        EditorGUILayout.Space(5);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 14;
        titleStyle.normal.textColor = new Color(0.3f, 0.8f, 1f);
        EditorGUILayout.LabelField("☁️ 云朵阴影管理器", titleStyle);
        EditorGUILayout.Space(5);

        // 总开关
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(enableCloudShadows, new GUIContent("启用云影", "总开关，关闭后所有云影消失"));
        EditorGUILayout.EndVertical();

        if (!enableCloudShadows.boolValue)
        {
            EditorGUILayout.HelpBox("云影系统已关闭", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.Space(5);

        // 外观设置
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("━━━━ 外观设置 ━━━━", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(intensity, new GUIContent("强度 (Intensity)", "云影的透明度，0=完全透明，1=完全不透明"));
        EditorGUILayout.PropertyField(density, new GUIContent("密度 (Density)", "云影的数量，0=无云影，1=最大数量"));
        EditorGUILayout.PropertyField(scaleRange, new GUIContent("缩放范围", "云影的随机缩放范围 (最小, 最大)"));
        EditorGUILayout.PropertyField(cloudSprites, new GUIContent("云影精灵", "用于云影的精灵数组（灰度/Alpha贴图）"), true);
        EditorGUILayout.PropertyField(cloudMaterial, new GUIContent("云影材质", "可选，推荐使用 Multiply 材质"));
        
        // 素材检查
        if (cloudSprites.arraySize == 0)
        {
            EditorGUILayout.HelpBox("⚠️ 未配置云影精灵！请拖入云影贴图到 Cloud Sprites 数组", MessageType.Warning);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // 移动设置
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("━━━━ 移动设置 ━━━━", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(direction, new GUIContent("移动方向", "云影移动的方向向量 (x, y)"));
        EditorGUILayout.PropertyField(speed, new GUIContent("移动速度", "云影移动的速度"));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // 区域设置
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("━━━━ 区域设置 ━━━━", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(areaSizeMode, new GUIContent("区域大小模式", "选择如何获取云影活动区域"));
        
        // 根据模式显示不同的选项
        CloudShadowManager.AreaSizeMode mode = (CloudShadowManager.AreaSizeMode)areaSizeMode.enumValueIndex;
        
        switch (mode)
        {
            case CloudShadowManager.AreaSizeMode.Manual:
                EditorGUILayout.PropertyField(areaSize, new GUIContent("区域大小", "云影活动区域的大小 (宽, 高)"));
                EditorGUILayout.HelpBox("手动设置云影活动区域大小", MessageType.Info);
                break;
                
            case CloudShadowManager.AreaSizeMode.FromNavGrid:
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(areaSize, new GUIContent("区域大小 (自动)", "从 NavGrid2D 获取"));
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.PropertyField(boundsPadding, new GUIContent("边界扩展", "在检测到的边界基础上扩展的距离"));
                EditorGUILayout.HelpBox("将从场景中的 NavGrid2D 组件获取世界边界", MessageType.Info);
                
                // 检查 NavGrid2D 是否存在
                if (FindFirstObjectByType<NavGrid2D>() == null)
                {
                    EditorGUILayout.HelpBox("⚠️ 场景中未找到 NavGrid2D 组件！", MessageType.Warning);
                }
                break;
                
            case CloudShadowManager.AreaSizeMode.AutoDetect:
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(areaSize, new GUIContent("区域大小 (自动)", "自动检测 Tilemap 边界"));
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.PropertyField(worldLayerNames, new GUIContent("世界层级名称", "用于检测边界的层级名称"), true);
                EditorGUILayout.PropertyField(boundsPadding, new GUIContent("边界扩展", "在检测到的边界基础上扩展的距离"));
                EditorGUILayout.HelpBox("将自动检测指定层级下所有 Tilemap 的边界", MessageType.Info);
                break;
        }
        
        // 刷新区域大小按钮
        if (mode != CloudShadowManager.AreaSizeMode.Manual)
        {
            EditorGUILayout.Space(5);
            GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
            if (GUILayout.Button("🔄 刷新区域大小", GUILayout.Height(25)))
            {
                manager.UpdateAreaSizeFromMode();
                EditorUtility.SetDirty(target);
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;
        }
        
        EditorGUILayout.HelpBox("云影会在以此物体为中心的矩形区域内循环移动", MessageType.Info);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // 渲染设置
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("━━━━ 渲染设置 ━━━━", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(sortingLayerName, new GUIContent("Sorting Layer", "渲染层级名称（建议：CloudShadow）"));
        EditorGUILayout.PropertyField(sortingOrder, new GUIContent("Sorting Order", "渲染顺序"));
        
        // Sorting Layer 检查
        int layerID = SortingLayer.NameToID(sortingLayerName.stringValue);
        if (layerID == 0 && sortingLayerName.stringValue != "Default")
        {
            EditorGUILayout.HelpBox($"⚠️ Sorting Layer '{sortingLayerName.stringValue}' 不存在！\n请在 Edit → Project Settings → Tags and Layers 中添加", MessageType.Warning);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // 天气联动
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("━━━━ 天气联动 ━━━━", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useWeatherGate, new GUIContent("启用天气控制", "根据天气自动开关云影"));
        
        if (useWeatherGate.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(currentWeather, new GUIContent("当前天气 (手动)", "手动设置当前天气（运行时会被 WeatherSystem 覆盖）"));
            EditorGUILayout.PropertyField(enableInSunny, new GUIContent("☀️ 晴天启用"));
            EditorGUILayout.PropertyField(enableInPartlyCloudy, new GUIContent("⛅ 多云启用"));
            EditorGUILayout.PropertyField(enableInOvercast, new GUIContent("☁️ 阴天启用"));
            EditorGUILayout.PropertyField(enableInRain, new GUIContent("🌧️ 雨天启用"));
            EditorGUILayout.PropertyField(enableInSnow, new GUIContent("❄️ 雪天启用"));
            EditorGUI.indentLevel--;
            
            EditorGUILayout.HelpBox("💡 提示：运行时会自动从 WeatherSystem 获取天气状态", MessageType.Info);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // 随机种子与预览
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("━━━━ 随机种子与预览 ━━━━", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(seed, new GUIContent("随机种子", "用于生成云影分布的随机种子"));
        EditorGUILayout.PropertyField(randomizeOnStart, new GUIContent("启动时随机化", "游戏开始时自动生成新的随机种子"));
        EditorGUILayout.PropertyField(previewInEditor, new GUIContent("编辑器预览", "在编辑器中实时预览云影移动"));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // 性能限制
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("━━━━ 性能限制 ━━━━", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(maxClouds, new GUIContent("最大云影数量", "同时存在的最大云影数量"));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // 操作按钮
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("━━━━ 操作 ━━━━", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        // Rebuild Now 按钮
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("🔄 立即重建 (Rebuild Now)", GUILayout.Height(30)))
        {
            manager.EditorRebuildNow();
            EditorUtility.SetDirty(target);
            SceneView.RepaintAll();
        }
        
        // Randomize Seed 按钮
        GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
        if (GUILayout.Button("🎲 随机种子 (Randomize)", GUILayout.Height(30)))
        {
            manager.RandomizeSeed();
            manager.EditorRebuildNow();
            EditorUtility.SetDirty(target);
            SceneView.RepaintAll();
        }
        
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Despawn All 按钮
        GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
        if (GUILayout.Button("🗑️ 清除所有云影 (Despawn All)", GUILayout.Height(25)))
        {
            manager.EditorDespawnAll();
            EditorUtility.SetDirty(target);
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // 使用提示
        EditorGUILayout.HelpBox(
            "💡 使用提示：\n" +
            "1. 拖入云影精灵到 Cloud Sprites 数组\n" +
            "2. 配置 Sorting Layer 为 'CloudShadow'（需在 Project Settings 中添加）\n" +
            "3. 调整 Intensity 和 Density 控制云影效果\n" +
            "4. 启用 '编辑器预览' 可在编辑器中实时查看效果\n" +
            "5. 点击 '立即重建' 应用参数变化",
            MessageType.Info
        );

        serializedObject.ApplyModifiedProperties();
    }
}
