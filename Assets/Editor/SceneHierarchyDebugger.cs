using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;

public class SceneHierarchyDebugger : EditorWindow
{
    [MenuItem("Tools/调试场景层级")]
    static void DebugSceneHierarchy()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== 场景层级检查 ===\n");
        
        // 查找 UI 根物体
        GameObject uiRoot = GameObject.Find("UI");
        if (uiRoot != null)
        {
            sb.AppendLine($"✓ 找到 UI 根物体");
            sb.AppendLine($"  - Canvas: {uiRoot.GetComponent<Canvas>() != null}");
            sb.AppendLine($"  - GraphicRaycaster: {uiRoot.GetComponent<GraphicRaycaster>() != null}");
            sb.AppendLine($"  - CanvasScaler: {uiRoot.GetComponent<CanvasScaler>() != null}");
            
            // 查找 PackagePanel
            Transform packagePanel = uiRoot.transform.Find("PackagePanel");
            if (packagePanel != null)
            {
                sb.AppendLine($"\n✓ 找到 PackagePanel");
                sb.AppendLine($"  - Active: {packagePanel.gameObject.activeSelf}");
                sb.AppendLine($"  - CanvasGroup: {packagePanel.GetComponent<CanvasGroup>() != null}");
                
                // 查找 Main
                Transform main = packagePanel.Find("Main");
                if (main != null)
                {
                    sb.AppendLine($"\n✓ 找到 Main");
                    
                    // 查找 0_Props
                    Transform props = main.Find("0_Props");
                    if (props != null)
                    {
                        sb.AppendLine($"\n✓ 找到 0_Props");
                        
                        // 查找 Up
                        Transform up = props.Find("Up");
                        if (up != null)
                        {
                            sb.AppendLine($"\n✓ 找到 Up (背包区域)");
                            sb.AppendLine($"  - 子物体数量: {up.childCount}");
                            
                            // 检查前3个槽位
                            for (int i = 0; i < Mathf.Min(3, up.childCount); i++)
                            {
                                Transform slot = up.GetChild(i);
                                sb.AppendLine($"\n  槽位 {i}: {slot.name}");
                                sb.AppendLine($"    - Active: {slot.gameObject.activeSelf}");
                                sb.AppendLine($"    - Image: {slot.GetComponent<Image>() != null}");
                                sb.AppendLine($"    - Toggle: {slot.GetComponent<Toggle>() != null}");
                                sb.AppendLine($"    - InventorySlotUI: {slot.GetComponent<InventorySlotUI>() != null}");
                                
                                var img = slot.GetComponent<Image>();
                                if (img != null)
                                {
                                    sb.AppendLine($"    - Image.raycastTarget: {img.raycastTarget}");
                                }
                                
                                var toggle = slot.GetComponent<Toggle>();
                                if (toggle != null)
                                {
                                    sb.AppendLine($"    - Toggle.interactable: {toggle.interactable}");
                                    sb.AppendLine($"    - Toggle.targetGraphic: {toggle.targetGraphic != null}");
                                }
                            }
                        }
                        
                        // 查找 Down
                        Transform down = props.Find("Down");
                        if (down != null)
                        {
                            sb.AppendLine($"\n✓ 找到 Down (装备区域)");
                            sb.AppendLine($"  - 子物体数量: {down.childCount}");
                            
                            // 检查第一个装备槽
                            if (down.childCount > 0)
                            {
                                Transform slot = down.GetChild(0);
                                sb.AppendLine($"\n  装备槽 0: {slot.name}");
                                sb.AppendLine($"    - Active: {slot.gameObject.activeSelf}");
                                sb.AppendLine($"    - Image: {slot.GetComponent<Image>() != null}");
                                sb.AppendLine($"    - EquipmentSlotUI: {slot.GetComponent<EquipmentSlotUI>() != null}");
                                
                                var img = slot.GetComponent<Image>();
                                if (img != null)
                                {
                                    sb.AppendLine($"    - Image.raycastTarget: {img.raycastTarget}");
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            sb.AppendLine("✗ 未找到 UI 根物体");
        }
        
        // 查找 EventSystem
        var eventSystem = GameObject.FindFirstObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            sb.AppendLine($"\n✓ 找到 EventSystem: {eventSystem.name}");
            sb.AppendLine($"  - Enabled: {eventSystem.enabled}");
        }
        else
        {
            sb.AppendLine($"\n✗ 未找到 EventSystem");
        }
        
        Debug.Log(sb.ToString());
    }
}
