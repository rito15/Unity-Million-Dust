using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 날짜 : 2021-10-05 PM 4:49:41
// 작성자 : Rito

namespace Rito.MillionDust
{
    public abstract class ConeBase : MonoBehaviour
    {
        [SerializeField] protected Material coneMaterial;

        [Space]
        [SerializeField] protected bool isRunning = false;

        [Range(0f, 200f)]
        [SerializeField] protected float force = 50f;

        [Range(1f, 50f)]
        [SerializeField] protected float distance = 5f;

        [Range(0.01f, 89.9f)]
        [SerializeField] protected float angle = 45f;

        protected Transform coneTransform;
        protected GameObject coneGO;


        public bool IsRunning { get => isRunning; set => isRunning = value; }
        public float SqrDistance => distance * distance;
        public float Distance => distance;
        public float SqrForce => force * force;
        public float Force => force;
        public float AngleRad => angle * Mathf.Deg2Rad;


        protected virtual void Awake()
        {
            CreateCone(coneMaterial);
        }

        private void Update()
        {
            ChangeConeScale();
        }

        public void ShowCone() => coneGO.SetActive(true);
        public void HideCone() => coneGO.SetActive(false);


        /// <summary> 옵션 변경에 따라 자식 스케일 변경 </summary>
        private void ChangeConeScale()
        {
            float r = Mathf.Tan(angle * Mathf.Deg2Rad) * distance * 0.5f;
            float z = distance * 0.5f;

            coneTransform.localScale = new Vector3(r, r, z);
        }

        /// <summary> 자식 원뿔 생성 </summary>
        protected void CreateCone(Material material)
        {
            coneGO = new GameObject("Cone Mesh");
            coneTransform = coneGO.transform;
            coneTransform.SetParent(transform, false);

            MeshRenderer mr = coneGO.AddComponent<MeshRenderer>();
            mr.material = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            MeshFilter mf = coneGO.AddComponent<MeshFilter>();
            mf.sharedMesh = MeshMaker.CreateConeMesh();
        }
    }
}