using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 날짜 : 2021-09-27 PM 3:13:42
// 작성자 : Rito

/// <summary> 
/// 진공 청소기 헤드 - 빨아들이는 부분
/// </summary>
public class VacuumCleanerHead : MonoBehaviour
{
    [SerializeField] private bool run = true;

    [Range(0f, 100f), Tooltip("빨아들이는 힘")]
    [SerializeField] private float suctionForce = 1f;

    [Range(1f, 50f), Tooltip("빨아들이는 범위(거리)")]
    [SerializeField] private float suctionRange = 5f;

    [Range(0.01f, 90f), Tooltip("빨아들이는 원뿔 각도")]
    [SerializeField] private float suctionAngle = 45f;

    [Range(0.01f, 5f), Tooltip("먼지가 사망하는 영역 반지름")]
    [SerializeField] private float deathRange = 0.2f;

    [Range(0.01f, 100f)]
    [SerializeField] private float moveSpeed = 50f;

    private Transform parent;
    private float deltaTime;
    private bool mouseLocked = false;

    public bool Running => run;
    public float SqrSuctionRange => suctionRange * suctionRange;
    public float SuctionForce => suctionForce;
    public float DeathRange => deathRange;
    public float SuctionAngleRad => suctionAngle * Mathf.Deg2Rad;

    public Vector3 Position => transform.position;
    public Vector3 Forward => transform.forward;

    private void Awake()
    {
        parent = transform.parent;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        DrawConeGizmo(Position, suctionRange, suctionAngle);

        //Gizmos.color = Color.red;
        //Gizmos.DrawWireSphere(Position, deathRange);
    }

    private void Update()
    {
        deltaTime = Time.deltaTime;

        MouseControl();

        if (mouseLocked)
        {
            Move();
            Rotate();
        }
    }

    private void MouseControl()
    {
        // On/Off
        run = Input.GetMouseButton(0);

        // 마우스 보이기/숨기기
        if (Input.GetMouseButtonDown(1))
        {
            mouseLocked ^= true;
            Cursor.lockState = mouseLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !mouseLocked;
        }
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

    // origin : 원뿔 꼭대기
    // height : 원뿔 높이
    // angle  : 원뿔 각도
    private void DrawConeGizmo(Vector3 origin, float height, float angle, int sample = 24)
    {
        float deltaRad = Mathf.PI * 2f / sample;
        float circleRadius = Mathf.Tan(angle * Mathf.Deg2Rad) * height;
        Vector3 forward = Vector3.forward * height;

        Vector3 prevPoint = default;
        for (int i = 0; i <= sample; i++)
        {
            float delta = deltaRad * i;
            Vector3 circlePoint = new Vector3(Mathf.Cos(delta), Mathf.Sin(delta), 0f) * circleRadius;
            circlePoint += forward;
            circlePoint = circlePoint.normalized * height;

            circlePoint = transform.TransformPoint(circlePoint);

            Gizmos.DrawLine(circlePoint, origin);
            if (i > 0)
                Gizmos.DrawLine(circlePoint, prevPoint);
            prevPoint = circlePoint;
        }
    }
}