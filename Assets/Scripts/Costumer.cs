using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody))]
public class Customer : MonoBehaviour
{
    private enum State { Browsing, Approaching, Waiting, Leaving }

    public enum DeliveryResult { NotWaiting, WrongPainting, Accepted }

    [Header("Movement")]
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private float arriveDistance = 0.3f;

    [Header("Browsing")]
    [Tooltip("Random time (seconds) the customer spends looking around before deciding what to do.")]
    [SerializeField] private Vector2 browseDurationRange = new Vector2(15f, 40f);
    [Tooltip("Random time (seconds) spent standing in front of each painting.")]
    [SerializeField] private Vector2 lookTimeRange = new Vector2(2f, 5f);

    [Header("Buying")]
    [Tooltip("Chance this customer wants to buy. The rest just browse and leave.")]
    [Range(0f, 1f)]
    [SerializeField] private float buyChance = 0.6f;
    [Tooltip("How close the customer stands to the player when asking for a painting.")]
    [SerializeField] private float stopDistance = 1.5f;
    [Tooltip("Seconds the customer waits for the painting before giving up.")]
    [SerializeField] private float patience = 40f;
    [Tooltip("Energy the player loses when the sale is completed.")]
    [SerializeField] private int energyCost = 2;
    [Tooltip("If another customer is already being served, try again after this many seconds.")]
    [SerializeField] private float retryApproachDelay = 3f;

    [Header("Speech")]
    [Tooltip("Where the speech bubble is anchored, in world units above the customer's pivot.")]
    [SerializeField] private Vector3 speechOffset = new Vector3(0f, 2.3f, 0f);

    [Header("Leaving")]
    [SerializeField] private float maxLeaveTime = 30f;

    [Header("Avoiding Other Customers")]
    [Tooltip("Customers closer than this start steering around each other.")]
    [SerializeField] private float avoidRadius = 1.5f;
    [Tooltip("How strongly customers push away from each other.")]
    [SerializeField] private float separationStrength = 1.5f;
    [Tooltip("How strongly a customer steers right when another one is in front, so they pass each other.")]
    [SerializeField] private float sidestepStrength = 1.5f;
    [Tooltip("Every this many seconds, check whether the customer has barely moved.")]
    [SerializeField] private float stuckCheckInterval = 1.2f;
    [Tooltip("Moving less than this in one check counts as being stuck.")]
    [SerializeField] private float stuckDistance = 0.15f;

    private Vector3 lastStuckCheckPosition;
    private float lastStuckCheckTime;
    private Vector3 unstuckDirection;
    private float unstuckUntil;
    private int stuckEvents;
    private bool repickPoint;

    private Rigidbody rb;
    private GameUI gameUI;
    private EnergySystem playerEnergy;
    private Label speechBubble;
    private string currentSpeech = "";

    private Transform target;
    private Transform[] patrolPoints;
    private Transform exitPoint;

    private State state = State.Browsing;
    private bool wantsToBuy;
    private float browseEndTime;

    private int currentPoint = -1;
    private bool isLooking;
    private float lookUntil;

    private string requestedPainting;
    private float waitStartTime;
    private float leaveStartTime;

    // Only one customer at a time walks up to the player, so they don't pile up.
    private static Customer currentBuyer;

    // Customers standing next to the player and waiting for their painting.
    private static readonly List<Customer> waitingCustomers = new List<Customer>();

    // Every customer in the scene, used to steer around each other.
    private static readonly List<Customer> allCustomers = new List<Customer>();

    public event System.Action<Customer> Despawned;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        currentBuyer = null;
        waitingCustomers.Clear();
        allCustomers.Clear();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Stop physics from tipping the customer over. Rotation around Y is still done by the script.
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void OnEnable()
    {
        allCustomers.Add(this);
    }

    void OnDisable()
    {
        allCustomers.Remove(this);
    }

    // Called by the spawner right after the customer is created.
    public void Initialize(Transform player, Transform[] browsePoints, Transform exit)
    {
        target = player;
        patrolPoints = browsePoints;
        exitPoint = exit;
    }

    void Start()
    {
        gameUI = FindAnyObjectByType<GameUI>();

        if (target != null)
            playerEnergy = target.GetComponentInParent<EnergySystem>();
        if (playerEnergy == null)
            playerEnergy = FindAnyObjectByType<EnergySystem>();

        if (gameUI != null)
            speechBubble = gameUI.CreateSpeechBubble();
        else
            Debug.LogWarning("Customer: no GameUI found in the scene, so speech bubbles can't be shown.", this);

        WarnAboutSetup();

        lastStuckCheckPosition = rb.position;
        lastStuckCheckTime = Time.time;

        wantsToBuy = Random.value <= buyChance;
        browseEndTime = Time.time + Random.Range(browseDurationRange.x, browseDurationRange.y);
    }

