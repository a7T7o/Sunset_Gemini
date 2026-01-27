using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 003批量工具 - 动画过渡（Animator Transitions）
/// 批量修改AnimatorController中的Transition参数
/// </summary>
public class Tool_003_BatchAnimTransitions : EditorWindow
{
    private AnimatorController controller;
    private Vector2 scrollPos;
    private Vector2 listScrollPos;
    
    // 过滤器
    private string filterFrom = "";
    private string filterTo = "";
    
    // 选中的Transitions
    private List<TransitionInfo> allTransitions = new List<TransitionInfo>();
    private List<bool> selectedTransitions = new List<bool>();
    
    // 参数勾选
    private bool chk_hasExitTime = false;
    private bool chk_exitTime = false;
    private bool chk_fixedDuration = false;
    private bool chk_duration = false;
    private bool chk_offset = false;
    private bool chk_interruptionSource = false;
    private bool chk_orderedInterruption = false;
    private bool chk_canTransitionToSelf = false;
    
    // Condition智能生成
    private bool enableSmartConditions = false;
    private string paramState = "State";
    private string paramDirection = "Direction";
    private string paramType = "Type";
    
    // 参数值
    private bool val_hasExitTime = false;
    private float val_exitTime = 0.75f;
    private bool val_fixedDuration = true;
    private float val_duration = 0.25f;
    private float val_offset = 0f;
    private TransitionInterruptionSource val_interruptionSource = TransitionInterruptionSource.None;
    private bool val_orderedInterruption = true;
    private bool val_canTransitionToSelf = true;
    
    private class TransitionInfo
    {
        public AnimatorStateTransition transition;
        public string fromState;
        public string toState;
        public AnimatorStateMachine stateMachine;
    }

    [MenuItem("Tools/003批量 (动画过渡)")]
    public static void ShowWindow()
    {
        var window = GetWindow<Tool_003_BatchAnimTransitions>("003批量-动画过渡");
        window.minSize = new Vector2(550, 700);
        window.Show();
    }

    private void OnEnable()
    {
        LoadSettings();
    }

    private void OnDisable()
    {
        SaveSettings();
    }

    private void OnGUI()
    {
        // 标题
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField("🎬 003批量工具 (动画过渡)", titleStyle, GUILayout.Height(28));
        
        EditorGUILayout.Space(5);
        
        // 选择Controller（固定顶部）
        EditorGUI.BeginChangeCheck();
        controller = (AnimatorController)EditorGUILayout.ObjectField("🎬 Animator Controller", controller, typeof(AnimatorController), false);
        if (EditorGUI.EndChangeCheck())
        {
            if (controller != null) ScanTransitions();
            else { allTransitions.Clear(); selectedTransitions.Clear(); }
        }
        
        if (controller == null)
        {
            EditorGUILayout.HelpBox("⚠️ 请在Project窗口选择一个AnimatorController", MessageType.Warning);
            if (GUILayout.Button("🔍 使用当前选中的Controller", GUILayout.Height(35)))
            {
                var selected = Selection.activeObject as AnimatorController;
                if (selected != null) { controller = selected; ScanTransitions(); }
                else EditorUtility.DisplayDialog("提示", "请先在Project窗口选中AnimatorController！", "确定");
            }
            return;
        }
        
        DrawLine();
        
        // ========== 开始整体滚动 ==========
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        // 统计信息
        int selectedCount = selectedTransitions.Count(x => x);
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"📊 总共: {allTransitions.Count} 条", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"✓ 已选: {selectedCount} 条", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        // 快速操作
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("✓ 全选")) { for (int i = 0; i < selectedTransitions.Count; i++) selectedTransitions[i] = true; }
        if (GUILayout.Button("✗ 全不选")) { for (int i = 0; i < selectedTransitions.Count; i++) selectedTransitions[i] = false; }
        if (GUILayout.Button("↔️ 反选")) { for (int i = 0; i < selectedTransitions.Count; i++) selectedTransitions[i] = !selectedTransitions[i]; }
        if (GUILayout.Button("🔄 刷新")) ScanTransitions();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(3);
        
        // 过滤器
        EditorGUILayout.LabelField("🔍 过滤器", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("来源:", GUILayout.Width(40));
        filterFrom = EditorGUILayout.TextField(filterFrom);
        if (GUILayout.Button("✗", GUILayout.Width(25))) filterFrom = "";
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("目标:", GUILayout.Width(40));
        filterTo = EditorGUILayout.TextField(filterTo);
        if (GUILayout.Button("✗", GUILayout.Width(25))) filterTo = "";
        EditorGUILayout.EndHorizontal();
        
        DrawLine();
        
        // Transition列表（局部滚动）
        EditorGUILayout.LabelField("📋 过渡列表（勾选要修改的）", EditorStyles.boldLabel);
        listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos, GUILayout.Height(180));
        
