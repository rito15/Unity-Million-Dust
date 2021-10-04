
// ��ü �浹 ���� �˻�
bool CheckSphereToSphereCollision(float3 posA, float radiusA, float3 posB, float radiusB)
{
    return SqrMagnitude(posA - posB) < Square(radiusA + radiusB);
}

// �� A���� �� B�� ����ĳ��Ʈ�Ͽ� ���� ���� ã��
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