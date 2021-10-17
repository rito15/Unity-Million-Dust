using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 날짜 : 2021-10-17 PM 4:23:05
// 작성자 : Rito

namespace Rito.MillionDust
{
    /*======================================================================*/
    /*                                                                      */
    /*                                Methods                               */
    /*                                                                      */
    /*======================================================================*/
    public partial class DustManager : MonoBehaviour
    {
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
            int oldDustCount = this.dustCount;
            Color oldDustColorA = this.dustColorA;
            Color oldDustColorB = this.dustColorB;
            Vector3 oldWorldPivot = this.worldBottomCenter;
            Vector3 oldWorldSize = this.worldSize;

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
                    oldWorldSize != this.worldSize)
                {
                    InitWorldBounds();
                }

                // 이전 값 저장
                oldDustCount = this.dustCount;
                oldDustColorA = this.dustColorA;
                oldDustColorB = this.dustColorB;
                oldWorldPivot = this.worldBottomCenter;
                oldWorldSize = this.worldSize;
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
            cannon.HideCone();

            currentCone = cleaner;
            currentCone.ShowCone();
        }

        private void InitKernels()
        {
            kernelPopulate = dustCompute.FindKernel("Populate");
            kernelSetDustColors = dustCompute.FindKernel("SetDustColors");
            kernelUpdate = dustCompute.FindKernel("Update");
            kernelVacuumUp = dustCompute.FindKernel("VacuumUp");
            kernelEmit = dustCompute.FindKernel("Emit");
            kernelBlow = dustCompute.FindKernel("BlowWind");
            kernelExplode = dustCompute.FindKernel("Explode");
        }

        private void InitComputeShader()
        {
            dustCompute.GetKernelThreadGroupSizes(kernelUpdate, out uint tx, out _, out _);
            kernelGroupSizeX = Mathf.CeilToInt((float)dustCount / tx);

            dustCompute.SetInt("dustCount", dustCount);
        }

        /// <summary> 먼지 개수에 영향 받는 컴퓨트 버퍼들 생성 </summary>
        private void InitComputeBuffers()
        {
            if (argsBuffer != null) argsBuffer.Release();
            if (dustBuffer != null) dustBuffer.Release();
            if (dustColorBuffer != null) dustColorBuffer.Release();
            if (dustVelocityBuffer != null) dustVelocityBuffer.Release();
            if (aliveNumberBuffer != null) aliveNumberBuffer.Release();

            // Args Buffer
            int subMeshIndex = 0;
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

            dustCompute.SetBuffer(kernelPopulate, "dustBuffer", dustBuffer);

            dustCompute.SetBuffer(kernelSetDustColors, "dustColorBuffer", dustColorBuffer);
            //dustCompute.SetBuffer(kernelUpdate, "dustColorBuffer", dustColorBuffer); // For Debug

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

            dustCompute.SetBuffer(kernelExplode, "dustBuffer", dustBuffer);
            dustCompute.SetBuffer(kernelExplode, "velocityBuffer", dustVelocityBuffer);
        }

        /// <summary> 먼지들을 영역 내의 무작위 위치에 생성 </summary>
        private void PopulateDusts()
        {
            Vector3 spawnCenter = spawnBottomCenter + Vector3.up * spawnSize.y * 0.5f;
            Bounds spawnBounds = new Bounds(spawnCenter, spawnSize);

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

            worldGO.tag = DustCollider.ColliderTag;

            CalculateWorldBounds(ref worldBounds);

            worldMF.sharedMesh = MeshMaker.CreateWorldBoundsMesh(worldBounds);
            worldMR.sharedMaterial = worldMaterial;

            if (!worldGO.TryGetComponent(out MeshCollider col))
                col = worldGO.AddComponent<MeshCollider>();
            col.sharedMesh = worldMF.sharedMesh;

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
            else if (Input.GetKeyDown(cannonKey)) ChangeCone(cannon);

            // 마우스 보이기 & 숨기기
            if (Input.GetKeyDown(showCursorKey))
                controller.ShowCursorToggle();

            // 동작 수행
            bool run = controller.MouseLocked && Input.GetKey(operationKey);
            cleaner.IsRunning = run && currentCone == cleaner;
            blower.IsRunning = run && currentCone == blower;
            emitter.IsRunning = run && currentCone == emitter;
            cannon.IsRunning = run && currentCone == cannon;
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
            dustCompute.SetFloat("radius", dustRadius);
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
        /***********************************************************************
        *                               Public Methods
        ***********************************************************************/
        #region .

        /// <summary> Explode(폭발) 커널 실행 </summary>
        public void Explode(in Vector3 position, in float sqrRange, in float force)
        {
            dustCompute.SetVector("explosionPosition", position);
            dustCompute.SetFloat("explosionSqrRange", sqrRange);
            dustCompute.SetFloat("explosionForce", force);
            dustCompute.Dispatch(kernelExplode, kernelGroupSizeX, 1, 1);
        }

        #endregion
    }
}