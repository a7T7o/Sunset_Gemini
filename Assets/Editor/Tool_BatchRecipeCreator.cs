using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using FarmGame.Data;

/// <summary>
/// 配方批量创建工具 V2
/// 表格式交互，像 Excel 一样填写
/// </summary>
public class Tool_BatchRecipeCreator : EditorWindow
{
    #region 数据结构

    [System.Serializable]
    private class RecipeEntry
    {
        public bool enabled = true;
        public string name = "";
        public int resultItemID = 0;
        public int resultAmount = 1;
        public List<IngredientEntry> ingredients = new List<IngredientEntry>();
        public bool foldout = false;
    }

    [System.Serializable]
    private class IngredientEntry
    {
        public int itemID = 0;
        public int amount = 1;
    }

    #endregion

    #region 字段

    private Vector2 scrollPos;
    private List<RecipeEntry> recipes = new List<RecipeEntry>();
    
    // === ID 设置 ===
    private int startID = 8000;
    
    // === 共享设置 ===
    private CraftingStation craftingStation = CraftingStation.None;
    private float craftingTime = 0f;
    private bool unlockedByDefault = true;
    private int craftingExp = 10;
    
    // === 快捷材料模板 ===
    private List<IngredientEntry> templateIngredients = new List<IngredientEntry>();
    
    // === 输出设置 ===
    private string outputFolder = "Assets/111_Data/Recipes";

    #endregion

    [MenuItem("Tools/📜 批量创建配方 SO")]
    public static void ShowWindow()
    {
        var window = GetWindow<Tool_BatchRecipeCreator>("批量创建配方SO");
        window.minSize = new Vector2(700, 500);
        window.Show();
    }

    private void OnEnable()
    {
        LoadSettings();
        if (recipes.Count == 0) AddNewRecipe();
    }

    private void OnDisable()
    {
        SaveSettings();
    }

    private void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(5);
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        DrawRecipeTable();
        EditorGUILayout.EndScrollView();
        
