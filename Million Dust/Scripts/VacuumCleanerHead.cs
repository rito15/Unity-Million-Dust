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
    [SerializeField] private bool isRunning = true;

    [Range(0f, 200f), Tooltip("빨아들이는 힘")]
    [SerializeField] private float suctionForce = 50f;

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
    private Mesh coneMesh;

    public bool IsRunning => isRunning;
    public float SqrSuctionRange => suctionRange * suctionRange;
    public float SuctionForce => suctionForce;
    public float DeathRange => deathRange;
    public float SuctionAngleRad => suctionAngle * Mathf.Deg2Rad;

    public Vector3 Position => transform.position;
    public Vector3 Forward => transform.forward;

    private void Awake()
    {
        parent = transform.parent;
        CreateChildCone();
    }
    private void Update()
    {
        deltaTime = Time.deltaTime;

        ChangeConeScale();
        MouseControl();
        if (mouseLocked)
        {
            Move();
            Rotate();
        }
    }

    /***********************************************************************
    *                               Cone Gizmo(Child)
    ***********************************************************************/
    #region .

    private Transform childConeTr;
    [Space]
    [SerializeField] private Material coneMaterial;

    /// <summary> 자식 게임오브젝트 생성하여 메시 렌더러, 필터 추가 </summary>
    private void CreateChildCone()
    {
        GameObject go = new GameObject("Cone Mesh");
        childConeTr = go.transform;
        childConeTr.SetParent(transform, false);

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.material = coneMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = CreateConeMesh();
    }

    /// <summary> 원뿔 모양 메시 생성 </summary>
    private Mesh CreateConeMesh(int sample = 24)
    {
        Mesh mesh = new Mesh();
        Vector3[] verts = new Vector3[sample + 1];
        int[] tris = new int[sample * 3];

        verts[0] = Vector3.zero; // 꼭짓점
        float deltaRad = Mathf.PI * 2f / sample;
        for (int i = 1; i <= sample; i++)
        {
            float r = i * deltaRad;
            verts[i] = new Vector3(Mathf.Cos(r), Mathf.Sin(r), 1f);
        }

        int t = 0;
        for (int i = 1; i < sample; i++)
        {
            tris[t] = 0;
            tris[t + 1] = i + 1;
            tris[t + 2] = i;
            t += 3;
        }
        tris[t] = 0;
        tris[t + 1] = 1;
        tris[t + 2] = sample;

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary> 옵션 변경에 따라 자식 스케일 변경 </summary>
    private void ChangeConeScale()
    {
        float r = Mathf.Tan(suctionAngle * Mathf.Deg2Rad) * suctionRange * 0.5f;
        float z = suctionRange * 0.5f;

        childConeTr.localScale = new Vector3(r, r, z);
    }

    #endregion
    /***********************************************************************
    *                               Movements
    ***********************************************************************/
    #region .

    private void MouseControl()
    {
        // On/Off
        isRunning = Input.GetMouseButton(0);

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

    #endregion
}