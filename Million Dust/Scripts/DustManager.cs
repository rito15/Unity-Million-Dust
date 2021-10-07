using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Threading;

// 날짜 : 2021-09-26 PM 9:47:41
// 작성자 : Rito
// https://www.youtube.com/watch?v=PGk0rnyTa1U

namespace Rito.MillionDust
{
    using SF = UnityEngine.SerializeField;

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

        [Header("Dust")]
        [SF] private Mesh dustMesh;
        [SF] private Material dustMaterial;

        [Space(4f)]
        [SF] private int dustCount = 100000; // 생성할 먼지 개수
        [Range(0.01f, 2f)]
        [SF] private float dustScale = 1f;

        [Space(4f)]
        [SF] private Color dustColorA = Color.black;
        [SF] private Color dustColorB = Color.gray;

        [Header("Spawn")]
        [SF] private Vector3 spawnBottomCenter = new Vector3(0, 0, 0); // 분포 중심 하단 위치(피벗)
        [SF] private Vector3 spawnSize = new Vector3(100, 25, 100);    // 분포 너비(XYZ)

        // 월드 큐브 콜라이더
        [Header("World")]
        [SF] private Vector3 worldBottomCenter = new Vector3(0, 0, 0);
        [SF] private Vector3 worldSize = new Vector3(100, 25, 100);
        [SF] private Material worldMaterial;

        [Header("Player")]
        [SF] private PlayerController controller;
        [SF] private VacuumCleaner cleaner;
        [SF] private Cone blower;
        [SF] private DustEmitter emitter;

        [Header("Physics")]
        [Range(-20f, 20f)]
        [SF] private float gravityX = 0;

        [Range(-20f, 20f)]
        [SF] private float gravityY = -9.8f;

        [Range(-20f, 20f)]
        [SF] private float gravityZ = 0;

        [Space]
        [Range(0f, 20f)]
        [SF] private float mass = 1f;           // 먼지 질량

        [Range(0f, 10f)]
        [SF] private float airResistance = 1f;  // 공기 저항력

        [Range(0f, 1f)]
        [SF] private float elasticity = 0.6f;   // 충돌 탄성력

        [Header("Compute Shader")]
        [SF] private ComputeShader dustCompute;
        private ComputeBuffer dustBuffer;         // 먼지 데이터 버퍼(위치, ...)
        private ComputeBuffer dustVelocityBuffer; // 먼지 현재 속도 버퍼
        private ComputeBuffer argsBuffer;         // 먼지 렌더링 데이터 버퍼
        private ComputeBuffer aliveNumberBuffer;  // 생존 먼지 개수 버퍼
        private ComputeBuffer dustColorBuffer;    // 먼지 색상 버퍼


        // Private Variables
        private uint[] aliveNumberArray;
        private int aliveNumber;
        private float deltaTime;
        private Bounds worldBounds;

        // Inputs & Mode
        private KeyCode cleanerKey = KeyCode.Alpha1;
        private KeyCode blowerKey  = KeyCode.Alpha2;
        private KeyCode emitterKey = KeyCode.Alpha3;

        private KeyCode operationKey  = KeyCode.Mouse0;
        private KeyCode showCursorKey = KeyCode.Mouse1;
        private Cone currentCone;

        // Compute Shader Data
        private int kernelPopulateID;
        private int kernelUpdateID;
        private int kernelVacuumUpID;
        private int kernelEmitID;
        private int kernelBlowID;
        private int kernelGroupSizeX;

        /***********************************************************************
        *                               Unity Events
        ***********************************************************************/
        #region .
        private void Start()
        {
            Init();
            InitCones();
            InitBuffers();
            SetBuffersToShaders();
            PopulateDusts();
            CreateWorldBoundsMesh();
        }

        private void Update()
        {
            deltaTime = Time.deltaTime;

            HandlePlayerInputs();
            UpdateCommonVariables();
            UpdateVacuumCleaner();
            UpdateEmitter();
            UpdateBlower();
            UpdatePhysics();

            dustMaterial.SetFloat("_Scale", dustScale);
            Graphics.DrawMeshInstancedIndirect(dustMesh, 0, dustMaterial, worldBounds, argsBuffer);
        }

        private void OnDestroy()
        {
            if (dustBuffer != null) dustBuffer.Release();
            if (argsBuffer != null) argsBuffer.Release();
            if (aliveNumberBuffer != null) aliveNumberBuffer.Release();
            if (dustVelocityBuffer != null) dustVelocityBuffer.Release();
            if (dustColorBuffer != null) dustColorBuffer.Release();
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

            GUI.Box(r, $"{aliveNumber:#,###,##0} / {dustCount:#,###,##0}", boxStyle);
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying) return;