        DrawBottomBar();
    }

    #region 顶部工具栏

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        // 左侧：添加按钮
        if (GUILayout.Button("➕ 添加配方", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            AddNewRecipe();
        }
        
        if (GUILayout.Button("➕ 添加5个", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            for (int i = 0; i < 5; i++) AddNewRecipe();
        }
        
        GUILayout.Space(10);
        
        // ID 设置
        GUILayout.Label("起始ID:", GUILayout.Width(45));
        startID = EditorGUILayout.IntField(startID, GUILayout.Width(60));
        
        GUILayout.Space(10);
        
        // 制作设施
        GUILayout.Label("设施:", GUILayout.Width(35));
        craftingStation = (CraftingStation)EditorGUILayout.EnumPopup(craftingStation, GUILayout.Width(100));
        
        GUILayout.FlexibleSpace();
        
        // 右侧：清空和设置
        if (GUILayout.Button("🗑️ 清空", EditorStyles.toolbarButton, GUILayout.Width(55)))
        {
            if (EditorUtility.DisplayDialog("确认", "清空所有配方？", "确定", "取消"))
            {
                recipes.Clear();
                AddNewRecipe();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 第二行：共享设置
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        GUILayout.Label("制作时间:", GUILayout.Width(55));
        craftingTime = EditorGUILayout.FloatField(craftingTime, GUILayout.Width(40));
        GUILayout.Label("秒", GUILayout.Width(20));
        
        GUILayout.Space(15);
        
        GUILayout.Label("经验:", GUILayout.Width(35));
        craftingExp = EditorGUILayout.IntField(craftingExp, GUILayout.Width(40));
        
        GUILayout.Space(15);
        
        unlockedByDefault = GUILayout.Toggle(unlockedByDefault, "默认解锁", GUILayout.Width(70));
        
        GUILayout.FlexibleSpace();
        
        // 输出路径
        GUILayout.Label("输出:", GUILayout.Width(35));
        outputFolder = EditorGUILayout.TextField(outputFolder, GUILayout.Width(200));
        if (GUILayout.Button("...", EditorStyles.toolbarButton, GUILayout.Width(25)))
        {
            string path = EditorUtility.OpenFolderPanel("选择输出文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
            {
                outputFolder = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 表格绘制

    private void DrawRecipeTable()
    {
        // 表头
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.Label("", GUILayout.Width(20));  // 勾选
        GUILayout.Label("ID", EditorStyles.boldLabel, GUILayout.Width(50));
        GUILayout.Label("配方名称", EditorStyles.boldLabel, GUILayout.Width(150));
        GUILayout.Label("产物ID", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("数量", EditorStyles.boldLabel, GUILayout.Width(45));
        GUILayout.Label("材料（点击展开编辑）", EditorStyles.boldLabel);
        GUILayout.Label("", GUILayout.Width(50));  // 操作
        EditorGUILayout.EndHorizontal();
        
        // 数据行
        int removeIndex = -1;
        int duplicateIndex = -1;
        
        for (int i = 0; i < recipes.Count; i++)
        {
            var recipe = recipes[i];
            int recipeID = startID + i;
            
            // 交替背景色
            Color bgColor = i % 2 == 0 ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.25f, 0.25f, 0.25f);
            Rect rowRect = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(rowRect, bgColor);
            
            // 勾选框
            recipe.enabled = EditorGUILayout.Toggle(recipe.enabled, GUILayout.Width(20));
            
            // ID（只读显示）
            GUI.enabled = false;
            EditorGUILayout.IntField(recipeID, GUILayout.Width(50));
            GUI.enabled = true;
            
            // 配方名称
            recipe.name = EditorGUILayout.TextField(recipe.name, GUILayout.Width(150));
            
            // 产物 ID
            recipe.resultItemID = EditorGUILayout.IntField(recipe.resultItemID, GUILayout.Width(70));
            
            // 产物数量
            recipe.resultAmount = EditorGUILayout.IntField(recipe.resultAmount, GUILayout.Width(45));
            
            // 材料预览/展开按钮
            string ingredientPreview = GetIngredientPreview(recipe.ingredients);
            if (GUILayout.Button(ingredientPreview, EditorStyles.miniButton))
            {
                recipe.foldout = !recipe.foldout;
            }
            
            // 操作按钮
            if (GUILayout.Button("📋", GUILayout.Width(24)))
            {
                duplicateIndex = i;
            }
            if (GUILayout.Button("✖", GUILayout.Width(24)))
            {
                removeIndex = i;
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 展开的材料编辑区
            if (recipe.foldout)
            {
                DrawIngredientEditor(recipe);
            }
        }
        
        // 处理删除和复制
        if (removeIndex >= 0 && recipes.Count > 1)
        {
            recipes.RemoveAt(removeIndex);
        }
        if (duplicateIndex >= 0)
        {
            var source = recipes[duplicateIndex];
            var copy = new RecipeEntry
            {
                enabled = true,
                name = source.name + "_copy",
                resultItemID = source.resultItemID,
                resultAmount = source.resultAmount,
                ingredients = new List<IngredientEntry>()
            };
            foreach (var ing in source.ingredients)
            {
                copy.ingredients.Add(new IngredientEntry { itemID = ing.itemID, amount = ing.amount });
            }
            recipes.Insert(duplicateIndex + 1, copy);
        }
    }

    private string GetIngredientPreview(List<IngredientEntry> ingredients)
    {
        if (ingredients.Count == 0) return "点击添加材料 ▼";
        
        var parts = new List<string>();
        foreach (var ing in ingredients)
        {
            parts.Add($"{ing.itemID}×{ing.amount}");
        }
        string preview = string.Join(", ", parts);
        if (preview.Length > 30) preview = preview.Substring(0, 27) + "...";
        return preview + " ▼";
    }

    #endregion

    #region 材料编辑器

    private void DrawIngredientEditor(RecipeEntry recipe)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUI.indentLevel++;
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("材料列表", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        
        // 快捷操作
        if (templateIngredients.Count > 0)
        {
            if (GUILayout.Button("粘贴模板", EditorStyles.miniButton, GUILayout.Width(65)))
            {
                recipe.ingredients.Clear();
                foreach (var ing in templateIngredients)
                {
                    recipe.ingredients.Add(new IngredientEntry { itemID = ing.itemID, amount = ing.amount });
                }
            }
        }
        if (recipe.ingredients.Count > 0)
        {
            if (GUILayout.Button("复制为模板", EditorStyles.miniButton, GUILayout.Width(75)))
            {
                templateIngredients.Clear();
                foreach (var ing in recipe.ingredients)
                {
                    templateIngredients.Add(new IngredientEntry { itemID = ing.itemID, amount = ing.amount });
                }
                Debug.Log($"已复制 {templateIngredients.Count} 个材料为模板");
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // 材料列表
        int removeIngIndex = -1;
        for (int j = 0; j < recipe.ingredients.Count; j++)
        {
            var ing = recipe.ingredients[j];
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label($"材料{j + 1}:", GUILayout.Width(45));
            
            GUILayout.Label("ID:", GUILayout.Width(20));
            ing.itemID = EditorGUILayout.IntField(ing.itemID, GUILayout.Width(70));
            
            GUILayout.Label("数量:", GUILayout.Width(35));
            ing.amount = EditorGUILayout.IntField(ing.amount, GUILayout.Width(40));
            
            // 尝试显示物品名称
            string itemName = GetItemName(ing.itemID);
            if (!string.IsNullOrEmpty(itemName))
            {
                GUILayout.Label($"({itemName})", EditorStyles.miniLabel, GUILayout.Width(100));
            }
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("✖", GUILayout.Width(22)))
            {
                removeIngIndex = j;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        if (removeIngIndex >= 0)
        {
            recipe.ingredients.RemoveAt(removeIngIndex);
        }
        
        // 添加材料按钮
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(20);
        if (GUILayout.Button("+ 添加材料", GUILayout.Width(100)))
        {
            recipe.ingredients.Add(new IngredientEntry { itemID = 0, amount = 1 });
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
    }

    private string GetItemName(int itemID)
    {
        // 尝试从数据库获取物品名称
        string dbPath = DatabaseSyncHelper.DatabasePath;
        if (string.IsNullOrEmpty(dbPath)) return null;
        
        var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(dbPath);
        if (db == null || db.allItems == null) return null;
        
        foreach (var item in db.allItems)
        {
            if (item != null && item.itemID == itemID)
                return item.itemName;
        }
        return null;
    }

    #endregion

    #region 底部栏

    private void DrawBottomBar()
    {
        EditorGUILayout.Space(5);
        
        // 统计信息
        int enabledCount = 0;
        foreach (var r in recipes) if (r.enabled && !string.IsNullOrEmpty(r.name)) enabledCount++;
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"共 {recipes.Count} 个配方，{enabledCount} 个将被创建", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        
        // 批量操作
        if (GUILayout.Button("全选", EditorStyles.miniButton, GUILayout.Width(45)))
        {
            foreach (var r in recipes) r.enabled = true;
        }
        if (GUILayout.Button("全不选", EditorStyles.miniButton, GUILayout.Width(50)))
        {
            foreach (var r in recipes) r.enabled = false;
        }
        if (GUILayout.Button("删除未勾选", EditorStyles.miniButton, GUILayout.Width(70)))
        {
            recipes.RemoveAll(r => !r.enabled);
            if (recipes.Count == 0) AddNewRecipe();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 创建按钮
        GUI.enabled = enabledCount > 0;
        GUI.backgroundColor = new Color(0.3f, 0.85f, 0.3f);
        
        if (GUILayout.Button($"🚀 创建 {enabledCount} 个配方 SO", GUILayout.Height(40)))
        {
            CreateRecipes();
        }
        
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
    }

    #endregion

    #region 创建逻辑

    private void AddNewRecipe()
    {
        recipes.Add(new RecipeEntry
        {
            enabled = true,
            name = "",
            resultItemID = 0,
            resultAmount = 1,
            ingredients = new List<IngredientEntry>()
        });
    }

    private void CreateRecipes()
    {
        EnsureFolderExists(outputFolder);
        
        int successCount = 0;
        List<string> createdFiles = new List<string>();
        
        for (int i = 0; i < recipes.Count; i++)
        {
            var entry = recipes[i];
            if (!entry.enabled || string.IsNullOrEmpty(entry.name)) continue;
            
            int recipeID = startID + i;
            
            var recipe = ScriptableObject.CreateInstance<RecipeData>();
            recipe.recipeID = recipeID;
            recipe.recipeName = entry.name;
            recipe.description = "";
            recipe.resultItemID = entry.resultItemID;
            recipe.resultAmount = entry.resultAmount;
            recipe.requiredStation = craftingStation;
            recipe.craftingTime = craftingTime;
            recipe.unlockedByDefault = unlockedByDefault;
            recipe.craftingExp = craftingExp;
            
            // 材料
            recipe.ingredients = new List<RecipeIngredient>();
            foreach (var ing in entry.ingredients)
            {
                recipe.ingredients.Add(new RecipeIngredient
                {
                    itemID = ing.itemID,
                    amount = ing.amount
                });
            }
            
            // 保存
            string safeName = SanitizeFileName(entry.name);
            string assetPath = $"{outputFolder}/Recipe_{recipeID}_{safeName}.asset";
            
            if (AssetDatabase.LoadAssetAtPath<RecipeData>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
            
            AssetDatabase.CreateAsset(recipe, assetPath);
            createdFiles.Add(assetPath);
            successCount++;
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // 选中创建的文件
        if (createdFiles.Count > 0)
        {
            var assets = new List<Object>();
            foreach (var path in createdFiles)
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset != null) assets.Add(asset);
            }
            Selection.objects = assets.ToArray();
        }
        
        // 同步数据库
        string syncMsg = "";
        if (DatabaseSyncHelper.DatabaseExists())
        {
            int syncCount = DatabaseSyncHelper.AutoCollectAllRecipes();
            syncMsg = syncCount >= 0 ? $"\n数据库已同步（{syncCount}个配方）" : "\n数据库同步失败";
        }
        
        EditorUtility.DisplayDialog("完成", $"成功创建 {successCount} 个配方{syncMsg}", "确定");
    }

    #endregion

    #region 辅助方法

    private string SanitizeFileName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        foreach (char c in invalid)
        {
            name = name.Replace(c, '_');
        }
        return name;
    }

    private void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        
        string[] folders = folderPath.Split('/');
        string currentPath = folders[0];
        
        for (int i = 1; i < folders.Length; i++)
        {
            string newPath = currentPath + "/" + folders[i];
            if (!AssetDatabase.IsValidFolder(newPath))
            {
                AssetDatabase.CreateFolder(currentPath, folders[i]);
            }
            currentPath = newPath;
        }
    }

    #endregion

    #region 设置保存/加载

    private void LoadSettings()
    {
        startID = EditorPrefs.GetInt("BatchRecipe_StartID", 8000);
        outputFolder = EditorPrefs.GetString("BatchRecipe_Output", "Assets/111_Data/Recipes");
        craftingStation = (CraftingStation)EditorPrefs.GetInt("BatchRecipe_Station", 0);
        craftingTime = EditorPrefs.GetFloat("BatchRecipe_Time", 0f);
        unlockedByDefault = EditorPrefs.GetBool("BatchRecipe_Unlocked", true);
        craftingExp = EditorPrefs.GetInt("BatchRecipe_Exp", 10);
    }

    private void SaveSettings()
    {
        EditorPrefs.SetInt("BatchRecipe_StartID", startID);
        EditorPrefs.SetString("BatchRecipe_Output", outputFolder);
        EditorPrefs.SetInt("BatchRecipe_Station", (int)craftingStation);
        EditorPrefs.SetFloat("BatchRecipe_Time", craftingTime);
        EditorPrefs.SetBool("BatchRecipe_Unlocked", unlockedByDefault);
        EditorPrefs.SetInt("BatchRecipe_Exp", craftingExp);
    }

    #endregion
}
