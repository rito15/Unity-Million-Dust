using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 날짜 : 2021-10-17 PM 7:29:16
// 작성자 : Rito

namespace Rito.MillionDust
{
    /// <summary> 
    /// Box Collider를 위한 Min-Max 데이터
    /// </summary>
    public struct MinMaxBounds
    {
        public Vector3 min;
        public Vector3 max;

        public static MinMaxBounds FromBounds(in Bounds bounds)
        {
            MinMaxBounds mmb = default;
            mmb.min = bounds.min;
            mmb.max = bounds.max;
            return mmb;
        }
    }
}