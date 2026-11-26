using UnityEngine;
using UnityEngine.InputSystem;

public class GrabAndThrow : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float throwForce = 8f;
    [Tooltip("How close a waiting customer must be to hand them the painting.")]
    [SerializeField] private float giveRange = 3f;
    [SerializeField] private bool debugLogs = true;

    private GameObject nearbyObject;
    private GameObject heldObject;
    private Collider[] heldColliders;
    private Animator animator;


    void Awake()
    {
        animator = GetComponentInChildren<Animator>();

        if (holdPoint == null)
        {
            Debug.LogWarning("GrabAndThrow: Hold Point is not assigned. Using the player itself instead.", this);
            holdPoint = transform;
        }
    }


    public void Interact(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (debugLogs)
        {
            Debug.Log("GrabAndThrow: Interact. Holding: " + (heldObject != null ? heldObject.name : "nothing") +
                      ", nearby object: " + (nearbyObject != null ? nearbyObject.name : "none"));
        }

        if (heldObject == null)
        {
            PickUp();
        }
        else if (!TryGiveToCustomer())
        {
            Throw();
        }
    }

    // Returns true if the interaction was used on a waiting customer
    // (so we don't throw the painting at them).
    bool TryGiveToCustomer()
    {
        Customer customer = Customer.FindWaitingNear(transform.position, giveRange);
        if (customer == null)
        {
            if (debugLogs)
                Debug.Log("GrabAndThrow: no customer waiting within " + giveRange + " units, so the painting is thrown.");
            return false;
        }

        PaintingItem painting = heldObject.GetComponentInChildren<PaintingItem>();
        if (painting == null)
            painting = heldObject.GetComponentInParent<PaintingItem>();

        if (painting == null)
        {
            Debug.LogWarning("GrabAndThrow: the held object (" + heldObject.name + ") has no PaintingItem script, so it can't be sold.", heldObject);
            return false;
        }

        Customer.DeliveryResult result = customer.ReceivePainting(painting);

        if (debugLogs)
            Debug.Log("GrabAndThrow: gave '" + painting.PaintingName + "' to " + customer.name + ", result: " + result);

        switch (result)
        {
            case Customer.DeliveryResult.Accepted:
                Destroy(heldObject);
                heldObject = null;
                heldColliders = null;
                return true;

            case Customer.DeliveryResult.WrongPainting:
                // Customer refused it, keep holding the painting.
                return true;

            default:
                return false;
        }
    }

    void PickUp()
    {
        if (nearbyObject == null)
        {
            if (debugLogs)
                Debug.Log("GrabAndThrow: nothing to pick up. Check the object's tag is PickUp, and that the player or the object has a trigger collider.");
            return;
        }

        if (animator != null)
            animator.SetTrigger("PickUp");

        heldObject = nearbyObject;
        nearbyObject = null;

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Disable every collider on the object (including children) while it is held.
        heldColliders = heldObject.GetComponentsInChildren<Collider>();
        foreach (Collider col in heldColliders)
            col.enabled = false;

        heldObject.transform.SetParent(holdPoint);
        heldObject.transform.localPosition = Vector3.zero;
        heldObject.transform.localRotation = Quaternion.identity;
    }


    void Throw()
    {
        heldObject.transform.SetParent(null);

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();

        if (heldColliders != null)
        {
            foreach (Collider col in heldColliders)
            {
                if (col != null)
                    col.enabled = true;
            }
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(
                transform.forward * throwForce,
                ForceMode.Impulse
            );
        }

        heldObject = null;
        heldColliders = null;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PickUp"))
            return;

        // If the collider is on a child, grab the whole object (the one with the Rigidbody).
        nearbyObject = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.gameObject;
    }


    void OnTriggerExit(Collider other)
    {
        GameObject root = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        if (root == nearbyObject)
            nearbyObject = null;
    }
}