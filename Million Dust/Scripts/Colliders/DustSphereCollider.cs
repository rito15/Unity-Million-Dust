using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 날짜 : 2021-10-12 AM 2:17:05
// 작성자 : Rito

namespace Rito.MillionDust
{
    /// <summary> 
    /// 컴퓨트 쉐이더 내에서 사용될 구체 콜라이더
    /// </summary>
    [DisallowMultipleComponent]
    public class DustSphereCollider : DustCollider
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            if (!TryGetComponent(out SphereCollider _))
                gameObject.AddComponent<SphereCollider>();

            dustManager.AddSphereCollider(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            dustManager.RemoveSphereCollider(this);
        }
    }
}