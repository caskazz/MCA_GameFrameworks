using UnityEngine;

public class EnemyLogic : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float speed = 3f;
    [SerializeField] private Transform[] patrolPoints;
    private int currentPoint = 0;
    [SerializeField] private float sightRange = 10f;
    [SerializeField] private float sightAngle = 60f;
    [SerializeField] private float forgetTime = 2f;
    private bool isFollowing = false;
    private float lastSeenTime = 0f;
    private Vector3 lastKnownPosition;
    [SerializeField] private float hitCooldown = 1f;
    private float lastHitTime = 0f;
    private int direction = 1;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (CanSeePlayer())
        {
            isFollowing = true;
            lastSeenTime = Time.time;
            lastKnownPosition = target.position;
        }
        else
        {
            if (Time.time - lastSeenTime > forgetTime && isFollowing)
            {
                isFollowing = false;
                ReturnToPatrol();
            }
        }

        if (isFollowing)
            MoveTo(lastKnownPosition);
        else
            Patrol();
    }

    bool CanSeePlayer()
    {
        if (target == null) return false;

        Vector3 dirToPlayer = target.position - transform.position;

        float distance = dirToPlayer.magnitude;
        if (distance > sightRange) return false;

        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        Debug.Log("ANGLE TO PLAYER: " + angle);

        if (angle > sightAngle / 2f) return false;

        if (Physics.Raycast(transform.position, dirToPlayer.normalized, out RaycastHit hit, sightRange))
        {
            return hit.transform == target;
        }
        Debug.Log("Raycast hit nothing");
        return false;
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        Vector3 targetPos = patrolPoints[currentPoint].position;
        targetPos.y = rb.position.y;

        MoveTo(targetPos);

        if (Vector3.Distance(rb.position, targetPos) < 0.1f)
        {
            currentPoint += direction;

            if (currentPoint >= patrolPoints.Length)
            {
                direction = -1;
                currentPoint = patrolPoints.Length - 2;
            }
            else if (currentPoint < 0)
            {
                direction = 1;
                currentPoint = 1;
            }
        }
    }

    void MoveTo(Vector3 destination)
    {
        destination.y = rb.position.y;

        Vector3 dir = (destination - rb.position);
        dir.y = 0;
        Vector3 velocity = dir.normalized * speed;
        rb.linearVelocity = velocity;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            rb.MoveRotation(Quaternion.Euler(0, targetRot.eulerAngles.y, 0));
        }

        if (dir.magnitude < 0.1f)
            rb.linearVelocity = Vector3.zero;
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (Time.time - lastHitTime < hitCooldown) return;

        //HealthSystem health = other.GetComponent<HealthSystem>();

        //if (health != null)
        //{
        //    health.TakeDamage();
       //     lastHitTime = Time.time;
        //}
    }

    void ReturnToPatrol()
    {
        if (patrolPoints.Length == 0) return;

        float minDistance = float.MaxValue;
        int nearestIndex = 0;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float dist = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearestIndex = i;
            }
        }

        currentPoint = nearestIndex;
    }
}
