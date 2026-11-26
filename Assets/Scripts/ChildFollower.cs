using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ChildFollower : MonoBehaviour
{
    private enum State { Following, Leaving }

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [Tooltip("How close the child gets to the target before it stops and just hangs around.")]
    [SerializeField] private float stopDistance = 1.5f;

    [Header("Interest")]
    [Tooltip("Random time (seconds) the child follows the target before losing interest.")]
    [SerializeField] private Vector2 followDurationRange = new Vector2(8f, 20f);

    [Header("Leaving")]
    [Tooltip("Max time spent walking away before the child disappears.")]
    [SerializeField] private float maxLeaveTime = 8f;
    [Tooltip("The child disappears once it is this far from the target.")]
    [SerializeField] private float despawnDistance = 25f;
    [Tooltip("Random angle (degrees) added to the walk-away direction so they don't all leave in a straight line.")]
    [SerializeField] private float leaveAngleVariation = 40f;

    [Header("Damage")]
    [Tooltip("Seconds between hits while touching the player.")]
    [SerializeField] private float hitCooldown = 1f;

    private float lastHitTime = -999f;

    private Rigidbody rb;
    private State state = State.Following;
    private float followDuration;
    private float stateStartTime;
    private Vector3 leaveDirection;

    // Lets the spawner know when this child is gone, so it can keep count.
    public event Action<ChildFollower> Despawned;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // Fallback in case the spawner didn't assign a target.
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        followDuration = UnityEngine.Random.Range(followDurationRange.x, followDurationRange.y);
        stateStartTime = Time.time;
    }

    public void Initialize(Transform newTarget)
    {
        target = newTarget;
    }

    void FixedUpdate()
    {
        if (target == null)
        {
            Despawn();
            return;
        }

        switch (state)
        {
            case State.Following:
                UpdateFollowing();
                break;
            case State.Leaving:
                UpdateLeaving();
                break;
        }
    }

    void UpdateFollowing()
    {
        if (Time.time - stateStartTime >= followDuration)
        {
            StartLeaving();
            return;
        }

        Vector3 toTarget = target.position - rb.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= stopDistance)
        {
            // Close enough: stand still but keep facing the target.
            Stop();
            FaceDirection(toTarget);
            return;
        }

        MoveInDirection(toTarget);
    }

    void StartLeaving()
    {
        state = State.Leaving;
        stateStartTime = Time.time;

        // Walk away from the target, with some randomness.
        Vector3 away = rb.position - target.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.001f) away = transform.forward;

        float angle = UnityEngine.Random.Range(-leaveAngleVariation, leaveAngleVariation);
        leaveDirection = Quaternion.Euler(0f, angle, 0f) * away.normalized;
    }

    void UpdateLeaving()
    {
        MoveInDirection(leaveDirection);

        bool timeUp = Time.time - stateStartTime >= maxLeaveTime;
        bool farEnough = Vector3.Distance(rb.position, target.position) >= despawnDistance;

        if (timeUp || farEnough)
            Despawn();
    }

    void MoveInDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            Stop();
            return;
        }

        Vector3 velocity = direction.normalized * speed;
        velocity.y = rb.linearVelocity.y; // keep gravity working
        rb.linearVelocity = velocity;

        FaceDirection(direction);
    }

    void Stop()
    {
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(direction);
        rb.MoveRotation(Quaternion.Euler(0f, targetRot.eulerAngles.y, 0f));
    }

    // Works whether the child's collider is a trigger or a solid collider.
    void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    void OnCollisionStay(Collision collision)
    {
        TryDamage(collision.collider);
    }

    void TryDamage(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time - lastHitTime < hitCooldown) return;

        EnergySystem energy = other.GetComponentInParent<EnergySystem>();
        if (energy == null) return;

        energy.TakeDamage();
        lastHitTime = Time.time;
    }

    void Despawn()
    {
        Despawned?.Invoke(this);
        Destroy(gameObject);
    }
}