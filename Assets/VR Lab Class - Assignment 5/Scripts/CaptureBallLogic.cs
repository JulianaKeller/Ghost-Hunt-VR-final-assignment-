using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;

public class CaptureBallLogic : NetworkBehaviour
{
    public float hoverTimeAfterHit = 2f;
    public LayerMask ghostLayer;

    private Rigidbody rb;
    private bool hasBeenThrown = false;
    private bool hasCaptured = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Make sure gravity is disabled when the ball spawns
        rb.useGravity = false;
        rb.isKinematic = true;

        // Add event listeners for grabbing and releasing
        //grabInteractable.selectEntered.AddListener(OnGrab);
        //grabInteractable.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args) //not needed, this is done in VirtualHand script
    {
        // Ball is being held, disable physics to avoid weird interactions
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnRelease(SelectExitEventArgs args) //not needed, this is done in VirtualHand script
    {
        // Ball is thrown, enable gravity and physics
        rb.isKinematic = false;
        rb.useGravity = true;
        hasBeenThrown = true;
    }

    public void Throw()
    {
        hasBeenThrown = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasCaptured) return; // Prevent multiple captures

        if (collision.gameObject.CompareTag("Ghost"))
        {
            Debug.Log("CaptureBall collided with a ghost");
            GameObject ghost = collision.gameObject;

            if (ghost != null && ghost.GetComponent<GeistBewegung>().IsParalyzed()) // Only capture if paralyzed
            {
                Debug.Log("Ghost is paralyized. CaptureBall is capturing ghost...");
                CaptureGhost(ghost);
            }
        }
    }

    void CaptureGhost(GameObject ghost)
    {

        hasCaptured = true;

        // Stop ball movement and make it hover
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;

        //Call Capture on ghost
        //ToDo

        // Destroy the ball after a delay
        Destroy(gameObject, hoverTimeAfterHit);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
