using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 002批量工具 - Hierarchy窗口专用
/// 整合：Order排序、Transform、碰撞器
/// V2.0: 智能Pivot换算 - 统一底部基点计算Order
/// </summary>
public class Tool_002_BatchHierarchy : EditorWindow
{
    private enum ToolMode { Order, Transform, 碰撞器 }
    private ToolMode currentMode = ToolMode.Order;
    private Vector2 scrollPos;
    
    private List<GameObject> selectedObjs = new List<GameObject>();

    [MenuItem("Tools/002批量 (Hierarchy窗口)")]
    public static void ShowWindow()
    {
        var window = GetWindow<Tool_002_BatchHierarchy>("002批量-Hierarchy");
        window.minSize = new Vector2(480, 650);
        window.Show();
    }

    private void OnEnable()
    {
        currentMode = (ToolMode)EditorPrefs.GetInt("Batch002_Mode", 0);
        LoadSettings();
        
        // 自动监听选择变化
        Selection.selectionChanged += OnSelectionChanged;
        
        // 初始加载当前选择
        GetSelectedObjects();
    }

    private void OnDisable()
    {
        EditorPrefs.SetInt("Batch002_Mode", (int)currentMode);
        SaveSettings();
        
        // 取消监听
        Selection.selectionChanged -= OnSelectionChanged;
    }
    
