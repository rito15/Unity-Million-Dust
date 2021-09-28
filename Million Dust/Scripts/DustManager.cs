using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Threading;

// 날짜 : 2021-09-26 PM 9:47:41
// 작성자 : Rito
// https://www.youtube.com/watch?v=PGk0rnyTa1U

[DisallowMultipleComponent]
public class DustManager : MonoBehaviour
{
    private const int TRUE = 1;
    private const int FALSE = 0;

    private struct Dust
    {
        public Vector3 position;
        public int isAlive;
    }

    [Header("Dust Options")]
    [SerializeField] private Mesh dustMesh;         // 먼지 메시
    [SerializeField] private Material dustMaterial; // 먼지 마테리얼

    [Header("Vacuum Cleaner Options")]
    [SerializeField] private VacuumCleanerHead cleanerHead; // 진공 청소기 흡입부

    [Space]
    [SerializeField] private int instanceNumber = 100000;    // 생성할 먼지 개수
    [SerializeField] private float distributionRange = 100f; // 먼지 분포 범위(정사각형 너비)
    [SerializeField] private float distributionHeight = 5f;  // 먼지 분포 높이
    [Range(0.01f, 2f)]
    [SerializeField] private float dustScale = 1f;           // 먼지 크기

    [Space]
    [SerializeField] private ComputeShader dustCompute;
    private ComputeBuffer dustBuffer; // 먼지 데이터 버퍼(위치, ...)
    private ComputeBuffer argsBuffer; // 먼지 렌더링 데이터 버퍼
    private ComputeBuffer aliveNumberBuffer; // 생존 먼지 개수 RW

    private Bounds frustumOverlapBounds;

    private uint[] aliveNumberArray;
    private int aliveNumber;

    private int kernelPopulateID;
    private int kernelUpdateID;
    private int kernelUpdateGroupSizeX;

    private float deltaTime;

    /***********************************************************************
    *                               Unity Events
    ***********************************************************************/
    #region .
    private void Start()
    {
        InitBuffers();
        InitComputeShader();
        PopulateDusts();
    }

    private void Update()
    {
        deltaTime = Time.deltaTime;
        UpdateDustPositionsGPU();

        dustMaterial.SetFloat("_Scale", dustScale);
        Graphics.DrawMeshInstancedIndirect(dustMesh, 0, dustMaterial, frustumOverlapBounds, argsBuffer);
    }
    private void OnDestroy()
    {
        dustBuffer.Release();
        argsBuffer.Release();
        aliveNumberBuffer.Release();
    }

    private GUIStyle boxStyle;
    private void OnGUI()
    {
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = 48;
        }

        float scWidth = Screen.width;
        float scHeight = Screen.height;
        Rect r = new Rect(scWidth * 0.04f, scHeight * 0.04f, scWidth * 0.25f, scHeight * 0.05f);

        GUI.Box(r, $"{aliveNumber:#,###,###} / {instanceNumber:#,###,###}", boxStyle);
    }
    #endregion
    /***********************************************************************
    *                               Init Methods
    ***********************************************************************/
    #region .
    /// <summary> 컴퓨트 버퍼들 생성 </summary>
    private void InitBuffers()
    {
        // Args Buffer
        // IndirectArguments로 사용되는 컴퓨트 버퍼의 stride는 20byte 이상이어야 한다.
        // 따라서 파라미터가 앞의 2개만 필요하지만, 뒤에 의미 없는 파라미터 3개를 더 넣어준다.
        uint[] argsData = new uint[] { (uint)dustMesh.GetIndexCount(0), (uint)instanceNumber, 0, 0, 0 };
        aliveNumber = instanceNumber;

        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(argsData);

        // Dust Buffer
        dustBuffer = new ComputeBuffer(instanceNumber, sizeof(float) * 3 + sizeof(int));
        dustMaterial.SetBuffer("_DustBuffer", dustBuffer);

        // Alive Number Buffer
        aliveNumberBuffer = new ComputeBuffer(1, sizeof(uint));
        aliveNumberArray = new uint[] { (uint)instanceNumber };
        aliveNumberBuffer.SetData(aliveNumberArray);

        // 카메라 프러스텀이 이 영역과 겹치지 않으면 렌더링되지 않는다.
        frustumOverlapBounds = new Bounds(
            Vector3.zero, 
            new Vector3(distributionRange, distributionHeight, distributionRange));
    }

    /// <summary> 컴퓨트 쉐이더 초기화 </summary>
    private void InitComputeShader()
    {
        kernelPopulateID = dustCompute.FindKernel("Populate");
        kernelUpdateID   = dustCompute.FindKernel("Update");

        dustCompute.SetBuffer(kernelPopulateID, "dustBuffer", dustBuffer);
        dustCompute.SetBuffer(kernelUpdateID, "dustBuffer", dustBuffer);
        dustCompute.SetBuffer(kernelUpdateID, "aliveNumberBuffer", aliveNumberBuffer);

        dustCompute.GetKernelThreadGroupSizes(kernelUpdateID, out uint tx, out _, out _);
        kernelUpdateGroupSizeX = Mathf.CeilToInt((float)instanceNumber / tx);
    }

    /// <summary> 먼지들을 영역 내의 무작위 위치에 생성한다. </summary>
    private void PopulateDusts()
    {
        Vector3 boundsMin, boundsMax;
        boundsMin.x = boundsMin.z = -0.5f * distributionRange;
        boundsMax.x = boundsMax.z = -boundsMin.x;
        boundsMin.y = 0f;
        boundsMax.y = distributionHeight;

        dustCompute.SetVector("boundsMin", boundsMin);
        dustCompute.SetVector("boundsMax", boundsMax);

        dustCompute.GetKernelThreadGroupSizes(kernelPopulateID, out uint tx, out _, out _);
        int groupSizeX = Mathf.CeilToInt((float)instanceNumber / tx);

        dustCompute.Dispatch(kernelPopulateID, groupSizeX, 1, 1);
    }
    #endregion
    /***********************************************************************
    *                               Update Methods
    ***********************************************************************/
    #region .
    private void UpdateDustPositionsGPU()
    {
        if (cleanerHead.Running == false) return;
        ref var head = ref cleanerHead;

        Vector3 centerPos = head.Position;
        float sqrRange = head.SqrSuctionRange;
        float sqrDeathRange = head.DeathRange * head.DeathRange;
        float sqrForce = deltaTime * head.SuctionForce * head.SuctionForce;

        dustCompute.SetFloat("deltaTime", deltaTime);

        dustCompute.SetVector("centerPos", centerPos);
        dustCompute.SetFloat("sqrRange", sqrRange);
        dustCompute.SetFloat("sqrDeathRange", sqrDeathRange);
        dustCompute.SetFloat("sqrForce", sqrForce);

        // 원뿔
        dustCompute.SetVector("forward", head.Forward);
        dustCompute.SetFloat("dotThreshold", Mathf.Cos(head.SuctionAngleRad));

        dustCompute.Dispatch(kernelUpdateID, kernelUpdateGroupSizeX, 1, 1);

        aliveNumberBuffer.GetData(aliveNumberArray);
        aliveNumber = (int)aliveNumberArray[0];
    }
    #endregion
}