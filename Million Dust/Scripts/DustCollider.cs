using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 날짜 : 2021-10-13 PM 8:59:37
// 작성자 : Rito

namespace Rito.MillionDust
{
    /// <summary> 
    /// 먼지 월드와 동기화되는 충돌체
    /// </summary>
    public class DustCollider : MonoBehaviour
    {
        public const string ColliderTag = "DustCollider";

        protected void Awake()
        {
            tag = ColliderTag;
        }
    }
}