    private void OnSelectionChanged()
    {
        // 自动获取选中对象
        GetSelectedObjects();
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawModeSwitch();
        
        EditorGUILayout.Space(3);
        DrawLine();
        
        // 显示选中对象（自动跟随Hierarchy）
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        if (selectedObjs.Count == 0)
        {
            EditorGUILayout.LabelField("⚠️ 未选择任何对象（自动跟随Hierarchy）", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.LabelField($"✓ 已选择 {selectedObjs.Count} 个对象", EditorStyles.boldLabel);
        }
        
        if (GUILayout.Button("🔄 刷新", GUILayout.Width(60)))
        {
            GetSelectedObjects();
        }
        EditorGUILayout.EndHorizontal();
        
        // 详细列表
        if (selectedObjs.Count > 0)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            int show = Mathf.Min(selectedObjs.Count, 6);
            for (int i = 0; i < show; i++)
            {
                if (selectedObjs[i] != null)
                    EditorGUILayout.LabelField($"• {selectedObjs[i].name}", EditorStyles.miniLabel);
            }
            if (selectedObjs.Count > 6) 
                EditorGUILayout.LabelField($"... 还有 {selectedObjs.Count - 6} 个", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
        
        DrawLine();
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        switch (currentMode)
        {
            case ToolMode.Order: DrawOrderMode(); break;
            case ToolMode.Transform: DrawTransformMode(); break;
            case ToolMode.碰撞器: DrawColliderMode(); break;
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void OnInspectorUpdate()
    {
        // 定期刷新，确保UI更新
        Repaint();
    }

    private void DrawHeader()
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField("🏗️ 002批量工具 (Hierarchy)", style, GUILayout.Height(28));
    }

    private void DrawModeSwitch()
    {
        EditorGUILayout.BeginHorizontal();
        
        GUI.backgroundColor = currentMode == ToolMode.Order ? new Color(1f, 0.8f, 0.3f) : Color.white;
        if (GUILayout.Button("📊 Order", GUILayout.Height(40)))
        {
            currentMode = ToolMode.Order;
            EditorPrefs.SetInt("Batch002_Mode", 0);
        }
        
        GUI.backgroundColor = currentMode == ToolMode.Transform ? new Color(1f, 0.8f, 0.3f) : Color.white;
        if (GUILayout.Button("📐 Transform", GUILayout.Height(40)))
        {
            currentMode = ToolMode.Transform;
            EditorPrefs.SetInt("Batch002_Mode", 1);
        }
        
        GUI.backgroundColor = currentMode == ToolMode.碰撞器 ? new Color(1f, 0.8f, 0.3f) : Color.white;
        if (GUILayout.Button("🔲 碰撞器", GUILayout.Height(40)))
        {
            currentMode = ToolMode.碰撞器;
            EditorPrefs.SetInt("Batch002_Mode", 2);
        }
        
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        // 恢复默认按钮
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("🔄 恢复默认", GUILayout.Width(100)))
        {
            if (EditorUtility.DisplayDialog("确认", $"恢复【{currentMode}】的默认设置？", "确定", "取消"))
            {
                ResetCurrentMode();
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLine()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 2);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
    }

    private void GetSelectedObjects()
    {
        selectedObjs.Clear();
        if (Selection.gameObjects != null && Selection.gameObjects.Length > 0)
        {
        selectedObjs.AddRange(Selection.gameObjects);
        }
        Repaint();
    }

    #region ========== Order排序模式 ==========

    // Sorting Layer 设置
    private bool sort_chk_layer = false;
    private string sort_layer = "Default";
    
    // 快速偏移
    private int sort_quickOffset = 1;
    
    // 按Y坐标计算Order参数
    private int sort_multiplier = 100;
    private int sort_orderOffset = 0;
    private bool sort_useSpriteBounds = true;
    private float sort_bottomOffset = 0f;
    private int sort_shadowOffset = -1;
    private int sort_glowOffset = 0;

    private void DrawOrderMode()
    {
        // 核心说明
        EditorGUILayout.HelpBox(
            "✨ 智能Collider底部计算：优先使用Collider2D底部（物理边界），回退到Sprite底部！\n\n" +
            "原理：Collider底部 = 玩家实际交互位置 = 最准确的排序基准\n" +
            "优势：自动处理分离设计（主体+子物体），每个物体用自己的Collider底部\n" +
            "适用于：任何Collider设计、分离式设计、混合Pivot场景",
            MessageType.Info);
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.LabelField("⚡ 快速操作", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Order偏移:", GUILayout.Width(80));
        sort_quickOffset = EditorGUILayout.IntField(sort_quickOffset, GUILayout.Width(50));
        
        GUI.enabled = selectedObjs.Count > 0;
        if (GUILayout.Button("↑ +", GUILayout.Width(50)))
            QuickOffsetOrder(sort_quickOffset);
        if (GUILayout.Button("↓ -", GUILayout.Width(50)))
            QuickOffsetOrder(-sort_quickOffset);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        
        DrawLine();
        
        // 按Y坐标计算Order（完整功能）
        EditorGUILayout.LabelField("📐 按Y坐标计算Order（智能Pivot换算）", EditorStyles.boldLabel);
        
        sort_multiplier = EditorGUILayout.IntField("Y坐标缩放倍数", sort_multiplier);
        EditorGUILayout.HelpBox("推荐值：100。数值越大，排序越精确", MessageType.None);
        
        sort_orderOffset = EditorGUILayout.IntField("Order偏移值", sort_orderOffset);
        EditorGUILayout.HelpBox("默认0即可。用于微调整体显示优先级", MessageType.None);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("计算方式", EditorStyles.boldLabel);
        
        sort_useSpriteBounds = EditorGUILayout.Toggle("使用边界计算（优先Collider）", sort_useSpriteBounds);
        EditorGUILayout.HelpBox(
            "✅ 推荐勾选！\n" +
            "• 优先：Collider2D.bounds.min.y（物理底部）\n" +
            "• 回退：Sprite.bounds.min.y（视觉底部）\n" +
            "• 自动处理子物体，每个用自己的Collider",
            MessageType.Info);
        
        sort_bottomOffset = EditorGUILayout.FloatField("底部偏移（世界单位）", sort_bottomOffset);
        EditorGUILayout.HelpBox(
            "正值=逻辑底部往上移，负值=往下移\n" +
            "树等高物体建议设0.2-0.5",
            MessageType.None);
        
        EditorGUILayout.LabelField("子物体设置", EditorStyles.boldLabel);
        sort_shadowOffset = EditorGUILayout.IntField("Shadow偏移值", sort_shadowOffset);
        sort_glowOffset = EditorGUILayout.IntField("Glow/特效偏移值", sort_glowOffset);
        
        DrawLine();
        
        EditorGUILayout.LabelField("⚙️ 可选：Sorting Layer 设置", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        sort_chk_layer = EditorGUILayout.Toggle(sort_chk_layer, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!sort_chk_layer);
        sort_layer = EditorGUILayout.TextField("Sorting Layer", sort_layer);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.HelpBox(
            "✅ 勾选后会同时设置Sorting Layer\n" +
            "✅ Order始终自动计算（基于Collider底部）\n" +
            "💡 一键完成！",
            MessageType.Info);
        
        GUI.enabled = selectedObjs.Count > 0;
        GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
        if (GUILayout.Button("🚀 设置选中物体的Order in Layer", GUILayout.Height(40)))
            SetOrderByY();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        
        EditorGUILayout.Space(5);
        
        GUI.enabled = selectedObjs.Count > 0;
        if (GUILayout.Button("📊 显示选中物体的当前Order", GUILayout.Height(30)))
            ShowCurrentOrders();
        GUI.enabled = true;
        
        EditorGUILayout.Space(10);
        
        // 使用说明
        EditorGUILayout.LabelField("使用说明：", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. 在Scene中选中要设置的固定物体（可以只选父物体）\n" +
            "2. 点击上面的按钮\n" +
            "3. 工具会自动处理该物体及其所有子物体的SpriteRenderer\n" +
            "4. Order会自动设置为：-Round(底部Y × 倍数) + 偏移值\n\n" +
            "特殊处理：\n" +
            "• Shadow子物体：Order = 父物体Order + shadowOffset（在父物体下面）\n" +
            "• Glow子物体：Order = 父物体Order + glowOffset（与父物体同层）\n" +
            "• 其他子物体：Order = 父物体Order（与父物体完全一致）\n\n" +
            "示例：物体底部Y=10，倍数=100，偏移=0\n" +
            "      → Order = -1000\n\n" +
            "💡 提示：不需要手动展开层级，工具会自动递归处理所有子物体！\n" +
            "💡 Pivot换算：自动处理，无需修改Sprite资源！", 
            MessageType.None);
    }

    private void QuickOffsetOrder(int offset)
    {
        // 🔥 修复：包含所有子物体的SpriteRenderer
        List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        foreach (var obj in selectedObjs)
        {
            SpriteRenderer[] srs = obj.GetComponentsInChildren<SpriteRenderer>(true);
            renderers.AddRange(srs);
        }
        
        if (renderers.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "选中对象及其子物体中没有SpriteRenderer", "确定");
            return;
        }
        
        Undo.RecordObjects(renderers.ToArray(), "Quick Offset Order");
        
        int skipped = 0;
        foreach (var sr in renderers)
        {
            // ✅ 跳过特殊标记的物体（Order < -9990）
            if (sr.sortingOrder < -9990)
            {
                skipped++;
                continue;
            }
            
            sr.sortingOrder += offset;
            EditorUtility.SetDirty(sr);
        }
        
        if (skipped > 0)
            Debug.Log($"<color=grey>[002批量] 跳过了 {skipped} 个特殊标记物体（Order < -9990）</color>");
        
        Debug.Log($"<color=green>[002批量] Order偏移 {offset:+0;-0}，共{renderers.Count}个对象（含子物体）</color>");
    }

    private void SetOrderByY()
    {
        if (selectedObjs.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先选择对象！", "确定");
            return;
        }
        
        int count = 0;
        List<SpriteRenderer> allRenderers = new List<SpriteRenderer>();
        
        // ✅ 关键修复：获取所有选中对象及其子物体的SpriteRenderer
        foreach (GameObject obj in selectedObjs)
        {
            // 获取自己和所有子物体的SpriteRenderer
            SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
            allRenderers.AddRange(renderers);
        }
        
        if (allRenderers.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "选中对象及其子物体中没有SpriteRenderer！", "确定");
            return;
        }
        
        // 为每个SpriteRenderer独立计算Order（基于它自己的位置）
        foreach (SpriteRenderer sr in allRenderers)
            {
                Undo.RecordObject(sr, "Set Order in Layer");
                
            // ✅ 跳过特殊标记的物体（Order < -9990）
            if (sr.sortingOrder < -9990)
            {
                Debug.Log($"<color=grey>[{GetGameObjectPath(sr.gameObject)}] Order={sr.sortingOrder} < -9990，跳过处理</color>");
                continue;
            }
            
            float sortingY = CalculateSortingY(sr, sr.transform);
                int calculatedOrder = -Mathf.RoundToInt(sortingY * sort_multiplier) + sort_orderOffset;
            
            // 特殊处理：Shadow子物体
            if (sr.gameObject.name.ToLower().Contains("shadow"))
            {
                // Shadow需要在父物体下方
                // 先找父物体的SR
                Transform parent = sr.transform.parent;
                if (parent != null)
                {
                    SpriteRenderer parentSr = parent.GetComponent<SpriteRenderer>();
                    if (parentSr != null)
                    {
                        float parentSortY = CalculateSortingY(parentSr, parent);
                        int parentOrder = -Mathf.RoundToInt(parentSortY * sort_multiplier) + sort_orderOffset;
                        calculatedOrder = parentOrder + sort_shadowOffset;
                        
                        Debug.Log($"  ↳ [Shadow: {sr.gameObject.name}] 父Order={parentOrder} → Shadow Order={calculatedOrder}");
                    }
                }
            }
            else if (sr.gameObject.name.ToLower().Contains("glow") || 
                     sr.gameObject.name.ToLower().Contains("light") || 
                     sr.gameObject.name.ToLower().Contains("effect"))
            {
                // Glow与父物体同层
                Transform parent = sr.transform.parent;
                if (parent != null)
                {
                    SpriteRenderer parentSr = parent.GetComponent<SpriteRenderer>();
                    if (parentSr != null)
                    {
                        float parentSortY = CalculateSortingY(parentSr, parent);
                        int parentOrder = -Mathf.RoundToInt(parentSortY * sort_multiplier) + sort_orderOffset;
                        calculatedOrder = parentOrder + sort_glowOffset;
                        
                        Debug.Log($"  ↳ [Glow: {sr.gameObject.name}] 父Order={parentOrder} → Glow Order={calculatedOrder}");
                    }
                }
            }
            
            // ✅ 可选：设置Sorting Layer
            if (sort_chk_layer)
            {
                sr.sortingLayerName = sort_layer;
            }
            
            sr.sortingOrder = calculatedOrder;
            EditorUtility.SetDirty(sr);
            count++;
            
            // 🔍 详细调试输出
            string path = GetGameObjectPath(sr.gameObject);
            Collider2D col = sr.GetComponent<Collider2D>();
            string source = col != null ? "Collider" : (sr.sprite != null ? "Sprite" : "Transform");
            
            string debugInfo = $"[{path}]\n" +
                              $"  Transform.Y = {sr.transform.position.y:F3}\n";
            
            if (col != null)
                debugInfo += $"  Collider.min.y = {col.bounds.min.y:F3} ✅\n";
            if (sr.sprite != null)
                debugInfo += $"  Sprite.min.y = {sr.bounds.min.y:F3}" + (col == null ? " ✅" : "") + "\n";
            
            debugInfo += $"  → 用{source}底部Y = {sortingY:F3}\n" +
                        $"  → 计算 = -Round({sortingY:F3} × {sort_multiplier}) + {sort_orderOffset}\n" +
                        $"  → Order = {calculatedOrder}";
            
            Debug.Log(debugInfo);
        }
        
        string msg = $"已设置 {count} 个SpriteRenderer";
        if (sort_chk_layer)
            msg += $"\n• Sorting Layer: {sort_layer}";
        msg += "\n• Order: 自动计算（基于Collider底部）";
        
        EditorUtility.DisplayDialog("完成", msg, "确定");
        Debug.Log($"<color=green>[002批量] 设置完成！共处理 {count} 个对象{(sort_chk_layer ? $"，Layer={sort_layer}" : "")}</color>");
    }
    
    /// <summary>
    /// 计算排序用的Y坐标
    /// 🔥 核心修正：双层结构（父物体无SR）时用父物体的Y坐标
    /// 核心：优先使用Collider底部，回退到Sprite底部
    /// </summary>
    private float CalculateSortingY(SpriteRenderer sr, Transform trans)
    {
        float sortingY;
        
        // 🔥 关键：双层结构检测（父物体无SpriteRenderer）
        // 如Tree_M1_XX（父）/ Tree（子）结构，用父物体的Y坐标（种植点）
        Transform parent = trans.parent;
        if (parent != null)
        {
            SpriteRenderer parentSr = parent.GetComponent<SpriteRenderer>();
            if (parentSr == null)
            {
                // 父物体没有SR → 双层结构，用父物体的Y坐标
                sortingY = parent.position.y + sort_bottomOffset;
                return sortingY;
            }
        }
        
        // 常规计算：优先Collider，回退Sprite，最后Transform
        Collider2D collider = sr.GetComponent<Collider2D>();
        
        if (collider != null && sort_useSpriteBounds)
        {
            // 使用Collider底部 = 物理边界的最低点
            sortingY = collider.bounds.min.y + sort_bottomOffset;
        }
        else if (sort_useSpriteBounds && sr.sprite != null)
        {
            // 回退：使用Sprite底部
            sortingY = sr.bounds.min.y + sort_bottomOffset;
                }
                else
                {
            // Fallback：使用Transform位置
            sortingY = trans.position.y + sort_bottomOffset;
        }
        
        return sortingY;
    }
    
    
    private void ShowCurrentOrders()
    {
        if (selectedObjs.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先选择对象！", "确定");
            return;
        }
        
        Debug.Log("========== 当前选中物体的Order信息 ==========");
        
        foreach (GameObject obj in selectedObjs)
        {
            SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
            
            Debug.Log($"[{obj.name}] 包含 {renderers.Length} 个SpriteRenderer:");
            
            foreach (SpriteRenderer sr in renderers)
            {
                string path = GetGameObjectPath(sr.gameObject);
                Debug.Log($"  • {path}\n    Layer: {sr.sortingLayerName}, Order: {sr.sortingOrder}");
            }
        }
        
        Debug.Log("==========================================");
    }
    
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        
        return path;
    }


    #endregion

    #region ========== Transform模式 ==========

    private bool tf_chk_pos = false;
    private bool tf_chk_rot = false;
    private bool tf_chk_scale = false;
    private bool tf_offset = false;
    
    private Vector3 tf_pos = Vector3.zero;
    private Vector3 tf_rot = Vector3.zero;
    private Vector3 tf_scale = Vector3.one;
    private float tf_quickY = 0.5f;

    private void DrawTransformMode()
    {
        EditorGUILayout.LabelField("⚡ 快速Y轴偏移", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("偏移值:", GUILayout.Width(60));
        tf_quickY = EditorGUILayout.FloatField(tf_quickY, GUILayout.Width(60));
        
        GUI.enabled = selectedObjs.Count > 0;
        if (GUILayout.Button("↑ 上移", GUILayout.Width(70)))
            QuickOffsetY(tf_quickY);
        if (GUILayout.Button("↓ 下移", GUILayout.Width(70)))
            QuickOffsetY(-tf_quickY);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        
        DrawLine();
        
        EditorGUILayout.LabelField("⚙️ 详细设置", EditorStyles.boldLabel);
        
        tf_offset = EditorGUILayout.ToggleLeft("偏移模式（非设置模式）", tf_offset);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        tf_chk_pos = EditorGUILayout.Toggle(tf_chk_pos, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!tf_chk_pos);
        tf_pos = EditorGUILayout.Vector3Field("Position", tf_pos);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        tf_chk_rot = EditorGUILayout.Toggle(tf_chk_rot, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!tf_chk_rot);
        tf_rot = EditorGUILayout.Vector3Field("Rotation", tf_rot);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        tf_chk_scale = EditorGUILayout.Toggle(tf_chk_scale, GUILayout.Width(20));
        EditorGUI.BeginDisabledGroup(!tf_chk_scale);
        tf_scale = EditorGUILayout.Vector3Field("Scale", tf_scale);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        
        DrawLine();
        
        GUI.enabled = selectedObjs.Count > 0;
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("🚀 应用Transform设置", GUILayout.Height(40)))
            ApplyTransformSettings();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
    }

    private void QuickOffsetY(float offset)
    {
        Undo.RecordObjects(selectedObjs.ToArray(), "Quick Offset Y");
        
        foreach (var obj in selectedObjs)
        {
            Vector3 pos = obj.transform.position;
            pos.y += offset;
            obj.transform.position = pos;
            EditorUtility.SetDirty(obj.transform);
        }
        
        Debug.Log($"<color=green>[002批量] Y轴偏移 {offset:+0.00;-0.00}，共{selectedObjs.Count}个对象</color>");
    }

    private void ApplyTransformSettings()
    {
        if (!tf_chk_pos && !tf_chk_rot && !tf_chk_scale)
        {
            EditorUtility.DisplayDialog("提示", "请至少勾选一个选项！", "确定");
            return;
        }
        
        Undo.RecordObjects(selectedObjs.ToArray(), "Apply Transform Settings");
        
        foreach (var obj in selectedObjs)
        {
            if (tf_chk_pos)
            {
                if (tf_offset)
                    obj.transform.position += tf_pos;
                else
                    obj.transform.position = tf_pos;
            }
            
            if (tf_chk_rot)
            {
                if (tf_offset)
                    obj.transform.eulerAngles += tf_rot;
                else
                    obj.transform.eulerAngles = tf_rot;
            }
            
            if (tf_chk_scale)
            {
                if (tf_offset)
                    obj.transform.localScale = Vector3.Scale(obj.transform.localScale, tf_scale);
                else
                    obj.transform.localScale = tf_scale;
            }
            
            EditorUtility.SetDirty(obj.transform);
        }
        
        Debug.Log($"<color=green>[002批量] Transform设置完成！{selectedObjs.Count}个对象</color>");
    }

    #endregion

    #region ========== 碰撞器模式 ==========

    private enum ColliderType { BoxCollider2D, CircleCollider2D, PolygonCollider2D }
    private ColliderType col_type = ColliderType.BoxCollider2D;
    private bool col_trigger = false;
    private bool col_addRb = false;
    
    private Vector2 col_boxSize = Vector2.one;
    private float col_circleRadius = 0.5f;

    private void DrawColliderMode()
    {
        EditorGUILayout.LabelField("⚡ 快速预设", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("角色碰撞器"))
        {
            col_type = ColliderType.BoxCollider2D;
            col_trigger = false;
            col_addRb = true;
            col_boxSize = new Vector2(0.8f, 1f);
        }
        if (GUILayout.Button("墙体碰撞器"))
        {
            col_type = ColliderType.BoxCollider2D;
            col_trigger = false;
            col_addRb = false;
            col_boxSize = Vector2.one;
        }
        if (GUILayout.Button("触发器"))
        {
            col_type = ColliderType.BoxCollider2D;
            col_trigger = true;
            col_addRb = false;
            col_boxSize = Vector2.one;
        }
        EditorGUILayout.EndHorizontal();
        
        DrawLine();
        
        EditorGUILayout.LabelField("⚙️ 详细设置", EditorStyles.boldLabel);
        
        col_type = (ColliderType)EditorGUILayout.EnumPopup("碰撞器类型", col_type);
        col_trigger = EditorGUILayout.Toggle("Is Trigger", col_trigger);
        col_addRb = EditorGUILayout.Toggle("添加Rigidbody2D", col_addRb);
        
        EditorGUILayout.Space();
        
        if (col_type == ColliderType.BoxCollider2D)
        {
            col_boxSize = EditorGUILayout.Vector2Field("Box Size", col_boxSize);
        }
        else if (col_type == ColliderType.CircleCollider2D)
        {
            col_circleRadius = EditorGUILayout.FloatField("Circle Radius", col_circleRadius);
        }
        
        DrawLine();
        
        GUI.enabled = selectedObjs.Count > 0;
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("🚀 添加碰撞器", GUILayout.Height(40)))
            ApplyCollider();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
    }

    private void ApplyCollider()
    {
        Undo.RecordObjects(selectedObjs.ToArray(), "Add Colliders");
        
        int count = 0;
        
        foreach (var obj in selectedObjs)
        {
            Collider2D collider = null;
            
            switch (col_type)
            {
                case ColliderType.BoxCollider2D:
                    var box = obj.GetComponent<BoxCollider2D>();
                    if (box == null) box = obj.AddComponent<BoxCollider2D>();
                    box.size = col_boxSize;
                    box.isTrigger = col_trigger;
                    collider = box;
                    break;
                    
                case ColliderType.CircleCollider2D:
                    var circle = obj.GetComponent<CircleCollider2D>();
                    if (circle == null) circle = obj.AddComponent<CircleCollider2D>();
                    circle.radius = col_circleRadius;
                    circle.isTrigger = col_trigger;
                    collider = circle;
                    break;
                    
                case ColliderType.PolygonCollider2D:
                    var poly = obj.GetComponent<PolygonCollider2D>();
                    if (poly == null) poly = obj.AddComponent<PolygonCollider2D>();
                    poly.isTrigger = col_trigger;
                    collider = poly;
                    break;
            }
            
            if (col_addRb)
            {
                var rb = obj.GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    rb = obj.AddComponent<Rigidbody2D>();
                    rb.bodyType = RigidbodyType2D.Dynamic;
                }
            }
            
            if (collider != null)
            {
                EditorUtility.SetDirty(obj);
                count++;
            }
        }
        
        EditorUtility.DisplayDialog("完成", $"成功添加碰撞器：{count}个对象", "确定");
        Debug.Log($"<color=green>[002批量] 添加碰撞器完成！{count}个对象</color>");
    }

