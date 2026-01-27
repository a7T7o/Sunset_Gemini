using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using FarmGame.Data;

/// <summary>
/// 工具材料等级批量修复工具
/// 根据文件名后缀自动设置 materialTier 字段
/// 文件名格式：Tool_{ItemID}_{ToolType}_{MaterialTierSuffix}.asset
/// </summary>
public class ToolMaterialTierFixer : EditorWindow
{
    private string toolsFolder = "Assets/111_Data/Items/Tools";
    private Vector2 scrollPos;
    private bool showPreview = true;
    
    [MenuItem("Tools/🔧 工具材料等级修复")]
    public static void ShowWindow()
    {
        var window = GetWindow<ToolMaterialTierFixer>("工具材料等级修复");
        window.minSize = new Vector2(500, 400);
    }
    
    [MenuItem("Tools/🔧 立即修复所有工具材料等级")]
    public static void FixAllToolsNow()
    {
        string toolsFolder = "Assets/111_Data/Items/Tools";
        string[] guids = AssetDatabase.FindAssets("t:ToolData", new[] { toolsFolder });
        
        int fixedCount = 0;
        int skippedCount = 0;
        
        Debug.Log($"<color=cyan>[ToolMaterialTierFixer] 开始修复，找到 {guids.Length} 个工具 SO 文件</color>");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            
            // 解析文件名获取材料等级后缀
            int expectedTier = ParseMaterialTierFromFileNameStatic(fileName);
            if (expectedTier < 0)
            {
                skippedCount++;
                continue;
            }
            
            ToolData toolData = AssetDatabase.LoadAssetAtPath<ToolData>(path);
            if (toolData == null) continue;
            
            int currentTier = (int)toolData.materialTier;
            
            if (currentTier != expectedTier)
            {
                // 修复
                toolData.materialTier = (MaterialTier)expectedTier;
                EditorUtility.SetDirty(toolData);
                Debug.Log($"<color=green>[已修复] {fileName}: {currentTier} → {expectedTier} ({GetTierNameStatic(expectedTier)})</color>");
                fixedCount++;
            }
            else
            {
                Debug.Log($"<color=gray>[已正确] {fileName}: {currentTier} ({GetTierNameStatic(currentTier)})</color>");
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"<color=cyan>[ToolMaterialTierFixer] 修复完成！已修复 {fixedCount} 个文件，跳过 {skippedCount} 个文件</color>");
        EditorUtility.DisplayDialog("修复完成", $"已修复 {fixedCount} 个工具 SO 文件", "确定");
    }
    