            CalculateWorldBounds();

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
        }
        #endregion
        /***********************************************************************
        *                               Tiny Methods
        ***********************************************************************/
        #region .
        private void CalculateWorldBounds()
        {
            Vector3 boundsCenter = worldBottomCenter + Vector3.up * worldSize.y * 0.5f;
            worldBounds = new Bounds(
                boundsCenter,
                worldSize
            );
        }
        private void ChangeCone(Cone next)
        {
            currentCone.HideCone();
            currentCone = next;
            currentCone.ShowCone();
        }
        #endregion
        /***********************************************************************
        *                               Init Methods
        ***********************************************************************/
        #region .
        private void Init()
        {
            aliveNumber = dustCount;

            kernelPopulateID = dustCompute.FindKernel("Populate");
            kernelUpdateID = dustCompute.FindKernel("Update");
            kernelVacuumUpID = dustCompute.FindKernel("VacuumUp");
            kernelEmitID = dustCompute.FindKernel("Emit");
            kernelBlowID = dustCompute.FindKernel("BlowWind");

            dustCompute.GetKernelThreadGroupSizes(kernelUpdateID, out uint tx, out _, out _);
            kernelGroupSizeX = Mathf.CeilToInt((float)dustCount / tx);

            dustCompute.SetInt("dustCount", dustCount);

            CalculateWorldBounds();
        }

        private void InitCones()
        {
            cleaner.HideCone();
            emitter.HideCone();
            blower.HideCone();

            currentCone = cleaner;
            currentCone.ShowCone();
        }

        /// <summary> 컴퓨트 버퍼들 생성 </summary>
        private void InitBuffers()
        {
            int subMeshIndex = 0;

            // Args Buffer
            uint[] argsData = new uint[]
            {
                (uint)dustMesh.GetIndexCount(subMeshIndex),
                (uint)dustCount,
                (uint)dustMesh.GetIndexStart(subMeshIndex),
                (uint)dustMesh.GetBaseVertex(subMeshIndex),
                0
            };
            argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(argsData);

            // Dust Buffer
            dustBuffer = new ComputeBuffer(dustCount, sizeof(float) * 3 + sizeof(int));

            // Color Buffer
            dustColorBuffer = new ComputeBuffer(dustCount, sizeof(float) * 3);

            // Dust Velocity Buffer
            dustVelocityBuffer = new ComputeBuffer(dustCount, sizeof(float) * 3);

            // Alive Number Buffer
            aliveNumberBuffer = new ComputeBuffer(1, sizeof(uint));
            aliveNumberArray = new uint[] { (uint)dustCount };
            aliveNumberBuffer.SetData(aliveNumberArray);
        }

        /// <summary> 컴퓨트 버퍼들을 쉐이더에 할당 </summary>
        private void SetBuffersToShaders()
        {
            dustMaterial.SetBuffer("_DustBuffer", dustBuffer);
            dustMaterial.SetBuffer("_DustColorBuffer", dustColorBuffer);
            dustCompute.SetBuffer(kernelPopulateID, "dustBuffer", dustBuffer);
            dustCompute.SetBuffer(kernelPopulateID, "dustColorBuffer", dustColorBuffer);

            dustCompute.SetBuffer(kernelUpdateID, "dustBuffer", dustBuffer);
            dustCompute.SetBuffer(kernelUpdateID, "velocityBuffer", dustVelocityBuffer);
            dustCompute.SetBuffer(kernelUpdateID, "aliveNumberBuffer", aliveNumberBuffer);

            dustCompute.SetBuffer(kernelVacuumUpID, "dustBuffer", dustBuffer);
            dustCompute.SetBuffer(kernelVacuumUpID, "velocityBuffer", dustVelocityBuffer);
            dustCompute.SetBuffer(kernelVacuumUpID, "aliveNumberBuffer", aliveNumberBuffer);

            dustCompute.SetBuffer(kernelEmitID, "dustBuffer", dustBuffer);
            dustCompute.SetBuffer(kernelEmitID, "velocityBuffer", dustVelocityBuffer);
            dustCompute.SetBuffer(kernelEmitID, "aliveNumberBuffer", aliveNumberBuffer);

            dustCompute.SetBuffer(kernelBlowID, "dustBuffer", dustBuffer);
            dustCompute.SetBuffer(kernelBlowID, "velocityBuffer", dustVelocityBuffer);
        }

        /// <summary> 먼지들을 영역 내의 무작위 위치에 생성한다. </summary>
        private void PopulateDusts()
        {
            Vector3 spawnCenter = spawnBottomCenter + Vector3.up * spawnSize.y * 0.5f;
            Bounds spawnBounds = new Bounds(spawnCenter, spawnSize);

            dustCompute.SetVector("spawnBoundsMin", spawnBounds.min);
            dustCompute.SetVector("spawnBoundsMax", spawnBounds.max);
            dustCompute.SetVector("dustColorA", dustColorA);
            dustCompute.SetVector("dustColorB", dustColorB);

            dustCompute.GetKernelThreadGroupSizes(kernelPopulateID, out uint tx, out _, out _);
            int groupSizeX = Mathf.CeilToInt((float)dustCount / tx);

            dustCompute.Dispatch(kernelPopulateID, groupSizeX, 1, 1);
        }

        /// <summary> 월드 영역 큐브 메시 생성 </summary>
        private void CreateWorldBoundsMesh()
        {
            GameObject go = new GameObject("World");
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = MeshMaker.CreateWorldBoundsMesh(worldBounds);
            mr.sharedMaterial = worldMaterial;
        }
        #endregion
        /***********************************************************************
        *                               Update Methods
        ***********************************************************************/
        #region .
        /// <summary> 사용자 입력 처리 </summary>
        private void HandlePlayerInputs()
        {
            // 모드 변경
            if (Input.GetKeyDown(cleanerKey)) ChangeCone(cleaner);
            else if (Input.GetKeyDown(blowerKey)) ChangeCone(blower);
            else if (Input.GetKeyDown(emitterKey)) ChangeCone(emitter);

            // 마우스 보이기 & 숨기기
            if (Input.GetKeyDown(showCursorKey))
                controller.ShowCursorToggle();

            // 동작 수행
            bool run = controller.MouseLocked && Input.GetKey(operationKey);
            cleaner.IsRunning = run && currentCone == cleaner;
            blower.IsRunning  = run && currentCone == blower;
            emitter.IsRunning = run && currentCone == emitter;
        }

        /// <summary> 컴퓨트 쉐이더 공통 변수들 업데이트 </summary>
        private void UpdateCommonVariables()
        {
            dustCompute.SetFloat("deltaTime", deltaTime);

            // 컨트롤러
            dustCompute.SetVector("controllerPos", controller.Position);
            dustCompute.SetVector("controllerForward", controller.Forward);

            // 물리
            dustCompute.SetVector("worldBoundsMin", worldBounds.min);
            dustCompute.SetVector("worldBoundsMax", worldBounds.max);
            dustCompute.SetVector("gravity", new Vector3(gravityX, gravityY, gravityZ));
            dustCompute.SetFloat("radius", dustScale);
            dustCompute.SetFloat("mass", mass);
            dustCompute.SetFloat("airResistance", airResistance);
            dustCompute.SetFloat("elasticity", elasticity);
        }

        /// <summary> 청소기 커널 실행 </summary>
        private void UpdateVacuumCleaner()
        {
            if (!cleaner.IsRunning) return;

            dustCompute.SetFloat("cleanerSqrForce", cleaner.SqrForce);
            dustCompute.SetFloat("cleanerSqrDist", cleaner.SqrDistance);
            dustCompute.SetFloat("cleanerSqrDeathRange", cleaner.SqrDeathRange);
            dustCompute.SetFloat("cleanerDotThreshold", Mathf.Cos(cleaner.AngleRad));

            dustCompute.Dispatch(kernelVacuumUpID, kernelGroupSizeX, 1, 1);
        }

        /// <summary> 방출기 커널 실행 </summary>
        private void UpdateEmitter()
        {
            if (!emitter.IsRunning) return;

            dustCompute.SetFloat("time", Time.time);
            dustCompute.SetMatrix("controllerMatrix", controller.LocalToWorld);
            dustCompute.SetFloat("emitterForce", emitter.Force);
            dustCompute.SetFloat("emitterDist", emitter.Distance);
            dustCompute.SetFloat("emitterAngleRad", emitter.AngleRad);
            dustCompute.SetInt("emissionPerSec", emitter.EmissionPerSec);

            dustCompute.Dispatch(kernelEmitID, kernelGroupSizeX, 1, 1);
        }

        /// <summary> 송풍기 커널 실행 </summary>
        private void UpdateBlower()
        {
            if (!blower.IsRunning) return;

            dustCompute.SetFloat("blowerSqrForce", blower.SqrForce);
            dustCompute.SetFloat("blowerSqrDist", blower.SqrDistance);
            dustCompute.SetFloat("blowerDotThreshold", Mathf.Cos(blower.AngleRad));

            dustCompute.Dispatch(kernelBlowID, kernelGroupSizeX, 1, 1);
        }

        /// <summary> 물리 업데이트 </summary>
        private void UpdatePhysics()
        {
            dustCompute.Dispatch(kernelUpdateID, kernelGroupSizeX, 1, 1);

            aliveNumberBuffer.GetData(aliveNumberArray);
            aliveNumber = (int)aliveNumberArray[0];
        }
        #endregion
    }
}