    #endregion

    #region ========== 设置保存/加载 ==========

    private void LoadSettings()
    {
        // 排序层设置
        sort_chk_layer = EditorPrefs.GetBool("Batch002_Sort_ChkLayer", false);
        sort_layer = EditorPrefs.GetString("Batch002_Sort_Layer", "Default");
        sort_quickOffset = EditorPrefs.GetInt("Batch002_Sort_QuickOffset", 1);
        
        // 排序层 - Y坐标计算
        sort_multiplier = EditorPrefs.GetInt("Batch002_Sort_Multiplier", 100);
        sort_orderOffset = EditorPrefs.GetInt("Batch002_Sort_OrderOffset", 0);
        sort_useSpriteBounds = EditorPrefs.GetBool("Batch002_Sort_UseSpriteBounds", true);
        sort_bottomOffset = EditorPrefs.GetFloat("Batch002_Sort_BottomOffset", 0f);
        sort_shadowOffset = EditorPrefs.GetInt("Batch002_Sort_ShadowOffset", -1);
        sort_glowOffset = EditorPrefs.GetInt("Batch002_Sort_GlowOffset", 0);
        
        // Transform
        tf_chk_pos = EditorPrefs.GetBool("Batch002_TF_ChkPos", false);
        tf_chk_rot = EditorPrefs.GetBool("Batch002_TF_ChkRot", false);
        tf_chk_scale = EditorPrefs.GetBool("Batch002_TF_ChkScale", false);
        tf_offset = EditorPrefs.GetBool("Batch002_TF_Offset", false);
        tf_quickY = EditorPrefs.GetFloat("Batch002_TF_QuickY", 0.5f);
        
        // 碰撞器
        col_type = (ColliderType)EditorPrefs.GetInt("Batch002_Col_Type", 0);
        col_trigger = EditorPrefs.GetBool("Batch002_Col_Trigger", false);
        col_addRb = EditorPrefs.GetBool("Batch002_Col_AddRb", false);
    }

