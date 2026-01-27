using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 批量应用树木渐变材质工具
/// 使用：选中多个树木预制体 → Tools → 遮挡透明 → 树木材质批量应用
/// 自动处理双层结构（父物体/Tree子物体/Shadow子物体）
/// </summary>
public class BatchApplyTreeMaterial : EditorWindow
{
    private Material treeMaterial;
    private bool applyToTreeChild = true;
    private bool keepShadowMaterial = true;
    private bool addOcclusionComponent = true;
    private Vector2 scrollPosition;
    private List<string> processLog = new List<string>();
    
    [MenuItem("Tools/遮挡透明/🌳 树木材质批量应用")]
    static void ShowWindow()
    {
        var window = GetWindow<BatchApplyTreeMaterial>("树木材质批量应用");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }
    
    void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🌳 树木渐变材质批量应用工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        EditorGUILayout.HelpBox(
            "此工具用于批量为树木应用渐变透明材质。\n" +
            "• 选中树木预制体或场景中的树木物体\n" +
            "• 拖入渐变材质（TreeOcclusion）\n" +
            "• 点击应用按钮",
            MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // 材质选择
        EditorGUILayout.LabelField("━━━━ 材质设置 ━━━━", EditorStyles.boldLabel);
        treeMaterial = (Material)EditorGUILayout.ObjectField(
            "渐变材质", 
            treeMaterial, 
            typeof(Material), 
            false);
        
        if (treeMaterial == null)
        {
            EditorGUILayout.HelpBox(
                "请拖入树木渐变材质（TreeOcclusion.mat）\n" +
                "位置：Assets/Shaders/Material/TreeOcclusion.mat",
                MessageType.Warning);
            
            if (GUILayout.Button("🔍 自动查找 TreeOcclusion 材质"))
            {
                FindTreeOcclusionMaterial();
            }
        }
        
        EditorGUILayout.Space(10);
        
        // 选项设置
        EditorGUILayout.LabelField("━━━━ 应用选项 ━━━━", EditorStyles.boldLabel);
        applyToTreeChild = EditorGUILayout.Toggle("应用到 Tree 子物体", applyToTreeChild);
        keepShadowMaterial = EditorGUILayout.Toggle("保持 Shadow 材质", keepShadowMaterial);
        addOcclusionComponent = EditorGUILayout.Toggle("添加遮挡组件", addOcclusionComponent);
        
        EditorGUILayout.Space(10);
        
        // 选中物体信息
        EditorGUILayout.LabelField("━━━━ 选中物体 ━━━━", EditorStyles.boldLabel);
        GameObject[] selected = Selection.gameObjects;
        EditorGUILayout.LabelField($"已选中：{selected.Length} 个物体");
        
        if (selected.Length == 0)
        {
            EditorGUILayout.HelpBox("请在 Hierarchy 或 Project 中选中要处理的树木物体", MessageType.Info);
        }
        
        EditorGUILayout.Space(10);
        
        // 应用按钮
        EditorGUI.BeginDisabledGroup(treeMaterial == null || selected.Length == 0);
        if (GUILayout.Button("✓ 应用材质到选中物体", GUILayout.Height(30)))
        {
            ApplyMaterialToSelected();
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.Space(10);
        
        // 执行日志
        if (processLog.Count > 0)
        {
            EditorGUILayout.LabelField("━━━━ 执行结果 ━━━━", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
            foreach (var log in processLog)
            {
                EditorGUILayout.LabelField(log);
            }
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("清除日志"))
            {
                processLog.Clear();
            }
        }
        
        EditorGUILayout.Space(10);
        
        // 帮助信息
        EditorGUILayout.LabelField("━━━━ 使用说明 ━━━━", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "💡 树木结构要求：\n" +
            "Tree_M1_00（父物体）\n" +
            "  ├─ Tree（子物体，应用渐变材质）\n" +
            "  └─ Shadow（子物体，保持默认材质）\n\n" +
            "💡 材质说明：\n" +
            "• 渐变材质使用 Custom/VerticalGradientOcclusion Shader\n" +
            "• Shadow 使用默认 Sprites/Default 材质\n" +
            "• 遮挡时树木从下到上渐变透明",
            MessageType.None);
    }
    
    void FindTreeOcclusionMaterial()
    {
        string[] guids = AssetDatabase.FindAssets("TreeOcclusion t:Material");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            treeMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            Debug.Log($"[树木材质工具] 找到材质：{path}");
        }
        else
        {
            EditorUtility.DisplayDialog("未找到", 
                "未找到 TreeOcclusion 材质。\n请先创建材质或手动拖入。", 
                "确定");
        }
    }
    
