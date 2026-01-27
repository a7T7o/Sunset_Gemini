using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

/// <summary>
/// OcclusionManager的自定义Inspector
/// 优化UI：标签使用ReorderableList + Tag Popup下拉框（学习Game Input Manager）
/// </summary>
[CustomEditor(typeof(OcclusionManager))]
public class OcclusionManagerEditor : Editor
{
    private SerializedProperty player;
    private SerializedProperty playerSprite;
    private SerializedProperty playerCollider;
    private SerializedProperty playerSorting;
    private SerializedProperty detectionRadius;
    private SerializedProperty detectionInterval;
    private SerializedProperty globalOccludedAlpha;
    private SerializedProperty globalFadeSpeed;
    private SerializedProperty useTagCustomParams;
    private SerializedProperty tagParams;
    private SerializedProperty useTagFilter;
    private SerializedProperty occludableTags;
    private SerializedProperty sameSortingLayerOnly;
    private SerializedProperty enableForestTransparency;
    private SerializedProperty rootConnectionDistance;
    private SerializedProperty maxForestSearchDepth;
    private SerializedProperty maxForestSearchRadius;
    private SerializedProperty showDebugGizmos;
    private SerializedProperty enableDetailedDebug;
    
    // Unity项目中的所有Tag
    private string[] allTags;
    
    private void OnEnable()
    {
        // 绑定属性
        player = serializedObject.FindProperty("player");
        playerSprite = serializedObject.FindProperty("playerSprite");
        playerCollider = serializedObject.FindProperty("playerCollider");
        playerSorting = serializedObject.FindProperty("playerSorting");
        detectionRadius = serializedObject.FindProperty("detectionRadius");
        detectionInterval = serializedObject.FindProperty("detectionInterval");
        globalOccludedAlpha = serializedObject.FindProperty("globalOccludedAlpha");
        globalFadeSpeed = serializedObject.FindProperty("globalFadeSpeed");
        useTagCustomParams = serializedObject.FindProperty("useTagCustomParams");
        tagParams = serializedObject.FindProperty("tagParams");
        useTagFilter = serializedObject.FindProperty("useTagFilter");
        occludableTags = serializedObject.FindProperty("occludableTags");
        sameSortingLayerOnly = serializedObject.FindProperty("sameSortingLayerOnly");
        enableForestTransparency = serializedObject.FindProperty("enableForestTransparency");
        rootConnectionDistance = serializedObject.FindProperty("rootConnectionDistance");
        maxForestSearchDepth = serializedObject.FindProperty("maxForestSearchDepth");
        maxForestSearchRadius = serializedObject.FindProperty("maxForestSearchRadius");
        showDebugGizmos = serializedObject.FindProperty("showDebugGizmos");
        enableDetailedDebug = serializedObject.FindProperty("enableDetailedDebug");
        
        // 获取项目中所有Tag
        allTags = UnityEditorInternal.InternalEditorUtility.tags;
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // ========== 玩家引用 ==========
        EditorGUILayout.LabelField("玩家引用", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        EditorGUILayout.PropertyField(player, new GUIContent("玩家Transform", "自动查找Player标签，或手动拖入"));
        EditorGUILayout.PropertyField(playerSprite, new GUIContent("玩家SpriteRenderer", "用于bounds检测"));
        EditorGUILayout.PropertyField(playerCollider, new GUIContent("玩家Collider2D", "用于获取中心点"));
        EditorGUILayout.PropertyField(playerSorting, new GUIContent("玩家DynamicSortingOrder", "用于获取当前Order"));
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(5);
        
        // ========== 检测设置 ==========
        EditorGUILayout.LabelField("检测设置", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        EditorGUILayout.PropertyField(detectionRadius, new GUIContent("检测半径", "只检测玩家周围此范围内的物体"));
        EditorGUILayout.PropertyField(detectionInterval, new GUIContent("检测间隔", "避免每帧检测，提升性能"));
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(5);
        
        // ========== 透明度设置（全局） ==========
        EditorGUILayout.LabelField("透明度设置（全局）", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(globalOccludedAlpha, new GUIContent("遮挡时透明度", "全局目标透明度（0=透明，1=不透明）"));
        EditorGUILayout.PropertyField(globalFadeSpeed, new GUIContent("过渡速度", "全局透明度渐变速度"));
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(5);
        
        // ========== 标签自定义参数 ==========
        EditorGUILayout.LabelField("标签自定义参数", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(useTagCustomParams, new GUIContent("启用标签自定义参数", "不同标签可以有不同的透明度"));
        if (useTagCustomParams.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(tagParams, new GUIContent("标签参数列表"), true);
            EditorGUI.indentLevel--;
        }
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(5);
        
        // ========== 过滤设置 ==========
        EditorGUILayout.LabelField("过滤设置", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        EditorGUILayout.PropertyField(useTagFilter, new GUIContent("启用标签过滤", "只检测指定标签的物体"));
        
        // 🔥 MaskField 多选标签（就像 GameInputManager 的 Interactable Tags）
        if (useTagFilter.boolValue)
        {
            // 计算当前选中的标签对应的 mask 值
            int currentMask = 0;
            for (int i = 0; i < occludableTags.arraySize; i++)
            {
                string tag = occludableTags.GetArrayElementAtIndex(i).stringValue;
                int tagIndex = System.Array.IndexOf(allTags, tag);
                if (tagIndex >= 0)
                {
                    currentMask |= (1 << tagIndex);
                }
            }
            
            // 显示 MaskField
            int newMask = EditorGUILayout.MaskField(new GUIContent("Occludable Tags", "可遮挡的标签列表"), currentMask, allTags);
            
            // 如果 mask 改变，更新数组
            if (newMask != currentMask)
            {
                occludableTags.ClearArray();
                for (int i = 0; i < allTags.Length; i++)
                {
                    if ((newMask & (1 << i)) != 0)
                    {
                        occludableTags.InsertArrayElementAtIndex(occludableTags.arraySize);
                        occludableTags.GetArrayElementAtIndex(occludableTags.arraySize - 1).stringValue = allTags[i];
                    }
                }
            }
        }
        
        EditorGUILayout.PropertyField(sameSortingLayerOnly, new GUIContent("只检测同Sorting Layer", "避免不同楼层互相影响"));
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(5);
        
        // ========== 树林整体透明 ==========
        EditorGUILayout.LabelField("树林整体透明", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        EditorGUILayout.PropertyField(enableForestTransparency, new GUIContent("启用树林整体透明", "进入树林时整片树木都透明"));
        
        if (enableForestTransparency.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(rootConnectionDistance, new GUIContent("树根连通距离", "两棵树的种植点距离小于此值才算连通"));
            EditorGUILayout.PropertyField(maxForestSearchDepth, new GUIContent("最大搜索深度", "限制最多搜索多少棵树"));
            EditorGUILayout.PropertyField(maxForestSearchRadius, new GUIContent("最大搜索半径", "超出此范围的树木不会被包含"));
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(5);
        
        // ========== 调试 ==========
        EditorGUILayout.LabelField("调试", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        EditorGUILayout.PropertyField(showDebugGizmos, new GUIContent("显示Gizmos", "Scene视图显示检测范围"));
        EditorGUILayout.PropertyField(enableDetailedDebug, new GUIContent("详细调试日志", "Console输出详细的检测过程"));
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(5);
        
        serializedObject.ApplyModifiedProperties();
    }
}
