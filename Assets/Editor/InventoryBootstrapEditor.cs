using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using FarmGame.Data;

[CustomEditor(typeof(InventoryBootstrap))]
public class InventoryBootstrapEditor : Editor
{
    #region 序列化属性
    
    private SerializedProperty runOnStartProp;
    private SerializedProperty runOnBuildProp;
    private SerializedProperty clearInventoryFirstProp;
    private SerializedProperty itemListsProp;
    
    #endregion

    #region 样式缓存
    
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private GUIStyle dropZoneStyle;
    private GUIStyle itemBoxStyle;
    private bool stylesInitialized = false;
    
    #endregion

    #region 状态
    
    private int dragTargetListIndex = -1;
    private Vector2 scrollPosition;
    
    // 列表内物品拖拽排序状态
    private int _dragSourceListIndex = -1;
    private int _dragSourceItemIndex = -1;
    private int _dragTargetItemIndex = -1;
    private bool _isDraggingItem = false;
    
    #endregion

    #region 初始化
    
    private void OnEnable()
    {
        runOnStartProp = serializedObject.FindProperty("runOnStart");
        runOnBuildProp = serializedObject.FindProperty("runOnBuild");
        clearInventoryFirstProp = serializedObject.FindProperty("clearInventoryFirst");
        itemListsProp = serializedObject.FindProperty("itemLists");
    }
    
    private void InitStyles()
    {
        if (stylesInitialized) return;
        
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft
        };
        
        boxStyle = new GUIStyle("box")
        {
            padding = new RectOffset(8, 8, 8, 8),
            margin = new RectOffset(0, 0, 4, 4)
        };
        
