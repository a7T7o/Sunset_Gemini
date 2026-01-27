using UnityEngine;
using UnityEditor;

/// <summary>
/// 批量添加遮挡透明组件（CompositeCollider2D方案）
/// 使用：选中多个树/房屋父物体 → Tools → 批量添加遮挡组件
/// 自动处理双层结构（父物体/Tree子物体/Shadow子物体）
/// </summary>
public class BatchAddOcclusionComponents : Editor
{
    [MenuItem("Tools/🌳 批量添加遮挡组件（CompositeCollider）")]
    static void AddOcclusionComponents()
    {
        GameObject[] selected = Selection.gameObjects;
        
        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先选中要处理的父物体（Tree_M1_XX）", "确定");
            return;
        }
        
        int successCount = 0;
        int skippedCount = 0;
        
        foreach (GameObject parentObj in selected)
        {
            Undo.RecordObject(parentObj, "Add Occlusion Components");
            
            // 0. 安全检查：跳过系统物体（名字包含System/Manager等）
            string objName = parentObj.name.ToLower();
            if (objName.Contains("system") || objName.Contains("manager") || 
                objName.Contains("service") || objName.Contains("controller"))
            {
                Debug.LogWarning($"[{parentObj.name}] 跳过系统/管理器物体");
                skippedCount++;
                continue;
            }
            
            // 1. 查找Tree子物体
            Transform treeChild = parentObj.transform.Find("Tree");
            if (treeChild == null)
            {
                Debug.LogWarning($"[{parentObj.name}] 未找到Tree子物体，跳过");
                skippedCount++;
                continue;
            }
            
            // 2. 确保Tree子物体有PolygonCollider2D
            PolygonCollider2D treePoly = treeChild.GetComponent<PolygonCollider2D>();
            if (treePoly == null)
            {
                Debug.LogWarning($"[{parentObj.name}] Tree子物体缺少PolygonCollider2D，跳过");
                skippedCount++;
                continue;
            }
            
            // 3. 父物体添加Rigidbody2D（Static）
            Rigidbody2D rb = parentObj.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = parentObj.AddComponent<Rigidbody2D>();
            }
            // 确保是Static（即使已存在也要检查）
            if (rb.bodyType != RigidbodyType2D.Static)
            {
                rb.bodyType = RigidbodyType2D.Static;
                EditorUtility.SetDirty(parentObj);
                Debug.Log($"[{parentObj.name}] 设置 Rigidbody2D → Static ✅");
            }
            
            // 4. Tree子物体的PolygonCollider2D设置CompositeOperation（必须在添加CompositeCollider2D之前）
            if (treePoly.compositeOperation != Collider2D.CompositeOperation.Merge)
            {
                treePoly.compositeOperation = Collider2D.CompositeOperation.Merge;
                EditorUtility.SetDirty(treeChild.gameObject);
                Debug.Log($"[{parentObj.name}] Tree子物体PolygonCollider2D → Composite Operation: Merge ✅");
            }
            
            // 5. 父物体添加CompositeCollider2D（Trigger）
            CompositeCollider2D composite = parentObj.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                composite = parentObj.AddComponent<CompositeCollider2D>();
            }
            // 确保配置正确
            composite.isTrigger = true;
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
            
            // 🔥 强制刷新CompositeCollider2D（重新生成轮廓）
            composite.GenerateGeometry();
            
            EditorUtility.SetDirty(parentObj);
            Debug.Log($"[{parentObj.name}] CompositeCollider2D配置完成（Trigger, Polygons, Synchronous）✅");
            
            // 6. 删除父物体的SpriteRenderer（如果有的话）⭐关键！
            SpriteRenderer parentRenderer = parentObj.GetComponent<SpriteRenderer>();
            if (parentRenderer != null)
            {
                DestroyImmediate(parentRenderer);
                Debug.Log($"[{parentObj.name}] 删除父物体的SpriteRenderer ✅（避免Order计算错误）");
            }
            
            // 7. 父物体添加OcclusionTransparency
            OcclusionTransparency occlusion = parentObj.GetComponent<OcclusionTransparency>();
            if (occlusion == null)
            {
                occlusion = parentObj.AddComponent<OcclusionTransparency>();
                Debug.Log($"[{parentObj.name}] 添加 OcclusionTransparency");
            }
            
            // 7. 配置默认值
            SerializedObject so = new SerializedObject(occlusion);
            so.FindProperty("occludedAlpha").floatValue = 0.3f;
            so.FindProperty("fadeSpeed").floatValue = 8f;
            so.FindProperty("canBeOccluded").boolValue = true;
            so.FindProperty("affectChildren").boolValue = true;
            
            // 设置标签（根据物体名称自动判断）
            SerializedProperty tagsProperty = so.FindProperty("occlusionTags");
            tagsProperty.arraySize = 1;
            
            if (parentObj.name.ToLower().Contains("tree"))
                tagsProperty.GetArrayElementAtIndex(0).stringValue = "Tree";
            else if (parentObj.name.ToLower().Contains("house") || parentObj.name.ToLower().Contains("building"))
                tagsProperty.GetArrayElementAtIndex(0).stringValue = "Building";
            else if (parentObj.name.ToLower().Contains("rock"))
                tagsProperty.GetArrayElementAtIndex(0).stringValue = "Rock";
            else
                tagsProperty.GetArrayElementAtIndex(0).stringValue = "Tree";  // 默认
            
            so.ApplyModifiedProperties();
            
            EditorUtility.SetDirty(parentObj);
            successCount++;
        }
        
        string message = $"✅ 成功处理：{successCount} 个物体\n";
        if (skippedCount > 0)
            message += $"⚠️ 跳过：{skippedCount} 个物体（检查Tree子物体和PolygonCollider2D）\n";
        
        message += "\n已添加组件：\n" +
                  "• Rigidbody2D (Static)\n" +
                  "• CompositeCollider2D (Trigger)\n" +
                  "• OcclusionTransparency\n" +
                  "• Tree子物体PolygonCollider2D → Composite Operation: Merge ✅\n\n" +
                  "💡 CompositeCollider2D会自动合并Tree子物体的轮廓！";
        
        EditorUtility.DisplayDialog("完成", message, "确定");
        Debug.Log($"<color=green>[批量添加遮挡组件] 成功: {successCount}, 跳过: {skippedCount}</color>");
    }
    
    [MenuItem("Tools/🌳 批量添加遮挡组件（CompositeCollider）", true)]
    static bool ValidateAddOcclusionComponents()
    {
        return Selection.gameObjects.Length > 0;
    }
}
