using UnityEngine;

public class MapDrop : MonoBehaviour
{
    [Header("Target Ground")]
    public float targetY = 0.3175f;

    [Header("Random Speed (acts as gravity scale)")]
    [SerializeField, Min(0f)] private float minSpeed = 6;  // �������� �� õõ��
    [SerializeField, Min(0f)] private float maxSpeed = 15;

    [Header("Fall Tuning")]
    [SerializeField] private float gravity = 5f;            // �⺻ �߷�
    [SerializeField, Range(0f, 1f)] private float bounciness = 0.3f; // 1=����ź��, 0=Ʀ ����
    [SerializeField, Range(0f, 10f)] private float airDrag = 0.3f;    // ��������(����=������)
    [SerializeField] private float minBounceSpeed = 0.2f;      // �� �ӵ� ���Ϸδ� �� �̻� Ƣ�� ����
    [SerializeField] private bool stopAtRest = true;           // ���߸� Update ����

    public float Rspeed { get; private set; } // �߷� �����Ϸ� Ȱ��
    private float vY = 0f;                    // ���� ���� �ӵ�(+��, -�Ʒ�)

    void Start()
    {
        Rspeed = Random.Range(minSpeed, maxSpeed);
        vY = 0f; // ó���� ���� ���¿��� ��������
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // �߷� ���� + ������ ���� ��������(�ӵ��� ����ϴ� ����)
        float effectiveG = gravity * Rspeed;
        vY -= effectiveG * dt;          // �Ʒ��� ����
        vY += -airDrag * vY * dt;       // �ӵ� ����(�ڿ�������)

        // ��ġ ����
        float newY = transform.position.y + vY * dt;

        // '����' �浹 ó��(targetY)
        if (newY <= targetY)
        {
            newY = targetY;

            if (Mathf.Abs(vY) > minBounceSpeed)
            {
                vY = -vY * bounciness;  // ���� ƨ��
            }
            else
            {
                vY = 0f;
                if (stopAtRest) enabled = false; // ����� ��ź �� ����
            }
        }

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    // Scene �信�� targetY Ȯ�ο� ������
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 left = new Vector3(transform.position.x - 0.5f, targetY, transform.position.z);
        Vector3 right = new Vector3(transform.position.x + 0.5f, targetY, transform.position.z);
        Gizmos.DrawLine(left, right);
    }
}