    void WarnAboutSetup()
    {
        if (rb.isKinematic)
            Debug.LogWarning("Customer: the Rigidbody is Kinematic, so it can't move by velocity. Untick Is Kinematic.", this);

        if ((rb.constraints & RigidbodyConstraints.FreezePositionX) != 0 ||
            (rb.constraints & RigidbodyConstraints.FreezePositionZ) != 0)
            Debug.LogWarning("Customer: the Rigidbody freezes position on X or Z. Untick those constraints.", this);

        if (target == null)
            Debug.LogWarning("Customer: no player target. Was this customer created by CustomerSpawner (Initialize not called)?", this);

        if (patrolPoints == null || patrolPoints.Length == 0)
            Debug.LogWarning("Customer: no browse points assigned, so it will just stand still.", this);

        if (exitPoint == null)
            Debug.LogWarning("Customer: no exit point, so it will disappear as soon as it decides to leave.", this);
    }

    void OnDestroy()
    {
        waitingCustomers.Remove(this);
        ReleaseBuyerSlot();

        if (gameUI != null)
            gameUI.RemoveSpeechBubble(speechBubble);
    }

    void FixedUpdate()
    {
        switch (state)
        {
            case State.Browsing: UpdateBrowsing(); break;
            case State.Approaching: UpdateApproaching(); break;
            case State.Waiting: UpdateWaiting(); break;
            case State.Leaving: UpdateLeaving(); break;
        }
    }

    void LateUpdate()
    {
        // Keep the speech bubble on screen above the customer's head.
        if (gameUI != null && speechBubble != null)
            gameUI.UpdateSpeechBubble(speechBubble, transform.position + speechOffset, currentSpeech);
    }

    // ---------- Browsing ----------

    void UpdateBrowsing()
    {
        if (Time.time >= browseEndTime)
        {
            if (wantsToBuy)
            {
                if (TryStartApproach()) return;
                browseEndTime = Time.time + retryApproachDelay;
            }
            else
            {
                StartLeaving();
                return;
            }
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Stop();
            return;
        }

        if (isLooking)
        {
            if (Time.time < lookUntil)
            {
                Stop();
                // Face the same way as the patrol point, so it looks at the painting.
                FaceDirection(patrolPoints[currentPoint].forward);
                return;
            }

            isLooking = false;
            currentPoint = PickNextPoint();
        }

        if (currentPoint < 0)
            currentPoint = PickNextPoint();

        // If we keep getting stuck on the way to this painting, go look at another one.
        if (repickPoint)
        {
            repickPoint = false;
            currentPoint = PickNextPoint();
        }

        Vector3 destination = patrolPoints[currentPoint].position;
        MoveToward(destination);

        if (FlatDistance(rb.position, destination) <= arriveDistance)
        {
            isLooking = true;
            lookUntil = Time.time + Random.Range(lookTimeRange.x, lookTimeRange.y);
        }
    }

    int PickNextPoint()
    {
        if (patrolPoints.Length == 1) return 0;

        int next;
        do
        {
            next = Random.Range(0, patrolPoints.Length);
        } while (next == currentPoint);

        return next;
    }

    // ---------- Buying ----------

    bool TryStartApproach()
    {
        if (target == null)
        {
            StartLeaving();
            return true;
        }

        if (currentBuyer != null && currentBuyer != this)
            return false;

        requestedPainting = PickRandomPaintingName();
        if (requestedPainting == null)
        {
            // We have nothing to sell right now.
            StartLeaving();
            return true;
        }

        currentBuyer = this;
        state = State.Approaching;
        return true;
    }

    string PickRandomPaintingName()
    {
        PaintingItem[] paintings = FindObjectsByType<PaintingItem>(FindObjectsSortMode.None);
        if (paintings.Length == 0)
        {
            Debug.LogWarning("Customer: wanted to buy, but no PaintingItem exists in the scene, so it will leave. Add the PaintingItem script to your paintings.", this);
            return null;
        }

        return paintings[Random.Range(0, paintings.Length)].PaintingName;
    }

    void UpdateApproaching()
    {
        if (target == null)
        {
            StartLeaving();
            return;
        }

        Vector3 toTarget = target.position - rb.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= stopDistance)
        {
            Stop();
            state = State.Waiting;
            waitingCustomers.Add(this);
            waitStartTime = Time.time;
            Say("I want the painting \"" + requestedPainting + "\"!");
            return;
        }

