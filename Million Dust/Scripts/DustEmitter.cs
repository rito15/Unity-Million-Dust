using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 날짜 : 2021-10-05 PM 4:33:25
// 작성자 : Rito

namespace Rito.MillionDust
{
    public class DustEmitter : Cone
    {
        // 초당 발사되는 먼지 개수
        [SerializeField, Range(1000, 100000)]
        private int emissionPerSec = 1000;

        public int EmissionPerSec => emissionPerSec;
    }
}