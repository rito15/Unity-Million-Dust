using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 날짜 : 2021-10-07 PM 4:41:16
// 작성자 : Rito

namespace Rito.MillionDust
{
    /// <summary> 
    /// 메시 생성 담당 정적 클래스
    /// </summary>
    public static class MeshMaker
    {
        /// <summary> 원뿔 모양 메시 생성 </summary>
        public static Mesh CreateConeMesh(int sample = 24)
        {
            Mesh mesh = new Mesh();
            Vector3[] verts = new Vector3[sample + 1];
            int[] tris = new int[sample * 3];

            verts[0] = Vector3.zero; // 꼭짓점
            float deltaRad = Mathf.PI * 2f / sample;
            for (int i = 1; i <= sample; i++)
            {
                float r = i * deltaRad;
                verts[i] = new Vector3(Mathf.Cos(r), Mathf.Sin(r), 1f);
            }

            int t = 0;
            for (int i = 1; i < sample; i++)
            {
                tris[t] = 0;
                tris[t + 1] = i + 1;
                tris[t + 2] = i;
                t += 3;
            }
            tris[t] = 0;
            tris[t + 1] = 1;
            tris[t + 2] = sample;

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary> 면이 내부를 향하는 큐브 생성 </summary>
        public static Mesh CreateWorldBoundsMesh(in Bounds bounds)
        {
            Vector3 m = bounds.min;
            Vector3 M = bounds.max;

            // Top
            Vector3 _0 = m.X_Z() + M._Y_();
            Vector3 _1 = m.X__() + M._YZ();
            Vector3 _2 = M;
            Vector3 _3 = m.__Z() + M.XY_();
            // Bottom
            Vector3 _4 = m;
            Vector3 _5 = m.XY_() + M.__Z();
            Vector3 _6 = m._Y_() + M.X_Z();
            Vector3 _7 = m._YZ() + M.X__();

            /* Vertices */
            const int SizeV = 8 * 3;
            Vector3[] V = new Vector3[SizeV];
            int i = 0;
            V[i++] = _0; V[i++] = _5; V[i++] = _4; V[i++] = _1; // Left
            V[i++] = _1; V[i++] = _6; V[i++] = _5; V[i++] = _2; // Forward
            V[i++] = _2; V[i++] = _7; V[i++] = _6; V[i++] = _3; // Right
            V[i++] = _3; V[i++] = _4; V[i++] = _7; V[i++] = _0; // Back
            V[i++] = _0; V[i++] = _2; V[i++] = _1; V[i++] = _3; // Top
            V[i++] = _5; V[i++] = _7; V[i++] = _4; V[i++] = _6; // Bottom

            /* Triangles */
            const int SizeT = 6 * 2 * 3;
            int[] T = new int[SizeT];
            for (i = 0; i < 6; i++)
            {
                int t = i * 6;
                int v = i * 4;
                T[t + 0] = v + 0;
                T[t + 1] = v + 1;
                T[t + 2] = v + 2;
                T[t + 3] = v + 0;
                T[t + 4] = v + 3;
                T[t + 5] = v + 1;
            }

            /* Colors */
            Color[] C = new Color[SizeV];
            Color gray = new Color(0.5f, 0.5f, 0.5f, 1f);
            Color grayDelta = new Color(0.05f, 0.05f, 0.05f, 0f);
            int[] D = { 0, 1, 0, 1, 2, 2 }; // Left, Forward, Right, Back, Top, Bot

            for (i = 0; i < 6; i++)
            {
                for(int j = 0; j < 4; j++)
                    C[i * 4 + j] = gray + grayDelta * D[i];
            }

            Mesh mesh = new Mesh();
            mesh.vertices = V;
            mesh.triangles = T;
            mesh.colors = C;

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
        }

        private static Vector3 X__(in this Vector3 vec) => new Vector3(vec.x, 0f, 0f);
        private static Vector3 _Y_(in this Vector3 vec) => new Vector3(0f, vec.y, 0f);
        private static Vector3 __Z(in this Vector3 vec) => new Vector3(0, 0f, vec.z);
        private static Vector3 XY_(in this Vector3 vec) => new Vector3(vec.x, vec.y, 0f);
        private static Vector3 _YZ(in this Vector3 vec) => new Vector3(0f, vec.y, vec.z);
        private static Vector3 X_Z(in this Vector3 vec) => new Vector3(vec.x, 0f, vec.z);
    }
}