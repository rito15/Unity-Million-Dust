using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 날짜 : 2021-09-26 PM 9:47:41
// 작성자 : Rito
// https://www.youtube.com/watch?v=PGk0rnyTa1U

namespace Rito.MillionDust
{
    using SF = UnityEngine.SerializeField;

    /*======================================================================*/
    /*                                                                      */
    /*                         Fields & Unitey Events                       */
    /*                                                                      */
    /*======================================================================*/

    [DisallowMultipleComponent]
    public partial class DustManager : MonoBehaviour
    {
        // Singleton
        public static DustManager Instance => _instance;
        private static DustManager _instance;

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
        [Range(0.01f, 1f)]
        [SF] private float dustRadius = 1f;

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
        [SF] private Cannon cannon;

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

        private GameObject worldGO;
        private MeshFilter worldMF;
        private MeshRenderer worldMR;

        // Inputs & Mode
        private KeyCode cleanerKey = KeyCode.Alpha1;
        private KeyCode blowerKey  = KeyCode.Alpha2;
        private KeyCode emitterKey = KeyCode.Alpha3;
        private KeyCode cannonKey  = KeyCode.Alpha4;

        private KeyCode operationKey  = KeyCode.Mouse0;
        private KeyCode showCursorKey = KeyCode.Mouse1;
        private Cone currentCone;

        // Compute Shader Data
        private int kernelPopulate;
        private int kernelSetDustColors;
        private int kernelUpdate;
        private int kernelVacuumUp;
        private int kernelEmit;
        private int kernelBlow;
        private int kernelExplode;
        private int kernelGroupSizeX;

        // 게임 시작 시 초기화 작업 완료 후 처리
        private Queue<Action> afterInitJobQueue = new Queue<Action>();

        private ColliderSet<DustSphereCollider, Vector4> sphereColliderSet;

        /***********************************************************************
        *                               Unity Events
        ***********************************************************************/
        #region .
        private void Awake()
        {
            _instance = this;
        }

        private void Start()
        {
            ClampDustCount();
            InitCones();

            InitKernels();
            InitComputeShader();
            InitComputeBuffers();
            SetBuffersToShaders();

            PopulateDusts();
            SetDustColors();
            InitWorldBounds();
            InitColliders();

            ProcessInitialJobs();

            StartCoroutine(DetectDataChangesRoutine());
        }

        /// <summary> 초기화 이전에 쌓인 작업들 처리 </summary>
        private void ProcessInitialJobs()
        {
            while (afterInitJobQueue.Count > 0)
                afterInitJobQueue.Dequeue()?.Invoke();
            afterInitJobQueue = null;
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

            CalculateWorldBounds(ref worldBounds);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
        }
        #endregion
    }
}