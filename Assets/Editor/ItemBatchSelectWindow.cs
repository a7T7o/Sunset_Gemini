using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using FarmGame.Data;

/// <summary>
/// 物品批量选择窗口
/// </summary>
public class ItemBatchSelectWindow : EditorWindow
{
    #region 静态方法
    
    private static SerializedProperty targetItemsProp;
    private static SerializedObject targetSerializedObject;
    
    public static void ShowWindow(SerializedProperty itemsProp, SerializedObject serializedObject)
    {
        targetItemsProp = itemsProp;
        targetSerializedObject = serializedObject;
        
        var window = GetWindow<ItemBatchSelectWindow>(true, "批量选择物品", true);
        window.minSize = new Vector2(500, 400);
        window.LoadAllItems();
        window.Show();
    }
    
    #endregion

    #region 字段
    
    private List<ItemData> allItems = new List<ItemData>();
    private HashSet<ItemData> selectedItems = new HashSet<ItemData>();
    private string searchFilter = "";
    private Vector2 scrollPosition;
    private int categoryFilter = -1;
    
    private string[] categoryNames;
    private GUIStyle itemButtonStyle;
    private GUIStyle selectedButtonStyle;
    private bool stylesInitialized = false;
    
    #endregion

    #region 初始化
    
    private void LoadAllItems()
    {
        allItems.Clear();
        selectedItems.Clear();
        
        // 查找所有 ItemData 资源
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null)
            {
                allItems.Add(item);
            }
        }
        
        // 按 ID 排序
        allItems = allItems.OrderBy(x => x.itemID).ToList();
        
        // 获取分类名称
        categoryNames = System.Enum.GetNames(typeof(ItemCategory));
    }
    
    private void InitStyles()
    {
        if (stylesInitialized) return;
        
        itemButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(8, 8, 4, 4),
            fixedHeight = 32
        };
        
        selectedButtonStyle = new GUIStyle(itemButtonStyle);
        selectedButtonStyle.normal.background = MakeColorTexture(new Color(0.3f, 0.5f, 0.3f, 1f));
        selectedButtonStyle.normal.textColor = Color.white;
        
        stylesInitialized = true;
    }
    
    private Texture2D MakeColorTexture(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
    
    #endregion

    #region GUI
    
    private void OnGUI()
    {
        InitStyles();
        
        // 顶部工具栏
        DrawToolbar();
        
        EditorGUILayout.Space(5);
        
        // 物品列表
        DrawItemList();
        
        EditorGUILayout.Space(5);
        
        // 底部按钮
        DrawBottomButtons();
    }
    
    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            // 搜索框
            GUILayout.Label("搜索:", GUILayout.Width(40));
            searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(150));
            
            if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                searchFilter = "";
                GUI.FocusControl(null);
            }
            
            GUILayout.Space(10);
            
            // 分类筛选
            GUILayout.Label("分类:", GUILayout.Width(35));
            string[] filterOptions = new string[categoryNames.Length + 1];
            filterOptions[0] = "全部";
            for (int i = 0; i < categoryNames.Length; i++)
            {
                filterOptions[i + 1] = categoryNames[i];
            }
            categoryFilter = EditorGUILayout.Popup(categoryFilter + 1, filterOptions, EditorStyles.toolbarPopup, GUILayout.Width(100)) - 1;
            
            GUILayout.FlexibleSpace();
            
            // 选中数量
            GUILayout.Label($"已选: {selectedItems.Count}", EditorStyles.miniLabel);
            
            // 全选/取消全选
            if (GUILayout.Button("全选", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                SelectAllFiltered();
            }
            
            if (GUILayout.Button("取消", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                selectedItems.Clear();
            }
        }
    }
    
    private void DrawItemList()
    {
        var filteredItems = GetFilteredItems();
        
        if (filteredItems.Count == 0)
        {
            EditorGUILayout.HelpBox("没有找到匹配的物品", MessageType.Info);
            return;
        }
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // 使用网格布局
        int columns = Mathf.Max(1, (int)(position.width - 20) / 200);
        int itemIndex = 0;
        
        while (itemIndex < filteredItems.Count)
        {
            EditorGUILayout.BeginHorizontal();
            
            for (int col = 0; col < columns && itemIndex < filteredItems.Count; col++)
            {
                var item = filteredItems[itemIndex];
                DrawItemButton(item);
                itemIndex++;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawItemButton(ItemData item)
    {
        bool isSelected = selectedItems.Contains(item);
        GUIStyle style = isSelected ? selectedButtonStyle : itemButtonStyle;
        
        using (new EditorGUILayout.HorizontalScope(style, GUILayout.Width(190), GUILayout.Height(32)))
        {
            // 复选框
            bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
            if (newSelected != isSelected)
            {
                if (newSelected)
                    selectedItems.Add(item);
                else
                    selectedItems.Remove(item);
            }
            
            // 图标
            if (item.icon != null)
            {
                Rect iconRect = GUILayoutUtility.GetRect(24, 24, GUILayout.Width(24), GUILayout.Height(24));
                GUI.DrawTexture(iconRect, item.icon.texture, ScaleMode.ScaleToFit);
            }
            else
            {
                GUILayout.Space(24);
            }
            
            // 名称和ID
            GUILayout.Label($"[{item.itemID}] {item.itemName}", GUILayout.ExpandWidth(true));
        }
        
        // 点击整行切换选中
        Rect lastRect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
        {
            if (isSelected)
                selectedItems.Remove(item);
            else
                selectedItems.Add(item);
            
            Event.current.Use();
            Repaint();
        }
    }
    
    private void DrawBottomButtons()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("取消", GUILayout.Width(80), GUILayout.Height(25)))
            {
                Close();
            }
            
            GUILayout.Space(10);
            
            using (new EditorGUI.DisabledScope(selectedItems.Count == 0))
            {
                if (GUILayout.Button($"添加 ({selectedItems.Count})", GUILayout.Width(100), GUILayout.Height(25)))
                {
                    AddSelectedItems();
                    Close();
                }
            }
        }
    }
    
    #endregion

    #region 辅助方法
    
    private List<ItemData> GetFilteredItems()
    {
        var result = allItems.AsEnumerable();
        
        // 分类筛选
        if (categoryFilter >= 0 && categoryFilter < categoryNames.Length)
        {
            var targetCategory = (ItemCategory)categoryFilter;
            result = result.Where(x => x.category == targetCategory);
        }
        
        // 搜索筛选
        if (!string.IsNullOrEmpty(searchFilter))
        {
            string filter = searchFilter.ToLower();
            result = result.Where(x => 
                x.itemName.ToLower().Contains(filter) || 
                x.itemID.ToString().Contains(filter));
        }
        
        return result.ToList();
    }
    
    private void SelectAllFiltered()
    {
        var filtered = GetFilteredItems();
        foreach (var item in filtered)
        {
            selectedItems.Add(item);
        }
    }
    
    private void AddSelectedItems()
    {
        if (targetItemsProp == null || targetSerializedObject == null) return;
        
        targetSerializedObject.Update();
        
        foreach (var item in selectedItems.OrderBy(x => x.itemID))
        {
            int newIndex = targetItemsProp.arraySize;
            targetItemsProp.InsertArrayElementAtIndex(newIndex);
            
            var newItem = targetItemsProp.GetArrayElementAtIndex(newIndex);
            newItem.FindPropertyRelative("item").objectReferenceValue = item;
            newItem.FindPropertyRelative("quality").intValue = 0;
            newItem.FindPropertyRelative("amount").intValue = 1;
        }
        
        targetSerializedObject.ApplyModifiedProperties();
        
        Debug.Log($"<color=green>[InventoryBootstrap] 已添加 {selectedItems.Count} 个物品</color>");
    }
    
    #endregion
}
