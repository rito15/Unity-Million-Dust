
// 구체 충돌 여부 검사
bool CheckSphereToSphereCollision(float3 posA, float radiusA, float3 posB, float radiusB)
{
    return SqrMagnitude(posA - posB) < Square(radiusA + radiusB);
}

// 점 A에서 점 B로 레이캐스트하여 평면과 접점 찾기
float3 RaycastToPlane(float3 A, float3 B, Plane plane)
{
    //A = Ray Origin;
    //B = Ray End;
    //P = Plane Point;
    //N = Plane Normal;
    float3 AB = (B - A);
    float3 nAB = normalize(AB);
    
    float d = dot(plane.normal, plane.position - A) / dot(plane.normal, nAB);
    float3 C = A + nAB * d;
    return C;
}

#define IN_BOUNDS 0
#define OUT_OF_PX 1 // +x
#define OUT_OF_MX 2 // -x
#define OUT_OF_PY 3 // +y
#define OUT_OF_MY 4 // -y
#define OUT_OF_PZ 5 // +z
#define OUT_OF_MZ 6 // -z

// 육면체 범위 내로 위치 제한 및 충돌 검사
// - cur : 현재 프레임에서의 위치
// - next : 다음 프레임에서의 위치
// - velocity : 현재 이동 속도
// - threshold : 입자의 크기
// - elasticity : 탄성력 계수(0 ~ 1)
// - bounds : 큐브 영역
void ConfineWithinCubeBounds(float3 cur, inout float3 next, inout float3 velocity, float threshold, float elasticity, Bounds bounds)
{
    // 1. 큐브 영역 밖에 있는지, 안에 있는지 검사
    int status = IN_BOUNDS;
         if(next.x > bounds.max.x - threshold) status = OUT_OF_PX;
    else if(next.x < bounds.min.x + threshold) status = OUT_OF_MX;
    else if(next.y > bounds.max.y - threshold) status = OUT_OF_PY;
    else if(next.y < bounds.min.y + threshold) status = OUT_OF_MY;
    else if(next.z > bounds.max.z - threshold) status = OUT_OF_PZ;
    else if(next.z < bounds.min.z + threshold) status = OUT_OF_MZ;
    else return; // 영역 내부에 있는 경우, 종료

    Plane plane;
    float limit;
    float3 reversedCurToNext;
    float3 reversedVelocity;

    switch(status)
    {
        case OUT_OF_PX:
            limit = bounds.max.x - threshold;
            if(cur.x > limit) // 외부에서 외부로 이동하는 경우, 단순히 위치만 변경하기
            {
                next.x = min(limit, next.x);
                return;
            }
            // 내부에서 외부로 이동하는 경우, 반사 벡터 계산을 위한 변수들 초기화
            plane.normal   = float3(1, 0, 0);
            plane.position = float3(limit, 0, 0);
            reversedCurToNext = ReverseX(next - cur);
            reversedVelocity  = ReverseX(velocity);
            break;

        case OUT_OF_MX:
            limit = bounds.min.x + threshold;
            if(cur.x < limit)
            {
                next.x = max(limit, next.x);
                return;
            }
            plane.normal   = float3(-1, 0, 0);
            plane.position = float3(limit, 0, 0);
            reversedCurToNext = ReverseX(next - cur);
            reversedVelocity  = ReverseX(velocity);
            break;

        case OUT_OF_PY:
            limit = bounds.max.y - threshold;
            if(cur.y > limit)
            {
                next.y = min(limit, next.y);
                return;
            }
            plane.normal   = float3(0, 1, 0);
            plane.position = float3(0, limit, 0);
            reversedCurToNext = ReverseY(next - cur);
            reversedVelocity  = ReverseY(velocity);
            break;

        case OUT_OF_MY:
            limit = bounds.min.y + threshold;
            if(cur.y < limit)
            {
                next.y = max(limit, next.y);
                return;
            }
            plane.normal   = float3(0, -1, 0);
            plane.position = float3(0, limit, 0);
            reversedCurToNext = ReverseY(next - cur);
            reversedVelocity  = ReverseY(velocity);
            break;

        case OUT_OF_PZ:
            limit = bounds.max.z - threshold;
            if(cur.z > limit)
            {
                next.z = min(limit, next.z);
                return;
            }
            plane.normal   = float3(0, 0, 1);
            plane.position = float3(0, 0, limit);
            reversedCurToNext = ReverseZ(next - cur);
            reversedVelocity  = ReverseZ(velocity);
            break;

        case OUT_OF_MZ:
            limit = bounds.min.z + threshold;
            if(cur.z < limit)
            {
                next.z = max(limit, next.z);
                return;
            }
            plane.normal   = float3(0, 0, -1);
            plane.position = float3(0, 0, limit);
            reversedCurToNext = ReverseZ(next - cur);
            reversedVelocity  = ReverseZ(velocity);
            break;
    }
    
    // 직선과 평면의 충돌 계산
    float3 currToNext = next - cur;
    float3 contact = RaycastToPlane(cur, next, plane); // 이동 벡터와 평면의 접점
    float rayLen   = length(currToNext);               // 이동 벡터의 길이
    float inLen    = length(cur - contact);            // 입사 벡터 길이
    float outLen   = (rayLen - inLen) * elasticity;    // 반사 벡터 길이(운동량 감소)
    float3 outVec  = reversedCurToNext * (outLen / rayLen);

    // Outputs
    next = contact + outVec;                  // 다음 프레임 위치 변경
    velocity = reversedVelocity * elasticity; // 속도 변경
}