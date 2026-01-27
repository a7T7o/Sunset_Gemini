using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;

/// <summary>
/// 耕地 Tile 批量创建工具
/// 菜单：Tools/Farm/Create Farmland Tiles
/// </summary>
public class FarmlandTileCreator : EditorWindow
{
    [MenuItem("Tools/Farm/Create Farmland Tiles")]
    public static void CreateAllTiles()
    {
        // Sprite 源目录
        string spriteBasePath = "Assets/Sprites/Farm/Farmland";
        // Tile 输出目录
        string tileBasePath = "Assets/ZZZ_999_Package/000_Tile/Farmland";
        
        int createdCount = 0;
        
        // 创建 Border Tiles (15个)
        string[] borderNames = {
            "Farm_D", "Farm_DL", "Farm_DLR", "Farm_DR",
            "Farm_L", "Farm_LR", "Farm_R",
            "Farm_U", "Farm_UD", "Farm_UDL", "Farm_UDLR", "Farm_UDR",
            "Farm_UL", "Farm_ULR", "Farm_UR"
        };
        
        foreach (var name in borderNames)
        {
            if (CreateTile($"{spriteBasePath}/Border/{name}.png", $"{tileBasePath}/Border/Tile_{name}.asset"))
                createdCount++;
        }
        
        // 创建 Center Tiles (2个)
        string[] centerNames = { "Farm_C0", "Farm_C1" };
        foreach (var name in centerNames)
        {
            if (CreateTile($"{spriteBasePath}/Center/{name}.png", $"{tileBasePath}/Center/Tile_{name}.asset"))
                createdCount++;
        }
        
        // 创建 Shadow Tiles (4个)
        string[] shadowNames = { "Farm_SLD", "Farm_SLU", "Farm_SRD", "Farm_SRU" };
        foreach (var name in shadowNames)
        {
            if (CreateTile($"{spriteBasePath}/Shadow/{name}.png", $"{tileBasePath}/Shadow/Tile_{name}.asset"))
                createdCount++;
        }
        
        // 创建 Water Tiles (3个)
        string[] waterNames = { "Water_A", "Water_B", "Water_C" };
        foreach (var name in waterNames)
        {
            if (CreateTile($"{spriteBasePath}/Water/{name}.png", $"{tileBasePath}/Water/Tile_{name}.asset"))
                createdCount++;
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[FarmlandTileCreator] 创建完成！共创建 {createdCount} 个 Tile 资产");
        EditorUtility.DisplayDialog("完成", $"成功创建 {createdCount} 个 Tile 资产！", "确定");
    }
    
    private static bool CreateTile(string spritePath, string tilePath)
    {
        // 加载 Sprite
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogWarning($"[FarmlandTileCreator] 找不到 Sprite: {spritePath}");
            return false;
        }
        
        // 检查 Tile 是否已存在
        if (File.Exists(tilePath))
        {
            Debug.Log($"[FarmlandTileCreator] Tile 已存在，跳过: {tilePath}");
            return false;
        }
        
        // 确保目录存在
        string directory = Path.GetDirectoryName(tilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        // 创建 Tile
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = Color.white;
        
        // 保存 Tile 资产
        AssetDatabase.CreateAsset(tile, tilePath);
        Debug.Log($"[FarmlandTileCreator] 创建 Tile: {tilePath}");
        
        return true;
    }
    
    [MenuItem("Tools/Farm/Validate Farmland Tiles")]
    public static void ValidateTiles()
    {
        string tileBasePath = "Assets/ZZZ_999_Package/000_Tile/Farmland";
        
        int found = 0;
        int missing = 0;
        
        // 检查所有预期的 Tile
        string[] allTiles = {
            // Border
            "Border/Tile_Farm_D", "Border/Tile_Farm_DL", "Border/Tile_Farm_DLR", "Border/Tile_Farm_DR",
            "Border/Tile_Farm_L", "Border/Tile_Farm_LR", "Border/Tile_Farm_R",
            "Border/Tile_Farm_U", "Border/Tile_Farm_UD", "Border/Tile_Farm_UDL", "Border/Tile_Farm_UDLR", "Border/Tile_Farm_UDR",
            "Border/Tile_Farm_UL", "Border/Tile_Farm_ULR", "Border/Tile_Farm_UR",
            // Center
            "Center/Tile_Farm_C0", "Center/Tile_Farm_C1",
            // Shadow
            "Shadow/Tile_Farm_SLD", "Shadow/Tile_Farm_SLU", "Shadow/Tile_Farm_SRD", "Shadow/Tile_Farm_SRU",
            // Water
            "Water/Tile_Water_A", "Water/Tile_Water_B", "Water/Tile_Water_C"
        };
        
        foreach (var tileName in allTiles)
        {
            string path = $"{tileBasePath}/{tileName}.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            
            if (tile != null)
            {
                found++;
                Debug.Log($"✓ {tileName}");
            }
            else
            {
                missing++;
                Debug.LogWarning($"✗ 缺失: {tileName}");
            }
        }
        
        Debug.Log($"[FarmlandTileCreator] 验证完成: {found} 个存在, {missing} 个缺失");
    }
}
