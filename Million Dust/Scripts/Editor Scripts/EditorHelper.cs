#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

// 날짜 : 2021-10-15 PM 6:28:21
// 작성자 : Rito

namespace Rito.MillionDust
{
    public static class EditorHelper
    {
        private static bool isPlaymode = false;

        [InitializeOnEnterPlayMode]
        private static void OnEnterPlayMode()
        {
            isPlaymode = true;
        }

        [InitializeOnLoadMethod]
        private static void OnLoadMethod()
        {
            if (isPlaymode) return; 

            AddNewTag(DustCollider<int>.ColliderTag);
        }

        /// <summary> 태그 중복 확인 및 추가 </summary>
        public static void AddNewTag(string tagName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            int tagCount = tagsProp.arraySize;

            // [1] 해당 태그가 존재하는지 확인
            bool found = false;
            for (int i = 0; i < tagCount; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);

                if (t.stringValue.Equals(tagName))
                {
                    found = true;
                    break;
                }
            }

            // [2] 배열 마지막에 태그 추가
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(tagCount);
                SerializedProperty n = tagsProp.GetArrayElementAtIndex(tagCount);
                n.stringValue = tagName;
                tagManager.ApplyModifiedProperties();
            }
        }
    }
}
#endif