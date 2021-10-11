
// ��ü �浹 ���� �˻�
bool CheckSphereToSphereCollision(float3 posA, float radiusA, float3 posB, float radiusB)
{
    return SqrMagnitude(posA - posB) < Square(radiusA + radiusB);
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
// - radius : ���� ������
// - elasticity : ź���� ���(0 ~ 1) : �浹 �� �����Ǵ� ��� ����
void CalculateSphereCollision(float3 cur, inout float3 next, inout float3 velocity, float3 spherePosition,
float dustRadius, float sphereRadius, float elasticity)
{
    // �浹 �� ���� ��ġ
    float3 contactPos = SphereCastToSphere(cur, next, spherePosition, dustRadius, sphereRadius);

    // �浹 ������ ��� ����
    float3 contactNormal = (contactPos - spherePosition) / (dustRadius + sphereRadius);

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

#define IN_BOUNDS 0
#define OUT_OF_PX 1 // +x
#define OUT_OF_MX 2 // -x
#define OUT_OF_PY 3 // +y
#define OUT_OF_MY 4 // -y
#define OUT_OF_PZ 5 // +z
#define OUT_OF_MZ 6 // -z

// ����ü ���� ���� ��ġ ���� �� �浹 �˻� => ���� ��ġ �� �ӵ� ����
// - cur  : ���� �����ӿ����� ��ġ
// - next : ���� �����ӿ����� ��ġ [INOUT]
// - velocity : ���� �̵� �ӵ�     [INOUT]
// - threshold : ������ ũ��
// - elasticity : ź���� ���(0 ~ 1)
// - bounds : ť�� ����
void ConfineWithinCubeBounds(float3 cur, inout float3 next, inout float3 velocity, float threshold, float elasticity, Bounds bounds)
{
    // 1. ť�� ���� �ۿ� �ִ���, �ȿ� �ִ��� �˻�
    int status = IN_BOUNDS;
         if(next.x > bounds.max.x - threshold) status = OUT_OF_PX;
    else if(next.x < bounds.min.x + threshold) status = OUT_OF_MX;
    else if(next.y > bounds.max.y - threshold) status = OUT_OF_PY;
    else if(next.y < bounds.min.y + threshold) status = OUT_OF_MY;
    else if(next.z > bounds.max.z - threshold) status = OUT_OF_PZ;
    else if(next.z < bounds.min.z + threshold) status = OUT_OF_MZ;
    else return; // ���� ���ο� �ִ� ���, ����

    Plane plane;
    float limit;
    float3 reversedCurToNext;
    float3 reversedVelocity;

    switch(status)
    {
        case OUT_OF_PX:
            limit = bounds.max.x - threshold;
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