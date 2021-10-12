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

        private GameObject worldGO;
        private MeshFilter worldMF;
        private MeshRenderer worldMR;

        // Inputs & Mode
        private KeyCode cleanerKey = KeyCode.Alpha1;
        private KeyCode blowerKey  = KeyCode.Alpha2;
        private KeyCode emitterKey = KeyCode.Alpha3;

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
        private int kernelGroupSizeX;

        // 게임 시작 시 초기화 작업 완료 후 처리
        private Queue<Action> afterInitJobQueue = new Queue<Action>();

        /***********************************************************************
        *                               Colliders
        ***********************************************************************/
        #region .

        // TODO : 공통 클래스 DustCollider, ColliderSet<DustCollider>로 묶어서 일반화

        private class SphereColliderSet
        {
            /* Collider */
            private ComputeBuffer colliderBuffer;
            private List<DustSphereCollider> colliders;

            /* Data */
            private Vector4[] dataArray;
            private int dataCount;

            /* Compute Shader, Compute Buffer */
            private ComputeShader computeShader;
            private int shaderKernel;
            private string bufferName;
            private string countVariableName;

            public SphereColliderSet(ComputeShader computeShader, int shaderKernel, string bufferName, string countVariableName)
            {
                this.colliders = new List<DustSphereCollider>(4);
                this.dataArray = new Vector4[4];
                this.computeShader = computeShader;
                this.shaderKernel = shaderKernel;
                this.bufferName = bufferName;
                this.countVariableName = countVariableName;
                this.dataCount = 0;

                colliderBuffer = new ComputeBuffer(1, 4); // 기본 값
                computeShader.SetBuffer(shaderKernel, bufferName, colliderBuffer);
                computeShader.SetInt(countVariableName, 0);
            }

            ~SphereColliderSet()
            {
                ReleaseBuffer();
            }

            private void ReleaseBuffer()
            {
                if (colliderBuffer != null)
                    colliderBuffer.Release();
            }

            private void ExpandDataArray()
            {
                Vector4[] newArray = new Vector4[this.dataArray.Length * 2];
                Array.Copy(this.dataArray, newArray, dataCount);
                this.dataArray = newArray;
            }

            /// <summary> Collider 리스트로부터 Vector4 배열에 데이터 전달 </summary>
            private void UpdateDataArray()
            {
                if (dataArray.Length < dataCount)
                    ExpandDataArray();

                for (int i = 0; i < dataCount; i++)
                {
                    dataArray[i] = colliders[i].SphereData;
                }
            }

            /// <summary> 컴퓨트 버퍼의 데이터를 새롭게 갱신하고 컴퓨트 쉐이더에 전달 </summary>
            public void UpdateBuffer()
            {
                ReleaseBuffer();
                if (dataCount == 0) return;

                UpdateDataArray();
                colliderBuffer = new ComputeBuffer(dataCount, sizeof(float) * 4);
                colliderBuffer.SetData(dataArray, 0, 0, dataCount);
                computeShader.SetBuffer(shaderKernel, bufferName, colliderBuffer);
                computeShader.SetInt(countVariableName, dataCount);
            }

            public void AddCollider(DustSphereCollider collider)
            {
                if (colliders.Contains(collider)) return;

                dataCount++;
                colliders.Add(collider);
                UpdateBuffer();
            }

            public void RemoveCollider(DustSphereCollider collider)
            {
                if (!colliders.Contains(collider)) return;

                dataCount--;
                colliders.Remove(collider);
                UpdateBuffer();
            }
        }

        private SphereColliderSet sphereColliderSet;

        public void AddSphereCollider(DustSphereCollider collider)
        {
            if (sphereColliderSet == null)
            {
                afterInitJobQueue.Enqueue(() => sphereColliderSet.AddCollider(collider));
            }
            else
            {
                sphereColliderSet.AddCollider(collider);
            }
        }

        public void RemoveSphereCollider(DustSphereCollider collider)
        {
            if (sphereColliderSet == null) return;

            sphereColliderSet.RemoveCollider(collider);
        }

        #endregion
        /***********************************************************************
        *                               Unity Events
        ***********************************************************************/
        #region .
        private void Start()
        {
            ClampDustCount();
            InitCones();

            InitKernels();
            InitComputeShader();
            InitArgsBuffer();
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
        /***********************************************************************
        *                               Private Methods
        ***********************************************************************/
        #region .
        private void CalculateWorldBounds(ref Bounds worldBounds)
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

        /// <summary> 필드 변경사항 감지하여 적용 </summary>
        private IEnumerator DetectDataChangesRoutine()
        {
            int oldDustCount      = this.dustCount;
            Color oldDustColorA   = this.dustColorA;
            Color oldDustColorB   = this.dustColorB;
            Vector3 oldWorldPivot = this.worldBottomCenter;
            Vector3 oldWorldSize  = this.worldSize;

            WaitForSeconds wfs = new WaitForSeconds(0.5f);
            while (true)
            {
                // 1. 먼지 개수
                if (oldDustCount != this.dustCount)
                {
                    ClampDustCount();
                    InitComputeShader();
                    InitComputeBuffers();
                    SetBuffersToShaders();
                    PopulateDusts();
                    SetDustColors();
                }
                // 2. 먼지 색상
                if (oldDustColorA != this.dustColorA ||
                    oldDustColorB != this.dustColorB)
                {
                    SetDustColors();
                }
                // 3. 월드 영역
                if (oldWorldPivot != this.worldBottomCenter ||
                    oldWorldSize  != this.worldSize)
                {
                    InitWorldBounds();
                }

                // 이전 값 저장
                oldDustCount  = this.dustCount;
                oldDustColorA = this.dustColorA;
                oldDustColorB = this.dustColorB;
                oldWorldPivot = this.worldBottomCenter;
                oldWorldSize  = this.worldSize;
                yield return wfs;
            }
        }

        #endregion
        /***********************************************************************
        *                               Init Methods
        ***********************************************************************/
        #region .
        private void ClampDustCount()
        {
            dustCount = Mathf.Clamp(dustCount, 1, 1_000_000);
        }

        private void InitCones()
        {
            cleaner.HideCone();
            emitter.HideCone();
            blower.HideCone();

            currentCone = cleaner;
            currentCone.ShowCone();
        }

        private void InitKernels()
        {
            kernelPopulate      = dustCompute.FindKernel("Populate");
            kernelSetDustColors = dustCompute.FindKernel("SetDustColors");
            kernelUpdate   = dustCompute.FindKernel("Update");
            kernelVacuumUp = dustCompute.FindKernel("VacuumUp");
            kernelEmit     = dustCompute.FindKernel("Emit");
            kernelBlow     = dustCompute.FindKernel("BlowWind");
        }

        private void InitComputeShader()
        {
            dustCompute.GetKernelThreadGroupSizes(kernelUpdate, out uint tx, out _, out _);
            kernelGroupSizeX = Mathf.CeilToInt((float)dustCount / tx);

            dustCompute.SetInt("dustCount", dustCount);
        }

        /// <summary> 메시 데이터 저장하는 인자 버퍼 생성 </summary>
        private void InitArgsBuffer()
        {
            if (argsBuffer != null) argsBuffer.Release();

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
        }

        /// <summary> 먼지 개수에 영향 받는 컴퓨트 버퍼들 생성 </summary>
        private void InitComputeBuffers()
        {
            if (dustBuffer         != null) dustBuffer.Release();
            if (dustColorBuffer    != null) dustColorBuffer.Release();
            if (dustVelocityBuffer != null) dustVelocityBuffer.Release();
            if (aliveNumberBuffer  != null) aliveNumberBuffer.Release();

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

            dustCompute.SetBuffer(kernelPopulate, "dustBuffer", dustBuffer);

            dustCompute.SetBuffer(kernelSetDustColors, "dustColorBuffer", dustColorBuffer);

            dustCompute.SetBuffer(kernelUpdate, "dustBuffer", dustBuffer);
            dustCompute.SetBuffer(kernelUpdate, "velocityBuffer", dustVelocityBuffer);
            dustCompute.SetBuffer(kernelUpdate, "aliveNumberBuffer", aliveNumberBuffer);

            dustCompute.SetBuffer(kernelVacuumUp, "dustBuffer", dustBuffer);
            dustCompute.SetBuffer(kernelVacuumUp, "velocityBuffer", dustVelocityBuffer);
            dustCompute.SetBuffer(kernelVacuumUp, "aliveNumberBuffer", aliveNumberBuffer);

            dustCompute.SetBuffer(kernelEmit, "dustBuffer", dustBuffer);
            dustCompute.SetBuffer(kernelEmit, "velocityBuffer", dustVelocityBuffer);
            dustCompute.SetBuffer(kernelEmit, "aliveNumberBuffer", aliveNumberBuffer);

            dustCompute.SetBuffer(kernelBlow, "dustBuffer", dustBuffer);
            dustCompute.SetBuffer(kernelBlow, "velocityBuffer", dustVelocityBuffer);
        }

        /// <summary> 먼지들을 영역 내의 무작위 위치에 생성 </summary>
        private void PopulateDusts()
        {
            Vector3 spawnCenter = spawnBottomCenter + Vector3.up * spawnSize.y * 0.5f;
            Bounds  spawnBounds = new Bounds(spawnCenter, spawnSize);

            dustCompute.SetVector("spawnBoundsMin", spawnBounds.min);
            dustCompute.SetVector("spawnBoundsMax", spawnBounds.max);
            dustCompute.Dispatch(kernelPopulate, kernelGroupSizeX, 1, 1);
        }

        /// <summary> 2가지 색상 사이의 무작위 색상을 먼지마다 설정 </summary>
        private void SetDustColors()
        {
            dustCompute.SetVector("dustColorA", dustColorA);
            dustCompute.SetVector("dustColorB", dustColorB);
            dustCompute.Dispatch(kernelSetDustColors, kernelGroupSizeX, 1, 1);
        }

        /// <summary> 월드 영역 큐브 메시 생성 및 컴퓨트 쉐이더에 값 전달 </summary>
        private void InitWorldBounds()
        {
            if (worldGO == null) worldGO = new GameObject("World");
            if (worldMF == null) worldMF = worldGO.AddComponent<MeshFilter>();
            if (worldMR == null) worldMR = worldGO.AddComponent<MeshRenderer>();

            CalculateWorldBounds(ref worldBounds);

            worldMF.sharedMesh = MeshMaker.CreateWorldBoundsMesh(worldBounds);
            worldMR.sharedMaterial = worldMaterial;

            dustCompute.SetVector("worldBoundsMin", worldBounds.min);
            dustCompute.SetVector("worldBoundsMax", worldBounds.max);
        }

        private void InitColliders()
        {
            sphereColliderSet = new SphereColliderSet(this.dustCompute, kernelUpdate, "sphereColliderBuffer", "sphereColliderCount");
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
            dustCompute.SetBool("cleanerKillOn", cleaner.KillMode);

            dustCompute.Dispatch(kernelVacuumUp, kernelGroupSizeX, 1, 1);
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

            dustCompute.Dispatch(kernelEmit, kernelGroupSizeX, 1, 1);
        }

        /// <summary> 송풍기 커널 실행 </summary>
        private void UpdateBlower()
        {
            if (!blower.IsRunning) return;

            dustCompute.SetFloat("blowerSqrForce", blower.SqrForce);
            dustCompute.SetFloat("blowerSqrDist", blower.SqrDistance);
            dustCompute.SetFloat("blowerDotThreshold", Mathf.Cos(blower.AngleRad));

            dustCompute.Dispatch(kernelBlow, kernelGroupSizeX, 1, 1);
        }

        /// <summary> 물리 업데이트 </summary>
        private void UpdatePhysics()
        {
            dustCompute.Dispatch(kernelUpdate, kernelGroupSizeX, 1, 1);

            aliveNumberBuffer.GetData(aliveNumberArray);
            aliveNumber = (int)aliveNumberArray[0];
        }
        #endregion
    }
}