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

// ��ü������ �浹 ���� �˻�
// xyz : Position
// w   : Radius
bool SphereToSphereIntersection(float4 sphereA, float4 sphereB)
{
    return SqrMagnitude(sphereA.rgb - sphereB.rgb) < Square(sphereA.w + sphereB.w);
}

// ��ü�� ����ü(AABB)�� �浹 ���� �˻�
// AABB : Axis Aligned Bounding Box
// S : ��ü�� ��ġ
// r : ��ü�� ������
bool SphereToAABBIntersection(float3 S, float r, Bounds aabb)
{
    // Future Works : ����ȭ

    // [1] AABB������ �ִ����� ���
    float3 C = S;
    if      (C.x < aabb.min.x) C.x = aabb.min.x;
    else if (C.x > aabb.max.x) C.x = aabb.max.x;
    if      (C.y < aabb.min.y) C.y = aabb.min.y;
    else if (C.y > aabb.max.y) C.y = aabb.max.y;
    if      (C.z < aabb.min.z) C.z = aabb.min.z;
    else if (C.z > aabb.max.z) C.z = aabb.max.z;

    // [2] �Ÿ� ��
    return SqrMagnitude(C - S) <= r * r;
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
void DustToSphereCollision(float3 cur, inout float3 next, inout float3 velocity,
float dustRadius, float4 sphere, float elasticity, inout bool handled)
{
    // �浹 �� ���� ��ġ
    float3 contactPos = SphereCastToSphere(cur, next, sphere.xyz, dustRadius, sphere.w);

    // Optional : ǥ�� �޶���� �ʰ� �̲�������
    if(SqrMagnitude(cur - contactPos) < (sphere.w * sphere.w) * 1.1)
        elasticity = 1;

    // �浹 ������ ��� ����
    float3 contactNormal = (contactPos - sphere.xyz) / (dustRadius + sphere.w);

    // �浹 �������� ���� ���� ��ġ�� ���� ���� : �׿� ����
    float3 extraVec = next - contactPos;

    // �׿� �ݻ� ����
    float3 rfExtraVec = reflect(extraVec, contactNormal) * elasticity;

    // ���� ������ ��ġ ����
    next = contactPos + rfExtraVec;

    // �ӵ� ����
    velocity = reflect(velocity, contactNormal) * elasticity;

    handled = true;
}

// �� A���� �� B�� ����ĳ��Ʈ�Ͽ� ���� ���� ã��
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

// XY���� ������ ���� ������ ���� ã��
float3 RaycastToPlaneXY(float3 A, float3 B, float planeZ)
{
    // A�� B���� P�� �� ������ ��ġ�� ���
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

// Box(AABB) Collider�� �浹 �˻��Ͽ� ���� ��ġ �� �ӵ� ����
// - cur  : ���� �����ӿ����� ��ġ
// - next : ���� �����ӿ����� ��ġ [INOUT]
// - velocity   : ���� �̵� �ӵ�   [INOUT]
// - dustRadius : ���� ������
// - box        : Box ���� ����
// - elasticity : ź���� ���(0 ~ 1) : �浹 �� �����Ǵ� ��� ����
void DustToAABBCollision(float3 cur, inout float3 next, inout float3 velocity,
float dustRadius, Bounds box, float elasticity, inout bool handled)
{
    /*
        [�帧]
        1. ������ xyz ���� ���� ��ȣ�� �Ǵ��Ͽ� ť���� ��� �ĺ� 6���� 3���� ���δ�.
        2. ���̸� ��� 3��(���� ������ ����Ͽ� Ȯ��)�� ���ʷ� ĳ��Ʈ�Ͽ� ������ ���Ѵ�.
        3. ���� ������ ������ �� ���� ���� �ִٸ� �ش� ��ġ�� �浹�������� �����Ѵ�.
        4. ���̿� �ӵ� ���Ϳ� ���� �ݻ� ���Ϳ� �ݻ� �ӵ��� ���Ѵ�.
        5. ź������ �����Ͽ� ���� ��ġ�� ���� �ӵ��� �����Ѵ�.
    */
    
    // ���� ������ ����ϱ�
    float3 boxMin = box.min - dustRadius;
    float3 boxMax = box.max + dustRadius;

    /* ���ο��� ���η� �̵��ϴ� ���, ���� ����� ť�� �ܰ����� ������Ű�� */
    if(ExRange3(cur, boxMin, boxMax))
    {
        // [���� ����]
        // - �ٸ� �ݶ��̴��� ��ġ�ų�, ���� �ٿ��� ��ġ�� �ݶ��̴� ���ο��� ������ ���������� ���Ѵ�.

        // [���� ���]
        // �ݶ��̴��� �߽ɿ������� ���� ���� ��ġ�� ���ϴ� ���͸� ���Ѵ�.
        // �� ���Ͱ� �ݶ��̴��� �� ��� �´굵�� �ø���.
        // �� ��ġ(�ݶ��̴��� �ܰ�)�� ������ �̵���Ű��, �ӵ��� 0���� �ٲ۴�.

        float3 boxCenter   = (boxMin + boxMax) * 0.5;
        float3 localBounds = (boxMax - boxMin) * 0.5;
        float3 localPos    = cur - boxCenter; // �ݶ��̴� �߽ɿ������� ���� ���� ��ġ������ ����
        localPos += 0.001; // 0�� ���� ����ó��
        
        float3 absLocalPos    = abs(localPos);
        float3 absLocalBounds = abs(localBounds);
        
        // [1] X ��ǥ ��ġ
        float3 localBorder = absLocalPos * (absLocalBounds.x / absLocalPos.x);

        // center-border ������ YZ ���� �������� Ȯ��
        if(localBorder.y > absLocalBounds.y || localBorder.z > absLocalBounds.z)
        {
            // [2] Y ��ġ
            localBorder = absLocalPos * (absLocalBounds.y / absLocalPos.y);
        
            if(localBorder.x > absLocalBounds.x || localBorder.z > absLocalBounds.z)
            {
                // [3] Z ��ġ
                localBorder = absLocalPos * (absLocalBounds.z / absLocalPos.z);
            }
        }
        
        // ��ȣ ����
        localBorder *= sign(localPos);
        
        // ť�� �ܰ����� ���� �̵�
        next = boxCenter + localBorder * 1.01; // * 1�̸� World Bounds�� ��ĥ ��� ���� �߻�
        //velocity *= 0.1;
        return;
    }
    
    float3 ray     = next - cur;
    float3 contact = 0;
    half3  raySign = (ray >= 0);
    int    flag    = FLAG_ERROR; // XYZ ���� �÷���

    /* ť�� 6�鿡 ĳ��Ʈ�Ͽ� �浹 ���� ���ϱ� */
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
    
    /* ���� ��� */
    float rayLen = length(ray);
    float inLen  = length(contact - cur);                 // �Ի� ���� ����
    float rfLen  = (rayLen - inLen) * elasticity;         // �ݻ� ���� ����(ź�� ����)
    
    float3 rfRay = Reverse(ray, flag) * (rfLen / rayLen); // �ݻ� ����
    float3 rfVel = Reverse(velocity, flag) * elasticity;  // �ݻ� �ӵ� ����(ź�� ����)
    
    /* ������� ���� */
    next     = contact + rfRay;
    velocity = rfVel;

    handled = true;
}

// ����ü ���� ���� ��ġ ���� �� �浹 �˻� => ���� ��ġ �� �ӵ� ����
// - cur  : ���� �����ӿ����� ��ġ
// - next : ���� �����ӿ����� ��ġ [INOUT]
// - velocity : ���� �̵� �ӵ�     [INOUT]
// - dustRadius : ������ ũ��
// - elasticity : ź���� ���(0 ~ 1)
// - bounds : ť�� ����
void ConfineWithinWorldBounds(float3 cur, inout float3 next, inout float3 velocity, float dustRadius, float elasticity, Bounds bounds)
{
    // ���� ũ�� ����ϱ�
    bounds.min += dustRadius;
    bounds.max -= dustRadius;

    // 1. ť�� ���� �ۿ� �ִ���, �ȿ� �ִ��� �˻�
    int state = IN_BOUNDS;
         if(next.x > bounds.max.x) state = PX;
    else if(next.x < bounds.min.x) state = MX;
    else if(next.y > bounds.max.y) state = PY;
    else if(next.y < bounds.min.y) state = MY;
    else if(next.z > bounds.max.z) state = PZ;
    else if(next.z < bounds.min.z) state = MZ;
    else return; // ���� ���ο� �ִ� ���, ����

    Plane plane = { float3(0, 0, 0), float3(0, 0, 0) };
    float limit = 0;
    int flag = FLAG_ERROR;

    switch(state)
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

    // �ݻ� ���� ���ϱ�
    float3 rfRay = Reverse(next - cur, flag);
    float3 rfVelocity = Reverse(velocity, flag);
    
    // ������ ����� �浹 ���
    float3 currToNext = next - cur;
    float3 contact = RaycastToPlane(cur, next, plane); // �̵� ���Ϳ� ����� ����
    float  rayLen  = length(currToNext);               // �̵� ������ ����
    float  inLen   = length(cur - contact);            // �Ի� ���� ����
    float  outLen  = (rayLen - inLen) * elasticity;    // �ݻ� ���� ����(��� ����)
    float3 outVec  = rfRay * (outLen / rayLen);

    // Outputs
    next = contact + outVec;            // ���� ������ ��ġ ����
    velocity = rfVelocity * elasticity; // �ӵ� ����
}