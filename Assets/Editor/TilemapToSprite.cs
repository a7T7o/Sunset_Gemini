using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Tilemap转Sprite工具
/// 将Tilemap渲染区域截取为单个Sprite图片
/// 用于：画好房子后转换为预制体
/// </summary>
public class TilemapToSprite : EditorWindow
{
    private List<Tilemap> selectedTilemaps = new List<Tilemap>();
    private string savePath = "Assets/Sprites/Generated";
    private string fileName = "Building";
    private int pixelsPerUnit = 16;
    private bool transparentBackground = true;
    private Vector2Int padding = new Vector2Int(1, 1);
    private bool compactMode = true; // 紧凑模式：自动裁剪空白
    private Vector2 scrollPos;

    [MenuItem("Tools/Tilemap转Sprite工具")]
    public static void ShowWindow()
    {
        GetWindow<TilemapToSprite>("Tilemap转Sprite");
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Tilemap转Sprite工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "使用方法：\n" +
            "1. 在Hierarchy（层级）窗口选中Tilemap对象（可多选）\n" +
            "2. 点击下方\"获取选中的Tilemap\"按钮\n" +
            "3. 设置参数后点击\"生成Sprite\"", 
            MessageType.Info);

        EditorGUILayout.Space();

        // 获取选中按钮
        if (GUILayout.Button("获取选中的Tilemap（从Hierarchy）", GUILayout.Height(35)))
        {
            GetSelectedTilemaps();
        }

        EditorGUILayout.Space();

        // 显示已选择的Tilemap
        if (selectedTilemaps.Count > 0)
        {
            GUILayout.Label($"已选择 {selectedTilemaps.Count} 个Tilemap:", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            foreach (var tilemap in selectedTilemaps)
            {
                if (tilemap != null)
                {
                    EditorGUILayout.LabelField($"• {tilemap.gameObject.name}");
                }
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            
            if (GUILayout.Button("清空列表"))
            {
                selectedTilemaps.Clear();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("还没有获取任何Tilemap\n请在Hierarchy中选择Tilemap后点击上方按钮", MessageType.Warning);
        }

        EditorGUILayout.Space();
        DrawSeparator();
        EditorGUILayout.Space();

        GUILayout.Label("导出设置", EditorStyles.boldLabel);

        savePath = EditorGUILayout.TextField("保存路径", savePath);
        fileName = EditorGUILayout.TextField("文件名", fileName);
        pixelsPerUnit = EditorGUILayout.IntField("Pixels Per Unit", pixelsPerUnit);
        padding = EditorGUILayout.Vector2IntField("边距（格子）", padding);
        transparentBackground = EditorGUILayout.Toggle("透明背景", transparentBackground);
        
        EditorGUILayout.Space();
        compactMode = EditorGUILayout.Toggle("紧凑模式（裁剪空白）", compactMode);
        if (compactMode)
        {
            EditorGUILayout.HelpBox("只导出有内容的区域，自动去除空白部分", MessageType.Info);
        }

        EditorGUILayout.Space();

        // 预览信息
        if (selectedTilemaps.Count > 0)
        {
            // 移除null的Tilemap
            selectedTilemaps.RemoveAll(t => t == null);
            
            if (selectedTilemaps.Count > 0)
            {
                BoundsInt bounds = GetCombinedBounds(selectedTilemaps.ToArray());
                EditorGUILayout.LabelField("导出范围", $"{bounds.size.x} x {bounds.size.y} 格子");
                EditorGUILayout.LabelField("像素尺寸", $"{bounds.size.x * pixelsPerUnit} x {bounds.size.y * pixelsPerUnit} px");
            }
        }

        EditorGUILayout.Space();

        GUI.enabled = selectedTilemaps.Count > 0;
        if (GUILayout.Button("生成Sprite", GUILayout.Height(40)))
        {
            GenerateSprite();
        }
        GUI.enabled = true;

        EditorGUILayout.EndScrollView();
    }

    private void GetSelectedTilemaps()
    {
        selectedTilemaps.Clear();

        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在Hierarchy窗口中选择包含Tilemap的对象！", "确定");
            return;
        }

        foreach (GameObject obj in selectedObjects)
        {
            // 检查对象本身是否有Tilemap
            Tilemap tilemap = obj.GetComponent<Tilemap>();
            if (tilemap != null && !selectedTilemaps.Contains(tilemap))
            {
                selectedTilemaps.Add(tilemap);
            }

            // 检查子物体是否有Tilemap（支持选中父物体）
            Tilemap[] childTilemaps = obj.GetComponentsInChildren<Tilemap>();
            foreach (Tilemap childTilemap in childTilemaps)
            {
                if (!selectedTilemaps.Contains(childTilemap))
                {
                    selectedTilemaps.Add(childTilemap);
                }
            }
        }

        if (selectedTilemaps.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "选中的对象中没有找到Tilemap组件！", "确定");
        }
        else
        {
            Debug.Log($"<color=green>已获取 {selectedTilemaps.Count} 个Tilemap</color>");
        }
    }

    private void DrawSeparator()
    {
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }

