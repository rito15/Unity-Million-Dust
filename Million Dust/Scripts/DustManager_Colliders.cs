using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 날짜 : 2021-10-17 PM 4:20:57
// 작성자 : Rito

namespace Rito.MillionDust
{
    /*======================================================================*/
    /*                                                                      */
    /*                              Colliders                               */
    /*                                                                      */
    /*======================================================================*/
    public partial class DustManager : MonoBehaviour
    {
        /***********************************************************************
        *                               Class Definitions
        ***********************************************************************/
        #region .
        private class ColliderSet
        {
            private struct ColliderData
            {
                public Matrix4x4 localToWorld;
                public Matrix4x4 worldToLocal;
                public Vector3 scale;

                public ColliderData(DustCollider collider)
                {
                    localToWorld = collider.transform.localToWorldMatrix;
                    worldToLocal = collider.transform.worldToLocalMatrix;
                    scale = collider.transform.lossyScale;
                }
            }

            /* Collider */
            private ComputeBuffer colliderBuffer;
            private List<DustCollider> colliders;

            /* Data */
            private ColliderData[] dataArray;
            private int dataCount;

            /* Compute Shader, Compute Buffer */
            private ComputeShader computeShader;
            private int shaderKernel; // Update Kernel
            private string bufferName;
            private string countVariableName;

            public ColliderSet(ComputeShader computeShader, int shaderKernel, string bufferName, string countVariableName)
            {
                this.colliders = new List<DustCollider>(4);
                this.dataArray = new ColliderData[4];
                this.computeShader = computeShader;
                this.shaderKernel = shaderKernel;
                this.bufferName = bufferName;
                this.countVariableName = countVariableName;
                this.dataCount = 0;

                colliderBuffer = new ComputeBuffer(1, 4); // 기본 값
                computeShader.SetBuffer(shaderKernel, bufferName, colliderBuffer);
                computeShader.SetInt(countVariableName, 0);
            }

            ~ColliderSet()
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
                ColliderData[] newArray = new ColliderData[this.dataArray.Length * 2];
                Array.Copy(this.dataArray, newArray, this.dataArray.Length);
                this.dataArray = newArray;
            }

            /// <summary> 컴퓨트 버퍼의 데이터를 새롭게 갱신하고 컴퓨트 쉐이더에 전달 </summary>
            private void ReallocateBuffer()
            {
                ReleaseBuffer();
                if (dataCount == 0) return;

                colliderBuffer = new ComputeBuffer(dataCount, sizeof(float) * 35);
                computeShader.SetInt(countVariableName, dataCount);
                UpdateColliderData();
            }

            /// <summary> 배열 내부의 콜라이더 데이터만 갱신하여 컴퓨트 쉐이더에 전달 </summary>
            public void UpdateColliderData()
            {
                if (dataArray.Length < dataCount)
                    ExpandDataArray();

                for (int i = 0; i < dataCount; i++)
                {
                    dataArray[i] = new ColliderData(colliders[i]);
                }

                colliderBuffer.SetData(dataArray, 0, 0, dataCount);
                computeShader.SetBuffer(shaderKernel, bufferName, colliderBuffer);
            }

            public void AddCollider(DustCollider collider)
            {
                if (colliders.Contains(collider)) return;

                dataCount++;
                colliders.Add(collider);
                ReallocateBuffer();
            }

            public void RemoveCollider(DustCollider collider)
            {
                if (!colliders.Contains(collider)) return;

                dataCount--;
                colliders.Remove(collider);
                ReallocateBuffer();
            }
        }
        #endregion
        /***********************************************************************
        *                               Private Generic Methods
        ***********************************************************************/
        #region .
        /// <summary> ColliderSet에 새로운 Collider 추가 </summary>
        private void AddCollider(Func<ColliderSet> getter, DustCollider collider)
        {
            var colSet = getter();

            if (colSet == null)
            {
                afterInitJobQueue.Enqueue(() => getter().AddCollider(collider));
            }
            else
            {
                colSet.AddCollider(collider);
            }
        }

        /// <summary> ColliderSet의 내부 컴퓨트 버퍼 갱신 </summary>
        private void UpdateCollider(ColliderSet set)
        {
            if (set != null)
                set.UpdateColliderData();
        }

        /// <summary> ColliderSet에서 Collider 제거 </summary>
        private void RemoveCollider(ColliderSet set, DustCollider collider)
        {
            if (set != null)
                set.RemoveCollider(collider);
        }
        #endregion
        /***********************************************************************
        *                               Public Methods
        ***********************************************************************/
        #region .
        public void AddSphereCollider(DustSphereCollider collider)
        {
            AddCollider(() => sphereColliderSet, collider);
        }
        public void UpdateSphereCollider()
        {
            UpdateCollider(sphereColliderSet);
        }
        public void RemoveSphereCollider(DustSphereCollider collider)
        {
            RemoveCollider(sphereColliderSet, collider);
        }

        public void AddBoxCollider(DustBoxCollider collider)
        {
            AddCollider(() => boxColliderSet, collider);
        }
        public void UpdateBoxCollider()
        {
            UpdateCollider(boxColliderSet);
        }
        public void RemoveBoxCollider(DustBoxCollider collider)
        {
            RemoveCollider(boxColliderSet, collider);
        }
        #endregion
    }
}