        for (int i = 0; i < allTransitions.Count; i++)
        {
            var info = allTransitions[i];
            
            // 过滤
            bool match = true;
            if (!string.IsNullOrEmpty(filterFrom) && !info.fromState.ToLower().Contains(filterFrom.ToLower())) match = false;
            if (!string.IsNullOrEmpty(filterTo) && !info.toState.ToLower().Contains(filterTo.ToLower())) match = false;
            if (!match) continue;
            
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            selectedTransitions[i] = EditorGUILayout.Toggle(selectedTransitions[i], GUILayout.Width(20));
            
            string icon = info.transition.hasExitTime ? "⏱️" : "⚡";
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            if (selectedTransitions[i]) labelStyle.normal.textColor = Color.cyan;
            
            EditorGUILayout.LabelField($"{icon} {info.fromState} → {info.toState}", labelStyle);
            EditorGUILayout.LabelField($"D:{info.transition.duration:F2}s", new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight }, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
        
        DrawLine();
        
        // 参数设置
        EditorGUILayout.LabelField("⚙️ 批量设置参数（勾选要修改的项）", EditorStyles.boldLabel);
        
        // 快速预设
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
        if (GUILayout.Button("⚡ 即时过渡", GUILayout.Height(25)))
        {
            chk_hasExitTime = true; val_hasExitTime = false;
            chk_duration = true; val_duration = 0f;
        }
        if (GUILayout.Button("🎨 平滑过渡", GUILayout.Height(25)))
        {
            chk_hasExitTime = true; val_hasExitTime = true;
            chk_duration = true; val_duration = 0.25f;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(3);
        
        // 参数列表
        DrawParam(ref chk_hasExitTime, "Has Exit Time", ref val_hasExitTime);
        DrawParamSlider(ref chk_exitTime, "Exit Time", ref val_exitTime, 0f, 1f);
        DrawParam(ref chk_fixedDuration, "Fixed Duration", ref val_fixedDuration);
        DrawParamSlider(ref chk_duration, "Transition Duration", ref val_duration, 0f, 2f);
        DrawParamSlider(ref chk_offset, "Transition Offset", ref val_offset, 0f, 1f);
        
        EditorGUILayout.BeginHorizontal();
        chk_interruptionSource = EditorGUILayout.Toggle(chk_interruptionSource, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!chk_interruptionSource);
        val_interruptionSource = (TransitionInterruptionSource)EditorGUILayout.EnumPopup("Interruption Source", val_interruptionSource);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        
        DrawParam(ref chk_orderedInterruption, "Ordered Interruption", ref val_orderedInterruption);
        DrawParam(ref chk_canTransitionToSelf, "Can Transition To Self", ref val_canTransitionToSelf);
        
        DrawLine();
        
        // 智能Condition生成
        EditorGUILayout.LabelField("🧠 智能Condition生成", EditorStyles.boldLabel);
        enableSmartConditions = EditorGUILayout.ToggleLeft("启用智能识别（根据State名称自动添加Condition）", enableSmartConditions);
        
        if (enableSmartConditions)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox(
                "命名规则：{State}_{Direction}_Clip 或 Carry_{Type}_{Direction}_Clip\n" +
                "例如：Fish_Down_Clip → State=9, Direction=0", MessageType.Info);
            
            paramState = EditorGUILayout.TextField("State参数名", paramState);
            paramDirection = EditorGUILayout.TextField("Direction参数名", paramDirection);
            paramType = EditorGUILayout.TextField("Type参数名", paramType);
            
            if (GUILayout.Button("📋 查看参数映射表")) ShowParameterMapping();
            EditorGUILayout.EndVertical();
        }
        
        DrawLine();
        
        // 应用按钮
        GUI.enabled = selectedCount > 0;
        GUI.backgroundColor = new Color(0.3f, 1f, 0.3f);
        if (GUILayout.Button($"🚀 应用到选中的 {selectedCount} 条过渡", GUILayout.Height(45)))
            ApplyToSelectedTransitions();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        
        EditorGUILayout.Space(5);
        
        // 恢复默认按钮
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("🔄 恢复默认设置"))
        {
            if (EditorUtility.DisplayDialog("确认", "恢复所有参数到默认值？", "确定", "取消"))
                ResetSettings();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndScrollView();
        // ========== 结束整体滚动 ==========
    }

