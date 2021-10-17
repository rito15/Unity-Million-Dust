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
        private class ColliderSet<TCol, TData> where TCol : DustCollider<TData>
        {
            /* Collider */
            private ComputeBuffer colliderBuffer;
            private List<TCol> colliders;

            /* Data */
            private TData[] dataArray;
            private int dataCount;
            private int dataStride;

            /* Compute Shader, Compute Buffer */
            private ComputeShader computeShader;
            private int shaderKernel; // Update Kernel
            private string bufferName;
            private string countVariableName;

            public ColliderSet(ComputeShader computeShader, int shaderKernel, string bufferName, string countVariableName, int dataStride)
            {
                this.colliders = new List<TCol>(4);
                this.dataArray = new TData[4];
                this.computeShader = computeShader;
                this.shaderKernel = shaderKernel;
                this.bufferName = bufferName;
                this.countVariableName = countVariableName;
                this.dataStride = dataStride;
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
                TData[] newArray = new TData[this.dataArray.Length * 2];
                Array.Copy(this.dataArray, newArray, this.dataArray.Length);
                this.dataArray = newArray;
            }

            /// <summary> Collider 리스트로부터 Vector4 배열에 데이터 전달 </summary>
            private void UpdateDataArray()
            {
                if (dataArray.Length < dataCount)
                    ExpandDataArray();

                for (int i = 0; i < dataCount; i++)
                {
                    dataArray[i] = colliders[i].Data;
                }
            }

            /// <summary> 컴퓨트 버퍼의 데이터를 새롭게 갱신하고 컴퓨트 쉐이더에 전달 </summary>
            public void UpdateBuffer()
            {
                ReleaseBuffer();
                if (dataCount == 0) return;

                UpdateDataArray();
                colliderBuffer = new ComputeBuffer(dataCount, dataStride);
                colliderBuffer.SetData(dataArray, 0, 0, dataCount);
                computeShader.SetBuffer(shaderKernel, bufferName, colliderBuffer);
                computeShader.SetInt(countVariableName, dataCount);
            }

            public void AddCollider(TCol collider)
            {
                if (colliders.Contains(collider)) return;

                dataCount++;
                colliders.Add(collider);
                UpdateBuffer();
            }

            public void RemoveCollider(TCol collider)
            {
                if (!colliders.Contains(collider)) return;

                dataCount--;
                colliders.Remove(collider);
                UpdateBuffer();
            }
        }
        #endregion
        /***********************************************************************
        *                               Public Methods
        ***********************************************************************/
        #region .
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

        public void UpdateSphereCollider()
        {
            if (sphereColliderSet == null) return;
            sphereColliderSet.UpdateBuffer();
        }

        public void RemoveSphereCollider(DustSphereCollider collider)
        {
            if (sphereColliderSet == null) return;

            sphereColliderSet.RemoveCollider(collider);
        }
        #endregion
    }
}