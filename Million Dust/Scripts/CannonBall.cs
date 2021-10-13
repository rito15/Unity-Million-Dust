using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 날짜 : 2021-10-13 PM 8:58:15
// 작성자 : Rito

namespace Rito.MillionDust
{
    /// <summary> 
    /// 투사체
    /// </summary>
    public class CannonBall : MonoBehaviour
    {
        [SerializeField] private Rigidbody rBody;

        private float explosionSqrRange;
        private float explosionForce;

        private void Init()
        {
            if (rBody == null)
                rBody = GetComponent<Rigidbody>();

            if (rBody == null)
                rBody = gameObject.AddComponent<Rigidbody>();

            TryGetComponent(out Collider col);
            col.isTrigger = true;
        }

        public void Shoot(in Vector3 movement, in float explosionRange, in float explosionForce, in float lifespan = 5f)
        {
            Init();
            Destroy(gameObject, lifespan);
            rBody.AddForce(movement, ForceMode.Impulse);

            this.explosionSqrRange = explosionRange * explosionRange;
            this.explosionForce = explosionForce;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(DustCollider.ColliderTag) == false) return;

            DustManager.Instance.Explode(transform.position, explosionSqrRange, explosionForce);
            Destroy(gameObject);
        }
    }
}