#define IN_BOUNDS 0
#define PX  1 // +x
#define MX -1 // -x
#define PY  2 // +y
#define MY -2 // -y
#define PZ  3 // +z
#define MZ -3 // -z

// 구체끼리의 충돌 여부 검사
// xyz : Position
// w : Radius
bool CheckSphereIntersection(float4 sphereA, float4 sphereB)
{
    return SqrMagnitude(sphereA.rgb - sphereB.rgb) < Square(sphereA.w + sphereB.w);
}

// 구체와 육면체(AABB)의 충돌 여부 검사
// AABB : Axis Aligned Bounding Box
bool CheckCubeIntersection(float3 position, float radius, float3 cubeMin, float3 cubeMax)
{
    if(position.x + radius < cubeMin.x) return false;
    if(position.y + radius < cubeMin.y) return false;
    if(position.z + radius < cubeMin.z) return false;
    if(position.x - radius > cubeMax.x) return false;
    if(position.y - radius > cubeMax.y) return false;
    if(position.z - radius > cubeMax.z) return false;
    return true;
}

// A -> B 위치로 Sphere Cast
// S : Target Sphere Position
// r1 : Radius of Casted Sphere
// r2 : Radius of Target Sphere
float3 SphereCastToSphere(float3 A, float3 B, float3 S, float r1, float r2)
{
    float3 nAB = normalize(B - A);
    float3 AS  = (S - A);
    float as2 = SqrMagnitude(AS);
    float ad  = dot(AS, nAB);
    float ad2 = ad * ad;
    float ds2 = as2 - ad2;
    float cs  = r1 + r2;
    float cs2 = cs * cs;
    float cd  = sqrt(cs2 - ds2);
    float ac  = ad - cd;

    float3 C = A + nAB * ac;            // 충돌 시 구체 중심 좌표
    //float3 E = C + (S - C) * r1 / cs; // 충돌 지점 좌표
    return C;
}

