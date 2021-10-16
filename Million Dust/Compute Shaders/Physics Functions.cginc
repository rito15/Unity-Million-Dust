#define IN_BOUNDS 0
#define PX  1 // +x
#define MX -1 // -x
#define PY  2 // +y
#define MY -2 // -y
#define PZ  3 // +z
#define MZ -3 // -z

// ��ü������ �浹 ���� �˻�
// xyz : Position
// w : Radius
bool CheckSphereIntersection(float4 sphereA, float4 sphereB)
{
    return SqrMagnitude(sphereA.rgb - sphereB.rgb) < Square(sphereA.w + sphereB.w);
}

// ��ü�� ����ü(AABB)�� �浹 ���� �˻�
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

// A -> B ��ġ�� Sphere Cast
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

    float3 C = A + nAB * ac;            // �浹 �� ��ü �߽� ��ǥ
    //float3 E = C + (S - C) * r1 / cs; // �浹 ���� ��ǥ
    return C;
}

// Sphere Collider�� �浹 �˻��Ͽ� ���� ��ġ �� �ӵ� ����
// - cur  : ���� �����ӿ����� ��ġ
// - next : ���� �����ӿ����� ��ġ [INOUT]
// - velocity : ���� �̵� �ӵ�     [INOUT]
// - sphere : ��ü �߽� ��ġ(xyz), ��ü ������(w)
// - dustRadius : ���� ������
// - elasticity : ź���� ���(0 ~ 1) : �浹 �� �����Ǵ� ��� ����
void CalculateSphereCollision(float3 cur, inout float3 next, inout float3 velocity,
float dustRadius, float4 sphere, float elasticity)
{
    // �浹 �� ���� ��ġ
    float3 contactPos = SphereCastToSphere(cur, next, sphere.xyz, dustRadius, sphere.w);

    // Option : ǥ�� �޶���� �ʰ� �̲�������
    if(SqrMagnitude(cur - contactPos) < (sphere.w * sphere.w) * 1.1)
        elasticity = 1;

    // �浹 ������ ��� ����
    float3 contactNormal = (contactPos - sphere.xyz) / (dustRadius + sphere.w);

    // �浹 �������� ���� ���� ��ġ�� ���� ���� : �׿� ����
    float3 extraVec = next - contactPos;

    // �ݻ� ����
    float3 outVec = reflect(extraVec, contactNormal) * elasticity;

    // ���� ������ ��ġ ����
    next = contactPos + outVec;

    // �ӵ� ����
    velocity = reflect(velocity, contactNormal) * elasticity;
}

// �� A���� �� B�� ����ĳ��Ʈ�Ͽ� ���� ���� ã��
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

// XY���� ������ ���� ������ ���� ã��
float3 RaycastToPlaneXY(float3 A, float3 B, float planeZ)
{
    // A�� B���� P�� �� ������ ��ġ�� ���
    if((planeZ - A.z) * (planeZ - B.z) <= 0)
        return A;

    float ratio = (B.z - planeZ) / (B.z - A.z);
    float3 C;
    C.xy = float2(A.xy - B.xy) * ratio + float2(B.xy);
    C.z = planeZ;
    return C;
}
float3 RaycastToPlaneXZ(float3 A, float3 B, float planeY)
{
    if((planeY - A.y) * (planeY - B.y) < 0)
        return A;

    float ratio = (B.y - planeY) / (B.y - A.y);
    float3 C;
    C.xz = float2(A.xz - B.xz) * ratio + float2(B.xz);
    C.y = planeY;
    return C;
}
float3 RaycastToPlaneYZ(float3 A, float3 B, float planeX)
{
    if((planeX - A.x) * (planeX - B.x) <= 0)
        return A;

    float ratio = (B.x - planeX) / (B.x - A.x);
    float3 C;
    C.yz = float2(A.yz - B.yz) * ratio + float2(B.yz);
    C.x = planeX;
    return C;
}

