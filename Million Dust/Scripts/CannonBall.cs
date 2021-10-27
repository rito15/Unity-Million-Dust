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

        private GameObject explosionPrefab;
        private float explosionParticleSize;

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

        /// <summary> 폭발 프리팹 등록 </summary>
        public void SetExplosionPrefab(GameObject prefab, float size)
        {
            this.explosionPrefab = prefab;
            this.explosionParticleSize = size;
        }

        /// <summary> 포탄 발사 </summary>
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

            // 1. Explode 커널 실행
            DustManager.Instance.Explode(transform.position, explosionSqrRange, explosionForce);

            // 2. 폭발 프리팹 생성
            if (explosionPrefab != null)
            {
                GameObject clone = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                clone.SetActive(true);

                ParticleSystem ps = clone.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var mainModule = ps.main;
                    mainModule.startSize = explosionParticleSize;
                }

                Destroy(clone, 2f);
            }

            Destroy(gameObject);
        }
    }
}