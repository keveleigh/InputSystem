﻿using System;
using System.Linq;
using System.Text;
using UnityEditor;

namespace UnityEngine.Experimental.Input.Editor
{
    class CopyPasteUtility
    {
        const string m_InputAssetMarker = "INPUTASSET\n";
        InputActionListTreeView m_TreeView;
        ActionInspectorWindow m_Window;

        public CopyPasteUtility(ActionInspectorWindow window)
        {
            m_Window = window;
            m_TreeView = window.m_TreeView;
        }
        public void HandleCopyEvent()
        {
            if (!CanCopySelection())
            {
                EditorGUIUtility.systemCopyBuffer = null;
                EditorApplication.Beep();
                return;
            }
            
            var selectedRows = m_TreeView.GetSelectedRows();
            var rowTypes = selectedRows.Select(r => r.GetType()).Distinct().ToList();
            
            // Don't allow to copy different type. It will hard to handle pasting
            if (rowTypes.Count() > 1)
            {
                EditorGUIUtility.systemCopyBuffer = null;
                EditorApplication.Beep();
                return;
            }
            
            var copyList = new StringBuilder(m_InputAssetMarker);
            foreach (var selectedRow in selectedRows)
            {
                copyList.Append(selectedRow.GetType().Name);
                copyList.Append(selectedRow.SerializeToString());
                copyList.Append(m_InputAssetMarker);

                if (selectedRow is ActionTreeItem && selectedRow.children != null && selectedRow.children.Count > 0)
                {
                    var action = selectedRow as ActionTreeItem;
                    
                    foreach (var child in action.children)
                    {
                        if(!(child is BindingTreeItem))
                            continue;
                        copyList.Append(child.GetType().Name);
                        copyList.Append((child as BindingTreeItem).SerializeToString());
                        copyList.Append(m_InputAssetMarker);
                    }
                }
                
            }
            EditorGUIUtility.systemCopyBuffer = copyList.ToString();
        }

        public bool CanCopySelection()
        {
            var selectedRows = m_TreeView.GetSelectedRows();
            var rowTypes = selectedRows.Select(r => r.GetType()).Distinct().ToList();
            return rowTypes.Count == 1;
        }

        public void HandlePasteEvent()
        {
            var json = EditorGUIUtility.systemCopyBuffer;
            var elements = json.Split(new[] { m_InputAssetMarker }, StringSplitOptions.RemoveEmptyEntries);
            if (!json.StartsWith(m_InputAssetMarker))
                return;
            for (var i = 0; i < elements.Length; i++)
            {
                var row = elements[i];
                if (row.StartsWith(typeof(ActionMapTreeItem).Name))
                {
                    row = row.Substring(typeof(ActionMapTreeItem).Name.Length);
                    var map = JsonUtility.FromJson<InputActionMap>(row);
                    InputActionSerializationHelpers.AddActionMapFromObject(m_Window.m_SerializedObject, map);
                    m_Window.Apply();
                    continue;
                }

                if (row.StartsWith(typeof(ActionTreeItem).Name))
                {
                    row = row.Substring(typeof(ActionTreeItem).Name.Length);
                    var action = JsonUtility.FromJson<InputAction>(row);
                    var actionMap = m_TreeView.GetSelectedActionMap();
                    SerializedProperty newActionProperty = null;
                    InputActionSerializationHelpers.AddActionFromObject(action, actionMap.elementProperty, ref newActionProperty);
                    m_Window.Apply();

                    while (i + 1 < elements.Length)
                    {
                        try
                        {
                            var nextRow = elements[i + 1];
                            if (!nextRow.StartsWith(typeof(BindingTreeItem).Name))
                            {
                                break;
                            }
                            nextRow = nextRow.Substring(typeof(BindingTreeItem).Name.Length);
                            var binding = JsonUtility.FromJson<InputBinding>(nextRow);
                            InputActionSerializationHelpers.AppendBindingFromObject(binding, newActionProperty, actionMap.elementProperty);
                            m_Window.Apply();
                            i++;
                        }
                        catch (ArgumentException e)
                        {
                            Debug.LogException(e);
                            break;
                        }
                    }
                    continue;
                }

                if (row.StartsWith(typeof(BindingTreeItem).Name))
                {
                    row = row.Substring(typeof(BindingTreeItem).Name.Length);
                    var binding = JsonUtility.FromJson<InputBinding>(row);
                    var selectedRow = m_TreeView.GetSelectedAction();
                    if (selectedRow == null)
                    {
                        EditorApplication.Beep();
                        continue;
                    }

                    var actionMap = m_TreeView.GetSelectedActionMap();
                    InputActionSerializationHelpers.AppendBindingFromObject(binding, selectedRow.elementProperty, actionMap.elementProperty);
                    m_Window.Apply();
                    continue;
                }
            }
        }
    }
}