// Cube(AABB) Collider�� �浹 �˻��Ͽ� ���� ��ġ �� �ӵ� ����
// - cur  : ���� �����ӿ����� ��ġ
// - next : ���� �����ӿ����� ��ġ [INOUT]
// - velocity : ���� �̵� �ӵ�     [INOUT]
// - dustRadius : ���� ������
// - cubeMin : ����ü �ּ� ���� ������
// - cubeMax : ����ü �ִ� ���� ������
// - elasticity : ź���� ���(0 ~ 1) : �浹 �� �����Ǵ� ��� ����
void CalculateCubeCollision(float3 cur, inout float3 next, inout float3 velocity,
float dustRadius, float3 cubeMin, float3 cubeMax, float elasticity)
{
    /*
        [�����]
        1. ������ xyz ���� ���� ��ȣ�� �Ǵ��Ͽ� ��� �ĺ� 6���� 3���� ���δ�.
        2. ���̸� ��� 3��(���� ������ ����Ͽ� Ȯ��)�� ���ʷ� ĳ��Ʈ�Ͽ� ������ ���Ѵ�.
        3. ���� ������ ������ �� ���� ���� �ִٸ� �ش� ��ġ�� �浹�������� �����Ѵ�.
        4. ���̿� �ӵ� ���Ϳ� ���� �ݻ� ���Ϳ� �ݻ� �ӵ��� ���Ѵ�.
        5. ź������ �����Ͽ� ���� ��ġ�� ���� �ӵ��� �����Ѵ�.
    */

    //if(rayLen < 0.1)
    //{
    //    next = cur;
    //    velocity *= elasticity;
    //}
    
    // ���� ������ ����ϱ�
    cubeMin -= dustRadius;
    cubeMax += dustRadius;

    // ���ο��� ���η� �̵��ϴ� ���, ���� ����� �ܺη� ������Ű��
    if(ExRange3(cur, cubeMin, cubeMax))
    {
        float3 cubeCenter = (cubeMin + cubeMax) * 0.5;
        float3 cubeScale = cubeMax - cubeMin;
        float3 inPos = (cur - cubeCenter) / cubeScale; // ť���� �߽��� �������� �ϴ� ���� ��ǥ
        float3 absInPos = abs(inPos);

        float maxElem = MaxElement(absInPos);
        if(maxElem == absInPos.x)
        {
            if(inPos.x > 0)
            {
                next.x = max(cubeMax.x, next.x);
            }
            else
            {
                next.x = min(cubeMin.x, next.x);
            }
        }
        else if(maxElem == absInPos.y)
        {
            if(inPos.y > 0)
            {
                next.y = max(cubeMax.y, next.y);
            }
            else
            {
                next.y = min(cubeMin.y, next.y);
            }
        }
        else
        {
            if(inPos.z > 0)
            {
                next.z = max(cubeMax.z, next.z);
            }
            else
            {
                next.z = min(cubeMin.z, next.z);
            }
            //next.z = inPos.z > 0 ? cubeMax.z : cubeMin.z;
        }

        //velocity = 0;
        return;
    }
    
    float3 ray = next - cur;
    
    int flag = -1; // 0 : X, 1 : Y, 2 : Z
    float3 contact = 0;
    half3 raySign = (ray >= 0);

    //if(flag < 0)
    {
        if(raySign.x > 0) contact = RaycastToPlaneYZ(cur, next, cubeMin.x);
        else              contact = RaycastToPlaneYZ(cur, next, cubeMax.x);

        if(InRange2(contact.yz, cubeMin.yz, cubeMax.yz))
            flag = 0;
    }
    if(flag < 0)
    {
        if(raySign.y > 0) contact = RaycastToPlaneXZ(cur, next, cubeMin.y);
        else              contact = RaycastToPlaneXZ(cur, next, cubeMax.y);

        if(InRange2(contact.xz, cubeMin.xz, cubeMax.xz))
            flag = 1;
    }
    if(flag < 0)
    {
        if(raySign.z > 0) contact = RaycastToPlaneXY(cur, next, cubeMin.z);
        else              contact = RaycastToPlaneXY(cur, next, cubeMax.z);

        if(InRange2(contact.xy, cubeMin.xy, cubeMax.xy))
            flag = 2;
    }
    // NOTHING
    if(flag < 0)
    {
        next = cur;
        velocity *= elasticity;
        return;
    }
    
    // ���� ���
    float rayLen = length(ray);
    if(rayLen < 0.1) // ���͸� ó��
        elasticity = rayLen;

    float inLen = length(contact - cur);                  // �Ի� ���� ����
    float rfLen = (rayLen - inLen) * elasticity;          // �ݻ� ���� ����(ź�� ����)
    
    float3 rfRay = Reverse(ray, flag) * (rfLen / rayLen); // �ݻ� ����
    float3 rfVel = Reverse(velocity, flag) * elasticity;  // �ݻ� �ӵ� ����(ź�� ����)
    
    next = contact + rfRay;
    velocity = rfVel;
}