    private BoundsInt GetCombinedBounds(Tilemap[] tilemaps)
    {
        if (tilemaps.Length == 0) return new BoundsInt();

        if (compactMode)
        {
            // 紧凑模式：只计算实际有Tile的区域
            Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, 0);
            Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, 0);
            bool foundAnyTile = false;

            foreach (var tilemap in tilemaps)
            {
                tilemap.CompressBounds();
                BoundsInt bounds = tilemap.cellBounds;

                // 遍历每个格子，找到实际有Tile的范围
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    for (int y = bounds.yMin; y < bounds.yMax; y++)
                    {
                        Vector3Int pos = new Vector3Int(x, y, 0);
                        if (tilemap.HasTile(pos))
                        {
                            min.x = Mathf.Min(min.x, x);
                            min.y = Mathf.Min(min.y, y);
                            max.x = Mathf.Max(max.x, x + 1);
                            max.y = Mathf.Max(max.y, y + 1);
                            foundAnyTile = true;
                        }
                    }
                }
            }

            if (!foundAnyTile)
            {
                return new BoundsInt(Vector3Int.zero, Vector3Int.one);
            }

            BoundsInt combinedBounds = new BoundsInt();
            combinedBounds.SetMinMax(min, max);

            // 添加边距
            combinedBounds.xMin -= padding.x;
            combinedBounds.yMin -= padding.y;
            combinedBounds.xMax += padding.x;
            combinedBounds.yMax += padding.y;

            return combinedBounds;
        }
        else
        {
            // 普通模式：使用整个边界框
            BoundsInt combinedBounds = tilemaps[0].cellBounds;

            foreach (var tilemap in tilemaps)
            {
                BoundsInt bounds = tilemap.cellBounds;
                
                tilemap.CompressBounds();
                bounds = tilemap.cellBounds;

                // 合并边界
                Vector3Int min = Vector3Int.Min(combinedBounds.min, bounds.min);
                Vector3Int max = Vector3Int.Max(combinedBounds.max, bounds.max);
                
                combinedBounds.min = min;
                combinedBounds.max = max;
            }

            // 添加边距
            combinedBounds.xMin -= padding.x;
            combinedBounds.yMin -= padding.y;
            combinedBounds.xMax += padding.x;
            combinedBounds.yMax += padding.y;

            return combinedBounds;
        }
    }

    private void GenerateSprite()
    {
        // 移除null的Tilemap
        selectedTilemaps.RemoveAll(t => t == null);
        
        if (selectedTilemaps.Count == 0)
        {
            EditorUtility.DisplayDialog("错误", "请先选择要导出的Tilemap！", "确定");
            return;
        }

        // 压缩所有Tilemap边界
        foreach (var tilemap in selectedTilemaps)
        {
            tilemap.CompressBounds();
        }

        BoundsInt bounds = GetCombinedBounds(selectedTilemaps.ToArray());
        
        int width = bounds.size.x * pixelsPerUnit;
        int height = bounds.size.y * pixelsPerUnit;

        if (width <= 0 || height <= 0)
        {
            EditorUtility.DisplayDialog("错误", "Tilemap区域为空！", "确定");
            return;
        }

        // 创建临时相机
        GameObject tempCameraObj = new GameObject("TempCamera");
        Camera tempCamera = tempCameraObj.AddComponent<Camera>();
        
        tempCamera.orthographic = true;
        tempCamera.clearFlags = transparentBackground ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
        tempCamera.backgroundColor = new Color(0, 0, 0, 0);

        // 计算相机位置和大小（使用第一个Tilemap的Grid）
        Grid grid = selectedTilemaps[0].layoutGrid;
        Vector3Int centerInt = new Vector3Int(
            Mathf.RoundToInt(bounds.center.x), 
            Mathf.RoundToInt(bounds.center.y), 
            0
        );
        Vector3 center = grid.GetCellCenterWorld(centerInt);
        tempCamera.transform.position = new Vector3(center.x, center.y, -10);
        tempCamera.orthographicSize = bounds.size.y * 0.5f;

        // 设置渲染纹理
        RenderTexture rt = new RenderTexture(width, height, 24);
        tempCamera.targetTexture = rt;
        tempCamera.aspect = (float)width / height;

        // 渲染
        tempCamera.Render();

        // 读取像素
        RenderTexture.active = rt;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();

        // 清理
        RenderTexture.active = null;
        tempCamera.targetTexture = null;
        DestroyImmediate(tempCameraObj);
        DestroyImmediate(rt);

        // 保存文件
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        string fullPath = $"{savePath}/{fileName}.png";
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(fullPath, bytes);
        
        AssetDatabase.Refresh();

        // 设置导入设置
        TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Debug.Log($"<color=green>Sprite生成成功！</color>\n路径: {fullPath}\n尺寸: {width}x{height}");
        EditorUtility.DisplayDialog("成功", 
            $"Sprite已生成！\n路径: {fullPath}\n尺寸: {width}x{height}px\n\n现在可以用这个Sprite创建预制体了", 
            "确定");

        // 在Project窗口中高亮显示
        Object obj = AssetDatabase.LoadAssetAtPath<Object>(fullPath);
        EditorGUIUtility.PingObject(obj);
        Selection.activeObject = obj;
    }
}