    private void DrawLine()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 2);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
    }

    private void DrawParam(ref bool check, string label, ref bool value)
    {
        EditorGUILayout.BeginHorizontal();
        check = EditorGUILayout.Toggle(check, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!check);
        value = EditorGUILayout.Toggle(label, value);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawParamSlider(ref bool check, string label, ref float value, float min, float max)
    {
        EditorGUILayout.BeginHorizontal();
        check = EditorGUILayout.Toggle(check, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!check);
        value = EditorGUILayout.Slider(label, value, min, max);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    private void ScanTransitions()
    {
        allTransitions.Clear();
        selectedTransitions.Clear();
        
        if (controller == null) return;
        
        foreach (var layer in controller.layers)
            ScanStateMachine(layer.stateMachine, layer.stateMachine);
        
        for (int i = 0; i < allTransitions.Count; i++)
            selectedTransitions.Add(false);
        
        Debug.Log($"<color=cyan>[003批量] 扫描完成！找到 {allTransitions.Count} 条Transition</color>");
    }

    private void ScanStateMachine(AnimatorStateMachine stateMachine, AnimatorStateMachine rootMachine)
    {
        if (stateMachine == null) return;
        
        foreach (var state in stateMachine.states)
        {
            foreach (var transition in state.state.transitions)
            {
                string toState = "AnyState";
                if (transition.destinationState != null) toState = transition.destinationState.name;
                else if (transition.destinationStateMachine != null) toState = transition.destinationStateMachine.name;
                
                allTransitions.Add(new TransitionInfo
                {
                    transition = transition,
                    fromState = state.state.name,
                    toState = toState,
                    stateMachine = stateMachine
                });
            }
        }
        
        foreach (var transition in stateMachine.anyStateTransitions)
        {
            string toState = "AnyState";
            if (transition.destinationState != null) toState = transition.destinationState.name;
            else if (transition.destinationStateMachine != null) toState = transition.destinationStateMachine.name;
            
            allTransitions.Add(new TransitionInfo
            {
                transition = transition,
                fromState = "AnyState",
                toState = toState,
                stateMachine = stateMachine
            });
        }
        
        foreach (var child in stateMachine.stateMachines)
            ScanStateMachine(child.stateMachine, rootMachine);
    }

    private void ApplyToSelectedTransitions()
    {
        List<TransitionInfo> targets = new List<TransitionInfo>();
        for (int i = 0; i < allTransitions.Count; i++)
        {
            if (selectedTransitions[i]) targets.Add(allTransitions[i]);
        }
        
        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先勾选要修改的Transition！", "确定");
            return;
        }
        
        string msg = $"将修改 {targets.Count} 条Transition的以下参数：\n\n";
        if (chk_hasExitTime) msg += $"• Has Exit Time → {val_hasExitTime}\n";
        if (chk_exitTime) msg += $"• Exit Time → {val_exitTime:F2}\n";
        if (chk_fixedDuration) msg += $"• Fixed Duration → {val_fixedDuration}\n";
        if (chk_duration) msg += $"• Transition Duration → {val_duration:F2}s\n";
        if (chk_offset) msg += $"• Transition Offset → {val_offset:F2}\n";
        if (chk_interruptionSource) msg += $"• Interruption Source → {val_interruptionSource}\n";
        if (chk_orderedInterruption) msg += $"• Ordered Interruption → {val_orderedInterruption}\n";
        if (chk_canTransitionToSelf) msg += $"• Can Transition To Self → {val_canTransitionToSelf}\n";
        if (enableSmartConditions) msg += $"\n🧠 启用智能Condition生成\n";
        msg += "\n是否继续？";
        
        if (!EditorUtility.DisplayDialog("确认", msg, "确定", "取消")) return;
        
        Undo.RecordObject(controller, "Batch Modify Transitions");
        
        int successCount = 0;
        int conditionCount = 0;
        
        foreach (var info in targets)
        {
            try
            {
                if (chk_hasExitTime) info.transition.hasExitTime = val_hasExitTime;
                if (chk_exitTime) info.transition.exitTime = val_exitTime;
                if (chk_fixedDuration) info.transition.hasFixedDuration = val_fixedDuration;
                if (chk_duration) info.transition.duration = val_duration;
                if (chk_offset) info.transition.offset = val_offset;
                if (chk_interruptionSource) info.transition.interruptionSource = val_interruptionSource;
                if (chk_orderedInterruption) info.transition.orderedInterruption = val_orderedInterruption;
                if (chk_canTransitionToSelf) info.transition.canTransitionToSelf = val_canTransitionToSelf;
                
                if (enableSmartConditions)
                {
                    if (ApplySmartConditions(info)) conditionCount++;
                }
                
                successCount++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"修改失败: {info.fromState} → {info.toState}\n{ex.Message}");
            }
        }
        
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        
        string result = $"成功修改 {successCount} 条Transition！";
        if (enableSmartConditions) result += $"\n自动生成Condition: {conditionCount} 条";
        
        EditorUtility.DisplayDialog("完成", result, "确定");
        Debug.Log($"<color=green>[003批量] 动画过渡修改完成！成功: {successCount}, Condition: {conditionCount}</color>");
    }

    private bool ApplySmartConditions(TransitionInfo info)
    {
        string stateName = info.toState;
        if (string.IsNullOrEmpty(stateName) || stateName == "AnyState") return false;
        
        stateName = stateName.Replace("_Clip", "");
        string[] parts = stateName.Split('_');
        if (parts.Length < 2) return false;
        
        info.transition.conditions = new AnimatorCondition[0];
        
        bool isCarry = parts[0] == "Carry";
        
        if (isCarry && parts.Length >= 3)
        {
            string carryType = parts[1];
            string direction = parts[2];
            
            AddCondition(info.transition, paramState, AnimatorConditionMode.Equals, GetStateValue("Carry"));
            AddCondition(info.transition, paramType, AnimatorConditionMode.Equals, GetCarryTypeValue(carryType));
            AddCondition(info.transition, paramDirection, AnimatorConditionMode.Equals, GetDirectionValue(direction));
            return true;
        }
        else if (parts.Length >= 2)
        {
            string state = parts[0];
            string direction = parts[1];
            
            AddCondition(info.transition, paramState, AnimatorConditionMode.Equals, GetStateValue(state));
            AddCondition(info.transition, paramDirection, AnimatorConditionMode.Equals, GetDirectionValue(direction));
            return true;
        }
        
        return false;
    }

    private void AddCondition(AnimatorStateTransition transition, string paramName, AnimatorConditionMode mode, float threshold)
    {
        var conditions = new List<AnimatorCondition>(transition.conditions);
        conditions.Add(new AnimatorCondition { parameter = paramName, mode = mode, threshold = threshold });
        transition.conditions = conditions.ToArray();
    }

    private float GetStateValue(string stateName)
    {
        return stateName switch
        {
            "Idle" => 0, "Walk" => 1, "Run" => 2, "Carry" => 3,
            "Collect" => 4, "Hit" => 5, "Slice" => 6, "Pierce" => 7,
            "Crush" => 8, "Fish" => 9, "Watering" => 10, "Death" => 11,
            _ => -1
        };
    }

    private float GetDirectionValue(string directionName)
    {
        return directionName switch
        {
            "Down" => 0, "Up" => 1, "Right" => 2, "Left" => 2, "Side" => 2,
            _ => 0
        };
    }

    private float GetCarryTypeValue(string typeName)
    {
        return typeName switch
        {
            "Idle" => 0, "Walk" => 1, "Run" => 2,
            _ => 0
        };
    }

    private void ShowParameterMapping()
    {
        string mapping = "📋 参数映射表\n\n" +
            "【AnimState 枚举】\n" +
            "Idle=0, Walk=1, Run=2, Carry=3, Collect=4\n" +
            "Hit=5, Slice=6, Pierce=7, Crush=8, Fish=9\n" +
            "Watering=10, Death=11\n\n" +
            "【AnimDirection 枚举】\n" +
            "Down=0, Up=1, Right=2, Left=2(Flip)\n\n" +
            "【CarryState 枚举】\n" +
            "Idle=0, Walk=1, Run=2\n\n" +
            "【命名示例】\n" +
            "Fish_Down_Clip → State=9, Direction=0\n" +
            "Carry_Walk_Right_Clip → State=3, Type=1, Direction=2";
        
        EditorUtility.DisplayDialog("参数映射表", mapping, "确定");
    }

    #region ========== 设置保存/加载 ==========

    private void LoadSettings()
    {
        chk_hasExitTime = EditorPrefs.GetBool("Batch003_ChkHasExitTime", false);
        chk_exitTime = EditorPrefs.GetBool("Batch003_ChkExitTime", false);
        chk_fixedDuration = EditorPrefs.GetBool("Batch003_ChkFixedDuration", false);
        chk_duration = EditorPrefs.GetBool("Batch003_ChkDuration", false);
        chk_offset = EditorPrefs.GetBool("Batch003_ChkOffset", false);
        chk_interruptionSource = EditorPrefs.GetBool("Batch003_ChkInterruptionSource", false);
        chk_orderedInterruption = EditorPrefs.GetBool("Batch003_ChkOrderedInterruption", false);
        chk_canTransitionToSelf = EditorPrefs.GetBool("Batch003_ChkCanTransitionToSelf", false);
        
        val_hasExitTime = EditorPrefs.GetBool("Batch003_ValHasExitTime", false);
        val_exitTime = EditorPrefs.GetFloat("Batch003_ValExitTime", 0.75f);
        val_fixedDuration = EditorPrefs.GetBool("Batch003_ValFixedDuration", true);
        val_duration = EditorPrefs.GetFloat("Batch003_ValDuration", 0.25f);
        val_offset = EditorPrefs.GetFloat("Batch003_ValOffset", 0f);
        val_interruptionSource = (TransitionInterruptionSource)EditorPrefs.GetInt("Batch003_ValInterruptionSource", 0);
        val_orderedInterruption = EditorPrefs.GetBool("Batch003_ValOrderedInterruption", true);
        val_canTransitionToSelf = EditorPrefs.GetBool("Batch003_ValCanTransitionToSelf", true);
        
        enableSmartConditions = EditorPrefs.GetBool("Batch003_SmartConditions", false);
        paramState = EditorPrefs.GetString("Batch003_ParamState", "State");
        paramDirection = EditorPrefs.GetString("Batch003_ParamDirection", "Direction");
        paramType = EditorPrefs.GetString("Batch003_ParamType", "Type");
    }

    private void SaveSettings()
    {
        EditorPrefs.SetBool("Batch003_ChkHasExitTime", chk_hasExitTime);
        EditorPrefs.SetBool("Batch003_ChkExitTime", chk_exitTime);
        EditorPrefs.SetBool("Batch003_ChkFixedDuration", chk_fixedDuration);
        EditorPrefs.SetBool("Batch003_ChkDuration", chk_duration);
        EditorPrefs.SetBool("Batch003_ChkOffset", chk_offset);
        EditorPrefs.SetBool("Batch003_ChkInterruptionSource", chk_interruptionSource);
        EditorPrefs.SetBool("Batch003_ChkOrderedInterruption", chk_orderedInterruption);
        EditorPrefs.SetBool("Batch003_ChkCanTransitionToSelf", chk_canTransitionToSelf);
        
        EditorPrefs.SetBool("Batch003_ValHasExitTime", val_hasExitTime);
        EditorPrefs.SetFloat("Batch003_ValExitTime", val_exitTime);
        EditorPrefs.SetBool("Batch003_ValFixedDuration", val_fixedDuration);
        EditorPrefs.SetFloat("Batch003_ValDuration", val_duration);
        EditorPrefs.SetFloat("Batch003_ValOffset", val_offset);
        EditorPrefs.SetInt("Batch003_ValInterruptionSource", (int)val_interruptionSource);
        EditorPrefs.SetBool("Batch003_ValOrderedInterruption", val_orderedInterruption);
        EditorPrefs.SetBool("Batch003_ValCanTransitionToSelf", val_canTransitionToSelf);
        
        EditorPrefs.SetBool("Batch003_SmartConditions", enableSmartConditions);
        EditorPrefs.SetString("Batch003_ParamState", paramState);
        EditorPrefs.SetString("Batch003_ParamDirection", paramDirection);
        EditorPrefs.SetString("Batch003_ParamType", paramType);
    }

    private void ResetSettings()
    {
        chk_hasExitTime = chk_exitTime = chk_fixedDuration = chk_duration = false;
        chk_offset = chk_interruptionSource = chk_orderedInterruption = chk_canTransitionToSelf = false;
        
        val_hasExitTime = false;
        val_exitTime = 0.75f;
        val_fixedDuration = true;
        val_duration = 0.25f;
        val_offset = 0f;
        val_interruptionSource = TransitionInterruptionSource.None;
        val_orderedInterruption = true;
        val_canTransitionToSelf = true;
        
        enableSmartConditions = false;
        paramState = "State";
        paramDirection = "Direction";
        paramType = "Type";
        
        SaveSettings();
        Repaint();
    }

    #endregion
}
