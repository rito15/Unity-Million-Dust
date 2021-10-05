using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 날짜 : 2021-09-27 PM 3:13:42
// 작성자 : Rito

namespace Rito.MillionDust
{
    public class VacuumCleaner : ConeBase
    {
        [Range(0.01f, 5f), Tooltip("먼지가 사망하는 영역 반지름")]
        [SerializeField] private float deathRange = 0.2f;

        public float SqrDeathRange => deathRange * deathRange;
    }
}