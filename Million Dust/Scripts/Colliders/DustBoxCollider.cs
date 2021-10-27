using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 날짜 : 2021-10-17 PM 5:33:04
// 작성자 : Rito

namespace Rito.MillionDust
{
    public class DustBoxCollider : DustCollider
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            if (!TryGetComponent(out BoxCollider _))
                gameObject.AddComponent<BoxCollider>();

            dustManager.AddBoxCollider(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            dustManager.RemoveBoxCollider(this);
        }
    }
}