#define IN_BOUNDS 0
#define PX  1 // +x
#define MX -1 // -x
#define PY  2 // +y
#define MY -2 // -y
#define PZ  3 // +z
#define MZ -3 // -z

#define FLAG_ERROR  -1
#define FLAG_X 0
#define FLAG_Y 1
#define FLAG_Z 2

// 구체끼리의 충돌 여부 검사
// xyz : Position
// w   : Radius
bool SphereToSphereIntersection(float4 sphereA, float4 sphereB)
{
    return SqrMagnitude(sphereA.rgb - sphereB.rgb) < Square(sphereA.w + sphereB.w);
}

// 구체와 육면체(AABB)의 충돌 여부 검사
// AABB : Axis Aligned Bounding Box
// S : 구체의 위치
// r : 구체의 반지름
bool SphereToAABBIntersection(float3 S, float r, Bounds aabb)
{
    // Future Works : 최적화

    // [1] AABB까지의 최단지점 계산
    float3 C = S;
    if      (C.x < aabb.min.x) C.x = aabb.min.x;
    else if (C.x > aabb.max.x) C.x = aabb.max.x;
    if      (C.y < aabb.min.y) C.y = aabb.min.y;
    else if (C.y > aabb.max.y) C.y = aabb.max.y;
    if      (C.z < aabb.min.z) C.z = aabb.min.z;
    else if (C.z > aabb.max.z) C.z = aabb.max.z;

    // [2] 거리 비교
    return SqrMagnitude(C - S) <= r * r;
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
void DustToSphereCollision(float3 cur, inout float3 next, inout float3 velocity,
float dustRadius, float4 sphere, float elasticity, inout bool handled)
{
    // 충돌 시 먼지 위치
    float3 contactPos = SphereCastToSphere(cur, next, sphere.xyz, dustRadius, sphere.w);

    // Optional : 표면 달라붙지 않고 미끄러지기
    if(SqrMagnitude(cur - contactPos) < (sphere.w * sphere.w) * 1.1)
        elasticity = 1;

    // 충돌 지점의 노멀 벡터
    float3 contactNormal = (contactPos - sphere.xyz) / (dustRadius + sphere.w);

    // 충돌 지점에서 원래 다음 위치를 향한 벡터 : 잉여 벡터
    float3 extraVec = next - contactPos;

    // 잉여 반사 벡터
    float3 rfExtraVec = reflect(extraVec, contactNormal) * elasticity;

    // 다음 프레임 위치 변경
    next = contactPos + rfExtraVec;

    // 속도 변경
    velocity = reflect(velocity, contactNormal) * elasticity;

    handled = true;
}

// 점 A에서 점 B로 레이캐스트하여 평면과 접점 찾기
float3 RaycastToPlane(float3 A, float3 B, Plane plane)
{
    //A = Ray Origin;
    //B = Ray End;
    float3 AB = (B - A);
    float3 nAB = normalize(AB);
    
    float d = dot(plane.normal, plane.position - A) / dot(plane.normal, nAB);
    float3 C = A + nAB * d;
    return C;
}

// XY평면과 평행한 평면과 레이의 접점 찾기
float3 RaycastToPlaneXY(float3 A, float3 B, float planeZ)
{
    // A가 B보다 P에 더 가까이 위치한 경우
    //if((planeZ - A.z) * (planeZ - B.z) <= 0)
    //    return A;

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

// Box(AABB) Collider에 충돌 검사하여 먼지 위치 및 속도 변경
// - cur  : 현재 프레임에서의 위치
// - next : 다음 프레임에서의 위치 [INOUT]
// - velocity   : 현재 이동 속도   [INOUT]
// - dustRadius : 먼지 반지름
// - box        : Box 영역 범위
// - elasticity : 탄성력 계수(0 ~ 1) : 충돌 시 보존되는 운동량 비율
void DustToAABBCollision(float3 cur, inout float3 next, inout float3 velocity,
float dustRadius, Bounds box, float elasticity, inout bool handled)
{
    /*
        [흐름]
        1. 레이의 xyz 성분 각각 부호를 판단하여 큐브의 평면 후보 6개를 3개로 줄인다.
        2. 레이를 평면 3개(먼지 반지름 고려하여 확장)에 차례로 캐스트하여 접점을 구한다.
        3. 얻은 접점이 각각의 면 범위 내에 있다면 해당 위치를 충돌지점으로 결정한다.
        4. 레이와 속도 벡터에 대해 반사 벡터와 반사 속도를 구한다.
        5. 탄성력을 적용하여 다음 위치와 다음 속도를 결정한다.
    */
    
    // 먼지 반지름 고려하기
    float3 boxMin = box.min - dustRadius;
    float3 boxMax = box.max + dustRadius;

    /* 내부에서 내부로 이동하는 경우, 가장 가까운 큐브 외곽으로 투영시키기 */
    if(ExRange3(cur, boxMin, boxMax))
    {
        // [구현 이유]
        // - 다른 콜라이더와 겹치거나, 월드 바운드와 겹치면 콜라이더 내부에서 먼지가 빠져나오지 못한다.

        // [구현 방식]
        // 콜라이더의 중심에서부터 현재 먼지 위치를 향하는 벡터를 구한다.
        // 이 벡터가 콜라이더의 한 면과 맞닿도록 늘린다.
        // 얻어낸 위치(콜라이더의 외곽)로 먼지를 이동시키고, 속도를 0으로 바꾼다.

        float3 boxCenter   = (boxMin + boxMax) * 0.5;
        float3 localBounds = (boxMax - boxMin) * 0.5;
        float3 localPos    = cur - boxCenter; // 콜라이더 중심에서부터 현재 먼지 위치까지의 벡터
        localPos += 0.001; // 0에 대한 예외처리
        
        float3 absLocalPos    = abs(localPos);
        float3 absLocalBounds = abs(localBounds);
        
        // [1] X 좌표 일치
        float3 localBorder = absLocalPos * (absLocalBounds.x / absLocalPos.x);

        // center-border 선분이 YZ 평면과 만나는지 확인
        if(localBorder.y > absLocalBounds.y || localBorder.z > absLocalBounds.z)
        {
            // [2] Y 일치
            localBorder = absLocalPos * (absLocalBounds.y / absLocalPos.y);
        
            if(localBorder.x > absLocalBounds.x || localBorder.z > absLocalBounds.z)
            {
                // [3] Z 일치
                localBorder = absLocalPos * (absLocalBounds.z / absLocalPos.z);
            }
        }
        
        // 부호 복원
        localBorder *= sign(localPos);
        
        // 큐브 외곽으로 먼지 이동
        next = boxCenter + localBorder * 1.01; // * 1이면 World Bounds와 겹칠 경우 버그 발생
        //velocity *= 0.1;
        return;
    }
    
    float3 ray     = next - cur;
    float3 contact = 0;
    half3  raySign = (ray >= 0);
    int    flag    = FLAG_ERROR; // XYZ 선택 플래그

    /* 큐브 6면에 캐스트하여 충돌 지점 구하기 */
    //if(flag == FLAG_ERROR)
    {
        if(raySign.x > 0) contact = RaycastToPlaneYZ(cur, next, boxMin.x);
        else              contact = RaycastToPlaneYZ(cur, next, boxMax.x);

        if(InRange2(contact.yz, boxMin.yz, boxMax.yz))
            flag = FLAG_X;
    }
    if(flag == FLAG_ERROR)
    {
        if(raySign.y > 0) contact = RaycastToPlaneXZ(cur, next, boxMin.y);
        else              contact = RaycastToPlaneXZ(cur, next, boxMax.y);

        if(InRange2(contact.xz, boxMin.xz, boxMax.xz))
            flag = FLAG_Y;
    }
    if(flag == FLAG_ERROR)
    {
        if(raySign.z > 0) contact = RaycastToPlaneXY(cur, next, boxMin.z);
        else              contact = RaycastToPlaneXY(cur, next, boxMax.z);

        if(InRange2(contact.xy, boxMin.xy, boxMax.xy))
            flag = FLAG_Z;
    }
    
    /* 최종 계산 */
    float rayLen = length(ray);
    float inLen  = length(contact - cur);                 // 입사 벡터 길이
    float rfLen  = (rayLen - inLen) * elasticity;         // 반사 벡터 길이(탄성 적용)
    
    float3 rfRay = Reverse(ray, flag) * (rfLen / rayLen); // 반사 벡터
    float3 rfVel = Reverse(velocity, flag) * elasticity;  // 반사 속도 벡터(탄성 적용)
    
    /* 변경사항 적용 */
    next     = contact + rfRay;
    velocity = rfVel;

    handled = true;
}

// 육면체 범위 내로 위치 제한 및 충돌 검사 => 먼지 위치 및 속도 변경
// - cur  : 현재 프레임에서의 위치
// - next : 다음 프레임에서의 위치 [INOUT]
// - velocity : 현재 이동 속도     [INOUT]
// - dustRadius : 먼지의 크기
// - elasticity : 탄성력 계수(0 ~ 1)
// - bounds : 큐브 영역
void ConfineWithinWorldBounds(float3 cur, inout float3 next, inout float3 velocity, float dustRadius, float elasticity, Bounds bounds)
{
    // 먼지 크기 고려하기
    bounds.min += dustRadius;
    bounds.max -= dustRadius;

    // 1. 큐브 영역 밖에 있는지, 안에 있는지 검사
    int state = IN_BOUNDS;
         if(next.x > bounds.max.x) state = PX;
    else if(next.x < bounds.min.x) state = MX;
    else if(next.y > bounds.max.y) state = PY;
    else if(next.y < bounds.min.y) state = MY;
    else if(next.z > bounds.max.z) state = PZ;
    else if(next.z < bounds.min.z) state = MZ;
    else return; // 영역 내부에 있는 경우, 종료

    Plane plane = { float3(0, 0, 0), float3(0, 0, 0) };
    float limit = 0;
    int flag = FLAG_ERROR;

    switch(state)
    {
        case PX:
            limit = bounds.max.x;
            if(cur.x > limit) // 외부에서 외부로 이동하는 경우, 단순히 위치만 변경하기
            {
                next.x = min(limit, next.x);
                return;
            }
            // 내부에서 외부로 이동하는 경우, 반사 벡터 계산을 위한 변수들 초기화
            plane.normal   = float3(1, 0, 0);
            plane.position = float3(limit, 0, 0);
            flag = FLAG_X;
            break;

        case MX:
            limit = bounds.min.x;
            if(cur.x < limit)
            {
                next.x = max(limit, next.x);
                return;
            }
            plane.normal   = float3(-1, 0, 0);
            plane.position = float3(limit, 0, 0);
            flag = FLAG_X;
            break;

        case PY:
            limit = bounds.max.y;
            if(cur.y > limit)
            {
                next.y = min(limit, next.y);
                return;
            }
            plane.normal   = float3(0, 1, 0);
            plane.position = float3(0, limit, 0);
            flag = FLAG_Y;
            break;

        case MY:
            limit = bounds.min.y;
            if(cur.y < limit)
            {
                next.y = max(limit, next.y);
                return;
            }
            plane.normal   = float3(0, -1, 0);
            plane.position = float3(0, limit, 0);
            flag = FLAG_Y;
            break;

        case PZ:
            limit = bounds.max.z;
            if(cur.z > limit)
            {
                next.z = min(limit, next.z);
                return;
            }
            plane.normal   = float3(0, 0, 1);
            plane.position = float3(0, 0, limit);
            flag = FLAG_Z;
            break;

        case MZ:
            limit = bounds.min.z;
            if(cur.z < limit)
            {
                next.z = max(limit, next.z);
                return;
            }
            plane.normal   = float3(0, 0, -1);
            plane.position = float3(0, 0, limit);
            flag = FLAG_Z;
            break;
    }

    // 반사 벡터 구하기
    float3 rfRay = Reverse(next - cur, flag);
    float3 rfVelocity = Reverse(velocity, flag);
    
    // 직선과 평면의 충돌 계산
    float3 currToNext = next - cur;
    float3 contact = RaycastToPlane(cur, next, plane); // 이동 벡터와 평면의 접점
    float  rayLen  = length(currToNext);               // 이동 벡터의 길이
    float  inLen   = length(cur - contact);            // 입사 벡터 길이
    float  outLen  = (rayLen - inLen) * elasticity;    // 반사 벡터 길이(운동량 감소)
    float3 outVec  = rfRay * (outLen / rayLen);

    // Outputs
    next = contact + outVec;            // 다음 프레임 위치 변경
    velocity = rfVelocity * elasticity; // 속도 변경
}