        dropZoneStyle = new GUIStyle("box")
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Italic,
            normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
        };
        
        itemBoxStyle = new GUIStyle()
        {
            padding = new RectOffset(4, 4, 2, 2),
            margin = new RectOffset(0, 0, 1, 1)
        };
        
        stylesInitialized = true;
    }
    
    #endregion

    #region Inspector 绘制
    
    public override void OnInspectorGUI()
    {
        InitStyles();
        serializedObject.Update();
        
        // 基础设置
        DrawBasicSettings();
        
        EditorGUILayout.Space(10);
        
        // 物品列表管理
        DrawItemListsSection();
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawBasicSettings()
    {
        EditorGUILayout.LabelField("启动设置", headerStyle);
        
        using (new EditorGUILayout.VerticalScope(boxStyle))
        {
            EditorGUILayout.PropertyField(runOnStartProp, new GUIContent("编辑器运行时注入"));
            EditorGUILayout.PropertyField(runOnBuildProp, new GUIContent("构建版本注入"));
            EditorGUILayout.PropertyField(clearInventoryFirstProp, new GUIContent("注入前清空背包"));
            
            EditorGUILayout.Space(5);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("立即注入", GUILayout.Height(25)))
                {
                    var bootstrap = (InventoryBootstrap)target;
                    bootstrap.Apply();
                }
                
                if (GUILayout.Button("清空背包", GUILayout.Height(25)))
                {
                    if (Application.isPlaying)
                    {
                        var inventory = Object.FindFirstObjectByType<InventoryService>();
                        if (inventory != null)
                        {
                            for (int i = 0; i < inventory.Size; i++)
                                inventory.ClearSlot(i);
                            Debug.Log("<color=yellow>[InventoryBootstrap] 背包已清空</color>");
                        }
                        else
                        {
                            Debug.LogWarning("[InventoryBootstrap] 找不到 InventoryService");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[InventoryBootstrap] 清空背包需要在运行模式下执行");
                    }
                }
            }
        }
    }
    
    private void DrawItemListsSection()
    {
        // 标题栏
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("物品列表", headerStyle);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("+ 添加列表", GUILayout.Width(80)))
            {
                AddNewList();
            }
        }
        
        EditorGUILayout.Space(5);
        
        // 列表内容
        if (itemListsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("暂无物品列表，点击上方按钮添加", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < itemListsProp.arraySize; i++)
            {
                DrawItemList(i);
            }
        }
    }
    
    #endregion

    #region 列表绘制
    
    private void DrawItemList(int listIndex)
    {
        var listProp = itemListsProp.GetArrayElementAtIndex(listIndex);
        var nameProp = listProp.FindPropertyRelative("name");
        var enabledProp = listProp.FindPropertyRelative("enabled");
        var foldoutProp = listProp.FindPropertyRelative("foldout");
        var itemsProp = listProp.FindPropertyRelative("items");
        
        // 列表容器背景色
        Color bgColor = enabledProp.boolValue ? 
            new Color(0.2f, 0.3f, 0.2f, 0.3f) : 
            new Color(0.3f, 0.2f, 0.2f, 0.2f);
        
        Rect boxRect = EditorGUILayout.BeginVertical(boxStyle);
        EditorGUI.DrawRect(boxRect, bgColor);
        
        // 列表头部
        DrawListHeader(listIndex, nameProp, enabledProp, foldoutProp, itemsProp);
        
        // 展开内容
        if (foldoutProp.boolValue)
        {
            EditorGUILayout.Space(5);
            
            // 物品列表
            DrawItemEntries(listIndex, itemsProp);
            
            EditorGUILayout.Space(5);
            
            // 拖放区域
            DrawDropZone(listIndex, itemsProp);
            
            // 批量操作按钮
            DrawBatchButtons(listIndex, itemsProp);
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }
    
    private void DrawListHeader(int listIndex, SerializedProperty nameProp, 
        SerializedProperty enabledProp, SerializedProperty foldoutProp, SerializedProperty itemsProp)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            // 折叠箭头
            foldoutProp.boolValue = EditorGUILayout.Foldout(foldoutProp.boolValue, "", true);
            
            // 启用复选框
            enabledProp.boolValue = EditorGUILayout.Toggle(enabledProp.boolValue, GUILayout.Width(20));
            
            // 列表名称
            nameProp.stringValue = EditorGUILayout.TextField(nameProp.stringValue, GUILayout.MinWidth(100));
            
            // 物品数量标签
            GUILayout.Label($"({itemsProp.arraySize} 项)", EditorStyles.miniLabel, GUILayout.Width(50));
            
            GUILayout.FlexibleSpace();
            
            // 上移按钮
            using (new EditorGUI.DisabledScope(listIndex == 0))
            {
                if (GUILayout.Button("↑", GUILayout.Width(25)))
                {
                    itemListsProp.MoveArrayElement(listIndex, listIndex - 1);
                }
            }
            
            // 下移按钮
            using (new EditorGUI.DisabledScope(listIndex == itemListsProp.arraySize - 1))
            {
                if (GUILayout.Button("↓", GUILayout.Width(25)))
                {
                    itemListsProp.MoveArrayElement(listIndex, listIndex + 1);
                }
            }
            
            // 删除按钮
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("确认删除", 
                    $"确定要删除列表 \"{nameProp.stringValue}\" 吗？\n此操作不可撤销。", 
                    "删除", "取消"))
                {
                    itemListsProp.DeleteArrayElementAtIndex(listIndex);
                }
            }
        }
    }
    
    #endregion

    #region 物品条目绘制
    
    private void DrawItemEntries(int listIndex, SerializedProperty itemsProp)
    {
        if (itemsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("列表为空，拖拽物品到下方区域添加", MessageType.None);
            return;
        }
        
        // 限制显示数量，避免性能问题
        int maxDisplay = 50;
        int displayCount = Mathf.Min(itemsProp.arraySize, maxDisplay);
        
        for (int i = 0; i < displayCount; i++)
        {
            DrawItemEntry(listIndex, itemsProp, i);
        }
        
        if (itemsProp.arraySize > maxDisplay)
        {
            EditorGUILayout.HelpBox($"仅显示前 {maxDisplay} 项，共 {itemsProp.arraySize} 项", MessageType.Info);
        }
    }
    
    private void DrawItemEntry(int listIndex, SerializedProperty itemsProp, int itemIndex)
    {
        var itemProp = itemsProp.GetArrayElementAtIndex(itemIndex);
        var itemDataProp = itemProp.FindPropertyRelative("item");
        var qualityProp = itemProp.FindPropertyRelative("quality");
        var amountProp = itemProp.FindPropertyRelative("amount");
        
        Rect entryRect = EditorGUILayout.BeginHorizontal(itemBoxStyle);
        
        // 背景色（包含拖拽目标高亮）
        bool isDragTarget = _isDraggingItem && _dragSourceListIndex == listIndex && _dragTargetItemIndex == itemIndex;
        if (isDragTarget)
        {
            EditorGUI.DrawRect(entryRect, new Color(0.3f, 0.6f, 0.3f, 0.4f));
        }
        else if (itemIndex % 2 == 0)
        {
            EditorGUI.DrawRect(entryRect, new Color(0, 0, 0, 0.1f));
        }
        
        // 拖拽手柄
        Rect dragHandleRect = GUILayoutUtility.GetRect(18, 20, GUILayout.Width(18));
        EditorGUIUtility.AddCursorRect(dragHandleRect, MouseCursor.Pan);
        GUI.Label(dragHandleRect, "≡", EditorStyles.centeredGreyMiniLabel);
        HandleItemDrag(dragHandleRect, listIndex, itemsProp, itemIndex);
        
        // 序号
        GUILayout.Label($"{itemIndex + 1}.", GUILayout.Width(22));
        
        // 物品图标预览
        ItemData itemData = itemDataProp.objectReferenceValue as ItemData;
        if (itemData != null && itemData.icon != null)
        {
            Rect iconRect = GUILayoutUtility.GetRect(24, 24, GUILayout.Width(24), GUILayout.Height(24));
            GUI.DrawTexture(iconRect, itemData.icon.texture, ScaleMode.ScaleToFit);
        }
        else
        {
            GUILayout.Space(24);
        }
        
        // 物品选择
        EditorGUILayout.PropertyField(itemDataProp, GUIContent.none, GUILayout.MinWidth(120));
        
        // 品质 - 使用纯数字输入框
        GUILayout.Label("品质", GUILayout.Width(30));
        int newQuality = EditorGUILayout.IntField(qualityProp.intValue, GUILayout.Width(35));
        qualityProp.intValue = Mathf.Clamp(newQuality, 0, 4);
        
        // 数量 - 使用纯数字输入框
        GUILayout.Label("×", GUILayout.Width(15));
        int newAmount = EditorGUILayout.IntField(amountProp.intValue, GUILayout.Width(50));
        amountProp.intValue = Mathf.Max(1, newAmount);
        
        GUILayout.FlexibleSpace();
        
        // 右键菜单按钮
        if (GUILayout.Button("⋮", GUILayout.Width(20)))
        {
            ShowItemContextMenu(listIndex, itemsProp, itemIndex);
        }
        
        // 删除按钮
        if (GUILayout.Button("×", GUILayout.Width(20)))
        {
            itemsProp.DeleteArrayElementAtIndex(itemIndex);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 处理拖拽目标检测
        HandleItemDragTarget(entryRect, listIndex, itemIndex);
    }
    
    #endregion

    #region 物品拖拽排序
    
    private void HandleItemDrag(Rect handleRect, int listIndex, SerializedProperty itemsProp, int itemIndex)
    {
        Event evt = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        
        switch (evt.type)
        {
            case EventType.MouseDown:
                if (handleRect.Contains(evt.mousePosition) && evt.button == 0)
                {
                    GUIUtility.hotControl = controlId;
                    _isDraggingItem = true;
                    _dragSourceListIndex = listIndex;
                    _dragSourceItemIndex = itemIndex;
                    _dragTargetItemIndex = itemIndex;
                    evt.Use();
                }
                break;
                
            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlId)
                {
                    GUIUtility.hotControl = 0;
                    
                    // 执行移动
                    if (_isDraggingItem && _dragSourceItemIndex != _dragTargetItemIndex && _dragTargetItemIndex >= 0)
                    {
                        itemsProp.MoveArrayElement(_dragSourceItemIndex, _dragTargetItemIndex);
                        serializedObject.ApplyModifiedProperties();
                    }
                    
                    ResetItemDragState();
                    evt.Use();
                    Repaint();
                }
                break;
                
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlId)
                {
                    evt.Use();
                    Repaint();
                }
                break;
        }
    }
    
    private void HandleItemDragTarget(Rect entryRect, int listIndex, int itemIndex)
    {
        if (!_isDraggingItem || _dragSourceListIndex != listIndex)
            return;
            
        Event evt = Event.current;
        if (evt.type == EventType.Repaint || evt.type == EventType.MouseDrag)
        {
            if (entryRect.Contains(evt.mousePosition))
            {
                _dragTargetItemIndex = itemIndex;
            }
        }
    }
    
    private void ResetItemDragState()
    {
        _isDraggingItem = false;
        _dragSourceListIndex = -1;
        _dragSourceItemIndex = -1;
        _dragTargetItemIndex = -1;
    }
    
    #endregion

    #region 拖放功能
    
    private void DrawDropZone(int listIndex, SerializedProperty itemsProp)
    {
        Rect dropRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        
        bool isDragTarget = dragTargetListIndex == listIndex;
        Color zoneColor = isDragTarget ? 
            new Color(0.3f, 0.5f, 0.3f, 0.5f) : 
            new Color(0.2f, 0.2f, 0.2f, 0.3f);
        
        EditorGUI.DrawRect(dropRect, zoneColor);
        
        // 边框
        Handles.color = isDragTarget ? Color.green : Color.gray;
        Handles.DrawWireDisc(dropRect.center, Vector3.forward, 1);
        
        GUI.Label(dropRect, isDragTarget ? "松开以添加物品" : "拖拽 ItemData 到此处添加", dropZoneStyle);
        
        // 处理拖放
        HandleDragAndDrop(dropRect, listIndex, itemsProp);
    }
    
    private void HandleDragAndDrop(Rect dropRect, int listIndex, SerializedProperty itemsProp)
    {
        Event evt = Event.current;
        
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropRect.Contains(evt.mousePosition)) 
                {
                    dragTargetListIndex = -1;
                    return;
                }
                
                // 检查是否有有效的 ItemData
                bool hasValidItems = false;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is ItemData)
                    {
                        hasValidItems = true;
                        break;
                    }
                }
                
                if (!hasValidItems)
                {
                    dragTargetListIndex = -1;
                    return;
                }
                
                dragTargetListIndex = listIndex;
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    int addedCount = 0;
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is ItemData itemData)
                        {
                            AddItemToList(itemsProp, itemData);
                            addedCount++;
                        }
                    }
                    
                    if (addedCount > 0)
                    {
                        Debug.Log($"<color=green>[InventoryBootstrap] 已添加 {addedCount} 个物品</color>");
                    }
                    
                    dragTargetListIndex = -1;
                }
                
                evt.Use();
                Repaint();
                break;
                
            case EventType.DragExited:
                dragTargetListIndex = -1;
                Repaint();
                break;
        }
    }
    
    #endregion

    #region 批量操作
    
    private void DrawBatchButtons(int listIndex, SerializedProperty itemsProp)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("批量选择添加", GUILayout.Height(22)))
            {
                ShowBatchSelectWindow(itemsProp);
            }
            
            if (GUILayout.Button("清空列表", GUILayout.Height(22)))
            {
                if (itemsProp.arraySize == 0 || EditorUtility.DisplayDialog("确认清空", 
                    "确定要清空此列表中的所有物品吗？", "清空", "取消"))
                {
                    itemsProp.ClearArray();
                }
            }
            
            if (GUILayout.Button("复制列表", GUILayout.Height(22)))
            {
                DuplicateList(listIndex);
            }
        }
    }
    
    private void ShowBatchSelectWindow(SerializedProperty itemsProp)
    {
        ItemBatchSelectWindow.ShowWindow(itemsProp, serializedObject);
    }
    
    #endregion

    #region 上下文菜单
    
    private void ShowItemContextMenu(int listIndex, SerializedProperty itemsProp, int itemIndex)
    {
        GenericMenu menu = new GenericMenu();
        
        menu.AddItem(new GUIContent("复制"), false, () => {
            DuplicateItem(itemsProp, itemIndex);
            serializedObject.ApplyModifiedProperties();
        });
        
        menu.AddSeparator("");
        
        if (itemIndex > 0)
        {
            menu.AddItem(new GUIContent("上移"), false, () => {
                itemsProp.MoveArrayElement(itemIndex, itemIndex - 1);
                serializedObject.ApplyModifiedProperties();
            });
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("上移"));
        }
        
        if (itemIndex < itemsProp.arraySize - 1)
        {
            menu.AddItem(new GUIContent("下移"), false, () => {
                itemsProp.MoveArrayElement(itemIndex, itemIndex + 1);
                serializedObject.ApplyModifiedProperties();
            });
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("下移"));
        }
        
        menu.AddSeparator("");
        
        menu.AddItem(new GUIContent("移到顶部"), false, () => {
            itemsProp.MoveArrayElement(itemIndex, 0);
            serializedObject.ApplyModifiedProperties();
        });
        
        menu.AddItem(new GUIContent("移到底部"), false, () => {
            itemsProp.MoveArrayElement(itemIndex, itemsProp.arraySize - 1);
            serializedObject.ApplyModifiedProperties();
        });
        
        menu.AddSeparator("");
        
        menu.AddItem(new GUIContent("删除"), false, () => {
            itemsProp.DeleteArrayElementAtIndex(itemIndex);
            serializedObject.ApplyModifiedProperties();
        });
        
        menu.ShowAsContext();
    }
    
    #endregion

    #region 辅助方法
    
    private void AddNewList()
    {
        int newIndex = itemListsProp.arraySize;
        itemListsProp.InsertArrayElementAtIndex(newIndex);
        
        var newList = itemListsProp.GetArrayElementAtIndex(newIndex);
        newList.FindPropertyRelative("name").stringValue = $"列表 {newIndex + 1}";
        newList.FindPropertyRelative("enabled").boolValue = true;
        newList.FindPropertyRelative("foldout").boolValue = true;
        newList.FindPropertyRelative("items").ClearArray();
    }
    
    private void DuplicateList(int listIndex)
    {
        var sourceProp = itemListsProp.GetArrayElementAtIndex(listIndex);
        
        int newIndex = itemListsProp.arraySize;
        itemListsProp.InsertArrayElementAtIndex(newIndex);
        
        var newList = itemListsProp.GetArrayElementAtIndex(newIndex);
        var sourceName = sourceProp.FindPropertyRelative("name").stringValue;
        newList.FindPropertyRelative("name").stringValue = sourceName + " (副本)";
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void AddItemToList(SerializedProperty itemsProp, ItemData itemData)
    {
        int newIndex = itemsProp.arraySize;
        itemsProp.InsertArrayElementAtIndex(newIndex);
        
        var newItem = itemsProp.GetArrayElementAtIndex(newIndex);
        newItem.FindPropertyRelative("item").objectReferenceValue = itemData;
        newItem.FindPropertyRelative("quality").intValue = 0;
        newItem.FindPropertyRelative("amount").intValue = 1;
    }
    
    private void DuplicateItem(SerializedProperty itemsProp, int itemIndex)
    {
        var sourceItem = itemsProp.GetArrayElementAtIndex(itemIndex);
        
        int newIndex = itemIndex + 1;
        itemsProp.InsertArrayElementAtIndex(newIndex);
        
        var newItem = itemsProp.GetArrayElementAtIndex(newIndex);
        newItem.FindPropertyRelative("item").objectReferenceValue = 
            sourceItem.FindPropertyRelative("item").objectReferenceValue;
        newItem.FindPropertyRelative("quality").intValue = 
            sourceItem.FindPropertyRelative("quality").intValue;
        newItem.FindPropertyRelative("amount").intValue = 
            sourceItem.FindPropertyRelative("amount").intValue;
    }
    
    #endregion
}