        MoveToward(target.position);
    }

    void UpdateWaiting()
    {
        Stop();

        if (target != null)
            FaceDirection(target.position - rb.position);

        if (Time.time - waitStartTime >= patience)
        {
            Say("Never mind, I'll look elsewhere.");
            StartLeaving();
        }
    }

    // Finds the closest customer that is waiting for a painting within range of a position.
    public static Customer FindWaitingNear(Vector3 position, float range)
    {
        Customer best = null;
        float bestDistance = range;

        foreach (Customer customer in waitingCustomers)
        {
            if (customer == null) continue;

            float distance = Vector3.Distance(position, customer.transform.position);
            if (distance <= bestDistance)
            {
                best = customer;
                bestDistance = distance;
            }
        }

        return best;
    }

    // Called by the player's GrabAndThrow script when handing over a painting.
    public DeliveryResult ReceivePainting(PaintingItem painting)
    {
        if (state != State.Waiting || painting == null)
            return DeliveryResult.NotWaiting;

        if (painting.PaintingName != requestedPainting)
        {
            Say("That's not it! I want the painting \"" + requestedPainting + "\".");
            return DeliveryResult.WrongPainting;
        }

        if (gameUI != null)
            gameUI.AddMoney(painting.Price);

        if (playerEnergy != null)
            playerEnergy.TakeDamage(energyCost);

        Say("Thank you!");
        StartLeaving();
        return DeliveryResult.Accepted;
    }

    // ---------- Leaving ----------

    void StartLeaving()
    {
        if (state == State.Browsing)
            ClearSpeech();

        state = State.Leaving;
        leaveStartTime = Time.time;
        waitingCustomers.Remove(this);
        ReleaseBuyerSlot();
    }

    void UpdateLeaving()
    {
        if (exitPoint == null)
        {
            Despawn();
            return;
        }

        MoveToward(exitPoint.position);

        bool arrived = FlatDistance(rb.position, exitPoint.position) <= arriveDistance;
        bool timeUp = Time.time - leaveStartTime >= maxLeaveTime;

        if (arrived || timeUp)
            Despawn();
    }

    void Despawn()
    {
        Despawned?.Invoke(this);
        Destroy(gameObject);
    }

    void ReleaseBuyerSlot()
    {
        if (currentBuyer == this)
            currentBuyer = null;
    }

    // ---------- Speech ----------

    void Say(string message)
    {
        currentSpeech = message;
        Debug.Log(name + ": " + message);
    }

    void ClearSpeech()
    {
        currentSpeech = "";
    }

    // ---------- Movement ----------

    void MoveToward(Vector3 destination)
    {
        Vector3 dir = destination - rb.position;
        dir.y = 0f;

        if (dir.magnitude <= arriveDistance * 0.5f)
        {
            Stop();
            return;
        }

        Vector3 desired = dir.normalized;
        Vector3 steer = desired + GetAvoidance(desired);

        CheckIfStuck();
        if (Time.time < unstuckUntil)
            steer += unstuckDirection * 1.5f;

        steer.y = 0f;
        steer = steer.sqrMagnitude > 0.0001f ? steer.normalized : desired;

        Vector3 velocity = steer * speed;
        velocity.y = rb.linearVelocity.y; // keep gravity working
        rb.linearVelocity = velocity;

        FaceDirection(steer);
    }

    void Stop()
    {
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        // Standing still on purpose is not being stuck.
        lastStuckCheckPosition = rb.position;
        lastStuckCheckTime = Time.time;
    }

    // Pushes away from nearby customers and steers to the right when one is straight ahead,
    // so two customers walking toward each other pass instead of blocking each other.
    Vector3 GetAvoidance(Vector3 desired)
    {
        Vector3 total = Vector3.zero;

        foreach (Customer other in allCustomers)
        {
            if (other == null || other == this) continue;

            Vector3 offset = rb.position - other.rb.position;
            offset.y = 0f;

            float distance = offset.magnitude;
            if (distance >= avoidRadius || distance < 0.0001f) continue;

            float weight = 1f - distance / avoidRadius;
            Vector3 away = offset / distance;

            total += away * (weight * separationStrength);

            // The other customer is ahead of us: sidestep to our right.
            float ahead = Vector3.Dot(desired, -away);
            if (ahead > 0.3f)
                total += Vector3.Cross(Vector3.up, desired) * (weight * sidestepStrength * ahead);
        }

        return total;
    }

    // Fallback: if the customer has barely moved for a while, sidestep for a moment,
    // and after repeated failures pick a different painting to look at.
    void CheckIfStuck()
    {
        if (Time.time - lastStuckCheckTime < stuckCheckInterval) return;

        float moved = FlatDistance(rb.position, lastStuckCheckPosition);
        lastStuckCheckPosition = rb.position;
        lastStuckCheckTime = Time.time;

        if (moved >= stuckDistance)
        {
            stuckEvents = 0;
            return;
        }

        Vector3 side = Vector3.Cross(Vector3.up, transform.forward);
        unstuckDirection = Random.value < 0.5f ? side : -side;
        unstuckUntil = Time.time + 0.8f;
        stuckEvents++;

        if (state == State.Browsing && stuckEvents >= 2)
        {
            repickPoint = true;
            stuckEvents = 0;
        }
    }

    void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(direction);
        rb.MoveRotation(Quaternion.Euler(0f, targetRot.eulerAngles.y, 0f));
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}