using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 날짜 : 2021-10-17 PM 5:33:04
// 작성자 : Rito

namespace Rito.MillionDust
{
    /// <summary> 
    /// 
    /// </summary>
    public class DustBoxCollider : DustCollider<Bounds>
    {
        public override Bounds Data => throw new NotImplementedException();

        private void OnDrawGizmos()
        {

        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (!TryGetComponent(out BoxCollider _))
                gameObject.AddComponent<BoxCollider>();

            //dustManager.AddSphereCollider(this);

#if UNITY_EDITOR
            //StartCoroutine(UpdateColliderDataRoutine());
#endif
        }

        private void OnDisable()
        {
            //dustManager.RemoveSphereCollider(this);
#if UNITY_EDITOR
            StopAllCoroutines();
#endif
        }
    }
}