// Sphere Collider에 충돌 검사하여 먼지 위치 및 속도 변경
// - cur  : 현재 프레임에서의 위치
// - next : 다음 프레임에서의 위치 [INOUT]
// - velocity : 현재 이동 속도     [INOUT]
// - sphere : 구체 중심 위치(xyz), 구체 반지름(w)
// - dustRadius : 먼지 반지름
// - elasticity : 탄성력 계수(0 ~ 1) : 충돌 시 보존되는 운동량 비율
void CalculateSphereCollision(float3 cur, inout float3 next, inout float3 velocity,
float dustRadius, float4 sphere, float elasticity)
{
    // 충돌 시 먼지 위치
    float3 contactPos = SphereCastToSphere(cur, next, sphere.xyz, dustRadius, sphere.w);

    // Option : 표면 달라붙지 않고 미끄러지기
    if(SqrMagnitude(cur - contactPos) < (sphere.w * sphere.w) * 1.1)
        elasticity = 1;

    // 충돌 지점의 노멀 벡터
    float3 contactNormal = (contactPos - sphere.xyz) / (dustRadius + sphere.w);

    // 충돌 지점에서 원래 다음 위치를 향한 벡터 : 잉여 벡터
    float3 extraVec = next - contactPos;

    // 반사 벡터
    float3 outVec = reflect(extraVec, contactNormal) * elasticity;

    // 다음 프레임 위치 변경
    next = contactPos + outVec;

    // 속도 변경
    velocity = reflect(velocity, contactNormal) * elasticity;
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

// XY평면과 평행한 평면과 레이의 접점 찾기
float3 RaycastToPlaneXY(float3 A, float3 B, float planeZ)
{
    float ratio = (B.z - planeZ) / (B.z - A.z);
    float3 C;
    C.xy = float2(A.xy - B.xy) * ratio + float2(B.xy);
    C.z = planeZ;
    return C;
}
float3 RaycastToPlaneXZ(float3 A, float3 B, float planeY)
{
    float ratio = (B.y - planeY) / (B.y - A.y);
    float3 C;
    C.xz = float2(A.xz - B.xz) * ratio + float2(B.xz);
    C.y = planeY;
    return C;
}
float3 RaycastToPlaneYZ(float3 A, float3 B, float planeX)
{
    float ratio = (B.x - planeX) / (B.x - A.x);
    float3 C;
    C.yz = float2(A.yz - B.yz) * ratio + float2(B.yz);
    C.x = planeX;
    return C;
}

bool InRange(float2 vec, float2 min, float2 max)
{
    return min.x <= vec.x && vec.x <= max.x &&
           min.y <= vec.y && vec.y <= max.y;
}

// Cube(AABB) Collider에 충돌 검사하여 먼지 위치 및 속도 변경
// - cur  : 현재 프레임에서의 위치
// - next : 다음 프레임에서의 위치 [INOUT]
// - velocity : 현재 이동 속도     [INOUT]
// - dustRadius : 먼지 반지름
// - cubeMin : 육면체 최소 지점 꼭짓점
// - cubeMax : 육면체 최대 지점 꼭짓점
// - elasticity : 탄성력 계수(0 ~ 1) : 충돌 시 보존되는 운동량 비율
void CalculateCubeCollision(float3 cur, inout float3 next, inout float3 velocity,
float dustRadius, float3 cubeMin, float3 cubeMax, float elasticity)
{
    /*
        [방법론]
        1. 레이의 xyz 성분 각각 부호를 판단하여 평면 후보 6개를 3개로 줄인다.
        2. 레이를 평면 3개(먼지 반지름 고려하여 확장)에 차례로 캐스트하여 접점을 구한다.
        3. 얻은 접점이 각각의 면 범위 내에 있다면 해당 위치를 충돌지점으로 결정한다.
        4. 레이와 속도 벡터에 대해 반사 벡터와 반사 속도를 구한다.
        5. 탄성력을 적용하여 다음 위치와 다음 속도를 결정한다.
    */
    
    float3 ray = next - cur;
    float rayLen = length(ray);

    // TODO
    // [1] +X, +Y, +Z 방향으로 입사하는 경우, 텔레포트하는 현상 해결(반대는 정상작동함)
    // [2] 큐브 콜라이더에 먼지가 닿은 채로 속도가 아주 작으면 
    //     바들바들거리면서 +XYZ 방향으로 조금씩 이동하다가, -XYZ 방향으로 텔포하는 현상 해결

    //if(rayLen < 0.1)
    //{
    //    next = cur;
    //    velocity *= elasticity;
    //}

    // 먼지 반지름 고려하기
    cubeMin -= dustRadius;
    cubeMax += dustRadius;
    
    float3 contact = 0;
    int flag = -1; // 0 : X, 1 : Y, 2 : Z

    half3 raySign = (ray >= 0);

    if(flag < 0)
    {
        if(raySign.x > 0) contact = RaycastToPlaneYZ(cur, next, cubeMin.x);
        else              contact = RaycastToPlaneYZ(cur, next, cubeMax.x);

        if(InRange(contact.yz, cubeMin.yz, cubeMax.yz))
            flag = 0;
    }
    if(flag < 0)
    {
        if(raySign.y > 0) contact = RaycastToPlaneXZ(cur, next, cubeMin.y);
        else              contact = RaycastToPlaneXZ(cur, next, cubeMax.y);

        if(InRange(contact.xz, cubeMin.xz, cubeMax.xz))
            flag = 1;
    }
    if(flag < 0)
    {
        if(raySign.z > 0) contact = RaycastToPlaneXY(cur, next, cubeMin.z);
        else              contact = RaycastToPlaneXY(cur, next, cubeMax.z);

        if(InRange(contact.xy, cubeMin.xy, cubeMax.xy))
            flag = 2;
    }
    
    // NOTHING
    if(flag < 0)
    {
        next = cur;
        velocity *= elasticity;
    }
    
    // 최종 계산
    float inLen = length(contact - cur);                  // 입사 벡터 길이
    float rfLen = (rayLen - inLen) * elasticity;          // 반사 벡터 길이(탄성 적용)

    float3 rfRay = Reverse(ray, flag) * (rfLen / rayLen); // 반사 벡터
    float3 rfVel = Reverse(velocity, flag) * elasticity;  // 반사 속도 벡터(탄성 적용)

    next = contact + rfLen;
    velocity = rfVel;
}

// 육면체 범위 내로 위치 제한 및 충돌 검사 => 먼지 위치 및 속도 변경
// - cur  : 현재 프레임에서의 위치
// - next : 다음 프레임에서의 위치 [INOUT]
// - velocity : 현재 이동 속도     [INOUT]
// - threshold : 입자의 크기
// - elasticity : 탄성력 계수(0 ~ 1)
// - bounds : 큐브 영역
void ConfineWithinCubeBounds(float3 cur, inout float3 next, inout float3 velocity, float threshold, float elasticity, Bounds bounds)
{
    // 1. 큐브 영역 밖에 있는지, 안에 있는지 검사
    int status = IN_BOUNDS;
         if(next.x > bounds.max.x - threshold) status = PX;
    else if(next.x < bounds.min.x + threshold) status = MX;
    else if(next.y > bounds.max.y - threshold) status = PY;
    else if(next.y < bounds.min.y + threshold) status = MY;
    else if(next.z > bounds.max.z - threshold) status = PZ;
    else if(next.z < bounds.min.z + threshold) status = MZ;
    else return; // 영역 내부에 있는 경우, 종료

    Plane plane;
    float limit;
    float3 reversedCurToNext; // 평면에 반사된 벡터
    float3 reversedVelocity;

    switch(status)
    {
        case PX:
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

        case MX:
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

        case PY:
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

        case MY:
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

        case PZ:
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

        case MZ:
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