    void ApplyMaterialToSelected()
    {
        processLog.Clear();
        GameObject[] selected = Selection.gameObjects;
        
        int successCount = 0;
        int skippedCount = 0;
        int errorCount = 0;
        
        foreach (GameObject obj in selected)
        {
            Undo.RecordObject(obj, "Apply Tree Material");
            
            // 查找 Tree 子物体
            Transform treeChild = obj.transform.Find("Tree");
            if (treeChild == null)
            {
                // 如果没有 Tree 子物体，检查自身是否有 SpriteRenderer
                SpriteRenderer selfRenderer = obj.GetComponent<SpriteRenderer>();
                if (selfRenderer != null && applyToTreeChild)
                {
                    // 直接应用到自身
                    if (ApplyMaterialToRenderer(selfRenderer, obj.name))
                    {
                        successCount++;
                        processLog.Add($"✓ {obj.name} - 应用到自身");
                    }
                    else
                    {
                        errorCount++;
                        processLog.Add($"✗ {obj.name} - 应用失败");
                    }
                }
                else
                {
                    skippedCount++;
                    processLog.Add($"⚠ {obj.name} - 未找到 Tree 子物体，跳过");
                }
                continue;
            }
            
            // 应用材质到 Tree 子物体
            if (applyToTreeChild)
            {
                SpriteRenderer treeRenderer = treeChild.GetComponent<SpriteRenderer>();
                if (treeRenderer != null)
                {
                    if (ApplyMaterialToRenderer(treeRenderer, obj.name))
                    {
                        successCount++;
                        processLog.Add($"✓ {obj.name}/Tree - 应用渐变材质");
                    }
                    else
                    {
                        errorCount++;
                        processLog.Add($"✗ {obj.name}/Tree - 应用失败");
                    }
                }
            }
            
            // 处理 Shadow 子物体
            if (keepShadowMaterial)
            {
                Transform shadowChild = obj.transform.Find("Shadow");
                if (shadowChild != null)
                {
                    SpriteRenderer shadowRenderer = shadowChild.GetComponent<SpriteRenderer>();
                    if (shadowRenderer != null)
                    {
                        // 确保 Shadow 使用默认材质
                        Material defaultMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
                        if (defaultMat != null && shadowRenderer.sharedMaterial != defaultMat)
                        {
                            shadowRenderer.sharedMaterial = defaultMat;
                            EditorUtility.SetDirty(shadowChild.gameObject);
                            processLog.Add($"  └─ {obj.name}/Shadow - 保持默认材质");
                        }
                    }
                }
            }
            
            // 添加遮挡组件
            if (addOcclusionComponent)
            {
                OcclusionTransparency occlusion = obj.GetComponent<OcclusionTransparency>();
                if (occlusion == null)
                {
                    occlusion = obj.AddComponent<OcclusionTransparency>();
                    processLog.Add($"  └─ {obj.name} - 添加 OcclusionTransparency");
                }
                
                // 设置标签
                if (!obj.CompareTag("Tree"))
                {
                    try
                    {
                        obj.tag = "Tree";
                        processLog.Add($"  └─ {obj.name} - 设置标签为 Tree");
                    }
                    catch
                    {
                        processLog.Add($"  ⚠ {obj.name} - 无法设置标签（请先在 Tags 中添加 Tree）");
                    }
                }
            }
            
            EditorUtility.SetDirty(obj);
        }
        
        // 显示结果
        string message = $"处理完成！\n\n" +
                        $"✓ 成功：{successCount} 个\n" +
                        $"⚠ 跳过：{skippedCount} 个\n" +
                        $"✗ 失败：{errorCount} 个";
        
        EditorUtility.DisplayDialog("执行结果", message, "确定");
        Debug.Log($"<color=green>[树木材质工具] 成功: {successCount}, 跳过: {skippedCount}, 失败: {errorCount}</color>");
    }
    
    bool ApplyMaterialToRenderer(SpriteRenderer renderer, string objName)
    {
        if (renderer == null || treeMaterial == null)
            return false;
        
        try
        {
            renderer.sharedMaterial = treeMaterial;
            EditorUtility.SetDirty(renderer.gameObject);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[树木材质工具] 应用材质失败 {objName}: {e.Message}");
            return false;
        }
    }
}
