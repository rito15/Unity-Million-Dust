using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 날짜 : 2021-10-13 PM 8:51:40
// 작성자 : Rito

namespace Rito.MillionDust
{
    /// <summary> 
    /// 대포 발사대
    /// </summary>
    public class Cannon : Cone
    {
        [Header("Cannon Options")]
        [SerializeField] private GameObject cannonBallPrefab;
        [SerializeField] private GameObject explosionPrefab;

        [Range(1, 200f)]
        [SerializeField] private float explosionRange = 25f;

        [Range(100f, 10000f)]
        [SerializeField] private float explosionForce = 3000f;

        [Range(0.1f, 2f)]
        [SerializeField] private float shootingInterval = 1f;

        private float currentCooldown = 0f;

        private void Update()
        {
            if (currentCooldown > 0f)
                currentCooldown -= Time.deltaTime;

            // 발사
            if (isRunning && currentCooldown <= 0f)
            {
                currentCooldown = shootingInterval;
                Shoot();
            }
        }

        public void Shoot()
        {
            GameObject clone = Instantiate(cannonBallPrefab, transform.position, Quaternion.identity);
            CannonBall ball = clone.GetComponent<CannonBall>();

            ball.SetExplosionPrefab(explosionPrefab, explosionRange * 2f);
            ball.Shoot(transform.forward * force, explosionRange, explosionForce);
        }
    }
}