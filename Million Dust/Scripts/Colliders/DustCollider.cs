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
    public abstract class DustCollider : MonoBehaviour
    {
        public const string ColliderTag = "DustCollider";
        protected DustManager dustManager;

        protected void Awake()
        {
            tag = ColliderTag;
        }

        protected virtual void OnEnable()
        {
            if (dustManager == null)
                dustManager = FindObjectOfType<DustManager>();
#if UNITY_EDITOR
            StartCoroutine(UpdateColliderDataRoutine());
#endif
        }

        protected virtual void OnDisable()
        {
#if UNITY_EDITOR
            StopAllCoroutines();
#endif
        }

#if UNITY_EDITOR
        private IEnumerator UpdateColliderDataRoutine()
        {
            yield return new WaitForEndOfFrame();

            Vector3 prevPosition = transform.position;
            Vector3 prevScale = transform.lossyScale;
            Vector3 prevAngles = transform.eulerAngles;
            Vector3 position, eulerAngles, lossyScale;
            WaitForSeconds wfs = new WaitForSeconds(0.2f);

            while (true)
            {
                position = transform.position;
                eulerAngles = transform.eulerAngles;
                lossyScale = transform.lossyScale;

                // 위치나 크기에 변화가 생기면 정보 업데이트
                if (position != prevPosition || lossyScale != prevScale || eulerAngles != prevAngles)
                {
                    dustManager.UpdateBoxCollider();
                }

                prevPosition = position;
                prevAngles = eulerAngles;
                prevScale = lossyScale;
                yield return wfs;
            }
        }
#endif
    }
}