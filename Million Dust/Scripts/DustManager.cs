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

        private enum SimulationMode
        {
            VacuumCleaner,
            DustEmitter
        }

        [Header("Dust")]
        [SerializeField] private Mesh dustMesh;         // 먼지 메시
        [SerializeField] private Material dustMaterial; // 먼지 마테리얼
        [SerializeField] private int instanceNumber = 100000;    // 생성할 먼지 개수
        [Range(0.01f, 2f)]
        [SerializeField] private float dustScale = 1f;           // 먼지 크기

        [Header("Spawn")]
        [SerializeField] private Vector3 spawnBottomCenter = new Vector3(0, 0, 0); // 분포 중심 하단 위치(피벗)
        [SerializeField] private Vector3 spawnSize = new Vector3(100, 25, 100);    // 분포 너비(XYZ)

        // 월드 큐브 콜라이더
        [Header("World")]
        [SerializeField] private Vector3 worldBottomCenter = new Vector3(0, 0, 0);
        [SerializeField] private Vector3 worldSize = new Vector3(100, 25, 100);
        [SerializeField] private Material worldMaterial;

        [Header("Player")]
        [SerializeField] private PlayerController controller;
        [SerializeField] private VacuumCleaner cleaner;
        [SerializeField] private DustEmitter emitter;

        [Header("Physics")]
        [Range(-20f, 20f)]
        [SerializeField] private float gravityX = 0;

        [Range(-20f, 20f)]
        [SerializeField] private float gravityY = -9.8f;

        [Range(-20f, 20f)]
        [SerializeField] private float gravityZ = 0;

        [Space]
        [Range(0f, 20f)]
        [SerializeField] private float mass = 1f;           // 먼지 질량

        [Range(0f, 10f)]
        [SerializeField] private float airResistance = 1f;  // 공기 저항력

        [Range(0f, 1f)]
        [SerializeField] private float elasticity = 0.6f;   // 충돌 탄성력

        [Space]
        [SerializeField] private ComputeShader dustCompute;
        private ComputeBuffer dustBuffer;         // 먼지 데이터 버퍼(위치, ...)
        private ComputeBuffer dustVelocityBuffer; // 먼지 현재 속도 버퍼
        private ComputeBuffer argsBuffer;         // 먼지 렌더링 데이터 버퍼
        private ComputeBuffer aliveNumberBuffer;  // 생존 먼지 개수 버퍼


        // Inputs & Mode
        private KeyCode changeModeKey = KeyCode.Mouse2;
        private KeyCode runKey = KeyCode.Mouse0;
        private KeyCode showCursorKey = KeyCode.Mouse1;
        private SimulationMode mode;


        private Bounds worldBounds;

        private uint[] aliveNumberArray;
        private int aliveNumber;

        private int kernelPopulateID;
        private int kernelUpdateID;
        private int kernelVacuumUpID;
        private int kernelEmitID;
        private int kernelGroupSizeX;

        private float deltaTime;

        /***********************************************************************
        *                               Unity Events
        ***********************************************************************/
        #region .
        private void Start()
        {
            Init();
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
            UpdatePhysics();

            dustMaterial.SetFloat("_Scale", dustScale);
            Graphics.DrawMeshInstancedIndirect(dustMesh, 0, dustMaterial, worldBounds, argsBuffer);
        }

        private void OnDestroy()
        {
            dustBuffer.Release();
            argsBuffer.Release();
            aliveNumberBuffer.Release();
            dustVelocityBuffer.Release();
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

            GUI.Box(r, $"{aliveNumber:#,###,##0} / {instanceNumber:#,###,##0}", boxStyle);
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
        #endregion
        /***********************************************************************
        *                               Init Methods
        ***********************************************************************/
        #region .
        private void Init()
        {
            aliveNumber = instanceNumber;

            kernelPopulateID = dustCompute.FindKernel("Populate");
            kernelUpdateID = dustCompute.FindKernel("Update");
            kernelVacuumUpID = dustCompute.FindKernel("VacuumUp");
            kernelEmitID = dustCompute.FindKernel("Emit");

            dustCompute.GetKernelThreadGroupSizes(kernelUpdateID, out uint tx, out _, out _);
            kernelGroupSizeX = Mathf.CeilToInt((float)instanceNumber / tx);

            dustCompute.SetInt("dustCount", instanceNumber);

            // Mode
            mode = SimulationMode.VacuumCleaner;
            cleaner.ShowCone();
            emitter.HideCone();

            CalculateWorldBounds();
        }

        /// <summary> 컴퓨트 버퍼들 생성 </summary>
        private void InitBuffers()
        {
            int subMeshIndex = 0;

            // Args Buffer
            uint[] argsData = new uint[] 
            {
                (uint)dustMesh.GetIndexCount(subMeshIndex),
                (uint)instanceNumber,
                (uint)dustMesh.GetIndexStart(subMeshIndex),
                (uint)dustMesh.GetBaseVertex(subMeshIndex),
                0 
            };
            argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(argsData);

            // Dust Buffer
            dustBuffer = new ComputeBuffer(instanceNumber, sizeof(float) * 3 + sizeof(int));

            // Dust Velocity Buffer
            dustVelocityBuffer = new ComputeBuffer(instanceNumber, sizeof(float) * 3);

            // Alive Number Buffer
            aliveNumberBuffer = new ComputeBuffer(1, sizeof(uint));
            aliveNumberArray = new uint[] { (uint)instanceNumber };
            aliveNumberBuffer.SetData(aliveNumberArray);
        }

        /// <summary> 컴퓨트 버퍼들을 쉐이더에 할당 </summary>
        private void SetBuffersToShaders()
        {
            dustMaterial.SetBuffer("_DustBuffer", dustBuffer);
            dustCompute.SetBuffer(kernelPopulateID, "dustBuffer", dustBuffer);

            dustCompute.SetBuffer(kernelUpdateID, "dustBuffer", dustBuffer);
            dustCompute.SetBuffer(kernelUpdateID, "velocityBuffer", dustVelocityBuffer);
            dustCompute.SetBuffer(kernelUpdateID, "aliveNumberBuffer", aliveNumberBuffer);

            dustCompute.SetBuffer(kernelVacuumUpID, "dustBuffer", dustBuffer);
            dustCompute.SetBuffer(kernelVacuumUpID, "velocityBuffer", dustVelocityBuffer);
            dustCompute.SetBuffer(kernelVacuumUpID, "aliveNumberBuffer", aliveNumberBuffer);

            dustCompute.SetBuffer(kernelEmitID, "dustBuffer", dustBuffer);
            dustCompute.SetBuffer(kernelEmitID, "velocityBuffer", dustVelocityBuffer);
            dustCompute.SetBuffer(kernelEmitID, "aliveNumberBuffer", aliveNumberBuffer);
        }

        /// <summary> 먼지들을 영역 내의 무작위 위치에 생성한다. </summary>
        private void PopulateDusts()
        {
            Vector3 spawnCenter = spawnBottomCenter + Vector3.up * spawnSize.y * 0.5f;
            Bounds spawnBounds = new Bounds(spawnCenter, spawnSize);

            dustCompute.SetVector("spawnBoundsMin", spawnBounds.min);
            dustCompute.SetVector("spawnBoundsMax", spawnBounds.max);

            dustCompute.GetKernelThreadGroupSizes(kernelPopulateID, out uint tx, out _, out _);
            int groupSizeX = Mathf.CeilToInt((float)instanceNumber / tx);

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
            if (Input.GetKeyDown(changeModeKey))
            {
                switch (mode)
                {
                    case SimulationMode.VacuumCleaner:
                        mode = SimulationMode.DustEmitter;
                        cleaner.HideCone();
                        emitter.ShowCone();
                        break;

                    case SimulationMode.DustEmitter:
                        mode = SimulationMode.VacuumCleaner;
                        emitter.HideCone();
                        cleaner.ShowCone();
                        break;
                }
            }

            // 마우스 보이기 & 숨기기
            if (Input.GetKeyDown(showCursorKey))
                controller.ShowCursorToggle();

            // 동작 수행
            bool run = controller.MouseLocked && Input.GetKey(runKey);
            cleaner.IsRunning = run && mode == SimulationMode.VacuumCleaner;
            emitter.IsRunning = run && mode == SimulationMode.DustEmitter;
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

            // * dustCount : 게임 시작 시 전달

            dustCompute.SetFloat("time", Time.time);
            dustCompute.SetMatrix("controllerMatrix", controller.LocalToWorld);
            dustCompute.SetFloat("emitterForce", emitter.Force);
            dustCompute.SetFloat("emitterDist", emitter.Distance);
            dustCompute.SetFloat("emitterAngleRad", emitter.AngleRad);

            dustCompute.Dispatch(kernelEmitID, kernelGroupSizeX, 1, 1);
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