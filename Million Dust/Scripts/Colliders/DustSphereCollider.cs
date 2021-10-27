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

#if UNITY_EDITOR
            StartCoroutine(UpdateColliderDataRoutine());
#endif
        }

        private void OnDisable()
        {
            dustManager.RemoveSphereCollider(this);
#if UNITY_EDITOR
            StopAllCoroutines();
#endif
        }

#if UNITY_EDITOR
        private IEnumerator UpdateColliderDataRoutine()
        {
            Vector3 prevPosition = transform.position;
            Vector3 prevScale = transform.lossyScale;
            Vector3 position, lossyScale, localScale;
            WaitForSeconds wfs = new WaitForSeconds(1f);

            while (true)
            {
                // 스케일 X에 XYZ를 모두 맞추기
                localScale = transform.localScale;
                if (localScale.x != localScale.y || localScale.x != localScale.z || localScale.y != localScale.z)
                {
                    transform.localScale = Vector3.one * localScale.x;
                }

                position = transform.position;
                lossyScale = transform.lossyScale;

                // 위치나 크기에 변화가 생기면 정보 업데이트
                if (position != prevPosition || lossyScale != prevScale)
                {
                    dustManager.UpdateSphereCollider();
                }

                prevPosition = position;
                prevScale = lossyScale;
                yield return wfs;
            }
        }
#endif
    }
}