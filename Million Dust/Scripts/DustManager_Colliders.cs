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