// ����ü ���� ���� ��ġ ���� �� �浹 �˻� => ���� ��ġ �� �ӵ� ����
// - cur  : ���� �����ӿ����� ��ġ
// - next : ���� �����ӿ����� ��ġ [INOUT]
// - velocity : ���� �̵� �ӵ�     [INOUT]
// - dustRadius : ������ ũ��
// - elasticity : ź���� ���(0 ~ 1)
// - bounds : ť�� ����
void ConfineWithinCubeBounds(float3 cur, inout float3 next, inout float3 velocity, float dustRadius, float elasticity, Bounds bounds)
{
    // ���� ũ�� ����ϱ�
    bounds.min += dustRadius;
    bounds.max -= dustRadius;

    // 1. ť�� ���� �ۿ� �ִ���, �ȿ� �ִ��� �˻�
    int status = IN_BOUNDS;
         if(next.x > bounds.max.x) status = PX;
    else if(next.x < bounds.min.x) status = MX;
    else if(next.y > bounds.max.y) status = PY;
    else if(next.y < bounds.min.y) status = MY;
    else if(next.z > bounds.max.z) status = PZ;
    else if(next.z < bounds.min.z) status = MZ;
    else return; // ���� ���ο� �ִ� ���, ����

    Plane plane;
    float limit;
    float3 reversedCurToNext; // ��鿡 �ݻ�� ����
    float3 reversedVelocity;

    switch(status)
    {
        case PX:
            limit = bounds.max.x;
            if(cur.x > limit) // �ܺο��� �ܺη� �̵��ϴ� ���, �ܼ��� ��ġ�� �����ϱ�
            {
                next.x = min(limit, next.x);
                return;
            }
            // ���ο��� �ܺη� �̵��ϴ� ���, �ݻ� ���� ����� ���� ������ �ʱ�ȭ
            plane.normal   = float3(1, 0, 0);
            plane.position = float3(limit, 0, 0);
            reversedCurToNext = ReverseX(next - cur);
            reversedVelocity  = ReverseX(velocity);
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
            reversedCurToNext = ReverseX(next - cur);
            reversedVelocity  = ReverseX(velocity);
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
            reversedCurToNext = ReverseY(next - cur);
            reversedVelocity  = ReverseY(velocity);
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
            reversedCurToNext = ReverseY(next - cur);
            reversedVelocity  = ReverseY(velocity);
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
            reversedCurToNext = ReverseZ(next - cur);
            reversedVelocity  = ReverseZ(velocity);
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
            reversedCurToNext = ReverseZ(next - cur);
            reversedVelocity  = ReverseZ(velocity);
            break;
    }
    
    // ������ ����� �浹 ���
    float3 currToNext = next - cur;
    float3 contact = RaycastToPlane(cur, next, plane); // �̵� ���Ϳ� ����� ����
    float rayLen   = length(currToNext);               // �̵� ������ ����
    float inLen    = length(cur - contact);            // �Ի� ���� ����
    float outLen   = (rayLen - inLen) * elasticity;    // �ݻ� ���� ����(��� ����)
    float3 outVec  = reversedCurToNext * (outLen / rayLen);

    // Outputs
    next = contact + outVec;                  // ���� ������ ��ġ ����
    velocity = reversedVelocity * elasticity; // �ӵ� ����
}