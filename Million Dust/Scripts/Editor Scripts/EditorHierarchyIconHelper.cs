#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

// 날짜 : 2021-10-25 AM 2:27:00
// 작성자 : Rito

namespace Rito.MillionDust
{
    /// <summary> 
    /// 
    /// </summary>
    public class EditorHierarchyIconHelper : MonoBehaviour
    {
        [UnityEditor.InitializeOnLoadMethod]
        private static void ApplyHierarchyIcon()
        {
            if (iconData == null || iconData.Length == 0)
                InitIconData();

            UnityEditor.EditorApplication.hierarchyWindowItemOnGUI += DrawHierarchyIcon;
        }

        private static (Type type, Texture icon, Color color)[] iconData;

        private static void InitIconData()
        {
            Texture boxCollider    = EditorGUIUtility.IconContent("d_BoxCollider Icon").image;
            Texture sphereCollider = EditorGUIUtility.IconContent("d_SphereCollider Icon").image;

            iconData = new (Type, Texture, Color)[]
            {
                (typeof(DustManager), EditorGUIUtility.FindTexture("GameManager Icon"), Color.magenta),
                (typeof(Cone),   EditorGUIUtility.FindTexture("d_Favorite On Icon"), Color.yellow),
                (typeof(Camera), EditorGUIUtility.FindTexture("Camera Gizmo"),       Color.cyan),
                (typeof(DustBoxCollider),    boxCollider,    Color.white),
                (typeof(DustSphereCollider), sphereCollider, Color.white),
            };
        }

        private static void DrawHierarchyIcon(int instanceID, Rect selectionRect)
        {
            Rect iconRect  = new Rect(selectionRect);
            iconRect.x     = 32f; // 하이라키 좌측 끝
            iconRect.width = 16f;

            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null)
                return;

            Color c = GUI.color;

            for (int i = 0; i < iconData.Length; i++)
            {
                ref var current = ref iconData[i];
                if (current.icon != null && go.GetComponent(current.type) != null)
                {
                    GUI.color = go.activeInHierarchy ? current.color : Color.white * 0.5f;
                    GUI.DrawTexture(iconRect, current.icon);
                    break;
                }
            }

            GUI.color = c;
        }
    }
}

#endif