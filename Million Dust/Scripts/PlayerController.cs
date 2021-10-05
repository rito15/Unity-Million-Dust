using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 날짜 : 2021-10-05 PM 4:33:57
// 작성자 : Rito

namespace Rito.MillionDust
{
    /// <summary> 
    /// 
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Range(0.01f, 100f)]
        [SerializeField] private float moveSpeed = 25f;

        private Transform parent;
        private float deltaTime;
        private bool mouseLocked = false;

        public bool MouseLocked => mouseLocked;
        public Vector3 Position => transform.position;
        public Vector3 Forward => transform.forward;
        public Matrix4x4 LocalToWorld => transform.localToWorldMatrix;

        private void Awake()
        {
            parent = transform.parent;
        }

        private void Update()
        {
            deltaTime = Time.deltaTime;

            if (mouseLocked)
            {
                Move();
                Rotate();
            }
        }

        // 마우스 보이기/숨기기
        public void ShowCursorToggle()
        {
            mouseLocked ^= true;
            Cursor.lockState = mouseLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !mouseLocked;
        }

        private void Move()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            float y = 0f;
            if (Input.GetKey(KeyCode.Space)) y += .5f;
            else if (Input.GetKey(KeyCode.LeftControl)) y -= .5f;

            Vector3 moveVec = new Vector3(x, y, z).normalized * moveSpeed;

            if (Input.GetKey(KeyCode.LeftShift))
                moveVec *= 2f;

            parent.Translate(moveVec * deltaTime, Space.Self);
        }

        private void Rotate()
        {
            float v = Input.GetAxisRaw("Mouse X") * deltaTime * 100f;
            float h = Input.GetAxisRaw("Mouse Y") * deltaTime * 100f;

            // 부모 : 좌우 회전
            parent.localRotation *= Quaternion.Euler(0, v, 0);

            // 상하 회전
            Vector3 eRot = transform.localEulerAngles;
            float nextX = eRot.x - h;
            if (0f < nextX && nextX < 90f)
            {
                eRot.x = nextX;
            }
            transform.localEulerAngles = eRot;
        }
    }
}