    private void SaveSettings()
    {
        // 排序层设置
        EditorPrefs.SetBool("Batch002_Sort_ChkLayer", sort_chk_layer);
        EditorPrefs.SetString("Batch002_Sort_Layer", sort_layer);
        EditorPrefs.SetInt("Batch002_Sort_QuickOffset", sort_quickOffset);
        
        // 排序层 - Y坐标计算
        EditorPrefs.SetInt("Batch002_Sort_Multiplier", sort_multiplier);
        EditorPrefs.SetInt("Batch002_Sort_OrderOffset", sort_orderOffset);
        EditorPrefs.SetBool("Batch002_Sort_UseSpriteBounds", sort_useSpriteBounds);
        EditorPrefs.SetFloat("Batch002_Sort_BottomOffset", sort_bottomOffset);
        EditorPrefs.SetInt("Batch002_Sort_ShadowOffset", sort_shadowOffset);
        EditorPrefs.SetInt("Batch002_Sort_GlowOffset", sort_glowOffset);
        
        // Transform
        EditorPrefs.SetBool("Batch002_TF_ChkPos", tf_chk_pos);
        EditorPrefs.SetBool("Batch002_TF_ChkRot", tf_chk_rot);
        EditorPrefs.SetBool("Batch002_TF_ChkScale", tf_chk_scale);
        EditorPrefs.SetBool("Batch002_TF_Offset", tf_offset);
        EditorPrefs.SetFloat("Batch002_TF_QuickY", tf_quickY);
        
        // 碰撞器
        EditorPrefs.SetInt("Batch002_Col_Type", (int)col_type);
        EditorPrefs.SetBool("Batch002_Col_Trigger", col_trigger);
        EditorPrefs.SetBool("Batch002_Col_AddRb", col_addRb);
    }

    private void ResetCurrentMode()
    {
        switch (currentMode)
        {
            case ToolMode.Order:
                sort_chk_layer = false;
                sort_layer = "Default";
                sort_quickOffset = 1;
                sort_multiplier = 100;
                sort_orderOffset = 0;
                sort_useSpriteBounds = true;
                sort_bottomOffset = 0f;
                sort_shadowOffset = -1;
                sort_glowOffset = 0;
                break;
                
            case ToolMode.Transform:
                tf_chk_pos = false;
                tf_chk_rot = false;
                tf_chk_scale = false;
                tf_offset = false;
                tf_pos = Vector3.zero;
                tf_rot = Vector3.zero;
                tf_scale = Vector3.one;
                tf_quickY = 0.5f;
                break;
                
            case ToolMode.碰撞器:
                col_type = ColliderType.BoxCollider2D;
                col_trigger = false;
                col_addRb = false;
                col_boxSize = Vector2.one;
                col_circleRadius = 0.5f;
                break;
        }
        
        SaveSettings();
        Repaint();
    }

    #endregion
}

