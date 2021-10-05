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

        [Header("Dust Options")]
        [SerializeField] private Mesh dustMesh;         // 먼지 메시
        [SerializeField] private Material dustMaterial; // 먼지 마테리얼

        [Header("Player Options")]
        [SerializeField] private PlayerController controller;
        [SerializeField] private VacuumCleaner cleaner;
        [SerializeField] private DustEmitter emitter;

        [Header("Dust Option")]
        [SerializeField] private int instanceNumber = 100000;    // 생성할 먼지 개수
        [SerializeField] private float distributionRange = 100f; // 먼지 분포 범위(정사각형 너비)
        [SerializeField] private float distributionHeight = 5f;  // 먼지 분포 높이
        [Range(0.01f, 2f)]
        [SerializeField] private float dustScale = 1f;           // 먼지 크기

        [Header("Physics Options")]
        [Range(0f, 20f)]
        [SerializeField] private float mass = 1f;           // 먼지 질량
        [Range(0f, 20f)]
        [SerializeField] private float gravityForce = 9.8f; // 중력 강도
        [Range(0f, 100f)]
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


        private Bounds frustumOverlapBounds;

        private uint[] aliveNumberArray;
        private int aliveNumber;

        private int kernelPopulateID;
        private int kernelUpdateID;
        private int kernelBlowID;
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
        }

        private void Update()
        {
            deltaTime = Time.deltaTime;

            HandlePlayerInputs();
            UpdateComputeShader();

            dustMaterial.SetFloat("_Scale", dustScale);
            Graphics.DrawMeshInstancedIndirect(dustMesh, 0, dustMaterial, frustumOverlapBounds, argsBuffer);
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
            kernelBlowID = dustCompute.FindKernel("Blow");

            dustCompute.GetKernelThreadGroupSizes(kernelUpdateID, out uint tx, out _, out _);
            kernelGroupSizeX = Mathf.CeilToInt((float)instanceNumber / tx);

            dustCompute.SetInt("dustCount", instanceNumber);

            // Mode
            mode = SimulationMode.VacuumCleaner;
            cleaner.ShowCone();
            emitter.HideCone();
        }

        /// <summary> 컴퓨트 버퍼들 생성 </summary>
        private void InitBuffers()
        {
            /* [Note]
             * 
             * argsBuffer
             * - IndirectArguments로 사용되는 컴퓨트 버퍼의 stride는 20byte 이상이어야 한다.
             * - 따라서 파라미터가 앞의 2개만 필요하지만, 뒤에 의미 없는 파라미터 3개를 더 넣어준다.
             */

            // Args Buffer
            uint[] argsData = new uint[] { dustMesh.GetIndexCount(0), (uint)instanceNumber, 0, 0, 0 };
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

            // 카메라 프러스텀이 이 영역과 겹치지 않으면 렌더링되지 않는다.
            frustumOverlapBounds = new Bounds(
                Vector3.zero,
                new Vector3(distributionRange, distributionHeight, distributionRange)
            );
        }

        /// <summary> 컴퓨트 버퍼들을 쉐이더에 할당 </summary>
        private void SetBuffersToShaders()
        {
            dustMaterial.SetBuffer("_DustBuffer", dustBuffer);
            dustCompute.SetBuffer(kernelPopulateID, "dustBuffer", dustBuffer);

            dustCompute.SetBuffer(kernelUpdateID, "dustBuffer", dustBuffer);
            dustCompute.SetBuffer(kernelUpdateID, "aliveNumberBuffer", aliveNumberBuffer);
            dustCompute.SetBuffer(kernelUpdateID, "velocityBuffer", dustVelocityBuffer);

            dustCompute.SetBuffer(kernelBlowID, "dustBuffer", dustBuffer);
            dustCompute.SetBuffer(kernelBlowID, "aliveNumberBuffer", aliveNumberBuffer);
            dustCompute.SetBuffer(kernelBlowID, "velocityBuffer", dustVelocityBuffer);
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

            if (cleaner.IsRunning) UpdateCleanerVariables();
            if (emitter.IsRunning) UpdateEmitterVariables();
        }

        private void UpdateCleanerVariables()
        {
            dustCompute.SetFloat("cleanerSqrForce", cleaner.SqrForce);
            dustCompute.SetFloat("cleanerSqrDist", cleaner.SqrDistance);
            dustCompute.SetFloat("cleanerSqrDeathRange", cleaner.SqrDeathRange);
            dustCompute.SetFloat("cleanerDotThreshold", Mathf.Cos(cleaner.AngleRad));
        }

        private void UpdateEmitterVariables()
        {
            // * dustCount : 게임 시작 시 전달

            dustCompute.SetFloat("time", Time.time);
            dustCompute.SetMatrix("controllerMatrix", controller.LocalToWorld);
            dustCompute.SetFloat("emitterForce", emitter.Force);
            dustCompute.SetFloat("emitterAngleRad", emitter.AngleRad);

            dustCompute.Dispatch(kernelBlowID, kernelGroupSizeX, 1, 1);
        }

        private void UpdateComputeShader()
        {
            dustCompute.SetFloat("deltaTime", deltaTime);

            // 컨트롤러
            dustCompute.SetVector("controllerPos", controller.Position);
            dustCompute.SetVector("controllerForward", controller.Forward);

            // 청소기
            dustCompute.SetInt("cleanerRunning", cleaner.IsRunning ? TRUE : FALSE);

            // 물리
            dustCompute.SetFloat("radius", dustScale);
            dustCompute.SetFloat("mass", mass);
            dustCompute.SetFloat("gravityForce", gravityForce);
            dustCompute.SetFloat("airResistance", airResistance);
            dustCompute.SetFloat("elasticity", elasticity);

            dustCompute.Dispatch(kernelUpdateID, kernelGroupSizeX, 1, 1);

            aliveNumberBuffer.GetData(aliveNumberArray);
            aliveNumber = (int)aliveNumberArray[0];
        }
        #endregion
    }
}