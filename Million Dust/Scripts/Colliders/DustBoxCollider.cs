using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 날짜 : 2021-10-17 PM 5:33:04
// 작성자 : Rito

namespace Rito.MillionDust
{
    public class DustBoxCollider : DustCollider<MinMaxBounds>
    {
        public override MinMaxBounds Data
        {
            get
            {
                Bounds b = default;
                b.center = transform.position;
                b.extents = transform.lossyScale * 0.5f;
                return MinMaxBounds.FromBounds(b);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (!TryGetComponent(out BoxCollider _))
                gameObject.AddComponent<BoxCollider>();

            dustManager.AddBoxCollider(this);

#if UNITY_EDITOR
            StartCoroutine(UpdateColliderDataRoutine());
#endif
        }

        private void OnDisable()
        {
            dustManager.RemoveBoxCollider(this);
#if UNITY_EDITOR
            StopAllCoroutines();
#endif
        }
#if UNITY_EDITOR
        private IEnumerator UpdateColliderDataRoutine()
        {
            Vector3 prevPosition = transform.position;
            Vector3 prevScale = transform.lossyScale;
            Vector3 position, lossyScale;
            WaitForSeconds wfs = new WaitForSeconds(1f);

            while (true)
            {
                position = transform.position;
                lossyScale = transform.lossyScale;

                // 위치나 크기에 변화가 생기면 정보 업데이트
                if (position != prevPosition || lossyScale != prevScale)
                {
                    dustManager.UpdateBoxCollider();
                }

                prevPosition = position;
                prevScale = lossyScale;
                yield return wfs;
            }
        }
#endif
    }
}