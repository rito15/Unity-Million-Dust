
// 구체 충돌 여부 검사
bool CheckSphereToSphereCollision(float3 posA, float radiusA, float3 posB, float radiusB)
{
    return SqrMagnitude(posA - posB) < Square(radiusA + radiusB);
}

// 점 A에서 점 B로 레이캐스트하여 평면과 접점 찾기
float3 RaycastToPlane(float3 A, float3 B, float3 P, float3 N)
{
    //A = Ray Origin;
    //B = Ray End;
    //P = Plane Point;
    //N = Plane Normal;
    float3 AB = (B - A);
    float3 nAB = normalize(AB);
    
    float d = dot(N, P - A) / dot(N, nAB);
    float3 C = A + nAB * d;
    return C;
}