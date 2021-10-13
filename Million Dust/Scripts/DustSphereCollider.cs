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
        [SerializeField] private Vector3 position = Vector3.zero;
        [SerializeField] private float radius = 1f;

        private DustManager dustManager;

        public Vector4 SphereData => new Vector4(
            position.x, position.y, position.z, radius
        );

        private void OnValidate()
        {
            ValidateData();
        }

        private void OnEnable()
        {
            if (dustManager == null)
                dustManager = FindObjectOfType<DustManager>();

            ValidateData();
            dustManager.AddSphereCollider(this);

            if (!TryGetComponent(out SphereCollider _))
                gameObject.AddComponent<SphereCollider>();
        }

        private void OnDisable()
        {
            dustManager.RemoveSphereCollider(this);
        }

        private void ValidateData()
        {
            if (radius < 0.1f)
                radius = 0.1f;

            transform.position = position;
            transform.localScale = Vector3.one * 2f * radius;
        }
    }
}