    private static int ParseMaterialTierFromFileNameStatic(string fileName)
    {
        var match = Regex.Match(fileName, @"Tool_\d+_\w+_(\d+)$");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int tier))
        {
            return tier;
        }
        return -1;
    }
    
    private static string GetTierNameStatic(int tier)
    {
        return tier switch
        {
            0 => "Wood",
            1 => "Stone",
            2 => "Iron",
            3 => "Brass",
            4 => "Steel",
            5 => "Gold",
            _ => "Unknown"
        };
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("工具材料等级批量修复", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "根据文件名后缀自动设置 materialTier 字段\n" +
            "文件名格式：Tool_{ItemID}_{ToolType}_{MaterialTierSuffix}.asset\n" +
            "例如：Tool_8_Pickaxe_2.asset → materialTier = 2 (Iron)", 
            MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        toolsFolder = EditorGUILayout.TextField("工具文件夹", toolsFolder);
        showPreview = EditorGUILayout.Toggle("显示预览", showPreview);
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("预览修改", GUILayout.Height(30)))
        {
            PreviewChanges();
        }
        if (GUILayout.Button("执行修复", GUILayout.Height(30)))
        {
            ExecuteFix();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // 显示材料等级对照表
        EditorGUILayout.LabelField("材料等级对照表", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("0 = Wood (木质)");
        EditorGUILayout.LabelField("1 = Stone (石质)");
        EditorGUILayout.LabelField("2 = Iron (生铁)");
        EditorGUILayout.LabelField("3 = Brass (黄铜)");
        EditorGUILayout.LabelField("4 = Steel (钢质)");
        EditorGUILayout.LabelField("5 = Gold (金质)");
        EditorGUILayout.EndVertical();
    }
    
    private void PreviewChanges()
    {
        string[] guids = AssetDatabase.FindAssets("t:ToolData", new[] { toolsFolder });
        
        Debug.Log($"<color=cyan>[ToolMaterialTierFixer] 找到 {guids.Length} 个工具 SO 文件</color>");
        Debug.Log("========== 预览修改 ==========");
        
        int needFixCount = 0;
        int correctCount = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            
            // 解析文件名获取材料等级后缀
            int expectedTier = ParseMaterialTierFromFileName(fileName);
            if (expectedTier < 0)
            {
                Debug.LogWarning($"[跳过] {fileName} - 无法解析材料等级后缀");
                continue;
            }
            
            ToolData toolData = AssetDatabase.LoadAssetAtPath<ToolData>(path);
            if (toolData == null) continue;
            
            int currentTier = (int)toolData.materialTier;
            
            if (currentTier != expectedTier)
            {
                Debug.Log($"<color=yellow>[需修复] {fileName}: {currentTier} → {expectedTier} ({GetTierName(expectedTier)})</color>");
                needFixCount++;
            }
            else
            {
                Debug.Log($"<color=green>[正确] {fileName}: {currentTier} ({GetTierName(currentTier)})</color>");
                correctCount++;
            }
        }
        
        Debug.Log("========== 预览完成 ==========");
        Debug.Log($"<color=cyan>需修复: {needFixCount} 个, 已正确: {correctCount} 个</color>");
    }
    
    private void ExecuteFix()
    {
        string[] guids = AssetDatabase.FindAssets("t:ToolData", new[] { toolsFolder });
        
        int fixedCount = 0;
        int skippedCount = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            
            // 解析文件名获取材料等级后缀
            int expectedTier = ParseMaterialTierFromFileName(fileName);
            if (expectedTier < 0)
            {
                skippedCount++;
                continue;
            }
            
            ToolData toolData = AssetDatabase.LoadAssetAtPath<ToolData>(path);
            if (toolData == null) continue;
            
            int currentTier = (int)toolData.materialTier;
            
            if (currentTier != expectedTier)
            {
                // 修复
                toolData.materialTier = (MaterialTier)expectedTier;
                EditorUtility.SetDirty(toolData);
                Debug.Log($"<color=green>[已修复] {fileName}: {currentTier} → {expectedTier} ({GetTierName(expectedTier)})</color>");
                fixedCount++;
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"<color=cyan>[ToolMaterialTierFixer] 修复完成！已修复 {fixedCount} 个文件，跳过 {skippedCount} 个文件</color>");
        EditorUtility.DisplayDialog("修复完成", $"已修复 {fixedCount} 个工具 SO 文件", "确定");
    }
    
    /// <summary>
    /// 从文件名解析材料等级后缀
    /// 文件名格式：Tool_{ItemID}_{ToolType}_{MaterialTierSuffix}
    /// 例如：Tool_8_Pickaxe_2 → 返回 2
    /// </summary>
    private int ParseMaterialTierFromFileName(string fileName)
    {
        // 正则匹配：Tool_{数字}_{工具类型}_{材料等级}
        var match = Regex.Match(fileName, @"Tool_\d+_\w+_(\d+)$");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int tier))
        {
            return tier;
        }
        return -1;
    }
    
    private string GetTierName(int tier)
    {
        return tier switch
        {
            0 => "Wood",
            1 => "Stone",
            2 => "Iron",
            3 => "Brass",
            4 => "Steel",
            5 => "Gold",
            _ => "Unknown"
        };
    }
}
